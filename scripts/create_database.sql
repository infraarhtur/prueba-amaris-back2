-- ==========================================
--  SCRIPT DE CREACIÓN BASE DE DATOS
--  Technical Test - Luis Monsalve
--  PostgreSQL 16+
-- ==========================================

-- Crear base de datos (ejecutar solo si no existe)
-- CREATE DATABASE technical_test OWNER postgres;

-- Conectarse a la base
-- \c technical_test;

-- Extensiones necesarias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================
--   TABLA: users
-- =========================
CREATE TABLE IF NOT EXISTS users (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    full_name               VARCHAR(100) NOT NULL,
    email                   VARCHAR(150) UNIQUE NOT NULL,
    password_hash           TEXT NOT NULL,
    role                    VARCHAR(20) NOT NULL CHECK (role IN ('Admin','User')),
    notification_preference VARCHAR(10) NOT NULL CHECK (notification_preference IN ('EMAIL','SMS')),
    balance_cop             NUMERIC(18,2) NOT NULL DEFAULT 500000,   -- saldo inicial
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =========================
--   TABLA: funds
-- =========================
CREATE TABLE IF NOT EXISTS funds (
    id              SERIAL PRIMARY KEY,
    code            VARCHAR(100) UNIQUE NOT NULL,
    name            VARCHAR(150) NOT NULL,
    min_amount_cop  NUMERIC(18,2) NOT NULL,
    category        VARCHAR(50) NOT NULL CHECK (category IN ('FPV','FIC')),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =========================
--   TABLA: subscriptions
-- =========================
CREATE TABLE IF NOT EXISTS subscriptions (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    fund_id         INT NOT NULL REFERENCES funds(id) ON DELETE CASCADE,
    amount_cop      NUMERIC(18,2) NOT NULL,
    status          VARCHAR(20) NOT NULL CHECK (status IN ('ACTIVE','CANCELLED')),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    cancelled_at    TIMESTAMPTZ
);

-- Índices para consultas por usuario y fondo
CREATE INDEX IF NOT EXISTS idx_subscriptions_user_id ON subscriptions(user_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_fund_id ON subscriptions(fund_id);

-- =========================
--   TABLA: transactions
-- =========================
CREATE TABLE IF NOT EXISTS transactions (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    fund_id         INT NOT NULL REFERENCES funds(id) ON DELETE CASCADE,
    type            VARCHAR(20) NOT NULL CHECK (type IN ('SUBSCRIBE','CANCEL')),
    amount_cop      NUMERIC(18,2) NOT NULL,
    occurred_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_transactions_user_id ON transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_transactions_fund_id ON transactions(fund_id);

-- =========================
--   TABLA: outbox_notifications
-- =========================
CREATE TABLE IF NOT EXISTS outbox_notifications (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    channel         VARCHAR(10) NOT NULL CHECK (channel IN ('EMAIL','SMS')),
    payload_json    JSONB NOT NULL,
    status          VARCHAR(20) NOT NULL CHECK (status IN ('PENDING','SENT','FAILED')) DEFAULT 'PENDING',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    sent_at         TIMESTAMPTZ
);

-- =========================
--   TRIGGER: updated_at
-- =========================
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_users_timestamp
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- =========================
--   SEED DATA: funds
-- =========================
INSERT INTO funds (code, name, min_amount_cop, category)
VALUES
('FPV_BTG_PACTUAL_RECAUDADORA', 'Recaudadora', 75000, 'FPV'),
('FPV_BTG_PACTUAL_ECOPETROL', 'Ecopetrol', 125000, 'FPV'),
('DEUDAPRIVADA', 'Deuda Privada', 50000, 'FIC'),
('FDO-ACCIONES', 'Fondo Acciones', 250000, 'FIC'),
('FPV_BTG_PACTUAL_DINAMICA', 'Dinámica', 100000, 'FPV')
ON CONFLICT (code) DO NOTHING;

-- =========================
--   USUARIO DEMO
-- =========================
INSERT INTO users (full_name, email, password_hash, role, notification_preference)
VALUES
('Demo User', 'demo@example.com', crypt('123456', gen_salt('bf')), 'User', 'EMAIL')
ON CONFLICT (email) DO NOTHING;

-- =========================
--   FUNCIONES AUXILIARES
-- =========================

-- Función: suscribirse a fondo
CREATE OR REPLACE FUNCTION subscribe_to_fund(p_user UUID, p_fund INT, p_amount NUMERIC)
RETURNS TEXT AS $$
DECLARE
    v_balance NUMERIC;
    v_min NUMERIC;
    v_fund_name TEXT;
BEGIN
    SELECT balance_cop INTO v_balance FROM users WHERE id = p_user FOR UPDATE;
    SELECT min_amount_cop, name INTO v_min, v_fund_name FROM funds WHERE id = p_fund;

    IF v_balance < p_amount THEN
        RAISE EXCEPTION 'Saldo insuficiente para suscribirse al fondo %', v_fund_name;
    END IF;

    IF p_amount < v_min THEN
        RAISE EXCEPTION 'El monto mínimo para el fondo % es %', v_fund_name, v_min;
    END IF;

    -- Descontar saldo
    UPDATE users SET balance_cop = balance_cop - p_amount WHERE id = p_user;

    -- Crear suscripción
    INSERT INTO subscriptions (user_id, fund_id, amount_cop, status)
    VALUES (p_user, p_fund, p_amount, 'ACTIVE');

    -- Registrar transacción
    INSERT INTO transactions (user_id, fund_id, type, amount_cop)
    VALUES (p_user, p_fund, 'SUBSCRIBE', p_amount);

    -- Crear notificación pendiente
    INSERT INTO outbox_notifications (user_id, channel, payload_json)
    VALUES (p_user, 'EMAIL', jsonb_build_object('message', format('Te has suscrito al fondo %', v_fund_name)));

    RETURN format('Suscripción creada al fondo % por %.2f COP', v_fund_name, p_amount);
END;
$$ LANGUAGE plpgsql;


-- Función: cancelar suscripción
CREATE OR REPLACE FUNCTION cancel_subscription(p_user UUID, p_subscription UUID)
RETURNS TEXT AS $$
DECLARE
    v_amount NUMERIC;
    v_fund INT;
    v_name TEXT;
    v_status TEXT;
BEGIN
    SELECT amount_cop, fund_id, status INTO v_amount, v_fund, v_status
    FROM subscriptions WHERE id = p_subscription AND user_id = p_user FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Suscripción no encontrada o no pertenece al usuario';
    END IF;

    IF v_status <> 'ACTIVE' THEN
        RAISE EXCEPTION 'La suscripción ya fue cancelada previamente';
    END IF;

    SELECT name INTO v_name FROM funds WHERE id = v_fund;

    -- Actualizar suscripción
    UPDATE subscriptions SET status = 'CANCELLED', cancelled_at = NOW()
    WHERE id = p_subscription;

    -- Acreditar saldo al usuario
    UPDATE users SET balance_cop = balance_cop + v_amount WHERE id = p_user;

    -- Registrar transacción
    INSERT INTO transactions (user_id, fund_id, type, amount_cop)
    VALUES (p_user, v_fund, 'CANCEL', v_amount);

    -- Crear notificación
    INSERT INTO outbox_notifications (user_id, channel, payload_json)
    VALUES (p_user, 'EMAIL', jsonb_build_object('message', format('Cancelaste tu suscripción al fondo %', v_name)));

    RETURN format('Suscripción al fondo % cancelada correctamente. Se devolvieron %.2f COP.', v_name, v_amount);
END;
$$ LANGUAGE plpgsql;

-- ==========================================
-- FIN SCRIPT
-- ==========================================


CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE clients (
    id uuid NOT NULL,
    balance numeric(18,2) NOT NULL,
    notification_channel integer NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_clients" PRIMARY KEY (id)
);

CREATE TABLE funds (
    id integer NOT NULL,
    name character varying(200) NOT NULL,
    minimum_amount numeric(18,2) NOT NULL,
    category integer NOT NULL,
    CONSTRAINT "PK_funds" PRIMARY KEY (id)
);

CREATE TABLE subscriptions (
    id uuid NOT NULL,
    client_id uuid NOT NULL,
    fund_id integer NOT NULL,
    amount numeric(18,2) NOT NULL,
    subscribed_at_utc timestamp with time zone NOT NULL,
    cancelled_at_utc timestamp with time zone,
    CONSTRAINT "PK_subscriptions" PRIMARY KEY (id),
    CONSTRAINT "FK_subscriptions_clients_client_id" FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_subscriptions_funds_fund_id" FOREIGN KEY (fund_id) REFERENCES funds (id) ON DELETE RESTRICT
);

CREATE TABLE transactions (
    id uuid NOT NULL,
    subscription_id uuid NOT NULL,
    fund_id integer NOT NULL,
    amount numeric(18,2) NOT NULL,
    type integer NOT NULL,
    occurred_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_transactions" PRIMARY KEY (id),
    CONSTRAINT "FK_transactions_funds_fund_id" FOREIGN KEY (fund_id) REFERENCES funds (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_transactions_subscriptions_subscription_id" FOREIGN KEY (subscription_id) REFERENCES subscriptions (id) ON DELETE RESTRICT
);

INSERT INTO clients (id, balance, created_at, notification_channel)
VALUES ('11111111-1111-1111-1111-111111111111', 500000.0, TIMESTAMPTZ '2024-01-01T00:00:00Z', 0);

INSERT INTO funds (id, category, minimum_amount, name)
VALUES (1, 0, 75000.0, 'FPV_BTG_PACTUAL_RECAUDADORA');
INSERT INTO funds (id, category, minimum_amount, name)
VALUES (2, 0, 125000.0, 'FPV_BTG_PACTUAL_ECOPTROL');
INSERT INTO funds (id, category, minimum_amount, name)
VALUES (3, 1, 50000.0, 'DEUDAPRIVADA');
INSERT INTO funds (id, category, minimum_amount, name)
VALUES (4, 1, 250000.0, 'FDO-ACCIONES');
INSERT INTO funds (id, category, minimum_amount, name)
VALUES (5, 0, 100000.0, 'FPV_BTG_PACTUAL_DINAMICA');

CREATE INDEX "IX_subscriptions_client_id" ON subscriptions (client_id);

CREATE INDEX "IX_subscriptions_fund_id" ON subscriptions (fund_id);

CREATE INDEX "IX_transactions_fund_id" ON transactions (fund_id);

CREATE INDEX "IX_transactions_subscription_id" ON transactions (subscription_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251111020229_Initial', '9.0.10');

COMMIT;


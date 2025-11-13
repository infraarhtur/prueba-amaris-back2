# Guía: Probar EventBridge y Disparar Lambda

Esta guía te muestra cómo probar que EventBridge está correctamente configurado y que dispara la Lambda cuando se publica un evento.

---

## 🚀 Prueba Rápida (Recomendado)

### Paso 1: Ver logs en tiempo real

Abre una terminal y ejecuta:

```bash
aws logs tail /aws/lambda/subscription-notification-handler --follow --region us-east-1
```

### Paso 2: Publicar evento de prueba

En otra terminal, ejecuta este comando (una sola línea):

```bash
aws events put-events --entries '[{"Source":"technicaltest.subscriptions","DetailType":"SubscriptionCreatedEvent","Detail":"{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+1234567890\",\"amount\":100000.0,\"subscribedAtUtc\":\"2024-01-01T00:00:00Z\"}","EventBusName":"technical-test-bus"}]' --region us-east-1
```

### Paso 3: Verificar ejecución

En la terminal de logs deberías ver:
- `START RequestId: ...`
- `Received EventBridge event: {...}`
- `Processing subscription created: ...`

✅ **Resultado esperado**: El evento se publica, EventBridge lo filtra, y la Lambda se ejecuta automáticamente.

---

## 📋 Método Detallado (Paso a Paso)

### 1. Verificar la configuración

```bash
# Verificar que la regla existe
aws events describe-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --region us-east-1

# Verificar que la Lambda está como target
aws events list-targets-by-rule \
  --event-bus-name technical-test-bus \
  --rule subscription-notifications \
  --region us-east-1
```

### 2. Ver logs en tiempo real

```bash
aws logs tail /aws/lambda/subscription-notification-handler --follow --region us-east-1
```

### 3. Publicar evento (versión multilínea)

```bash
aws events put-events \
  --entries '[
    {
      "Source": "technicaltest.subscriptions",
      "DetailType": "SubscriptionCreatedEvent",
      "Detail": "{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+1234567890\",\"amount\":100000.0,\"subscribedAtUtc\":\"2024-01-01T00:00:00Z\"}",
      "EventBusName": "technical-test-bus"
    }
  ]' \
  --region us-east-1
```

### 4. Verificar que la Lambda se ejecutó

En los logs deberías ver algo como:

```
START RequestId: xxx Version: $LATEST
Received EventBridge event: {
  "version": "0",
  "id": "...",
  "detail-type": "SubscriptionCreatedEvent",
  "source": "technicaltest.subscriptions",
  "account": "142911054234",
  "time": "2024-01-01T00:00:00Z",
  "region": "us-east-1",
  "resources": [],
  "detail": {
    "subscriptionId": "11111111-1111-1111-1111-111111111111",
    "productId": 1,
    "clientId": "11111111-1111-1111-1111-111111111111",
    "customerEmail": "test@example.com",
    "customerPhone": "+1234567890",
    "amount": 100000,
    "subscribedAtUtc": "2024-01-01T00:00:00Z"
  }
}
END RequestId: xxx
```

---

## 🔧 Método con Script Automatizado

### Ejecutar script de prueba

```bash
./scripts/test_eventbridge.sh
```

Este script automáticamente:
- ✅ Verifica que la regla existe
- ✅ Verifica que la Lambda está configurada como target
- ✅ Publica un evento de prueba
- ✅ Muestra los logs de la Lambda

---

## 📱 Probar desde la Consola de AWS

### Paso 1: Ir a EventBridge

1. Ve a la consola de AWS → **EventBridge**
2. Navega a **Event buses** → **technical-test-bus**
3. Ve a la pestaña **Rules** → **subscription-notifications**

### Paso 2: Enviar evento de prueba

1. Haz clic en la regla `subscription-notifications`
2. Haz clic en **Send test event** (o **Enviar evento de prueba**)
3. Selecciona **Custom event** (Evento personalizado)
4. Usa este formato:

```json
{
  "source": "technicaltest.subscriptions",
  "detail-type": "SubscriptionCreatedEvent",
  "detail": {
    "subscriptionId": "11111111-1111-1111-1111-111111111111",
    "productId": 1,
    "clientId": "11111111-1111-1111-1111-111111111111",
    "customerEmail": "test@example.com",
    "customerPhone": "+1234567890",
    "amount": 100000.0,
    "subscribedAtUtc": "2024-01-01T00:00:00Z"
  }
}
```

5. Haz clic en **Send** (Enviar)

### Paso 3: Verificar ejecución de Lambda

1. Ve a la consola de AWS → **Lambda**
2. Selecciona la función `subscription-notification-handler`
3. Ve a la pestaña **Monitor** → **Logs**
4. Deberías ver una nueva invocación con el evento

---

## 📊 Comandos Útiles

### Ver logs recientes (últimos 5 minutos)

```bash
aws logs tail /aws/lambda/subscription-notification-handler --region us-east-1 --since 5m
```

### Verificar configuración de la regla

```bash
aws events list-targets-by-rule \
  --event-bus-name technical-test-bus \
  --rule subscription-notifications \
  --region us-east-1
```

### Verificar permisos de la Lambda

```bash
aws lambda get-policy \
  --function-name subscription-notification-handler \
  --region us-east-1
```

### Verificar estado de la regla

```bash
aws events describe-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --region us-east-1 \
  --query 'State' \
  --output text
```

Debe retornar: `ENABLED`

### Publicar evento de cancelación

**Comando en una línea:**
```bash
aws events put-events --entries '[{"Source":"technicaltest.subscriptions","DetailType":"SubscriptionCancelledEvent","Detail":"{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+1234567890\",\"amount\":100000.0,\"cancelledAtUtc\":\"2024-01-01T12:00:00Z\"}","EventBusName":"technical-test-bus"}]' --region us-east-1
```

**Versión multilínea:**
```bash
aws events put-events \
  --entries '[
    {
      "Source": "technicaltest.subscriptions",
      "DetailType": "SubscriptionCancelledEvent",
      "Detail": "{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+1234567890\",\"amount\":100000.0,\"cancelledAtUtc\":\"2024-01-01T12:00:00Z\"}",
      "EventBusName": "technical-test-bus"
    }
  ]' \
  --region us-east-1
```

---

## 📈 Verificar Métricas

### Ver métricas de EventBridge

**macOS:**
```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/Events \
  --metric-name Invocations \
  --dimensions Name=RuleName,Value=subscription-notifications \
  --start-time $(date -u -v-1H +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region us-east-1
```

**Linux:**
```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/Events \
  --metric-name Invocations \
  --dimensions Name=RuleName,Value=subscription-notifications \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region us-east-1
```

### Ver métricas de Lambda

**macOS:**
```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/Lambda \
  --metric-name Invocations \
  --dimensions Name=FunctionName,Value=subscription-notification-handler \
  --start-time $(date -u -v-1H +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region us-east-1
```

**Linux:**
```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/Lambda \
  --metric-name Invocations \
  --dimensions Name=FunctionName,Value=subscription-notification-handler \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region us-east-1
```

---

## 🐛 Troubleshooting

### La Lambda no se ejecuta

1. **Verificar permisos**:
   ```bash
   aws lambda get-policy \
     --function-name subscription-notification-handler \
     --region us-east-1
   ```

2. **Verificar que el target está configurado**:
   ```bash
   aws events list-targets-by-rule \
     --event-bus-name technical-test-bus \
     --rule subscription-notifications \
     --region us-east-1
   ```

3. **Verificar que la regla está habilitada**:
   ```bash
   aws events describe-rule \
     --name subscription-notifications \
     --event-bus-name technical-test-bus \
     --region us-east-1 \
     --query 'State' \
     --output text
   ```
   Debe retornar: `ENABLED`

### El evento no coincide con el patrón

Verifica que el evento tiene:
- `Source`: exactamente `"technicaltest.subscriptions"`
- `DetailType`: exactamente `"SubscriptionCreatedEvent"` o `"SubscriptionCancelledEvent"`
- Se publica en el bus correcto: `"technical-test-bus"`

### No veo logs

1. Espera unos segundos (EventBridge puede tardar un poco en procesar)
2. Verifica que la Lambda tiene permisos para escribir logs
3. Revisa CloudWatch Logs directamente en la consola de AWS

### Error de SES (Email no verificado)

⚠️ **Es normal si estás en modo sandbox**: SES en modo sandbox solo permite enviar a emails verificados.

**Solución**:
1. Verificar el email en SES, o
2. Solicitar salir del modo sandbox

✅ **Lo importante**: El evento se publicó, EventBridge lo filtró correctamente, y la Lambda se ejecutó. El error de SES es solo un problema de configuración de email, no del flujo EventBridge → Lambda.

---

## 📝 Estructura del Evento

### Evento de Creación (SubscriptionCreatedEvent)

El evento debe tener esta estructura:

```json
{
  "source": "technicaltest.subscriptions",
  "detail-type": "SubscriptionCreatedEvent",
  "detail": {
    "subscriptionId": "uuid",
    "productId": 1,
    "clientId": "uuid",
    "customerEmail": "email@example.com",
    "customerPhone": "+1234567890",
    "amount": 100000.0,
    "subscribedAtUtc": "2024-01-01T00:00:00Z"
  }
}
```

### Evento de Cancelación (SubscriptionCancelledEvent)

```json
{
  "source": "technicaltest.subscriptions",
  "detail-type": "SubscriptionCancelledEvent",
  "detail": {
    "subscriptionId": "uuid",
    "productId": 1,
    "clientId": "uuid",
    "customerEmail": "email@example.com",
    "customerPhone": "+1234567890",
    "amount": 100000.0,
    "cancelledAtUtc": "2024-01-01T12:00:00Z"
  }
}
```

---

## ✅ Resumen de Comandos Verificados

### Comando Simple (Una Línea) - Evento de Creación

```bash
aws events put-events --entries '[{"Source":"technicaltest.subscriptions","DetailType":"SubscriptionCreatedEvent","Detail":"{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+1234567890\",\"amount\":100000.0,\"subscribedAtUtc\":\"2024-01-01T00:00:00Z\"}","EventBusName":"technical-test-bus"}]' --region us-east-1
```

### Comando Simple (Una Línea) - Evento de Cancelación

```bash
aws events put-events --entries '[{"Source":"technicaltest.subscriptions","DetailType":"SubscriptionCancelledEvent","Detail":"{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+1234567890\",\"amount\":100000.0,\"cancelledAtUtc\":\"2024-01-01T12:00:00Z\"}","EventBusName":"technical-test-bus"}]' --region us-east-1
```

### Ver Logs en Tiempo Real

```bash
aws logs tail /aws/lambda/subscription-notification-handler --follow --region us-east-1
```

---

## 📚 Recursos Relacionados

- **Script de prueba**: `./scripts/test_eventbridge.sh`
- **Script de cancelación**: `./scripts/test_eventbridge_cancellation.sh`
- **Guía rápida**: Ver `PRUEBA_RAPIDA.md`

---

## 🔍 Información de Configuración

- **Bus de eventos**: `technical-test-bus`
- **Regla**: `subscription-notifications`
- **Lambda**: `subscription-notification-handler`
- **Región**: `us-east-1`
- **Source**: `technicaltest.subscriptions`
- **DetailType**: `SubscriptionCreatedEvent` o `SubscriptionCancelledEvent`

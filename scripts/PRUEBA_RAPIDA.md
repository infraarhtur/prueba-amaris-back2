# Prueba Rápida de EventBridge → Lambda

## Método Rápido (Recomendado)

### 1. Abrir terminal para ver logs en tiempo real

```bash
aws logs tail /aws/lambda/subscription-notification-handler --follow --region us-east-1
```

### 2. En otra terminal, publicar evento de prueba

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

### 3. Verificar en la terminal de logs

Deberías ver que la Lambda se ejecutó y procesó el evento.

---

## Método con Script

### Ejecutar script automatizado

```bash
./scripts/test_eventbridge.sh
```

Este script hace todo automáticamente:
- Verifica la configuración
- Publica el evento
- Muestra los logs

---

## Comandos Útiles

### Ver logs recientes

```bash
aws logs tail /aws/lambda/subscription-notification-handler --region us-east-1 --since 5m
```

### Verificar que la regla está configurada

```bash
aws events list-targets-by-rule \
  --event-bus-name technical-test-bus \
  --rule subscription-notifications \
  --region us-east-1
```

### Publicar evento de cancelación

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

## Notas Importantes

⚠️ **Error de SES**: Si ves un error sobre email no verificado, es normal. SES en modo sandbox solo permite enviar a emails verificados. Para producción, necesitas:
1. Verificar el email en SES, o
2. Solicitar salir del modo sandbox

✅ **Lo importante**: El evento se publicó, EventBridge lo filtró correctamente, y la Lambda se ejecutó. El error de SES es solo un problema de configuración de email, no del flujo EventBridge → Lambda.


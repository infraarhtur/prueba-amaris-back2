# Comandos para Topic SNS: subscription-notifications-sms

## 📊 Estado Actual

- **Topic ARN**: `arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms`
- **Región**: `us-east-1`
- **Estado**: ✅ Creado y operativo
- **Suscripciones activas**: 1
  - **Número**: `+573208965783`
  - **Protocolo**: SMS
  - **Subscription ARN**: `arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms:7af7c06c-99c8-4ffd-b59c-8cfca76e5dde`

---

## 📋 Comando Original para Crear el Topic

```bash
aws sns create-topic \
  --name subscription-notifications-sms \
  --region us-east-1
```

**Resultado esperado:**
```json
{
    "TopicArn": "arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms"
}
```

**Nota**: El topic ya está creado. Este comando solo es necesario si necesitas recrearlo.

---

## ✅ Verificar que el Topic Está Creado Correctamente

### 1. Listar todos los topics y buscar el nuestro

```bash
aws sns list-topics \
  --region us-east-1 \
  --query 'Topics[?contains(TopicArn, `subscription-notifications-sms`)]' \
  --output table
```

### 2. Obtener atributos del topic

```bash
aws sns get-topic-attributes \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1
```

### 3. Verificar suscripciones al topic

```bash
aws sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1 \
  --output table
```

### 4. Script de verificación completo

```bash
./scripts/verificar_sns_topic.sh
```

Este script verifica automáticamente:
- ✅ Existencia del topic
- ✅ Suscripciones configuradas
- ✅ Permisos de la Lambda

---

## 🧪 Cómo Probar el Topic SNS

### Opción 1: Publicar mensaje directamente al topic (Prueba Básica)

```bash
aws sns publish \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --message "Mensaje de prueba: Suscripción creada exitosamente" \
  --region us-east-1
```

**Resultado**: El mensaje se enviará a todas las suscripciones activas (actualmente: +573208965783)

**Nota**: El parámetro `--subject` no es necesario para SMS, solo para email.

---

### Opción 2: Suscribir un nuevo número de teléfono

Si necesitas agregar más números:

```bash
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --protocol sms \
  --notification-endpoint +1234567890 \
  --region us-east-1
```

**Reemplaza `+1234567890` con el número real** (formato: +[código país][número])

**Ejemplo real usado:**
```bash
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --protocol sms \
  --notification-endpoint +573208965783 \
  --region us-east-1
```

**Resultado esperado:**
```json
{
    "SubscriptionArn": "arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms:7af7c06c-99c8-4ffd-b59c-8cfca76e5dde"
}
```

---

### Opción 3: Probar el Flujo Completo (EventBridge → Lambda → SNS)

Este es el flujo real de la aplicación cuando se crea una suscripción:

#### Paso 1: Abrir terminal para ver logs en tiempo real

```bash
aws logs tail /aws/lambda/subscription-notification-handler \
  --follow \
  --region us-east-1
```

#### Paso 2: En otra terminal, publicar evento de prueba

```bash
aws events put-events \
  --entries '[
    {
      "Source": "technicaltest.subscriptions",
      "DetailType": "SubscriptionCreatedEvent",
      "Detail": "{\"subscriptionId\":\"11111111-1111-1111-1111-111111111111\",\"productId\":1,\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"customerEmail\":\"test@example.com\",\"customerPhone\":\"+573208965783\",\"amount\":100000.0,\"subscribedAtUtc\":\"2024-01-01T00:00:00Z\"}",
      "EventBusName": "technical-test-bus"
    }
  ]' \
  --region us-east-1
```

**Nota**: El `customerPhone` en el evento debe coincidir con un número suscrito para recibir el SMS.

#### Paso 3: Usar el script automatizado

```bash
./scripts/test_eventbridge.sh
```

Este script:
- ✅ Verifica la configuración
- ✅ Publica el evento
- ✅ Muestra los logs

#### Paso 4: Verificar en los logs

Deberías ver en los logs de la Lambda que:
1. El evento fue recibido
2. La Lambda procesó el evento
3. La Lambda publicó al topic SNS
4. El mensaje fue enviado al número suscrito

---

## 🔧 Gestión de Suscripciones

### Listar todas las suscripciones

```bash
aws sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1 \
  --output table
```

### Eliminar una suscripción

```bash
aws sns unsubscribe \
  --subscription-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms:7af7c06c-99c8-4ffd-b59c-8cfca76e5dde \
  --region us-east-1
```

**Nota**: Reemplaza el `subscription-arn` con el ARN real de la suscripción que quieres eliminar.

---

## 🔍 Verificar Estado Actual del Topic

### Usar el script de verificación (Recomendado)

```bash
./scripts/verificar_sns_topic.sh
```

### Verificación manual

```bash
#!/bin/bash

REGION="us-east-1"
TOPIC_ARN="arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms"

echo "🔍 Verificando Topic SNS: subscription-notifications-sms"
echo "========================================================="
echo ""

echo "1️⃣  Verificando que el topic existe..."
aws sns get-topic-attributes \
  --topic-arn "$TOPIC_ARN" \
  --region "$REGION" \
  --query '{TopicArn:Attributes.TopicArn,Owner:Attributes.Owner,DisplayName:Attributes.DisplayName}' \
  --output table

echo ""
echo "2️⃣  Verificando suscripciones..."
SUBSCRIPTIONS=$(aws sns list-subscriptions-by-topic \
  --topic-arn "$TOPIC_ARN" \
  --region "$REGION" \
  --output json)

SUBSCRIPTION_COUNT=$(echo "$SUBSCRIPTIONS" | jq '.Subscriptions | length')

if [ "$SUBSCRIPTION_COUNT" -eq 0 ]; then
  echo "⚠️  No hay suscripciones configuradas"
  echo "   Para recibir SMS, necesitas suscribir un número de teléfono:"
  echo "   aws sns subscribe --topic-arn $TOPIC_ARN --protocol sms --notification-endpoint +TU_NUMERO --region $REGION"
else
  echo "✅ Encontradas $SUBSCRIPTION_COUNT suscripción(es):"
  echo "$SUBSCRIPTIONS" | jq -r '.Subscriptions[] | "   - \(.Protocol): \(.Endpoint) [\(.SubscriptionArn)]"'
fi

echo ""
echo "3️⃣  Verificando permisos de la Lambda..."
LAMBDA_NAME="subscription-notification-handler"
if aws lambda get-function --function-name "$LAMBDA_NAME" --region "$REGION" > /dev/null 2>&1; then
  POLICY=$(aws lambda get-policy \
    --function-name "$LAMBDA_NAME" \
    --region "$REGION" 2>/dev/null | jq -r '.Policy' 2>/dev/null || echo "{}")
  
  if echo "$POLICY" | jq -e '.Statement[] | select(.Resource | contains("sns"))' > /dev/null 2>&1; then
    echo "✅ La Lambda tiene permisos para publicar en SNS"
  else
    echo "⚠️  No se encontraron permisos específicos para SNS en la Lambda"
    echo "   Verifica que el rol NotificationLambdaRole tenga permisos sns:Publish"
  fi
else
  echo "⚠️  No se encontró la Lambda $LAMBDA_NAME"
fi

echo ""
echo "========================================================="
echo "✅ Verificación completada"
```

---

## 📝 Notas Importantes

### 1. SMS en AWS SNS

- **Costos**: Los SMS tienen costos asociados que varían por país
- **Sandbox**: En modo sandbox, puedes tener límites de envío
- **Producción**: Para producción, solicita aumento de límites en AWS Support
- **Colombia**: El costo aproximado es de $0.00645 USD por SMS

### 2. Suscripciones

- Puedes tener múltiples suscripciones al mismo topic
- Cada suscripción puede ser a un número diferente
- Las suscripciones son inmediatas (no requieren confirmación para SMS)
- Para eliminar: `aws sns unsubscribe --subscription-arn <ARN>`

### 3. Permisos

- La Lambda necesita permisos `sns:Publish` en el topic
- Verifica que el rol `NotificationLambdaRole` tenga estos permisos
- El rol debe tener una política con:
  ```json
  {
    "Effect": "Allow",
    "Action": "sns:Publish",
    "Resource": "arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms"
  }
  ```

### 4. Monitoreo

- **CloudWatch Metrics**: Revisa métricas de publicación en SNS
- **CloudWatch Logs**: Revisa logs de la Lambda para ver errores
- **SNS Delivery Status**: Verifica el estado de entrega de mensajes

### 5. Formato de Números

- **Formato requerido**: `+[código país][número]`
- **Ejemplo Colombia**: `+573208965783`
- **Ejemplo USA**: `+1234567890`
- Sin el `+` inicial, AWS puede rechazar el número

---

## 🚀 Comandos Rápidos de Referencia

```bash
# ============================================
# CREAR Y CONFIGURAR
# ============================================

# Crear topic
aws sns create-topic --name subscription-notifications-sms --region us-east-1

# Verificar topic
aws sns get-topic-attributes \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1

# ============================================
# SUSCRIPCIONES
# ============================================

# Suscribir número de teléfono
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --protocol sms \
  --notification-endpoint +573208965783 \
  --region us-east-1

# Ver suscripciones
aws sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1 \
  --output table

# Eliminar suscripción
aws sns unsubscribe \
  --subscription-arn <SUBSCRIPTION_ARN> \
  --region us-east-1

# ============================================
# PUBLICAR MENSAJES
# ============================================

# Publicar mensaje de prueba
aws sns publish \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --message "Mensaje de prueba" \
  --region us-east-1

# ============================================
# VERIFICACIÓN
# ============================================

# Script de verificación completo
./scripts/verificar_sns_topic.sh

# Ver logs de Lambda
aws logs tail /aws/lambda/subscription-notification-handler \
  --follow \
  --region us-east-1

# Probar flujo completo
./scripts/test_eventbridge.sh
```

---

## 🔗 Recursos Relacionados

- **Script de verificación**: `./scripts/verificar_sns_topic.sh`
- **Script de prueba EventBridge**: `./scripts/test_eventbridge.sh`
- **Documentación EventBridge**: `README3.md`
- **Lambda Function**: `subscription-notification-handler`
- **EventBridge Bus**: `technical-test-bus`
- **EventBridge Rule**: `subscription-notifications`

---

## ❓ Troubleshooting

### El mensaje no se envía

1. **Verifica que hay suscripciones activas:**
   ```bash
   aws sns list-subscriptions-by-topic \
     --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
     --region us-east-1
   ```

2. **Verifica permisos de la Lambda:**
   ```bash
   aws lambda get-policy \
     --function-name subscription-notification-handler \
     --region us-east-1
   ```

3. **Revisa logs de CloudWatch:**
   ```bash
   aws logs tail /aws/lambda/subscription-notification-handler \
     --since 10m \
     --region us-east-1
   ```

### Error: "Invalid parameter: PhoneNumber"

- Verifica que el número tenga el formato correcto: `+[código país][número]`
- Asegúrate de incluir el `+` al inicio
- Verifica que el código de país sea válido

### Error: "Topic does not exist"

- Verifica que el topic esté creado:
  ```bash
  aws sns list-topics --region us-east-1
  ```
- Si no existe, créalo con el comando de creación

### La Lambda no publica al topic

- Verifica que la Lambda tenga permisos `sns:Publish`
- Revisa los logs de la Lambda para ver errores específicos
- Verifica que el ARN del topic sea correcto en el código de la Lambda

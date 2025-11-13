#!/bin/bash

# Script para probar EventBridge y disparar la Lambda
# Este script publica un evento de prueba que coincide con la regla subscription-notifications

set -e

# Variables
BUS_NAME="technical-test-bus"
RULE_NAME="subscription-notifications"
LAMBDA_NAME="subscription-notification-handler"
REGION="us-east-1"
ACCOUNT_ID="142911054234"

echo "🚀 Probando EventBridge - Paso a paso"
echo "======================================"
echo ""

# Paso 1: Verificar que la regla existe
echo "📋 Paso 1: Verificando que la regla existe..."
aws events describe-rule \
  --name "$RULE_NAME" \
  --event-bus-name "$BUS_NAME" \
  --region "$REGION" > /dev/null
echo "✅ Regla '$RULE_NAME' encontrada"
echo ""

# Paso 2: Verificar que la Lambda está configurada como target
echo "📋 Paso 2: Verificando targets de la regla..."
TARGETS=$(aws events list-targets-by-rule \
  --event-bus-name "$BUS_NAME" \
  --rule "$RULE_NAME" \
  --region "$REGION")

echo "$TARGETS" | jq '.'
LAMBDA_ARN=$(echo "$TARGETS" | jq -r '.Targets[0].Arn // empty')
if [ -z "$LAMBDA_ARN" ]; then
  echo "❌ Error: No se encontró Lambda como target"
  exit 1
fi
echo "✅ Lambda configurada como target: $LAMBDA_ARN"
echo ""

# Paso 3: Verificar que la Lambda tiene permisos
echo "📋 Paso 3: Verificando permisos de la Lambda..."
aws lambda get-policy \
  --function-name "$LAMBDA_NAME" \
  --region "$REGION" > /dev/null 2>&1 || echo "⚠️  No se pudo verificar permisos (puede estar bien si ya se configuró)"
echo ""

# Paso 4: Publicar evento de prueba - SubscriptionCreatedEvent
echo "📋 Paso 4: Publicando evento de prueba (SubscriptionCreatedEvent)..."
EVENT_ID=$(aws events put-events \
  --entries "[{
    \"Source\": \"technicaltest.subscriptions\",
    \"DetailType\": \"SubscriptionCreatedEvent\",
    \"Detail\": \"{\\\"subscriptionId\\\":\\\"11111111-1111-1111-1111-111111111111\\\",\\\"productId\\\":1,\\\"clientId\\\":\\\"11111111-1111-1111-1111-111111111111\\\",\\\"customerEmail\\\":\\\"test@example.com\\\",\\\"customerPhone\\\":\\\"+1234567890\\\",\\\"amount\\\":100000.0,\\\"subscribedAtUtc\\\":\\\"2024-01-01T00:00:00Z\\\"}\",
    \"EventBusName\": \"$BUS_NAME\"
  }]" \
  --region "$REGION" \
  --query 'Entries[0].EventId' \
  --output text)

if [ -z "$EVENT_ID" ] || [ "$EVENT_ID" == "None" ]; then
  echo "❌ Error: No se pudo publicar el evento"
  exit 1
fi

echo "✅ Evento publicado exitosamente"
echo "   Event ID: $EVENT_ID"
echo ""

# Paso 5: Esperar un momento para que EventBridge procese
echo "📋 Paso 5: Esperando 3 segundos para que EventBridge procese el evento..."
sleep 3
echo ""

# Paso 6: Ver logs de la Lambda
echo "📋 Paso 6: Verificando logs de la Lambda..."
echo "   (Esto mostrará los últimos logs de ejecución)"
echo ""
echo "📊 Últimos logs de CloudWatch:"
echo "   Ejecuta este comando para ver logs en tiempo real:"
echo "   aws logs tail /aws/lambda/$LAMBDA_NAME --follow --region $REGION"
echo ""

# Mostrar los últimos logs
aws logs tail "/aws/lambda/$LAMBDA_NAME" \
  --region "$REGION" \
  --since 1m \
  --format short 2>/dev/null || echo "   ⚠️  No hay logs recientes (puede que la Lambda aún no se haya ejecutado)"

echo ""
echo "======================================"
echo "✅ Prueba completada"
echo ""
echo "📝 Próximos pasos:"
echo "   1. Revisa los logs de la Lambda en CloudWatch"
echo "   2. Verifica que la Lambda se ejecutó correctamente"
echo "   3. Si quieres probar otro evento, ejecuta este script de nuevo"
echo ""
echo "🔍 Para ver logs en tiempo real:"
echo "   aws logs tail /aws/lambda/$LAMBDA_NAME --follow --region $REGION"
echo ""
echo "📋 Para probar un evento de cancelación, ejecuta:"
echo "   ./scripts/test_eventbridge_cancellation.sh"


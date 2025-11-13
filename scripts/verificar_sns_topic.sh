#!/bin/bash

# Script para verificar y probar el topic SNS subscription-notifications-sms

set -e

REGION="us-east-1"
TOPIC_ARN="arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms"
TOPIC_NAME="subscription-notifications-sms"

echo "🔍 Verificando Topic SNS: $TOPIC_NAME"
echo "========================================================="
echo ""

# 1. Verificar que el topic existe
echo "1️⃣  Verificando que el topic existe..."
if aws sns get-topic-attributes \
  --topic-arn "$TOPIC_ARN" \
  --region "$REGION" \
  --query 'Attributes.TopicArn' \
  --output text > /dev/null 2>&1; then
  echo "✅ Topic encontrado: $TOPIC_ARN"
  
  # Mostrar atributos
  echo ""
  echo "   Atributos del topic:"
  aws sns get-topic-attributes \
    --topic-arn "$TOPIC_ARN" \
    --region "$REGION" \
    --query '{TopicArn:Attributes.TopicArn,Owner:Attributes.Owner,DisplayName:Attributes.DisplayName}' \
    --output table
else
  echo "❌ Error: El topic no existe"
  echo ""
  echo "   Para crearlo, ejecuta:"
  echo "   aws sns create-topic --name $TOPIC_NAME --region $REGION"
  exit 1
fi

echo ""
echo "2️⃣  Verificando suscripciones..."
SUBSCRIPTIONS=$(aws sns list-subscriptions-by-topic \
  --topic-arn "$TOPIC_ARN" \
  --region "$REGION" \
  --output json 2>/dev/null || echo '{"Subscriptions":[]}')

SUBSCRIPTION_COUNT=$(echo "$SUBSCRIPTIONS" | jq '.Subscriptions | length' 2>/dev/null || echo "0")

if [ "$SUBSCRIPTION_COUNT" -eq 0 ]; then
  echo "⚠️  No hay suscripciones configuradas"
  echo ""
  echo "   Para recibir SMS, necesitas suscribir un número de teléfono:"
  echo "   aws sns subscribe \\"
  echo "     --topic-arn $TOPIC_ARN \\"
  echo "     --protocol sms \\"
  echo "     --notification-endpoint +TU_NUMERO \\"
  echo "     --region $REGION"
else
  echo "✅ Encontradas $SUBSCRIPTION_COUNT suscripción(es):"
  echo "$SUBSCRIPTIONS" | jq -r '.Subscriptions[] | "   - \(.Protocol): \(.Endpoint) [Estado: \(.SubscriptionArn)]"' 2>/dev/null || echo "$SUBSCRIPTIONS"
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
echo ""
echo "📝 Próximos pasos para probar:"
echo ""
echo "   Opción 1: Publicar mensaje directamente"
echo "   aws sns publish \\"
echo "     --topic-arn $TOPIC_ARN \\"
echo "     --message \"Mensaje de prueba\" \\"
echo "     --region $REGION"
echo ""
echo "   Opción 2: Probar flujo completo (EventBridge → Lambda → SNS)"
echo "   ./scripts/test_eventbridge.sh"
echo ""


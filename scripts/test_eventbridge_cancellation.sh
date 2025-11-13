#!/bin/bash

# Script para probar EventBridge con evento de cancelación
# Este script publica un evento SubscriptionCancelledEvent

set -e

# Variables
BUS_NAME="technical-test-bus"
REGION="us-east-1"

echo "🚀 Probando EventBridge - Evento de Cancelación"
echo "================================================"
echo ""

# Publicar evento de prueba - SubscriptionCancelledEvent
echo "📋 Publicando evento de prueba (SubscriptionCancelledEvent)..."
EVENT_ID=$(aws events put-events \
  --entries "[{
    \"Source\": \"technicaltest.subscriptions\",
    \"DetailType\": \"SubscriptionCancelledEvent\",
    \"Detail\": \"{\\\"subscriptionId\\\":\\\"11111111-1111-1111-1111-111111111111\\\",\\\"productId\\\":1,\\\"clientId\\\":\\\"11111111-1111-1111-1111-111111111111\\\",\\\"customerEmail\\\":\\\"test@example.com\\\",\\\"customerPhone\\\":\\\"+1234567890\\\",\\\"amount\\\":100000.0,\\\"cancelledAtUtc\\\":\\\"2024-01-01T12:00:00Z\\\"}\",
    \"EventBusName\": \"$BUS_NAME\"
  }]" \
  --region "$REGION" \
  --query 'Entries[0].EventId' \
  --output text)

if [ -z "$EVENT_ID" ] || [ "$EVENT_ID" == "None" ]; then
  echo "❌ Error: No se pudo publicar el evento"
  exit 1
fi

echo "✅ Evento de cancelación publicado exitosamente"
echo "   Event ID: $EVENT_ID"
echo ""
echo "📊 Revisa los logs de la Lambda para ver el evento procesado:"
echo "   aws logs tail /aws/lambda/subscription-notification-handler --follow --region $REGION"


#!/bin/bash

# Script de diagnóstico completo para problemas con SMS
# Verifica todo el flujo: EventBridge → Lambda → SNS → SMS

set -e

REGION="us-east-1"
BUS_NAME="technical-test-bus"
RULE_NAME="subscription-notifications"
LAMBDA_NAME="subscription-notification-handler"
TOPIC_ARN="arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms"
ACCOUNT_ID="142911054234"

echo "🔍 DIAGNÓSTICO COMPLETO: Problemas con SMS"
echo "=========================================="
echo ""
echo "Este script verificará todo el flujo para identificar el problema."
echo ""

ERRORS=0
WARNINGS=0

# ============================================
# 1. VERIFICAR CREDENCIALES AWS
# ============================================
echo "1️⃣  Verificando credenciales de AWS..."
if aws sts get-caller-identity --region "$REGION" > /dev/null 2>&1; then
    ACCOUNT=$(aws sts get-caller-identity --region "$REGION" --query 'Account' --output text)
    echo "✅ Credenciales configuradas (Account: $ACCOUNT)"
else
    echo "❌ ERROR: Credenciales de AWS NO configuradas o inválidas"
    echo "   Configura tus credenciales con: aws configure"
    ERRORS=$((ERRORS + 1))
    exit 1
fi
echo ""

# ============================================
# 2. VERIFICAR TOPIC SNS
# ============================================
echo "2️⃣  Verificando Topic SNS..."
if aws sns get-topic-attributes \
  --topic-arn "$TOPIC_ARN" \
  --region "$REGION" > /dev/null 2>&1; then
  echo "✅ Topic SNS existe: $TOPIC_ARN"
else
  echo "❌ ERROR: Topic SNS NO existe"
  echo "   Crea el topic con: aws sns create-topic --name subscription-notifications-sms --region $REGION"
  ERRORS=$((ERRORS + 1))
fi
echo ""

# ============================================
# 3. VERIFICAR SUSCRIPCIONES SNS
# ============================================
echo "3️⃣  Verificando suscripciones SNS..."
SUBSCRIPTIONS=$(aws sns list-subscriptions-by-topic \
  --topic-arn "$TOPIC_ARN" \
  --region "$REGION" \
  --output json 2>/dev/null || echo '{"Subscriptions":[]}')

SUBSCRIPTION_COUNT=$(echo "$SUBSCRIPTIONS" | jq '.Subscriptions | length' 2>/dev/null || echo "0")

if [ "$SUBSCRIPTION_COUNT" -eq 0 ]; then
  echo "❌ ERROR: No hay suscripciones configuradas en el topic SNS"
  echo "   Esto es probablemente la causa del problema."
  echo ""
  echo "   Para suscribir un número de teléfono:"
  echo "   aws sns subscribe \\"
  echo "     --topic-arn $TOPIC_ARN \\"
  echo "     --protocol sms \\"
  echo "     --notification-endpoint +TU_NUMERO \\"
  echo "     --region $REGION"
  ERRORS=$((ERRORS + 1))
else
  echo "✅ Encontradas $SUBSCRIPTION_COUNT suscripción(es):"
  echo "$SUBSCRIPTIONS" | jq -r '.Subscriptions[] | "   - \(.Protocol): \(.Endpoint) [ARN: \(.SubscriptionArn)]"' 2>/dev/null || echo "$SUBSCRIPTIONS"
  
  # Verificar estado de las suscripciones
  echo ""
  echo "   Verificando estado de suscripciones..."
  for sub in $(echo "$SUBSCRIPTIONS" | jq -r '.Subscriptions[].SubscriptionArn' 2>/dev/null); do
    if [ "$sub" != "PendingConfirmation" ]; then
      echo "   ✅ Suscripción $sub está activa"
    else
      echo "   ⚠️  Suscripción $sub está pendiente de confirmación"
      WARNINGS=$((WARNINGS + 1))
    fi
  done
fi
echo ""

# ============================================
# 4. VERIFICAR LAMBDA
# ============================================
echo "4️⃣  Verificando Lambda Function..."
if aws lambda get-function --function-name "$LAMBDA_NAME" --region "$REGION" > /dev/null 2>&1; then
  echo "✅ Lambda existe: $LAMBDA_NAME"
  
  # Verificar estado de la Lambda
  LAMBDA_STATE=$(aws lambda get-function-configuration \
    --function-name "$LAMBDA_NAME" \
    --region "$REGION" \
    --query 'State' \
    --output text 2>/dev/null || echo "Unknown")
  
  if [ "$LAMBDA_STATE" == "Active" ]; then
    echo "   ✅ Lambda está en estado: Active"
  else
    echo "   ⚠️  Lambda está en estado: $LAMBDA_STATE"
    WARNINGS=$((WARNINGS + 1))
  fi
  
  # Verificar última modificación
  LAST_MODIFIED=$(aws lambda get-function-configuration \
    --function-name "$LAMBDA_NAME" \
    --region "$REGION" \
    --query 'LastModified' \
    --output text 2>/dev/null || echo "Unknown")
  echo "   Última modificación: $LAST_MODIFIED"
else
  echo "❌ ERROR: Lambda NO existe: $LAMBDA_NAME"
  echo "   La Lambda debe existir para procesar eventos y enviar SMS"
  ERRORS=$((ERRORS + 1))
fi
echo ""

# ============================================
# 5. VERIFICAR PERMISOS DE LAMBDA PARA SNS
# ============================================
echo "5️⃣  Verificando permisos de Lambda para publicar en SNS..."
if aws lambda get-function --function-name "$LAMBDA_NAME" --region "$REGION" > /dev/null 2>&1; then
  POLICY=$(aws lambda get-policy \
    --function-name "$LAMBDA_NAME" \
    --region "$REGION" 2>/dev/null | jq -r '.Policy' 2>/dev/null || echo "{}")
  
  if echo "$POLICY" | jq -e '.Statement[] | select(.Action | contains("sns:Publish"))' > /dev/null 2>&1; then
    echo "✅ Lambda tiene permisos para publicar en SNS"
  else
    echo "⚠️  ADVERTENCIA: No se encontraron permisos explícitos para SNS en la Lambda"
    echo "   Verifica que el rol IAM de la Lambda tenga permisos sns:Publish"
    echo "   Rol necesario: NotificationLambdaRole"
    WARNINGS=$((WARNINGS + 1))
  fi
else
  echo "⚠️  No se puede verificar (Lambda no existe)"
fi
echo ""

# ============================================
# 6. VERIFICAR EVENTBRIDGE BUS
# ============================================
echo "6️⃣  Verificando EventBridge Bus..."
if aws events describe-event-bus --name "$BUS_NAME" --region "$REGION" > /dev/null 2>&1; then
  echo "✅ EventBridge Bus existe: $BUS_NAME"
else
  echo "❌ ERROR: EventBridge Bus NO existe: $BUS_NAME"
  echo "   Crea el bus con: aws events create-event-bus --name $BUS_NAME --region $REGION"
  ERRORS=$((ERRORS + 1))
fi
echo ""

# ============================================
# 7. VERIFICAR REGLA DE EVENTBRIDGE
# ============================================
echo "7️⃣  Verificando regla de EventBridge..."
if aws events describe-rule \
  --name "$RULE_NAME" \
  --event-bus-name "$BUS_NAME" \
  --region "$REGION" > /dev/null 2>&1; then
  echo "✅ Regla existe: $RULE_NAME"
  
  # Verificar estado de la regla
  RULE_STATE=$(aws events describe-rule \
    --name "$RULE_NAME" \
    --event-bus-name "$BUS_NAME" \
    --region "$REGION" \
    --query 'State' \
    --output text 2>/dev/null || echo "Unknown")
  
  if [ "$RULE_STATE" == "ENABLED" ]; then
    echo "   ✅ Regla está HABILITADA (State: $RULE_STATE)"
  else
    echo "   ❌ ERROR: Regla está DESHABILITADA (State: $RULE_STATE)"
    echo "   Habilítala con: aws events enable-rule --name $RULE_NAME --event-bus-name $BUS_NAME --region $REGION"
    ERRORS=$((ERRORS + 1))
  fi
else
  echo "❌ ERROR: Regla NO existe: $RULE_NAME"
  ERRORS=$((ERRORS + 1))
fi
echo ""

# ============================================
# 8. VERIFICAR TARGETS DE LA REGLA
# ============================================
echo "8️⃣  Verificando targets de la regla..."
TARGETS=$(aws events list-targets-by-rule \
  --rule "$RULE_NAME" \
  --event-bus-name "$BUS_NAME" \
  --region "$REGION" \
  --output json 2>/dev/null || echo '{"Targets":[]}')

TARGET_COUNT=$(echo "$TARGETS" | jq '.Targets | length' 2>/dev/null || echo "0")

if [ "$TARGET_COUNT" -eq 0 ]; then
  echo "❌ ERROR: La regla NO tiene targets configurados"
  echo "   La Lambda debe estar configurada como target"
  ERRORS=$((ERRORS + 1))
else
  echo "✅ La regla tiene $TARGET_COUNT target(s):"
  echo "$TARGETS" | jq -r '.Targets[] | "   - \(.Id): \(.Arn)"' 2>/dev/null
  
  # Verificar que la Lambda esté en los targets
  LAMBDA_IN_TARGETS=$(echo "$TARGETS" | jq -r ".Targets[] | select(.Arn | contains(\"$LAMBDA_NAME\")) | .Arn" 2>/dev/null || echo "")
  
  if [ -z "$LAMBDA_IN_TARGETS" ]; then
    echo "   ❌ ERROR: La Lambda NO está configurada como target"
    ERRORS=$((ERRORS + 1))
  else
    echo "   ✅ Lambda está configurada como target: $LAMBDA_IN_TARGETS"
  fi
fi
echo ""

# ============================================
# 9. VERIFICAR LOGS DE LA LAMBDA (ÚLTIMAS 5 MINUTAS)
# ============================================
echo "9️⃣  Verificando logs recientes de la Lambda..."
echo "   (Últimos 5 minutos)"
echo ""

LOGS=$(aws logs tail "/aws/lambda/$LAMBDA_NAME" \
  --region "$REGION" \
  --since 5m \
  --format short 2>/dev/null || echo "")

if [ -z "$LOGS" ]; then
  echo "⚠️  No hay logs recientes en los últimos 5 minutos"
  echo "   Esto puede significar que:"
  echo "   1. La Lambda no se está ejecutando (EventBridge no está enviando eventos)"
  echo "   2. No se han publicado eventos recientemente"
  echo "   3. Hay un problema con los permisos de CloudWatch Logs"
  WARNINGS=$((WARNINGS + 1))
else
  echo "✅ Logs encontrados:"
  echo "$LOGS" | head -20
  echo ""
  
  # Buscar errores en los logs
  ERROR_LOG_COUNT=$(echo "$LOGS" | grep -i "error\|exception\|failed\|fail" | wc -l || echo "0")
  if [ "$ERROR_LOG_COUNT" -gt 0 ]; then
    echo "⚠️  Se encontraron $ERROR_LOG_COUNT líneas con errores en los logs:"
    echo "$LOGS" | grep -i "error\|exception\|failed\|fail" | head -5
    WARNINGS=$((WARNINGS + 1))
  else
    echo "✅ No se encontraron errores obvios en los logs recientes"
  fi
fi
echo ""

# ============================================
# 10. VERIFICAR MÉTRICAS DE SNS (ÚLTIMAS 24 HORAS)
# ============================================
echo "🔟 Verificando métricas de publicación en SNS..."
echo "   (Últimas 24 horas)"
echo ""

# Intentar obtener métricas
METRICS=$(aws cloudwatch get-metric-statistics \
  --namespace AWS/SNS \
  --metric-name NumberOfMessagesPublished \
  --dimensions Name=TopicArn,Value="$TOPIC_ARN" \
  --start-time "$(date -u -v-24H +%Y-%m-%dT%H:%M:%S 2>/dev/null || date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S)" \
  --end-time "$(date -u +%Y-%m-%dT%H:%M:%S)" \
  --period 3600 \
  --statistics Sum \
  --region "$REGION" \
  --output json 2>/dev/null || echo '{"Datapoints":[]}')

METRIC_COUNT=$(echo "$METRICS" | jq '.Datapoints | length' 2>/dev/null || echo "0")

if [ "$METRIC_COUNT" -gt 0 ]; then
  TOTAL_MESSAGES=$(echo "$METRICS" | jq '[.Datapoints[].Sum] | add' 2>/dev/null || echo "0")
  echo "✅ Se publicaron aproximadamente $TOTAL_MESSAGES mensajes en las últimas 24 horas"
else
  echo "⚠️  No se encontraron métricas de publicación en las últimas 24 horas"
  echo "   Esto puede indicar que no se están publicando mensajes al topic SNS"
  WARNINGS=$((WARNINGS + 1))
fi
echo ""

# ============================================
# RESUMEN Y RECOMENDACIONES
# ============================================
echo "=========================================="
echo "📊 RESUMEN DEL DIAGNÓSTICO"
echo "=========================================="
echo ""

if [ "$ERRORS" -eq 0 ] && [ "$WARNINGS" -eq 0 ]; then
  echo "✅ Todo parece estar configurado correctamente"
  echo ""
  echo "📝 Si aún así no recibes SMS, verifica:"
  echo "   1. Que el número de teléfono en los eventos coincida con una suscripción activa"
  echo "   2. Que tu número de teléfono tenga el formato correcto (+código_país+número)"
  echo "   3. Que el número no esté bloqueado por el operador"
  echo "   4. Revisa los logs detallados de la Lambda:"
  echo "      aws logs tail /aws/lambda/$LAMBDA_NAME --follow --region $REGION"
  echo ""
  echo "🧪 Para probar el flujo completo:"
  echo "   ./scripts/test_eventbridge.sh"
elif [ "$ERRORS" -eq 0 ]; then
  echo "⚠️  Se encontraron $WARNINGS advertencia(s) pero no errores críticos"
  echo ""
  echo "📝 Revisa las advertencias arriba y las recomendaciones."
elif [ "$ERRORS" -gt 0 ]; then
  echo "❌ Se encontraron $ERRORS error(es) crítico(s)"
  echo "⚠️  Se encontraron $WARNINGS advertencia(s)"
  echo ""
  echo "📝 CORRIGE LOS ERRORES PRIMERO antes de continuar."
fi

echo ""
echo "=========================================="
echo "🔧 COMANDOS ÚTILES"
echo "=========================================="
echo ""
echo "📋 Ver logs de la Lambda en tiempo real:"
echo "   aws logs tail /aws/lambda/$LAMBDA_NAME --follow --region $REGION"
echo ""
echo "📋 Probar flujo completo (EventBridge → Lambda → SNS):"
echo "   ./scripts/test_eventbridge.sh"
echo ""
echo "📋 Publicar mensaje directamente a SNS (prueba rápida):"
echo "   aws sns publish \\"
echo "     --topic-arn $TOPIC_ARN \\"
echo "     --message \"Mensaje de prueba\" \\"
echo "     --region $REGION"
echo ""
echo "📋 Listar suscripciones SNS:"
echo "   aws sns list-subscriptions-by-topic \\"
echo "     --topic-arn $TOPIC_ARN \\"
echo "     --region $REGION \\"
echo "     --output table"
echo ""
echo "=========================================="

exit $ERRORS


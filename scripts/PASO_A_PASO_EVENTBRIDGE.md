# Paso a Paso: Configurar AWS EventBridge para NotifyAsync y NotifyCancellationAsync

Este documento contiene todos los pasos necesarios para configurar AWS EventBridge y que se active la Lambda `subscription-notification-handler` cuando se ejecuten `NotifyAsync` o `NotifyCancellationAsync`.

---

## ✅ PASOS COMPLETADOS (Código)

Los siguientes pasos ya están implementados en el código:

1. ✅ Paquete NuGet `AWSSDK.EventBridge` agregado al proyecto
2. ✅ Configuración de AWS agregada en `appsettings.json`
3. ✅ Servicio `IEventBridgeService` creado
4. ✅ Implementación `EventBridgeService` creada
5. ✅ `NotificationService` modificado para publicar eventos a EventBridge
6. ✅ Servicios registrados en `Program.cs`

---

## 📋 PASOS PENDIENTES (Configuración AWS)

### Paso 7: Verificar que el EventBridge Bus existe

**Comando:**
```bash
aws events describe-event-bus --name technical-test-bus --region us-east-1
```

**Resultado esperado:**
- Si el bus existe, verás información del bus
- Si no existe, verás un error `ResourceNotFoundException`

**Si el bus NO existe, créalo con:**
```bash
aws events create-event-bus --name technical-test-bus --region us-east-1
```

**Ejecuta el comando y comparte el resultado.**

---

### Paso 8: Verificar que la regla subscription-notifications está configurada

**Comando para verificar si la regla existe:**
```bash
aws events describe-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --region us-east-1
```

**Resultado esperado:**
- Debe mostrar información de la regla
- El campo `State` debe ser `ENABLED`

**Si la regla NO existe, créala con:**
```bash
aws events put-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --event-pattern '{"source":["technicaltest.subscriptions"],"detail-type":["SubscriptionCreatedEvent","SubscriptionCancelledEvent"]}' \
  --state ENABLED \
  --region us-east-1
```

**Ejecuta el comando y comparte el resultado.**

---

### Paso 9: Verificar que la Lambda está configurada como target

**Comando para verificar los targets de la regla:**
```bash
aws events list-targets-by-rule \
  --rule subscription-notifications \
  --event-bus-name technical-test-bus \
  --region us-east-1
```

**Resultado esperado:**
- Debe mostrar la Lambda `subscription-notification-handler` como target

**Si la Lambda NO está configurada como target, agrégala con:**
```bash
aws events put-targets \
  --rule subscription-notifications \
  --event-bus-name technical-test-bus \
  --targets "Id"="1","Arn"="arn:aws:lambda:us-east-1:142911054234:function:subscription-notification-handler" \
  --region us-east-1
```

**Nota:** Reemplaza `142911054234` con tu Account ID de AWS si es diferente.

**Ejecuta el comando y comparte el resultado.**

---

### Paso 10: Configurar credenciales de AWS

**Opciones para configurar credenciales:**

#### Opción A: Variables de entorno (Recomendado para desarrollo)

```bash
export AWS_ACCESS_KEY_ID="tu-access-key-id"
export AWS_SECRET_ACCESS_KEY="tu-secret-access-key"
export AWS_REGION="us-east-1"
```

#### Opción B: Archivo de credenciales (~/.aws/credentials)

```bash
aws configure
```

Te pedirá:
- AWS Access Key ID: [tu-access-key-id]
- AWS Secret Access Key: [tu-secret-access-key]
- Default region name: us-east-1
- Default output format: json

#### Opción C: IAM Role (Para EC2/ECS/Lambda)

Si estás ejecutando en AWS, puedes usar un IAM Role en lugar de credenciales.

**Verifica que las credenciales funcionan:**
```bash
aws sts get-caller-identity --region us-east-1
```

**Ejecuta el comando y comparte el resultado (sin mostrar las credenciales).**

---

### Paso 11: Instalar dependencias y compilar el proyecto

**Comando para restaurar paquetes NuGet:**
```bash
cd /Users/arhtur/pruebaTecnicaAmaris/TechnicalTest.Api
dotnet restore
```

**Comando para compilar:**
```bash
dotnet build
```

**Ejecuta los comandos y comparte el resultado.**

---

### Paso 12: Probar el flujo completo

#### 12.1: Abrir una terminal para ver logs de la Lambda en tiempo real

```bash
aws logs tail /aws/lambda/subscription-notification-handler \
  --follow \
  --region us-east-1
```

**Deja esta terminal abierta para ver los logs.**

#### 12.2: En otra terminal, ejecutar la aplicación

```bash
cd /Users/arhtur/pruebaTecnicaAmaris/TechnicalTest.Api
dotnet run
```

**Espera a que la aplicación inicie (verás un mensaje como "Now listening on: https://localhost:5001").**

#### 12.3: Crear una suscripción desde la API

**Opción A: Usar Swagger UI**
1. Abre tu navegador en `https://localhost:5001/swagger` (o el puerto que muestre la aplicación)
2. Autentícate primero (endpoint `/api/auth/login`)
3. Crea una suscripción usando el endpoint de suscripciones

**Opción B: Usar curl**

Primero, obtén un token JWT:
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "tu-email@example.com",
    "password": "tu-password"
  }'
```

Luego, crea una suscripción (reemplaza `TOKEN` con el token obtenido):
```bash
curl -X POST "https://localhost:5001/api/subscriptions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{
    "productId": 1,
    "clientId": "tu-client-id"
  }'
```

#### 12.4: Verificar en los logs de la Lambda

En la terminal donde estás viendo los logs de la Lambda, deberías ver:
- `START RequestId: ...`
- `Received EventBridge event: {...}`
- `Processing subscription created: ...` o `Processing subscription cancelled: ...`

**Ejecuta los pasos y comparte los resultados.**

---

## 🔍 Comandos de Verificación Rápida

### Verificar todo el flujo de una vez:

```bash
#!/bin/bash

REGION="us-east-1"
EVENT_BUS="technical-test-bus"
RULE_NAME="subscription-notifications"
LAMBDA_NAME="subscription-notification-handler"

echo "🔍 Verificando configuración de EventBridge..."
echo ""

echo "1️⃣ Verificando EventBus..."
if aws events describe-event-bus --name "$EVENT_BUS" --region "$REGION" > /dev/null 2>&1; then
  echo "✅ EventBus '$EVENT_BUS' existe"
else
  echo "❌ EventBus '$EVENT_BUS' NO existe"
  echo "   Crear con: aws events create-event-bus --name $EVENT_BUS --region $REGION"
fi

echo ""
echo "2️⃣ Verificando regla..."
if aws events describe-rule --name "$RULE_NAME" --event-bus-name "$EVENT_BUS" --region "$REGION" > /dev/null 2>&1; then
  STATE=$(aws events describe-rule --name "$RULE_NAME" --event-bus-name "$EVENT_BUS" --region "$REGION" --query 'State' --output text)
  echo "✅ Regla '$RULE_NAME' existe (Estado: $STATE)"
else
  echo "❌ Regla '$RULE_NAME' NO existe"
fi

echo ""
echo "3️⃣ Verificando targets de la regla..."
TARGETS=$(aws events list-targets-by-rule --rule "$RULE_NAME" --event-bus-name "$EVENT_BUS" --region "$REGION" --output json 2>/dev/null)
if [ $? -eq 0 ] && [ "$(echo "$TARGETS" | jq '.Targets | length')" -gt 0 ]; then
  echo "✅ La regla tiene targets configurados:"
  echo "$TARGETS" | jq -r '.Targets[] | "   - \(.Id): \(.Arn)"'
else
  echo "❌ La regla NO tiene targets configurados"
fi

echo ""
echo "4️⃣ Verificando Lambda..."
if aws lambda get-function --function-name "$LAMBDA_NAME" --region "$REGION" > /dev/null 2>&1; then
  echo "✅ Lambda '$LAMBDA_NAME' existe"
else
  echo "❌ Lambda '$LAMBDA_NAME' NO existe"
fi

echo ""
echo "5️⃣ Verificando credenciales de AWS..."
if aws sts get-caller-identity --region "$REGION" > /dev/null 2>&1; then
  ACCOUNT=$(aws sts get-caller-identity --region "$REGION" --query 'Account' --output text)
  echo "✅ Credenciales configuradas (Account: $ACCOUNT)"
else
  echo "❌ Credenciales NO configuradas o inválidas"
fi

echo ""
echo "=========================================="
echo "✅ Verificación completada"
```

Guarda este script como `verificar_eventbridge.sh` y ejecútalo:
```bash
chmod +x verificar_eventbridge.sh
./verificar_eventbridge.sh
```

---

## 📝 Resumen de Comandos por Paso

### Paso 7: Verificar EventBus
```bash
aws events describe-event-bus --name technical-test-bus --region us-east-1
```

### Paso 8: Verificar Regla
```bash
aws events describe-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --region us-east-1
```

### Paso 9: Verificar Targets
```bash
aws events list-targets-by-rule \
  --rule subscription-notifications \
  --event-bus-name technical-test-bus \
  --region us-east-1
```

### Paso 10: Verificar Credenciales
```bash
aws sts get-caller-identity --region us-east-1
```

### Paso 11: Compilar Proyecto
```bash
cd /Users/arhtur/pruebaTecnicaAmaris/TechnicalTest.Api
dotnet restore
dotnet build
```

### Paso 12: Ver Logs de Lambda
```bash
aws logs tail /aws/lambda/subscription-notification-handler --follow --region us-east-1
```

---

## 🐛 Troubleshooting

### Error: "Unable to locate credentials"
- Verifica que las credenciales estén configuradas (Paso 10)
- Verifica que `AWS_ACCESS_KEY_ID` y `AWS_SECRET_ACCESS_KEY` estén en las variables de entorno

### Error: "ResourceNotFoundException" al verificar EventBus
- El EventBus no existe, créalo con el comando del Paso 7

### Error: "ResourceNotFoundException" al verificar la regla
- La regla no existe, créala con el comando del Paso 8

### La Lambda no se ejecuta cuando se publica un evento
- Verifica que la regla esté `ENABLED` (Paso 8)
- Verifica que la Lambda esté configurada como target (Paso 9)
- Verifica los permisos: la regla necesita permisos para invocar la Lambda

### No veo logs en la Lambda
- Espera unos segundos (EventBridge puede tardar)
- Verifica que la Lambda tenga permisos para escribir logs en CloudWatch
- Verifica que estés usando el nombre correcto del log group

---

## 📚 Recursos Relacionados

- **Script de prueba EventBridge**: `./scripts/test_eventbridge.sh`
- **Documentación EventBridge**: `scripts/TEST_EVENTBRIDGE.md`
- **Documentación SNS**: `scripts/COMANDOS_SNS_TOPIC.md`

---

## ✅ Checklist Final

Antes de probar, verifica que:

- [ ] EventBus `technical-test-bus` existe
- [ ] Regla `subscription-notifications` existe y está `ENABLED`
- [ ] Lambda `subscription-notification-handler` está configurada como target
- [ ] Credenciales de AWS están configuradas
- [ ] Proyecto compila sin errores
- [ ] Aplicación está ejecutándose
- [ ] Logs de Lambda están siendo monitoreados

---

**¡Ejecuta los comandos paso a paso y comparte los resultados para continuar!**


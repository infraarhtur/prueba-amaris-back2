# 🔧 Solución: No están llegando los SMS

## 📊 Diagnóstico Realizado

El diagnóstico muestra que la infraestructura AWS está **correctamente configurada**:

✅ **Topic SNS existe**: `arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms`  
✅ **Suscripciones SNS activas**:
   - `+573208965783`
   - `+573223032928`
✅ **Lambda existe y está activa**: `subscription-notification-handler`  
✅ **EventBridge configurado correctamente**: Bus, regla y targets funcionando  
✅ **La Lambda SÍ se ejecuta** cuando se publican eventos

## 🔍 Problema Identificado

El problema **NO es técnico**, sino de **coincidencia de números**:

**Las suscripciones SNS solo pueden recibir SMS en números que estén suscritos al topic SNS.**

### ¿Cómo funciona?

1. Cuando creas una suscripción, la API publica un evento a EventBridge con el número del cliente (`client.Phone`)
2. La Lambda procesa el evento y publica un mensaje al topic SNS
3. **SNS solo envía SMS a números que estén suscritos al topic**

### Problema probable

Los números de teléfono de tus clientes en la base de datos **no coinciden** con los números suscritos en SNS.

**Números suscritos en SNS:**
- `+573208965783`
- `+573223032928`

**Si un cliente tiene un número diferente** (ejemplo: `+573123456789`), el SMS NO llegará porque ese número no está suscrito.

## ✅ Soluciones

### Solución 1: Verificar números de teléfono de clientes

**Paso 1:** Verifica qué números tienen tus clientes en la base de datos.

Si tienes acceso a la base de datos:
```sql
SELECT id, email, phone, "NotificationChannel" 
FROM clients 
WHERE "NotificationChannel" = 'Sms';
```

**Paso 2:** Compara esos números con las suscripciones SNS activas.

**Paso 3:** Si un cliente tiene un número diferente, tienes dos opciones:
- **Opción A**: Actualizar el número del cliente en la base de datos para que coincida con uno de los números suscritos
- **Opción B**: Suscribir el número del cliente al topic SNS (ver Solución 2)

### Solución 2: Suscribir números faltantes al topic SNS

Si un cliente tiene un número que NO está suscrito, suscríbelo:

```bash
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --protocol sms \
  --notification-endpoint +573123456789 \
  --region us-east-1
```

**Reemplaza `+573123456789` con el número real del cliente** (formato: `+[código país][número]`)

### Solución 3: Verificar en los logs de la aplicación

Cuando creas una suscripción, la aplicación registra el número de teléfono. Verifica los logs:

```bash
# Si estás ejecutando la aplicación, busca en los logs mensajes como:
# "📱 Enviando notificación con número de teléfono: +57..."
# "✅ Evento SubscriptionCreatedEvent publicado exitosamente a EventBridge... Phone: +57..."
```

### Solución 4: Probar el flujo completo

**Paso 1:** Verifica que un número esté suscrito:
```bash
aws sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1 \
  --output table
```

**Paso 2:** Crea una suscripción en la aplicación con un cliente que tenga ese número exacto.

**Paso 3:** Verifica los logs de la Lambda para ver si intentó enviar SMS:
```bash
aws logs tail /aws/lambda/subscription-notification-handler \
  --follow \
  --region us-east-1
```

**Paso 4:** Si la Lambda publicó al topic SNS pero no llegó el SMS, verifica:
- Que el número tenga el formato correcto (`+código_país+número`)
- Que no esté en modo "Sandbox" de AWS SNS (tiene límites de envío)

### Solución 5: Prueba directa con SNS

Prueba enviar un SMS directamente al topic para verificar que funciona:

```bash
aws sns publish \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --message "Mensaje de prueba - $(date)" \
  --region us-east-1
```

**Si recibes el SMS**, el problema es que los números en los eventos no coinciden con las suscripciones.  
**Si NO recibes el SMS**, el problema puede ser:
- Límites de Sandbox de AWS SNS
- Problemas con el operador telefónico
- El número está bloqueado

## 🔍 Pasos de Diagnóstico Rápido

### 1. Ejecutar diagnóstico completo

```bash
./scripts/diagnostico_sms.sh
```

### 2. Verificar suscripciones SNS

```bash
aws sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:us-east-1:142911054234:subscription-notifications-sms \
  --region us-east-1 \
  --output table
```

### 3. Verificar logs de la Lambda (última hora)

```bash
aws logs tail /aws/lambda/subscription-notification-handler \
  --region us-east-1 \
  --since 1h \
  --format short
```

Busca en los logs:
- ✅ `Processing subscription created:` - La Lambda procesó el evento
- ✅ `Publishing to SNS topic:` - Intentó publicar al topic SNS
- ❌ Errores relacionados con SNS o números de teléfono

### 4. Verificar eventos publicados a EventBridge

Revisa los logs de tu aplicación .NET. Deberías ver:
```
📤 Publicando SubscriptionCreatedEvent a EventBridge... Phone: +57...
✅ SubscriptionCreatedEvent publicado exitosamente... Phone: +57...
```

## 📋 Checklist de Verificación

- [ ] Ejecuté `./scripts/diagnostico_sms.sh` y no hay errores críticos
- [ ] Los números de teléfono de mis clientes coinciden con las suscripciones SNS
- [ ] O suscribí todos los números de clientes al topic SNS
- [ ] Verifiqué los logs de la Lambda y vi que intenta publicar a SNS
- [ ] Probé enviar un SMS directamente al topic y funcionó
- [ ] Verifiqué que los números tengan formato internacional (`+código+número`)

## 🚨 Problemas Comunes

### "La Lambda se ejecuta pero no veo intentos de enviar SMS"

**Causa**: La Lambda puede estar fallando antes de llegar a la parte de SMS (por ejemplo, fallando en el envío de email).

**Solución**: Revisa los logs completos de la Lambda para ver si hay errores antes del envío de SMS.

### "El número está suscrito pero no llega el SMS"

**Causas posibles**:
1. **AWS SNS Sandbox**: Si estás en modo sandbox, solo puedes enviar a números verificados
2. **Formato incorrecto**: El número debe tener formato `+código_país+número`
3. **Operador telefónico**: Algunos operadores bloquean SMS de AWS
4. **Límites de AWS**: Puede haber límites diarios de envío

**Soluciones**:
- Salir del modo Sandbox de AWS SNS (contactar AWS Support)
- Verificar que el número tenga el formato correcto
- Verificar en CloudWatch si hay errores de entrega de SNS

### "No sé qué números tienen mis clientes"

**Solución**: 
1. Consulta la base de datos (SQL mostrado arriba)
2. O usa el endpoint de la API para listar clientes
3. O revisa los logs de la aplicación cuando se crean suscripciones

## 📞 Siguiente Paso Recomendado

**Ejecuta este comando para ver los logs más recientes de la Lambda:**

```bash
aws logs tail /aws/lambda/subscription-notification-handler \
  --region us-east-1 \
  --since 1h \
  --format short | grep -i "phone\|sms\|sns\|customerPhone"
```

Esto te mostrará si la Lambda está recibiendo números de teléfono y si está intentando enviar SMS.

## 🔗 Recursos Relacionados

- **Script de diagnóstico**: `./scripts/diagnostico_sms.sh`
- **Script de prueba EventBridge**: `./scripts/test_eventbridge.sh`
- **Documentación SNS**: `./scripts/COMANDOS_SNS_TOPIC.md`
- **Documentación EventBridge**: `./scripts/PASO_A_PASO_EVENTBRIDGE.md`


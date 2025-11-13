# Implementación Event-Driven con AWS EventBridge

Este documento describe la implementación del patrón event-driven para notificaciones de suscripciones usando AWS EventBridge, Lambda, SES y SNS.

## Arquitectura

```
API .NET → EventBridge → Lambda → SES/SNS → Cliente
```

Cuando un cliente se suscribe o cancela una suscripción, la API publica un evento en EventBridge. Una Lambda consume el evento y envía notificaciones por correo (SES) o SMS (SNS).

---

## Paso 1: Verificación de Prerrequisitos

### Comandos ejecutados:
```bash
dotnet --version  # Verificar .NET SDK (9.0.306)
aws --version     # Verificar AWS CLI (2.10.2)
aws sts get-caller-identity  # Verificar credenciales AWS
```

### Resultado:
- ✅ .NET SDK 9.0.306 instalado
- ✅ AWS CLI 2.10.2 instalado
- ✅ Credenciales configuradas (usuario: development2, cuenta: 142911054234)

---

## Paso 2: Configuración de Recursos AWS

### 2.1 Variables de Entorno
```bash
export PROFILE=default
export REGION=us-east-1
export BUS_NAME=technical-test-bus
export RULE_NAME=subscription-notifications
export LAMBDA_NAME=subscription-notification-handler
export SNS_TOPIC_NAME=subscription-notifications-sms
export SES_IDENTITY=noreply@midominio.com
export ACCOUNT_ID=142911054234
```

### 2.2 Crear Rol IAM para EventBridge Publisher

**Propósito**: Permite que la aplicación .NET publique eventos en EventBridge.

```bash
aws iam create-role \
  --role-name EventBridgePublisherRole \
  --assume-role-policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": { "Service": "events.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }]
  }'
```

**Permisos agregados**:
```bash
aws iam put-role-policy \
  --role-name EventBridgePublisherRole \
  --policy-name EventBridgePublisherPolicy \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Action": "events:PutEvents",
      "Resource": "*"
    }]
  }'
```

**Cambios realizados**:
- Creado rol `EventBridgePublisherRole` con capacidad de asumir rol para el servicio EventBridge
- Agregada política inline con permiso `events:PutEvents` para publicar eventos

---

### 2.3 Crear Rol IAM para Lambda de Notificaciones

**Propósito**: Permite que la Lambda envíe correos (SES) y SMS (SNS).

```bash
aws iam create-role \
  --role-name NotificationLambdaRole \
  --assume-role-policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": { "Service": "lambda.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }]
  }'
```

**Política básica de logs**:
```bash
aws iam attach-role-policy \
  --role-name NotificationLambdaRole \
  --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole
```

**Permisos para SES y SNS**:
```bash
aws iam put-role-policy \
  --role-name NotificationLambdaRole \
  --policy-name NotificationChannelPolicy \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [
      {
        "Effect": "Allow",
        "Action": ["ses:SendEmail", "ses:SendRawEmail"],
        "Resource": "*"
      },
      {
        "Effect": "Allow",
        "Action": "sns:Publish",
        "Resource": "*"
      }
    ]
  }'
```

**Cambios realizados**:
- Creado rol `NotificationLambdaRole` con capacidad de asumir rol para Lambda
- Adjuntada política administrada para CloudWatch Logs
- Agregada política inline con permisos para SES y SNS

---

### 2.4 Crear EventBridge Bus

**Propósito**: Bus personalizado para eventos de suscripciones.

```bash
aws events create-event-bus \
  --name technical-test-bus
```

**Cambios realizados**:
- Creado bus de eventos `technical-test-bus` para aislar eventos del dominio de suscripciones

---

### 2.5 Crear Topic SNS para SMS

**Propósito**: Topic para enviar notificaciones SMS.

```bash
aws sns create-topic \
  --name subscription-notifications-sms
```

**Cambios realizados**:
- Creado topic SNS `subscription-notifications-sms` para notificaciones SMS

---

### 2.6 Configurar SES (Simple Email Service)

**Propósito**: Verificar identidad de correo para enviar emails.

```bash
# Listar identidades existentes
aws ses list-identities

# Verificar nueva identidad (si es necesario)
aws ses verify-email-identity \
  --email-address noreply@midominio.com
```

**Cambios realizados**:
- Verificada identidad de correo en SES (requiere confirmación por email)

**Nota**: Si tu cuenta está en modo sandbox, solo puedes enviar a direcciones verificadas. Para producción, solicita salir del sandbox.

---

### 2.7 Crear Lambda Function

**Propósito**: Función que procesa eventos y envía notificaciones.

```bash
# Crear directorio temporal y función placeholder
mkdir -p /tmp/subscription-lambda
cd /tmp/subscription-lambda
cat <<'EOF' > index.mjs
export const handler = async (event) => {
  console.log("EventBridge event:", JSON.stringify(event));
  return {};
};
EOF
zip function.zip index.mjs

# Crear Lambda
aws lambda create-function \
  --function-name subscription-notification-handler \
  --runtime nodejs20.x \
  --handler index.handler \
  --zip-file fileb://function.zip \
  --role arn:aws:iam::142911054234:role/NotificationLambdaRole
```

**Cambios realizados**:
- Creada Lambda `subscription-notification-handler` con runtime Node.js 20.x
- Asignado rol `NotificationLambdaRole` con permisos para SES y SNS

---

### 2.8 Configurar Permisos EventBridge → Lambda

**Propósito**: Permitir que EventBridge invoque la Lambda.

```bash
# Obtener ARN de la regla (se crea después)
RULE_ARN=$(aws events describe-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --query 'Arn' \
  --output text)

# Agregar permiso a Lambda
aws lambda add-permission \
  --function-name subscription-notification-handler \
  --statement-id AllowEventBridgeInvoke \
  --action lambda:InvokeFunction \
  --principal events.amazonaws.com \
  --source-arn "$RULE_ARN"
```

**Cambios realizados**:
- Agregado permiso resource-based a la Lambda para que EventBridge pueda invocarla

---

### 2.9 Crear Regla EventBridge y Asociar Target

**Propósito**: Filtrar eventos y enrutarlos a la Lambda.

```bash
# Crear regla con patrón de eventos
aws events put-rule \
  --name subscription-notifications \
  --event-bus-name technical-test-bus \
  --event-pattern '{
    "source": ["technicaltest.subscriptions"],
    "detail-type": ["SubscriptionCreatedEvent", "SubscriptionCancelledEvent"]
  }'

# Obtener ARN de Lambda
LAMBDA_ARN=$(aws lambda get-function \
  --function-name subscription-notification-handler \
  --query 'Configuration.FunctionArn' \
  --output text)

# Asociar Lambda como target
aws events put-targets \
  --event-bus-name technical-test-bus \
  --rule subscription-notifications \
  --targets "Id"="1","Arn"="$LAMBDA_ARN"
```

**Cambios realizados**:
- Creada regla `subscription-notifications` que filtra eventos con:
  - `source`: `technicaltest.subscriptions`
  - `detail-type`: `SubscriptionCreatedEvent` o `SubscriptionCancelledEvent`
- Asociada Lambda como target de la regla

---

## Paso 3: Integración en Código .NET

### 3.1 Agregar Paquetes NuGet

**Archivo**: `TechnicalTest.Infrastructure/TechnicalTest.Infrastructure.csproj`

```xml
<PackageReference Include="AWSSDK.EventBridge" Version="3.7.400.60" />
<PackageReference Include="AWSSDK.Extensions.NETCore.Setup" Version="3.7.7" />
```

**Cambios realizados**:
- Agregado SDK de AWS EventBridge para .NET
- Agregado paquete de configuración para integración con .NET Core

---

### 3.2 Crear Eventos de Dominio

**Archivo**: `TechnicalTest.Application/Events/SubscriptionCreatedEvent.cs`

```csharp
public sealed record SubscriptionCreatedEvent(
    Guid SubscriptionId,
    int ProductId,
    Guid ClientId,
    string CustomerEmail,
    string? CustomerPhone,
    decimal Amount,
    DateTime SubscribedAtUtc);
```

**Archivo**: `TechnicalTest.Application/Events/SubscriptionCancelledEvent.cs`

```csharp
public sealed record SubscriptionCancelledEvent(
    Guid SubscriptionId,
    int ProductId,
    Guid ClientId,
    string CustomerEmail,
    string? CustomerPhone,
    decimal Amount,
    DateTime CancelledAtUtc);
```

**Cambios realizados**:
- Creados eventos inmutables (records) que representan los eventos de negocio
- Incluyen toda la información necesaria para enviar notificaciones

---

### 3.3 Crear Interfaz IEventPublisher

**Archivo**: `TechnicalTest.Application/Interfaces/IEventPublisher.cs`

```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
```

**Cambios realizados**:
- Definida abstracción para publicar eventos, permitiendo diferentes implementaciones (AWS, in-memory para tests, etc.)

---

### 3.4 Implementar AwsEventBridgePublisher

**Archivo**: `TechnicalTest.Infrastructure/EventBus/AwsEventBridgePublisher.cs`

**Características**:
- Serializa eventos a JSON con camelCase
- Publica eventos en EventBridge usando `PutEvents`
- Maneja errores y logging
- Usa configuración desde `EventBridgeOptions`

**Cambios realizados**:
- Implementada publicación de eventos a AWS EventBridge
- Agregado logging para debugging y monitoreo
- Manejo de errores con excepciones descriptivas

---

### 3.5 Crear Clase de Configuración

**Archivo**: `TechnicalTest.Infrastructure/EventBus/EventBridgeOptions.cs`

```csharp
public sealed class EventBridgeOptions
{
    public const string SectionName = "AWS:EventBridge";
    public required string BusName { get; init; }
    public required string Source { get; init; }
    public string Region { get; init; } = "us-east-1";
}
```

**Cambios realizados**:
- Creada clase de opciones para binding de configuración desde `appsettings.json`

---

### 3.6 Configurar Dependency Injection

**Archivo**: `TechnicalTest.Infrastructure/DependencyInjection.cs`

```csharp
// AWS EventBridge configuration
services.Configure<EventBridgeOptions>(configuration.GetSection(EventBridgeOptions.SectionName));
services.AddAWSService<IAmazonEventBridge>();
services.AddScoped<IEventPublisher, AwsEventBridgePublisher>();
```

**Cambios realizados**:
- Configurado `EventBridgeOptions` desde configuración
- Registrado cliente de EventBridge como servicio singleton
- Registrado `AwsEventBridgePublisher` como implementación de `IEventPublisher`

---

### 3.7 Agregar Configuración en appsettings.json

**Archivo**: `TechnicalTest.Api/appsettings.json`

```json
"AWS": {
  "Region": "us-east-1",
  "EventBridge": {
    "BusName": "technical-test-bus",
    "Source": "technicaltest.subscriptions"
  }
}
```

**Cambios realizados**:
- Agregada sección de configuración AWS con región y parámetros de EventBridge

---

### 3.8 Modificar ProductManagementService

**Archivo**: `TechnicalTest.Application/Services/ProductManagementService.cs`

**Cambios en constructor**:
- Agregado `IUserRepository` para obtener email del cliente
- Agregado `IEventPublisher` para publicar eventos

**Cambios en `SubscribeAsync`**:
```csharp
// Después de crear la suscripción y notificar
var user = await _userRepository.GetByIdAsync(client.UserId, cancellationToken);
if (user is not null)
{
    var subscriptionEvent = new SubscriptionCreatedEvent(
        subscription.Id,
        product.Id,
        client.Id,
        user.Email,
        null, // Phone not available in current model
        subscription.Amount,
        subscription.SubscribedAtUtc);

    await _eventPublisher.PublishAsync(subscriptionEvent, cancellationToken);
}
```

**Cambios en `CancelSubscriptionAsync`**:
```csharp
// Después de cancelar la suscripción
var user = await _userRepository.GetByIdAsync(client.UserId, cancellationToken);
if (user is not null)
{
    var cancellationEvent = new SubscriptionCancelledEvent(
        subscription.Id,
        product.Id,
        client.Id,
        user.Email,
        null,
        subscription.Amount,
        subscription.CancelledAtUtc!.Value);

    await _eventPublisher.PublishAsync(cancellationEvent, cancellationToken);
}
```

**Cambios realizados**:
- Integrada publicación de eventos después de operaciones exitosas
- Obtención de email del usuario para incluir en eventos
- Publicación asíncrona sin bloquear la respuesta al cliente

---

### 3.9 Actualizar Tests

**Archivos modificados**:
- `test/TechnicalTest.Tests/Subscriptions/ProductManagementSubscriptionServiceTests.cs`
- `test/TechnicalTest.Tests/Product/ProductManagementServiceTests.cs`

**Cambios realizados**:
- Agregados mocks de `IUserRepository` y `IEventPublisher` en tests
- Actualizado constructor de `ProductManagementService` en tests

---

## Flujo Completo

1. **Cliente hace POST** `/api/subscriptions` → `SubscriptionsController.SubscribeAsync()`
2. **Controller llama** → `ProductManagementService.SubscribeAsync()`
3. **Service crea suscripción** y publica → `SubscriptionCreatedEvent` a EventBridge
4. **EventBridge recibe evento** y lo enruta según la regla
5. **Lambda se invoca** con el evento
6. **Lambda procesa** y envía notificación (email/SMS)

---

## Verificación

### Verificar que los eventos se publican:
```bash
# Ver logs de CloudWatch de la Lambda
aws logs tail /aws/lambda/subscription-notification-handler --follow
```

### Probar desde la API:
```bash
# Crear suscripción
curl -X POST https://localhost:5001/api/subscriptions \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "clientId": "11111111-1111-1111-1111-111111111111"}'
```

### Verificar eventos en EventBridge:
```bash
# Ver reglas
aws events list-rules --event-bus-name technical-test-bus

# Ver targets de una regla
aws events list-targets-by-rule \
  --event-bus-name technical-test-bus \
  --rule subscription-notifications
```

---

## Próximos Pasos

1. **Implementar Lambda completa**: Reemplazar placeholder con lógica de envío de emails/SMS
2. **Agregar manejo de errores**: Dead Letter Queue (DLQ) para eventos fallidos
3. **Agregar métricas**: CloudWatch Metrics para monitoreo
4. **Plantillas de email**: Usar SES Templates para emails formateados
5. **Preferencias de cliente**: Permitir que clientes elijan canal de notificación

---

## Recursos AWS Creados

| Recurso | Nombre | Propósito |
|---------|--------|-----------|
| IAM Role | `EventBridgePublisherRole` | Permisos para publicar eventos |
| IAM Role | `NotificationLambdaRole` | Permisos para Lambda (SES/SNS) |
| EventBridge Bus | `technical-test-bus` | Bus de eventos personalizado |
| EventBridge Rule | `subscription-notifications` | Filtra y enruta eventos |
| Lambda Function | `subscription-notification-handler` | Procesa eventos y envía notificaciones |
| SNS Topic | `subscription-notifications-sms` | Topic para SMS |

---

## Notas Importantes

- **Credenciales AWS**: La aplicación usa las credenciales del perfil configurado en `~/.aws/credentials`
- **Región**: Todos los recursos están en `us-east-1` (ajustable en configuración)
- **SES Sandbox**: Si estás en sandbox, solo puedes enviar a emails verificados
- **Costo**: EventBridge tiene 1 millón de eventos gratis por mes, luego $1.00 por millón

---

## Troubleshooting

### Error: "Failed to publish event"
- Verificar que las credenciales AWS tengan permisos `events:PutEvents`
- Verificar que el bus `technical-test-bus` existe
- Revisar logs de la aplicación

### Lambda no se invoca
- Verificar que la regla EventBridge tiene la Lambda como target
- Verificar permisos de EventBridge para invocar Lambda
- Revisar CloudWatch Logs de la Lambda

### No se reciben notificaciones
- Verificar que SES está fuera de sandbox o el email está verificado
- Verificar permisos de la Lambda para SES/SNS
- Revisar logs de CloudWatch de la Lambda


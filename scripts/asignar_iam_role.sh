#!/bin/bash

# Script para crear y asignar IAM Role a la instancia EC2
# Ejecutar desde la máquina local

set -e  # Salir si hay algún error

export AWS_REGION=us-east-1
export INSTANCE_NAME=prueba-amaris-ec2
export ROLE_NAME=prueba-amaris-ec2-role

echo "=== Creando y asignando IAM Role a la instancia EC2 ==="
echo ""

# Obtener Instance ID
echo "1. Obteniendo Instance ID..."
export INSTANCE_ID=$(aws ec2 describe-instances \
  --region $AWS_REGION \
  --filters "Name=tag:Name,Values=$INSTANCE_NAME" "Name=instance-state-name,Values=running" \
  --query 'Reservations[0].Instances[0].InstanceId' \
  --output text)

if [ -z "$INSTANCE_ID" ]; then
  echo "ERROR: No se encontró la instancia con nombre: $INSTANCE_NAME"
  exit 1
fi

echo "   Instance ID: $INSTANCE_ID"
echo ""

# Crear política de confianza
echo "2. Creando política de confianza..."
cat > /tmp/trust-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ec2.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
EOF
echo "   ✓ Política de confianza creada"
echo ""

# Crear IAM Role
echo "3. Creando IAM Role: $ROLE_NAME..."
if aws iam create-role \
  --role-name $ROLE_NAME \
  --assume-role-policy-document file:///tmp/trust-policy.json \
  --region $AWS_REGION 2>/dev/null; then
  echo "   ✓ IAM Role creado"
else
  echo "   ⚠ IAM Role ya existe, continuando..."
fi
echo ""

# Crear política con permisos para SNS y EventBridge
echo "4. Creando política de permisos..."
cat > /tmp/sns-eventbridge-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sns:*",
        "events:*"
      ],
      "Resource": "*"
    }
  ]
}
EOF
echo "   ✓ Política de permisos creada"
echo ""

# Crear política IAM
echo "5. Creando política IAM: prueba-amaris-sns-eventbridge-policy..."
if aws iam create-policy \
  --policy-name prueba-amaris-sns-eventbridge-policy \
  --policy-document file:///tmp/sns-eventbridge-policy.json \
  --region $AWS_REGION 2>/dev/null; then
  echo "   ✓ Política IAM creada"
else
  echo "   ⚠ Política IAM ya existe, continuando..."
fi
echo ""

# Obtener Account ID y Policy ARN
echo "6. Obteniendo Account ID..."
export ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export POLICY_ARN="arn:aws:iam::${ACCOUNT_ID}:policy/prueba-amaris-sns-eventbridge-policy"
echo "   Account ID: $ACCOUNT_ID"
echo "   Policy ARN: $POLICY_ARN"
echo ""

# Adjuntar política al rol
echo "7. Adjuntando política al rol..."
aws iam attach-role-policy \
  --role-name $ROLE_NAME \
  --policy-arn $POLICY_ARN \
  --region $AWS_REGION
echo "   ✓ Política adjuntada al rol"
echo ""

# Crear Instance Profile
echo "8. Creando Instance Profile: $ROLE_NAME..."
if aws iam create-instance-profile \
  --instance-profile-name $ROLE_NAME \
  --region $AWS_REGION 2>/dev/null; then
  echo "   ✓ Instance Profile creado"
else
  echo "   ⚠ Instance Profile ya existe, continuando..."
fi
echo ""

# Agregar rol al Instance Profile
echo "9. Agregando rol al Instance Profile..."
if aws iam add-role-to-instance-profile \
  --instance-profile-name $ROLE_NAME \
  --role-name $ROLE_NAME \
  --region $AWS_REGION 2>/dev/null; then
  echo "   ✓ Rol agregado al Instance Profile"
else
  echo "   ⚠ Rol ya está en el Instance Profile, continuando..."
fi
echo ""

# Esperar un momento para que se propague
echo "10. Esperando propagación de cambios..."
sleep 3
echo ""

# Asignar el Instance Profile a la instancia EC2
echo "11. Asignando Instance Profile a la instancia EC2..."
if aws ec2 associate-iam-instance-profile \
  --instance-id $INSTANCE_ID \
  --iam-instance-profile Name=$ROLE_NAME \
  --region $AWS_REGION 2>/dev/null; then
  echo "   ✓ Instance Profile asignado a la instancia"
else
  echo "   ⚠ Ya existe una asociación, verificando..."
fi
echo ""

# Verificar que se asignó correctamente
echo "12. Verificando asignación..."
aws ec2 describe-iam-instance-profile-associations \
  --filters "Name=instance-id,Values=$INSTANCE_ID" \
  --region $AWS_REGION \
  --output table

echo ""
echo "=== ¡Proceso completado! ==="
echo ""
echo "Para verificar desde la instancia EC2, ejecuta:"
echo "  ssh -i ~/.ssh/${KEY_NAME}.pem ec2-user@${PUBLIC_IP}"
echo "  curl -s http://169.254.169.254/latest/meta-data/iam/security-credentials/"


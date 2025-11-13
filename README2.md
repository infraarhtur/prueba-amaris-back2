## Despliegue en EC2 con Docker Compose

Pasos reproducibles que seguimos para levantar el proyecto en una instancia Amazon Linux 2023 utilizando AWS CLI, Docker y Docker Compose.

### 1. Preparar entorno local

```bash
export AWS_REGION=us-east-1
export KEY_NAME=prueba-amaris-key
export SECURITY_GROUP_NAME=prueba-amaris-sg
export INSTANCE_NAME=prueba-amaris-ec2
export INSTANCE_TYPE=t3.small

export AMI_ID=$(aws ec2 describe-images --owners amazon \
  --filters "Name=name,Values=al2023-ami-2023.*-x86_64" \
  --query "Images | sort_by(@, &CreationDate)[-1].ImageId" \
  --region $AWS_REGION --output text)
echo $AMI_ID
```

- Define variables reutilizables y obtiene la AMI de Amazon Linux 2023 más reciente.

### 2. Crear par de claves SSH

```bash
mkdir -p ~/.ssh
chmod 700 ~/.ssh

aws ec2 create-key-pair \
  --region $AWS_REGION \
  --key-name $KEY_NAME \
  --query 'KeyMaterial' \
  --output text > ~/.ssh/${KEY_NAME}.pem

chmod 400 ~/.ssh/${KEY_NAME}.pem
ls -l ~/.ssh/${KEY_NAME}.pem
```

- Garantiza que la clave quede almacenada con permisos correctos.

### 3. Crear grupo de seguridad y reglas

```bash
export SG_ID=$(aws ec2 create-security-group \
  --group-name $SECURITY_GROUP_NAME \
  --description "SG para prueba tecnica Amaris" \
  --region $AWS_REGION \
  --query 'GroupId' --output text)
echo $SG_ID

aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 22 --cidr 0.0.0.0/0 --region $AWS_REGION
aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 8080 --cidr 0.0.0.0/0 --region $AWS_REGION
```

- Abre SSH y el puerto público de la API (ajustar CIDR según políticas).

### 4. Lanzar instancia EC2

```bash
export INSTANCE_ID=$(aws ec2 run-instances \
  --region $AWS_REGION \
  --image-id $AMI_ID \
  --instance-type $INSTANCE_TYPE \
  --key-name $KEY_NAME \
  --security-group-ids $SG_ID \
  --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=$INSTANCE_NAME}]" \
  --query 'Instances[0].InstanceId' \
  --output text)
echo $INSTANCE_ID

aws ec2 wait instance-running --instance-ids $INSTANCE_ID --region $AWS_REGION

export PUBLIC_IP=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --region $AWS_REGION \
  --query 'Reservations[0].Instances[0].PublicIpAddress' \
  --output text)
echo $PUBLIC_IP
```

- Lanza la instancia, espera a que arranque y obtiene la IP pública.

### 5. Conexión SSH a la instancia

```bash
ssh -i ~/.ssh/${KEY_NAME}.pem ec2-user@${PUBLIC_IP}
```

- Accede usando el usuario `ec2-user` propio de Amazon Linux.

### 6. Configurar Docker y Docker Compose en la instancia

```bash
sudo dnf update -y
sudo dnf install -y docker
sudo systemctl enable --now docker
sudo usermod -aG docker ec2-user

mkdir -p ~/.docker/cli-plugins
curl -SL https://github.com/docker/compose/releases/download/v2.29.7/docker-compose-linux-x86_64 -o ~/.docker/cli-plugins/docker-compose
chmod +x ~/.docker/cli-plugins/docker-compose
docker compose version
```

- Instala Docker, habilita el servicio y añade el plugin oficial de Docker Compose v2.29.7.

### 7. Transferir el proyecto a la instancia

En la máquina local (fuera de la sesión SSH, usando la IP pública obtenida):

```bash
export KEY_NAME=prueba-amaris-key
export PUBLIC_IP=44.223.40.48

scp -i ~/.ssh/${KEY_NAME}.pem -r /Users/arhtur/pruebaTecnicaAmaris ec2-user@${PUBLIC_IP}:/home/ec2-user/
```

- Copia el repositorio completo al home del usuario remoto.

### 8. Construir e iniciar contenedores

En la sesión SSH:

```bash
cd /home/ec2-user/pruebaTecnicaAmaris
docker compose build
docker compose up -d
docker compose ps
```

- Compila la imagen .NET, levanta los servicios y verifica que `api` exponga `0.0.0.0:8080`.

### 9. Validar funcionamiento

Desde la máquina local:

```bash
curl http://44.223.40.48:8080
```

- Comprueba que la API responde. También se puede abrir `http://44.223.40.48:8080/swagger` si Swagger está habilitado.

### 10. Opciones adicionales

- Crear servicio `systemd` para reinicios automáticos (`docker compose up -d` en boot).
- Restringir el grupo de seguridad a rangos IP confiables.
- Apagar contenedores (`docker compose down`) y detener la instancia (`aws ec2 stop-instances ...`) cuando no se use para evitar cargos.

---

## Actualizar proyecto y imagen Docker en EC2

Pasos para actualizar el código del proyecto y reconstruir la imagen Docker en la instancia EC2 existente.

### 1. Obtener IP pública de la instancia (si no la tienes)

Desde tu máquina local:

```bash
export AWS_REGION=us-east-1
export INSTANCE_NAME=prueba-amaris-ec2
export KEY_NAME=prueba-amaris-key

# Obtener el Instance ID por nombre
export INSTANCE_ID=$(aws ec2 describe-instances \
  --region $AWS_REGION \
  --filters "Name=tag:Name,Values=$INSTANCE_NAME" "Name=instance-state-name,Values=running" \
  --query 'Reservations[0].Instances[0].InstanceId' \
  --output text)
echo "Instance ID: $INSTANCE_ID"

# Obtener la IP pública
export PUBLIC_IP=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --region $AWS_REGION \
  --query 'Reservations[0].Instances[0].PublicIpAddress' \
  --output text)
echo "Public IP: $PUBLIC_IP"
```

- Si ya conoces la IP pública, puedes definirla directamente: `export PUBLIC_IP=44.223.40.48`

### 2. Transferir archivos actualizados a la instancia

Desde tu máquina local (fuera de cualquier sesión SSH):

```bash
# Asegúrate de tener las variables definidas
export KEY_NAME=prueba-amaris-key
export PUBLIC_IP=44.223.40.48  # Actualiza con tu IP si es diferente

# Transferir el proyecto usando rsync (excluye bin/, obj/, node_modules/, etc.)
rsync -avz --progress \
  --exclude 'bin/' \
  --exclude 'obj/' \
  --exclude 'node_modules/' \
  --exclude '.git/' \
  --exclude 'TestResults/' \
  --exclude '.vs/' \
  --exclude '.vscode/' \
  -e "ssh -i ~/.ssh/${KEY_NAME}.pem" \
  /Users/arhtur/pruebaTecnicaAmaris/ \
  ec2-user@${PUBLIC_IP}:/home/ec2-user/pruebaTecnicaAmaris/
```

- `rsync` es más eficiente que `scp` y permite excluir directorios innecesarios (bin, obj, etc.).
- Solo transfiere archivos que han cambiado, haciendo la actualización más rápida.

### 3. Conectarse a la instancia EC2

```bash
ssh -i ~/.ssh/${KEY_NAME}.pem ec2-user@${PUBLIC_IP}
```

- Una vez conectado, estarás en la sesión SSH de la instancia.

### 4. Detener contenedores actuales

Dentro de la sesión SSH:

```bash
cd /home/ec2-user/pruebaTecnicaAmaris

# Ver estado actual de los contenedores
docker compose ps

# Detener y eliminar contenedores (sin eliminar volúmenes ni imágenes)
docker compose down
```

- Esto detiene los contenedores pero mantiene los volúmenes (datos de PostgreSQL) y las imágenes antiguas.

### 5. Limpiar imágenes Docker antiguas (opcional pero recomendado)

Dentro de la sesión SSH:

```bash
# Ver imágenes existentes
docker images

# Eliminar la imagen antigua de la aplicación (opcional, libera espacio)
docker rmi back-test-amaris:latest

# Limpiar imágenes huérfanas y caché (opcional)
docker image prune -f
```

- Esto libera espacio en disco eliminando la imagen antigua antes de construir la nueva.

### 6. Reconstruir la imagen Docker

Dentro de la sesión SSH:

```bash
# Asegúrate de estar en el directorio del proyecto
cd /home/ec2-user/pruebaTecnicaAmaris

# Reconstruir la imagen sin usar caché (para asegurar cambios)
docker compose build --no-cache

# O si prefieres usar caché (más rápido pero puede no detectar algunos cambios)
# docker compose build
```

- `--no-cache` garantiza que todos los cambios se reflejen, aunque tarda más.

### 7. Iniciar los contenedores actualizados

Dentro de la sesión SSH:

```bash
# Levantar los servicios en modo detached
docker compose up -d

# Verificar que los contenedores estén corriendo
docker compose ps

# Ver los logs para verificar que todo inició correctamente
docker compose logs api
```

- Verifica que ambos contenedores (`api` y `db`) estén en estado "Up".

### 8. Validar funcionamiento

Desde tu máquina local (fuera de la sesión SSH):

```bash
# Probar que la API responde
curl http://${PUBLIC_IP}:8080

# O abrir en el navegador
# http://${PUBLIC_IP}:8080/swagger
```

- Si todo está correcto, deberías recibir una respuesta de la API.

### 9. Verificar logs en caso de problemas

Si algo no funciona, dentro de la sesión SSH:

```bash
# Ver logs de la API
docker compose logs api

# Ver logs de la base de datos
docker compose logs db

# Ver logs de todos los servicios
docker compose logs

# Ver logs en tiempo real
docker compose logs -f api
```

### Resumen rápido de comandos

**En máquina local:**
```bash
export KEY_NAME=prueba-amaris-key
export PUBLIC_IP=44.223.40.48
rsync -avz --progress --exclude 'bin/' --exclude 'obj/' --exclude 'node_modules/' --exclude '.git/' --exclude 'TestResults/' -e "ssh -i ~/.ssh/${KEY_NAME}.pem" /Users/arhtur/pruebaTecnicaAmaris/ ec2-user@${PUBLIC_IP}:/home/ec2-user/pruebaTecnicaAmaris/
ssh -i ~/.ssh/${KEY_NAME}.pem ec2-user@${PUBLIC_IP}
```

**Dentro de la sesión SSH:**
```bash
cd /home/ec2-user/pruebaTecnicaAmaris
docker compose down
docker compose build --no-cache
docker compose up -d
docker compose ps
docker compose logs api
```

**Validar desde máquina local:**
```bash
curl http://${PUBLIC_IP}:8080
```

---

## Solucionar error de credenciales AWS en contenedor Docker

Si ves el error `Unable to get IAM security credentials from EC2 Instance Metadata Service`, el contenedor no puede acceder a las credenciales IAM de la instancia EC2.

### Solución: Configurar acceso a IMDS y asignar IAM Role

**1. Asignar IAM Role a la instancia EC2 (requerido)**

Desde tu máquina local, crea y asigna un IAM Role con permisos para SNS y EventBridge:

```bash
export AWS_REGION=us-east-1
export INSTANCE_NAME=prueba-amaris-ec2
export ROLE_NAME=prueba-amaris-ec2-role

# Obtener Instance ID
export INSTANCE_ID=$(aws ec2 describe-instances \
  --region $AWS_REGION \
  --filters "Name=tag:Name,Values=$INSTANCE_NAME" "Name=instance-state-name,Values=running" \
  --query 'Reservations[0].Instances[0].InstanceId' \
  --output text)

# Crear política de confianza para EC2
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

# Crear IAM Role
aws iam create-role \
  --role-name $ROLE_NAME \
  --assume-role-policy-document file:///tmp/trust-policy.json \
  --region $AWS_REGION

# Crear política con permisos para SNS y EventBridge
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

# Crear política IAM
aws iam create-policy \
  --policy-name prueba-amaris-sns-eventbridge-policy \
  --policy-document file:///tmp/sns-eventbridge-policy.json \
  --region $AWS_REGION

# Obtener ARN de la política (ajusta el account-id)
export ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export POLICY_ARN="arn:aws:iam::${ACCOUNT_ID}:policy/prueba-amaris-sns-eventbridge-policy"

# Adjuntar política al rol
aws iam attach-role-policy \
  --role-name $ROLE_NAME \
  --policy-arn $POLICY_ARN \
  --region $AWS_REGION

# Crear Instance Profile
aws iam create-instance-profile \
  --instance-profile-name $ROLE_NAME \
  --region $AWS_REGION

# Agregar rol al Instance Profile
aws iam add-role-to-instance-profile \
  --instance-profile-name $ROLE_NAME \
  --role-name $ROLE_NAME \
  --region $AWS_REGION

# Asignar el Instance Profile a la instancia EC2
aws ec2 associate-iam-instance-profile \
  --instance-id $INSTANCE_ID \
  --iam-instance-profile Name=$ROLE_NAME \
  --region $AWS_REGION

# Verificar que se asignó correctamente
aws ec2 describe-iam-instance-profile-associations \
  --filters "Name=instance-id,Values=$INSTANCE_ID" \
  --region $AWS_REGION
```

**2. Actualizar docker-compose.yml**

El archivo `docker-compose.yml` ya está configurado con `network_mode: host` para permitir acceso a IMDS. Si aún no lo has actualizado, transfiere el archivo actualizado:

```bash
# Desde tu máquina local
export KEY_NAME=prueba-amaris-key
export PUBLIC_IP=44.223.40.48
scp -i ~/.ssh/${KEY_NAME}.pem docker-compose.yml ec2-user@${PUBLIC_IP}:/home/ec2-user/pruebaTecnicaAmaris/
```

**3. Reiniciar contenedores**

Dentro de la sesión SSH en EC2:

```bash
cd /home/ec2-user/pruebaTecnicaAmaris
docker compose down
docker compose build --no-cache
docker compose up -d
docker compose logs api
```

**4. Verificar que funciona**

```bash
# Verificar que la instancia puede acceder a IMDS (desde la instancia EC2)
curl -s http://169.254.169.254/latest/meta-data/iam/security-credentials/

# Debería mostrar el nombre del rol IAM asignado
# Luego verificar los logs de la API para confirmar que no hay errores de credenciales
docker compose logs api | grep -i "credential\|error" | tail -20
```

**Nota importante:** 
- El `docker-compose.yml` actualizado usa `network_mode: host` para el servicio `api`, lo que permite acceso directo a IMDS.
- La conexión a la base de datos ahora usa `localhost:5435` porque el contenedor está en la red del host.
- Asegúrate de que el IAM Role tenga permisos para SNS y EventBridge en la región correcta.



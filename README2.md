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



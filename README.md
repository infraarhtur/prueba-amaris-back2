
# Guía del Proyecto

Este repositorio corresponde a la prueba técnica de Amaris desarrollada en .NET.  
En las secciones siguientes encontrarás la configuración necesaria para aplicar migraciones automáticas, ejecutar pruebas unitarias y generar reportes de cobertura.

## 1. Configuración de `TechnicalTest.Api`

Para asegurarte de que la base de datos se migre cada vez que se construye o publica el proyecto, agrega el siguiente bloque en `Program.cs`:

```csharp
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}
```

## 2. Pruebas unitarias

- **Ejecutar todas las pruebas**

  ```bash
  dotnet test
  ```

- **Ejecutar pruebas filtradas**

  ```bash
  dotnet test --filter subscriptions
  ```

## 3. Reportes de cobertura

### 3.1 Generar reporte en formato XML

```bash
dotnet test /Users/arhtur/pruebaTecnicaAmaris/TechnicalTest.Solution.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory /Users/arhtur/pruebaTecnicaAmaris/test/TestResults \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

### 3.2 Instalar el generador de reportes HTML

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### 3.3 Generar reporte en formato HTML

```bash
reportgenerator \
  "-reports:/Users/arhtur/pruebaTecnicaAmaris/test/TestResults/**/coverage.cobertura.xml" \
  "-targetdir:/Users/arhtur/pruebaTecnicaAmaris/test/TestResults/CoverageReport" \
  "-reporttypes:Html"
```

## 4. Integración continua (CI) con GitHub Actions

- Este repositorio cuenta con un workflow básico en `.github/workflows/dotnet-ci.yml` que se ejecuta en cada `push` o `pull_request` hacia `main`.
- El pipeline realiza los pasos `dotnet restore`, `dotnet build --configuration Release` y `dotnet test --configuration Release --collect:"XPlat Code Coverage"`.
- Puedes revisar las ejecuciones desde la pestaña **Actions** del repositorio en GitHub y usarlo como base para agregar tareas adicionales (linters, build de contenedores, despliegues, etc.).



# Comandos útiles de .NET:
- dotnet build - Compilar el proyecto
- dotnet run - Compilar y ejecutar
- dotnet restore - Restaurar paquetes NuGet
- dotnet clean - Limpiar archivos de compilación
- dotnet test - Ejecutar tests
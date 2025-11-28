# Guía de Release para Mantenedores

Esta guía describe el proceso completo para crear y publicar una nueva versión de Toon.Format.

## Proceso de Release

### 1. Preparación

1. Asegúrate de estar en la rama `main` y tenerla actualizada:
   ```powershell
   git checkout main
   git pull origin main
   ```

2. Crea una rama de release:
   ```powershell
   git checkout -b release/v0.1.0
   ```

3. Actualiza la versión en `src/ToonFormat/ToonFormat.csproj`:
   ```xml
   <Version>0.1.0</Version>
   ```

4. Actualiza el CHANGELOG.md con las novedades de la versión

### 2. Verificación

1. Ejecuta todos los tests:
   ```powershell
   dotnet test
   ```

2. Construye el paquete localmente:
   ```powershell
   .\build-nuget.ps1
   ```

3. Inspecciona el contenido del paquete generado en `artifacts/`

### 3. Commit y Push

```powershell
git add .
git commit -m "chore: bump version to 0.1.0"
git push origin release/v0.1.0
```

### 4. Pull Request

1. Crea un Pull Request desde `release/v0.1.0` hacia `main`
2. Espera la aprobación de revisores
3. Merge a `main`

### 5. Crear Release Tag

```powershell
git checkout main
git pull origin main
git tag -a v0.1.0 -m "Release v0.1.0"
git push origin v0.1.0
```

### 6. Publicación

#### Opción A: GitHub Actions (Recomendado)

El workflow se activará automáticamente al crear el release tag. Asegúrate de que:
- El secreto `NUGET_API_KEY` esté configurado en GitHub
- El workflow termine exitosamente

#### Opción B: Publicación Manual

```powershell
# Configura tu API key
$env:NUGET_API_KEY = "tu-api-key"

# Publica
.\build-nuget.ps1 -Publish
```

### 7. Verificación Post-Publicación

1. Verifica que el paquete aparezca en https://www.nuget.org/packages/Toon.Format/
2. Prueba instalar el paquete en un proyecto de prueba:
   ```powershell
   dotnet add package Toon.Format --version 0.1.0
   ```

## Checklist de Release

- [ ] Versión actualizada en .csproj
- [ ] CHANGELOG.md actualizado
- [ ] Todos los tests pasan
- [ ] Paquete construido y verificado localmente
- [ ] PR creado y aprobado
- [ ] Merged a main
- [ ] Tag creado y pusheado
- [ ] Paquete publicado en NuGet
- [ ] Verificación en NuGet.org
- [ ] Release notes publicadas en GitHub

## Versionado Semántico

- **PATCH** (0.0.x): Correcciones de bugs, mejoras menores
- **MINOR** (0.x.0): Nuevas características compatibles hacia atrás
- **MAJOR** (x.0.0): Cambios que rompen compatibilidad

## Comandos Útiles

### Ver versiones publicadas
```powershell
dotnet nuget search Toon.Format --exact-match
```

### Deslistar una versión (no elimina, solo oculta)
```powershell
dotnet nuget delete Toon.Format 0.1.0 --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

### Verificar configuración del paquete
```powershell
dotnet pack src/ToonFormat/ToonFormat.csproj --no-build -c Release /p:PackageOutputPath=./temp
# Inspeccionar el .nupkg generado
```

## Solución de Problemas

### Error: "Package already exists"
- No puedes sobrescribir versiones en NuGet
- Incrementa la versión y vuelve a publicar

### Error: "Invalid API Key"
- Verifica que la API Key tenga permisos de "Push"
- Asegúrate de que no haya expirado

### Tests fallan en CI pero pasan localmente
- Verifica que todas las dependencias estén en el repositorio
- Comprueba diferencias de entorno (versión de .NET, etc.)

## Recursos

- [Semantic Versioning](https://semver.org/)
- [NuGet Best Practices](https://docs.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github)

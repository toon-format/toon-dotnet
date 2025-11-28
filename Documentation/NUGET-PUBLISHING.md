# Guía para publicar Toon.Format en NuGet

Esta guía describe los pasos para publicar el paquete `Toon.Format` en NuGet.org.

## Prerrequisitos

1. **Cuenta en NuGet.org**: Necesitas una cuenta en [nuget.org](https://www.nuget.org/)
2. **API Key**: Genera una API Key desde tu perfil en NuGet.org
   - Ve a https://www.nuget.org/account/apikeys
   - Crea una nueva API Key con permisos de "Push"
   - Guarda la key de forma segura

## Configuración

### Configurar la API Key como variable de entorno (Recomendado)

**PowerShell:**
```powershell
$env:NUGET_API_KEY = "tu-api-key-aqui"
```

**O establecerla permanentemente:**
```powershell
[Environment]::SetEnvironmentVariable("NUGET_API_KEY", "tu-api-key-aqui", "User")
```

## Publicación

### Opción 1: Usar el script automatizado (Recomendado)

**Solo construir el paquete:**
```powershell
.\build-nuget.ps1
```

**Construir y publicar:**
```powershell
.\build-nuget.ps1 -Publish
```

**Con API Key específica:**
```powershell
.\build-nuget.ps1 -Publish -ApiKey "tu-api-key"
```

### Opción 2: Comandos manuales

1. **Limpiar y restaurar:**
   ```powershell
   dotnet clean -c Release
   dotnet restore
   ```

2. **Ejecutar tests:**
   ```powershell
   dotnet test -c Release
   ```

3. **Crear el paquete:**
   ```powershell
   dotnet pack .\src\ToonFormat\ToonFormat.csproj -c Release -o .\artifacts
   ```

4. **Publicar en NuGet:**
   ```powershell
   dotnet nuget push .\artifacts\Toon.Format.0.1.0.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
   ```

## Verificación del paquete

Antes de publicar, puedes inspeccionar el contenido del paquete:

```powershell
# Renombrar .nupkg a .zip temporalmente
Copy-Item .\artifacts\Toon.Format.0.1.0.nupkg .\artifacts\Toon.Format.0.1.0.zip
Expand-Archive .\artifacts\Toon.Format.0.1.0.zip .\artifacts\package-content
```

## Versionado

El proyecto sigue [Semantic Versioning](https://semver.org/):
- **MAJOR**: Cambios incompatibles en la API
- **MINOR**: Nueva funcionalidad compatible
- **PATCH**: Correcciones de bugs

Actualiza la versión en `src/ToonFormat/ToonFormat.csproj`:
```xml
<Version>0.1.0</Version>
```

## Notas importantes

1. **No puedes sobrescribir versiones**: Una vez publicada una versión, no se puede modificar
2. **Symbols Package**: El proyecto está configurado para generar símbolos de depuración (.snupkg)
3. **Source Link**: Incluido para mejorar la experiencia de depuración
4. **Multi-target**: El paquete soporta .NET 8.0 y .NET 9.0

## Deslistar una versión

Si necesitas deslistar (no eliminar) una versión publicada:

```powershell
dotnet nuget delete Toon.Format 0.1.0 --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json --non-interactive
```

O desde el portal web de NuGet.org.

## Recursos

- [Documentación oficial de NuGet](https://docs.microsoft.com/en-us/nuget/)
- [Mejores prácticas para paquetes NuGet](https://docs.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [Portal de NuGet.org](https://www.nuget.org/)

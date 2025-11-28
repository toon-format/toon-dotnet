# Script para construir y publicar el paquete NuGet
param(
    [string]$Configuration = "Release",
    [switch]$Publish,
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"

Write-Host "🔨 Building ToonFormat NuGet Package..." -ForegroundColor Cyan

# Limpiar builds anteriores
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c $Configuration
Remove-Item -Path ".\src\ToonFormat\bin\$Configuration\*.nupkg" -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\src\ToonFormat\bin\$Configuration\*.snupkg" -Force -ErrorAction SilentlyContinue

# Restaurar dependencias
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Ejecutar tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed. Aborting package creation." -ForegroundColor Red
    exit 1
}

# Crear el paquete NuGet
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
dotnet pack .\src\ToonFormat\ToonFormat.csproj -c $Configuration --no-restore -o .\artifacts

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Package creation failed." -ForegroundColor Red
    exit 1
}

$packagePath = Get-ChildItem -Path ".\artifacts\*.nupkg" | Select-Object -First 1

Write-Host "✅ Package created successfully: $($packagePath.Name)" -ForegroundColor Green

# Publicar si se especifica el flag
if ($Publish) {
    if ([string]::IsNullOrEmpty($ApiKey)) {
        Write-Host "❌ API Key not provided. Set NUGET_API_KEY environment variable or use -ApiKey parameter." -ForegroundColor Red
        exit 1
    }

    Write-Host "📦 Publishing to NuGet..." -ForegroundColor Cyan
    dotnet nuget push $packagePath.FullName --api-key $ApiKey --source $Source --skip-duplicate

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Package published successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Package publication failed." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Package created but not published. Use -Publish flag to publish." -ForegroundColor Yellow
    Write-Host "Package location: $($packagePath.FullName)" -ForegroundColor Yellow
}

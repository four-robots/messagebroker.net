# MessageBroker.NET NuGet Package Build Script for Windows
param(
    [switch]$SkipNativeBuild = $false
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "MessageBroker.NET NuGet Package Build" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: dotnet CLI is not installed" -ForegroundColor Red
    Write-Host "Please install .NET SDK from https://dot.net"
    exit 1
}

# Build native bindings first
if (-not $SkipNativeBuild) {
    Write-Host "Step 1: Building native bindings..." -ForegroundColor Yellow
    Push-Location native

    Write-Host "Building Windows bindings (nats-bindings.dll)..." -ForegroundColor Gray
    & .\build.ps1

    if (Test-Path "nats-bindings.dll") {
        Write-Host "✓ Windows bindings built successfully" -ForegroundColor Green
        Copy-Item nats-bindings.dll ..\src\MessageBroker.Nats\ -Force
    } else {
        Write-Host "⚠ Warning: Windows bindings build failed" -ForegroundColor Yellow
    }

    Pop-Location
} else {
    Write-Host "Step 1: Skipping native build (--SkipNativeBuild specified)" -ForegroundColor Yellow
}

# Clean previous builds
Write-Host ""
Write-Host "Step 2: Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean MessageBroker.NET.sln -c Release

# Restore dependencies
Write-Host ""
Write-Host "Step 3: Restoring dependencies..." -ForegroundColor Yellow
dotnet restore MessageBroker.NET.sln

# Build solution
Write-Host ""
Write-Host "Step 4: Building solution (multi-targeting: net8.0, net9.0, net10.0)..." -ForegroundColor Yellow
dotnet build MessageBroker.NET.sln -c Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Create output directory for packages
New-Item -ItemType Directory -Force -Path .\nupkg | Out-Null

# Pack MessageBroker.Core
Write-Host ""
Write-Host "Step 5: Creating MessageBroker.Core NuGet package..." -ForegroundColor Yellow
dotnet pack src\MessageBroker.Core\MessageBroker.Core.csproj -c Release --no-build --output .\nupkg

# Pack MessageBroker.Nats
Write-Host ""
Write-Host "Step 6: Creating MessageBroker.Nats NuGet package..." -ForegroundColor Yellow
dotnet pack src\MessageBroker.Nats\MessageBroker.Nats.csproj -c Release --no-build --output .\nupkg

# List created packages
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "✓ NuGet packages created successfully!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Packages created in .\nupkg\:" -ForegroundColor White
Get-ChildItem .\nupkg\*.nupkg | ForEach-Object {
    Write-Host "  $($_.Name) - $([math]::Round($_.Length / 1KB, 2)) KB" -ForegroundColor Gray
}

Write-Host ""
Write-Host "To publish to NuGet.org:" -ForegroundColor Yellow
Write-Host "  dotnet nuget push .\nupkg\MessageBroker.Core.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Gray
Write-Host "  dotnet nuget push .\nupkg\MessageBroker.Nats.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Gray
Write-Host ""
Write-Host "To install locally for testing:" -ForegroundColor Yellow
Write-Host "  dotnet add package MessageBroker.Nats --version 1.0.0 --source .\nupkg" -ForegroundColor Gray
Write-Host ""

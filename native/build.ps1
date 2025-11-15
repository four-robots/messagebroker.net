# Build script for Windows

$ErrorActionPreference = "Stop"

Write-Host "Building NATS bindings for Windows..." -ForegroundColor Green

# Get the directory of this script
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Ensure go.mod dependencies are downloaded
Write-Host "Downloading Go dependencies..." -ForegroundColor Cyan
go mod download
go mod tidy

# Build the shared library
Write-Host "Building shared library..." -ForegroundColor Cyan
go build -buildmode=c-shared -o nats-bindings.dll nats-bindings.go

# Copy to the .NET output directories
Write-Host "Copying to MessageBroker.Nats output directories..." -ForegroundColor Cyan
$TargetDirs = @(
    "..\src\MessageBroker.Nats\bin\Debug\net9.0",
    "..\src\MessageBroker.Nats\bin\Release\net9.0",
    "..\src\MessageBroker.Examples\bin\Debug\net9.0",
    "..\src\MessageBroker.Examples\bin\Release\net9.0"
)

foreach ($dir in $TargetDirs) {
    if (Test-Path $dir) {
        Write-Host "  -> $dir" -ForegroundColor Gray
        Copy-Item nats-bindings.dll -Destination $dir -Force
    }
}

# Also copy to the source directory for debugging
Write-Host "  -> ..\src\MessageBroker.Nats\" -ForegroundColor Gray
Copy-Item nats-bindings.dll -Destination ..\src\MessageBroker.Nats\ -Force

Write-Host ""
Write-Host "Build complete! nats-bindings.dll created successfully." -ForegroundColor Green
Write-Host ""
Write-Host "To use the library:"
Write-Host "  1. Build your .NET projects: dotnet build"
Write-Host "  2. Run this script again to copy the DLL to output directories"
Write-Host "  3. Or add a pre-build event to your .csproj files"

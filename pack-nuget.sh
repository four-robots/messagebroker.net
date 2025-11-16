#!/bin/bash
set -e

echo "======================================"
echo "DotGnatly NuGet Package Build"
echo "======================================"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet CLI is not installed"
    echo "Please install .NET SDK from https://dot.net"
    exit 1
fi

# Build native bindings first
echo "Step 1: Building native bindings..."
cd native

# Build for Linux (if on Linux/macOS)
if [[ "$OSTYPE" == "linux-gnu"* ]] || [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Building Linux bindings (nats-bindings.so)..."
    ./build.sh
    if [ -f "nats-bindings.so" ]; then
        echo "✓ Linux bindings built successfully"
        cp nats-bindings.so ../src/DotGnatly.Natives/
    else
        echo "⚠ Warning: Linux bindings build failed"
    fi
fi

# Build for Windows (if on Windows or cross-compiling)
if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    echo "Building Windows bindings (nats-bindings.dll)..."
    ./build.ps1
    if [ -f "nats-bindings.dll" ]; then
        echo "✓ Windows bindings built successfully"
        cp nats-bindings.dll ../src/DotGnatly.Natives/
    else
        echo "⚠ Warning: Windows bindings build failed"
    fi
fi

cd ..

# Clean previous builds
echo ""
echo "Step 2: Cleaning previous builds..."
dotnet clean DotGnatly.sln -c Release

# Restore dependencies
echo ""
echo "Step 3: Restoring dependencies..."
dotnet restore DotGnatly.sln

# Build solution
echo ""
echo "Step 4: Building solution (multi-targeting: net8.0, net9.0, net10.0)..."
dotnet build DotGnatly.sln -c Release --no-restore

# Create output directory for packages
mkdir -p ./nupkg

# Pack DotGnatly.Natives first (dependency)
echo ""
echo "Step 5: Creating DotGnatly.Natives NuGet package..."
dotnet pack src/DotGnatly.Natives/DotGnatly.Natives.csproj -c Release --no-build --output ./nupkg

# Pack DotGnatly.Core
echo ""
echo "Step 6: Creating DotGnatly.Core NuGet package..."
dotnet pack src/DotGnatly.Core/DotGnatly.Core.csproj -c Release --no-build --output ./nupkg

# Pack DotGnatly.Nats
echo ""
echo "Step 7: Creating DotGnatly.Nats NuGet package..."
dotnet pack src/DotGnatly.Nats/DotGnatly.Nats.csproj -c Release --no-build --output ./nupkg

# List created packages
echo ""
echo "======================================"
echo "✓ NuGet packages created successfully!"
echo "======================================"
echo ""
echo "Packages created in ./nupkg/:"
ls -lh ./nupkg/*.nupkg

echo ""
echo "To publish to NuGet.org (in order):"
echo "  dotnet nuget push ./nupkg/DotGnatly.Natives.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo "  dotnet nuget push ./nupkg/DotGnatly.Core.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo "  dotnet nuget push ./nupkg/DotGnatly.Nats.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo ""
echo "To install locally for testing:"
echo "  dotnet add package DotGnatly.Nats --version 1.0.0 --source ./nupkg"
echo ""

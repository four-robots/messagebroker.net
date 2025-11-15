#!/bin/bash
set -e

echo "======================================"
echo "MessageBroker.NET NuGet Package Build"
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
        cp nats-bindings.so ../src/MessageBroker.Nats/
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
        cp nats-bindings.dll ../src/MessageBroker.Nats/
    else
        echo "⚠ Warning: Windows bindings build failed"
    fi
fi

cd ..

# Clean previous builds
echo ""
echo "Step 2: Cleaning previous builds..."
dotnet clean MessageBroker.NET.sln -c Release

# Restore dependencies
echo ""
echo "Step 3: Restoring dependencies..."
dotnet restore MessageBroker.NET.sln

# Build solution
echo ""
echo "Step 4: Building solution (multi-targeting: net8.0, net9.0, net10.0)..."
dotnet build MessageBroker.NET.sln -c Release --no-restore

# Create output directory for packages
mkdir -p ./nupkg

# Pack MessageBroker.Core
echo ""
echo "Step 5: Creating MessageBroker.Core NuGet package..."
dotnet pack src/MessageBroker.Core/MessageBroker.Core.csproj -c Release --no-build --output ./nupkg

# Pack MessageBroker.Nats
echo ""
echo "Step 6: Creating MessageBroker.Nats NuGet package..."
dotnet pack src/MessageBroker.Nats/MessageBroker.Nats.csproj -c Release --no-build --output ./nupkg

# List created packages
echo ""
echo "======================================"
echo "✓ NuGet packages created successfully!"
echo "======================================"
echo ""
echo "Packages created in ./nupkg/:"
ls -lh ./nupkg/*.nupkg

echo ""
echo "To publish to NuGet.org:"
echo "  dotnet nuget push ./nupkg/MessageBroker.Core.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo "  dotnet nuget push ./nupkg/MessageBroker.Nats.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo ""
echo "To install locally for testing:"
echo "  dotnet add package MessageBroker.Nats --version 1.0.0 --source ./nupkg"
echo ""

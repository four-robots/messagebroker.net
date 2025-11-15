#!/bin/bash
# Build script for Linux/macOS

set -e

echo "Building NATS bindings for Linux/macOS..."

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Ensure go.mod dependencies are downloaded
echo "Downloading Go dependencies..."
go mod download
go mod tidy

# Build the shared library
echo "Building shared library..."
go build -buildmode=c-shared -o nats-bindings.so nats-bindings.go

# Copy to the .NET output directories
echo "Copying to MessageBroker.Nats output directories..."
TARGET_DIRS=(
    "../src/MessageBroker.Nats/bin/Debug/net9.0"
    "../src/MessageBroker.Nats/bin/Release/net9.0"
    "../src/MessageBroker.Examples/bin/Debug/net9.0"
    "../src/MessageBroker.Examples/bin/Release/net9.0"
)

for dir in "${TARGET_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo "  -> $dir"
        cp nats-bindings.so "$dir/"
    fi
done

# Also copy to the source directory for debugging
echo "  -> ../src/MessageBroker.Nats/"
cp nats-bindings.so ../src/MessageBroker.Nats/

echo ""
echo "Build complete! nats-bindings.so created successfully."
echo ""
echo "To use the library:"
echo "  1. Build your .NET projects: dotnet build"
echo "  2. Run this script again to copy the .so to output directories"
echo "  3. Or add a pre-build event to your .csproj files"

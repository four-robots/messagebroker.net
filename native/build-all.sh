#!/bin/bash
# Cross-platform build script that builds for both Windows and Linux

set -e

echo "Building NATS bindings for all platforms..."

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Ensure go.mod dependencies are downloaded
echo "Downloading Go dependencies..."
go mod download
go mod tidy

# Build for Linux
echo ""
echo "Building for Linux..."
GOOS=linux GOARCH=amd64 go build -buildmode=c-shared -o nats-bindings.so nats-bindings.go

# Build for Windows
echo ""
echo "Building for Windows..."
GOOS=windows GOARCH=amd64 go build -buildmode=c-shared -o nats-bindings.dll nats-bindings.go

# Copy to the source directory
echo ""
echo "Copying binaries to source directory..."
cp nats-bindings.so ../src/MessageBroker.Nats/
cp nats-bindings.dll ../src/MessageBroker.Nats/

echo ""
echo "Build complete!"
echo "  - nats-bindings.so (Linux)"
echo "  - nats-bindings.dll (Windows)"
echo ""
echo "Note: Cross-compilation may require additional setup."
echo "For production builds, compile on the target platform."

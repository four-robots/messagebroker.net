# DotGnatly.Natives

Native NATS server bindings for DotGnatly.

## Overview

This package contains platform-specific native libraries required by DotGnatly.Nats to control NATS server instances. The native bindings are built from Go source code and provide P/Invoke interop between .NET and the NATS server.

## Supported Platforms

- **Windows x64**: `nats-bindings.dll`
- **Linux x64**: `nats-bindings.so`

## NATS Server Version

This package includes NATS Server **v2.11.0**.

## Usage

This package is automatically referenced by `DotGnatly.Nats` and you typically don't need to reference it directly. However, you can reference it explicitly if you need to:

1. Update native bindings independently for hotfixes
2. Pin a specific version of the native bindings
3. Test new native binding versions

```xml
<ItemGroup>
  <PackageReference Include="DotGnatly.Natives" Version="1.0.0" />
</ItemGroup>
```

## Independent Versioning

This package can be versioned and updated independently from `DotGnatly.Nats`, allowing for:

- **Hotfixes**: Quickly patch native binding issues without releasing a new version of the main library
- **Platform Updates**: Add support for new platforms or architectures
- **NATS Upgrades**: Update to newer NATS server versions independently

## Building from Source

The native bindings are built from the Go source code in the `/native` directory of the DotGnatly repository.

**Build on Linux/macOS:**
```bash
cd native
./build.sh
```

**Build on Windows:**
```powershell
cd native
.\build.ps1
```

For detailed build instructions, see [native/README.md](../../native/README.md).

## Package Contents

```
DotGnatly.Natives.1.0.0.nupkg
├── runtimes/
│   ├── win-x64/
│   │   └── native/
│   │       └── nats-bindings.dll
│   └── linux-x64/
│       └── native/
│           └── nats-bindings.so
└── README.md
```

## License

MIT License - See repository root for full license text.

## More Information

- **GitHub**: https://github.com/four-robots/messagebroker.net
- **Documentation**: See the `/docs` folder in the repository
- **Main Package**: [DotGnatly.Nats](https://www.nuget.org/packages/DotGnatly.Nats)

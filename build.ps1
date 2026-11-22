#!/usr/bin/env pwsh
# build.ps1 - Build the OstoraVivi BepInEx plugin
# Requires: dotnet SDK 6.0+, Visual Studio Build Tools

set -e

echo "=== Building OstoraVivi Plugin ==="

# Change to project directory
cd "C:\Users\rayane kh\ostora-vivi"

# Restore NuGet packages and build
dotnet restore OstoraVivi.csproj
if [ $? -ne 0 ]; then
    echo "ERROR: dotnet restore failed"
    exit 1
fi

dotnet build -c Release OstoraVivi.csproj
if [ $? -ne 0 ]; then
    echo "ERROR: dotnet build failed"
    exit 1
fi

echo "Build successful."

# Copy DLL to BepInEx plugins directory
BEPINEX_PLUGINS_DIR="C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\plugins"
BUILD_OUTPUT="OstoraVivi/bin/Release/net6.0/OstoraVivi.dll"

if [ -f "$BUILD_OUTPUT" ]; then
    cp "$BUILD_OUTPUT" "$BEPINEX_PLUGINS_DIR/"
    echo "DLL copied to $BEPINEX_PLUGINS_DIR"
else
    echo "WARNING: Build output not found at $BUILD_OUTPUT"
    exit 1
fi

echo "=== Build complete ==="
echo "DLL placed in: $BEPINEX_PLUGINS_DIR/OstoraVivi.dll"
echo "Restart Among Us to load the plugin."
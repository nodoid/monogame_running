#!/usr/bin/env bash
#
# Compiles the HLSL effects in Content/Effects for Android and iOS.
#
# MonoGame's effect compiler (mgfxc) only runs on Windows, or on macOS/Linux through Wine.
# This script runs it inside a Parallels Desktop Windows virtual machine instead, then copies
# the compiled .xnb files into Content/Effects/Compiled/<Platform>/, where the Android and iOS
# projects package them. Run it whenever you change a .fx file; nobody else needs Windows.
#
# Requirements:
#   - Parallels Desktop with a running Windows virtual machine that has the .NET SDK installed.
#   - The VM shares your Mac user folders (Parallels: Options > Sharing > Share Mac user folders),
#     so Windows can see your Documents folder as \\Mac\Home\Documents.
#
# Usage:
#   compile_shaders.sh [path/to/MonsterMaze.Core/Content]
#
# Environment variables:
#   MM_WINDOWS_VM   the Parallels VM name        (default: "Windows 11")
#   MM_STAGING      staging folder in Documents  (default: MonsterMazeShaderBuild)

set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
content="$(cd "${1:-$here/../Template/MonsterMaze.Core/Content}" && pwd)"
vm="${MM_WINDOWS_VM:-Windows 11}"
staging_name="${MM_STAGING:-MonsterMazeShaderBuild}"
staging="$HOME/Documents/$staging_name"
mgcb_version="3.8.5.1"
dotnet='"C:\Program Files\dotnet\dotnet.exe"'

echo "Compiling shaders from $content/Effects in '$vm'..."

# 1. Copy the shader sources somewhere Windows can see them.
rm -rf "$staging"
mkdir -p "$staging/.config"
cp "$content"/Effects/*.fx "$staging/"
cat > "$staging/.config/dotnet-tools.json" <<JSON
{ "version": 1, "isRoot": true, "tools": { "dotnet-mgcb": { "version": "$mgcb_version", "commands": [ "mgcb" ] } } }
JSON

# 2. Write a content project per platform that builds every .fx file.
for platform in Android iOS; do
    {
        echo "/outputDir:out/$platform"
        echo "/intermediateDir:obj/$platform"
        echo "/platform:$platform"
        echo "/profile:Reach"
        echo "/compress:False"
        echo
        for fx in "$staging"/*.fx; do
            name="$(basename "$fx")"
            echo "#begin $name"
            echo "/importer:EffectImporter"
            echo "/processor:EffectProcessor"
            echo "/processorParam:DebugMode=Auto"
            echo "/build:$name"
            echo
        done
    } > "$staging/$platform.mgcb"
done

# 3. Build them in Windows.
windows_dir="\\\\Mac\\Home\\Documents\\$staging_name"
prlctl exec "$vm" --current-user cmd /c \
    "pushd $windows_dir && $dotnet tool restore && $dotnet mgcb /@:Android.mgcb && $dotnet mgcb /@:iOS.mgcb"

# 4. Bring the compiled effects home.
for platform in Android iOS; do
    mkdir -p "$content/Effects/Compiled/$platform"
    cp "$staging/out/$platform/"*.xnb "$content/Effects/Compiled/$platform/"
done

rm -rf "$staging"
echo "Done. Compiled effects are in $content/Effects/Compiled/"

#!/bin/sh

VERSION=0.3.3

rm -rf artifacts
mkdir artifacts

echo ""
echo "///////////////////////////////////////////////"
echo "// Build IL2C.Core"
echo ""

dotnet pack --configuration Release --include-symbols -p:VersionPrefix=${VERSION} IL2C.Core/IL2C.Core.csproj
cp IL2C.Core/bin/Release/IL2C.Core.${VERSION}.symbols.nupkg artifacts/IL2C.Core.${VERSION}.nupkg

echo ""
echo "///////////////////////////////////////////////"
echo "// Build IL2C.Build"
echo ""

dotnet pack --configuration Release --include-symbols -p:VersionPrefix=${VERSION} IL2C.Tasks/IL2C.Tasks.csproj
cp IL2C.Tasks/bin/Release/IL2C.Build.${VERSION}.symbols.nupkg artifacts/IL2C.Build.${VERSION}.nupkg

echo ""
echo "Done."
echo ""

#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/src/GammonX/GammonX.Mars.Training/GammonX.Mars.Training.csproj"
BUILD_ARTIFACTS="$REPO_ROOT/src/GammonX/artifacts/bin/mars-training-publish-build"
OUTPUT_DIR="$REPO_ROOT/src/GammonX/artifacts/bin/GammonX.Mars.Training/linux-x64"

dotnet build "$PROJECT" \
	-c Release \
	-r linux-x64 \
	--self-contained false \
	--artifacts-path "$BUILD_ARTIFACTS"

dotnet publish "$PROJECT" \
	-c Release \
	-r linux-x64 \
	--self-contained true \
	--no-build \
	--no-restore \
	-p:BuildProjectReferences=false \
	-p:PublishSingleFile=true \
	-p:IncludeNativeLibrariesForSelfExtract=true \
	--artifacts-path "$BUILD_ARTIFACTS" \
	-o "$OUTPUT_DIR"
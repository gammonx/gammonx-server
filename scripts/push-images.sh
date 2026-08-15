#!/usr/bin/env bash
# Build and push gammonx container images to ECR.
#
# Usage:
#   scripts/push-images.sh [--env dev|acc|prod] [--service game|lambda|mars|all] [--no-push]
#
# Examples:
#   scripts/push-images.sh                          # build + push all 3 to dev
#   scripts/push-images.sh --env acc                # all 3 to acc
#   scripts/push-images.sh --service game           # game only
#   scripts/push-images.sh --service mars           # mars bot only
#   scripts/push-images.sh --no-push                # build only (smoke test)
#
# Requires: docker, aws CLI with credentials, git.
# Images are tagged :latest and :<short-sha> and pushed to:
#   <account>.dkr.ecr.<region>.amazonaws.com/gammonx-<env>-<svc>-ecrrepo

set -euo pipefail

ENV="dev"
SERVICE="all"
PUSH="true"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)     ENV="$2";      shift 2 ;;
    --service) SERVICE="$2";  shift 2 ;;
    --no-push) PUSH="false";  shift ;;
    -h|--help)
      sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'
      exit 0 ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

case "$ENV" in
  dev|acc|prod) ;;
  *) echo "Invalid --env '$ENV' (expected: dev, acc, prod)" >&2; exit 1 ;;
esac

case "$SERVICE" in
  all|game|lambda|mars) ;;
  *) echo "Invalid --service '$SERVICE' (expected: all, game, lambda, mars)" >&2; exit 1 ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

REGION="${AWS_REGION:-eu-central-1}"
APP="gammonx"
SHA="$(git rev-parse --short HEAD)"

ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
REGISTRY="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

want() {
  [[ "$SERVICE" == "all" || "$SERVICE" == "$1" ]]
}

if [[ "$PUSH" == "true" ]]; then
  echo ">> Logging in to ECR: $REGISTRY"
  aws ecr get-login-password --region "$REGION" \
    | docker login --username AWS --password-stdin "$REGISTRY"
fi

build_and_push() {
  local key="$1" suffix="$2" context="$3" dockerfile="$4" target="$5"
  # Defaults to arm64 (Graviton) for cost. Must match the service's
  # runtime_platform.cpu_architecture in gammonx-iaas.
  local platform="${6:-linux/arm64}"

  local repo="${APP}-${suffix}-ecrrepo"
  local image="${REGISTRY}/${repo}"

  echo ""
  echo ">> [$key] Building $repo (target=${target:-<default>}, platform=${platform})"

  # --provenance=false / --sbom=false: AWS Lambda rejects images pushed as an
  # OCI image index, which is what buildx produces when attestations are on.
  # Without these flags, lambda-service pushes fail with "media type ... is not supported".
  local args=(
    --platform "$platform"
    --provenance=false
    --sbom=false
    --file "$dockerfile"
    --tag "${image}:latest"
    --tag "${image}:${SHA}"
  )
  [[ -n "$target" ]] && args+=(--target "$target")

  if [[ "$PUSH" == "true" ]]; then
    args+=(--push)
  else
    args+=(--load)
  fi

  docker buildx build "${args[@]}" "$context"

  if [[ "$PUSH" == "true" ]]; then
    echo ">> [$key] Pushed ${image}:{latest,${SHA}}"
  fi
}

if want game; then
  build_and_push game gameservice "$REPO_ROOT" "src/GammonX/Dockerfile" server-final
fi

if want lambda; then
  build_and_push lambda lambdaservice "$REPO_ROOT" "src/GammonX/Dockerfile" lambda-runtime
fi

if want mars; then
  # x86_64, not arm64: TorchSharp-cpu pulls libtorch-cpu, which ships no
  # libtorch-cpu-linux-arm64 package. Built for arm64 the assembly refuses to
  # load ("architecture is not compatible") and the server never binds a port.
  build_and_push mars marsservice "$REPO_ROOT" "src/GammonX/Dockerfile" mars-server-final linux/amd64
fi

echo ""
echo "Done. sha=${SHA} env=${ENV} service=${SERVICE} push=${PUSH}"

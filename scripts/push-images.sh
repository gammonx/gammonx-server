#!/usr/bin/env bash
# Build and push gammonx container images to ECR.
#
# Usage:
#   scripts/push-images.sh [--env dev|acc|prod] [--service game|lambda|wildbg|mars|all]
#                          [--wildbg-path PATH] [--no-push]
#
# Examples:
#   scripts/push-images.sh                          # build + push all 4 to dev
#   scripts/push-images.sh --env acc                # all 4 to acc
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
WILDBG_PATH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)         ENV="$2";          shift 2 ;;
    --service)     SERVICE="$2";      shift 2 ;;
    --wildbg-path) WILDBG_PATH="$2";  shift 2 ;;
    --no-push)     PUSH="false";      shift ;;
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
  all|game|lambda|wildbg|mars) ;;
  *) echo "Invalid --service '$SERVICE' (expected: all, game, lambda, wildbg, mars)" >&2; exit 1 ;;
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

  local repo="${APP}-${suffix}-ecrrepo"
  local image="${REGISTRY}/${repo}"

  echo ""
  echo ">> [$key] Building $repo (target=${target:-<default>})"

  # --provenance=false / --sbom=false: AWS Lambda rejects images pushed as an
  # OCI image index, which is what buildx produces when attestations are on.
  # Without these flags, lambda-service pushes fail with "media type ... is not supported".
  local args=(
    --platform linux/arm64
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

if want wildbg; then
  WB="${WILDBG_PATH:-${REPO_ROOT}/../gammonx-wildbg}"
  if [[ ! -f "${WB}/dockerfile" ]]; then
    echo "wildbg dockerfile not found at ${WB}/dockerfile" >&2
    echo "Clone gammonx/gammonx-wildbg alongside this repo, or pass --wildbg-path." >&2
    exit 1
  fi
  build_and_push wildbg wildbgservice "$WB" "${WB}/dockerfile" ""
fi

if want mars; then
  build_and_push mars marsservice "$REPO_ROOT" "src/GammonX/Dockerfile" mars-server-final
fi

echo ""
echo "Done. sha=${SHA} env=${ENV} service=${SERVICE} push=${PUSH}"

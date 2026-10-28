#!/usr/bin/env bash
# Bash version of run-sonarqube.ps1. Usage: SONAR_ADMIN_PASSWORD=<pw> bash run-sonarqube.sh
set -euo pipefail
if [ "${SP_COPY:-}" != "1" ]; then tmp=$(mktemp); cp "$0" "$tmp"; SP_COPY=1 exec bash "$tmp" "$@"; fi
HOST=http://localhost:9000; PW="${SONAR_ADMIN_PASSWORD:-admin}"
docker ps --format '{{.Names}}' | grep -q '^sonarqube$' || docker run -d --name sonarqube -p 9000:9000 sonarqube:community
until curl -s "$HOST/api/system/status" | grep -q '"status":"UP"'; do sleep 5; done
curl -s -u "admin:$PW" -X POST "$HOST/api/projects/create?project=shramikpay&name=ShramikPay" >/dev/null || true
TOKEN=$(curl -s -u "admin:$PW" -X POST "$HOST/api/user_tokens/generate?name=shramikpay-$RANDOM&type=PROJECT_ANALYSIS_TOKEN&projectKey=shramikpay" | sed -E 's/.*"token":"([^"]+)".*/\1/')
dotnet tool update --global dotnet-sonarscanner >/dev/null; export PATH="$PATH:$HOME/.dotnet/tools"
scan () {
  git checkout -q "$1"; rm -rf tests/ShramikPay.Tests/TestResults
  dotnet sonarscanner begin /k:"shramikpay" /n:"ShramikPay" /v:"${1#v}" /d:sonar.host.url="$HOST" /d:sonar.token="$TOKEN" /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
  dotnet build --no-incremental
  dotnet test --no-build --collect:"XPlat Code Coverage;Format=opencover"
  dotnet sonarscanner end /d:sonar.token="$TOKEN"
}
scan v1.0.0
read -r -p "Baseline analysed. Take the BEFORE screenshots, then press Enter to analyse v1.1.1..."
scan v1.1.1
git checkout -q main
echo "Done: $HOST/dashboard?id=shramikpay"

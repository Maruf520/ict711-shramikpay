# Reproduces the two SonarQube analyses in the report (v1.0.0 baseline, v1.1.0 remediated).
# Requirements: Docker Desktop, .NET 8 SDK, Java 17+, git.   Usage:  ./run-sonarqube.ps1 -AdminPassword <your SonarQube admin password>
param([string]$AdminPassword = "admin", [string]$HostUrl = "http://localhost:9000")
$ErrorActionPreference = "Stop"
if (-not (docker ps --format "{{.Names}}" | Select-String -Quiet "^sonarqube$")) { docker run -d --name sonarqube -p 9000:9000 sonarqube:community | Out-Null }
Write-Host "Waiting for SonarQube..."
do { Start-Sleep 5; $s = try { (Invoke-RestMethod "$HostUrl/api/system/status").status } catch { "" } } until ($s -eq "UP")
$cred = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:$AdminPassword"))
$h = @{ Authorization = "Basic $cred" }
try { Invoke-RestMethod -Method Post -Headers $h "$HostUrl/api/projects/create?project=shramikpay&name=ShramikPay" | Out-Null } catch {}
$token = (Invoke-RestMethod -Method Post -Headers $h "$HostUrl/api/user_tokens/generate?name=shramikpay-$(Get-Random)&type=PROJECT_ANALYSIS_TOKEN&projectKey=shramikpay").token
dotnet tool update --global dotnet-sonarscanner | Out-Null
function Scan($tag) {
  git checkout -q $tag
  Remove-Item -Recurse -Force TestResults, tests/ShramikPay.Tests/TestResults -ErrorAction SilentlyContinue
  dotnet sonarscanner begin /k:"shramikpay" /n:"ShramikPay" /v:"$($tag.TrimStart('v'))" /d:sonar.host.url="$HostUrl" /d:sonar.token="$token" /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
  dotnet build --no-incremental
  dotnet test --no-build --collect:"XPlat Code Coverage;Format=opencover"
  dotnet sonarscanner end /d:sonar.token="$token"
}
Scan "v1.0.0"
Read-Host "Baseline analysed. Take the BEFORE screenshots, then press Enter to analyse v1.1.1"
Scan "v1.1.0"
git checkout -q main
Write-Host "Done: $HostUrl/dashboard?id=shramikpay"

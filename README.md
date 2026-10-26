# ShramikPay - Garment Worker Attendance & Payroll API

ICT711 Assessment 3 case study (ASP.NET Core 8, C#). Fictional factory: Shapla Apparels Ltd., Gazipur.

## Build and test
    dotnet restore
    dotnet build
    dotnet test --collect:"XPlat Code Coverage"      # xUnit + coverlet

## SonarQube analysis (Windows PowerShell or bash)
    ./run-sonarqube.ps1 -AdminPassword <pw>     (Windows)
    SONAR_ADMIN_PASSWORD=<pw> bash run-sonarqube.sh   (macOS/Linux)



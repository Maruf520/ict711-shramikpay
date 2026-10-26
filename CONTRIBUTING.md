# ShramikPay configuration management rules
* `main` is protected: changes only via reviewed pull requests with a passing quality gate.
* Branches: `develop` (integration), `feature/SP-<id>-<slug>`, `fix/SP-<id>-<slug>`, `release/<semver>`, `hotfix/<semver>`.
* Commit messages start with the issue key: `SP-102 Fix ...`.
* Releases follow Semantic Versioning with annotated tags (`v1.1.0`); `<Version>` in the csproj must match.
* Secrets only in environment variables / CI secrets: `ShramikPay__DataKey`, `ShramikPay__AdminPassword`, `SONAR_TOKEN`.

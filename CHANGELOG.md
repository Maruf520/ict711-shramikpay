# Changelog
## [1.1.1] - patch (SP-107, found by SonarQube re-analysis of 1.1.0)
### Fixed
- Error-handling middleware now passes `context.RequestAborted` to `WriteAsJsonAsync` so a cancelled request stops writing (SonarQube bug, Major).
- `Program` awaits `RunAsync()` instead of the blocking `Run()` (code smell).
- Test host arguments moved to a `static readonly` array (code smell, Info).
## [1.1.0] - quality remediation release
### Fixed (SP-102)
- Overtime rate used integer division (Tk 64 instead of 64.42/h); all money is now `decimal`, rounded to whole Taka.
- Overtime above 2 h/day was silently not paid; all hours are now paid and BLA daily (10 h) / weekly (60 h) breaches raise compliance alerts.
- Worker grade never stored; clock-out before clock-in accepted; unknown worker crashed with NullReferenceException; usernames not trimmed.
- Payments are idempotent per worker and month; approved months are locked; every change is written to a hash-chained audit log.
### Security (SP-101)
- AES-256-GCM replaces DES-ECB; PBKDF2-SHA256 (210,000 iterations, random salt) replaces MD5; 256-bit session tokens with expiry.
- No hard-coded admin password or keys; HTTPS-only wallet gateway with certificate validation; CORS allow-list; no developer exception page in production.
- Open self-registration removed (HR only); workers can read only their own payslip; wrong role returns 403 instead of 500.
- Salary-sheet export uses server-generated names and neutralises CSV formula injection; no shell commands.
### Changed (SP-103)
- `RequireRoles` endpoint filter and shared `PayCalculator` remove duplicated code; cognitive complexity of payroll 34 -> below 15.
### Tests (SP-104)
- 48 xUnit tests; line coverage ~30 % -> ~97 %.
## [1.0.0] - baseline

# HackerRank Orchestrate - October 2026

## Ghost Data Still Exists (C# / ASP.NET Core Solution)

An enterprise-grade, deterministic AI-powered deletion-audit agent built with **C# and ASP.NET Core Web API (.NET 10.0)** to verify whether personal data was truly deleted across complex distributed systems, replicas, caches, logs, support tickets, and backups.

A deletion workflow may report **Complete** while data remains in downstream systems, search indexes, or backup archives. Unsafe data flows and automatic restoration processes can also silently recreate deleted records.

This solution:
1. Validates whether requests authorize deletion (`COMPLETE` / `INCOMPLETE`) or represent non-deletion actions (`NOT_APPLICABLE`).
2. Resolves current and historical identity aliases across all systems.
3. Reconstructs the true state at `audit_at` by cross-referencing activity logs and multi-format evidence.
4. Identifies remaining records, exposed PII, active synchronization flows, and restore risks.
5. Emits an RFC 4180-compliant `output.csv` with an ordered JSON plan enforcing strict phase monotonicity (`FINDING` → `REMEDIATION` → `VERIFICATION`) and remediation precedence.

Read [`problem_statement.md`](./problem_statement.md) and [`AGENTS.md`](./AGENTS.md) for the complete challenge contract.

---

## Challenge and Submission Link

The October challenge and official submission URL is:

**https://www.hackerrank.com/contests/hackerrank-orchestrate-october26/challenges/ghost-data-still-exists**

---

## Solution Architecture

The solution is implemented under `code/GhostDataAudit.Api` (API & Engine) and `code/GhostDataAudit.Tests` (Automated Test Suite) using .NET 10.0 (`net10.0`):

- **`DataLoaderService`**: Zero-dependency RFC 4180 CSV parser supporting quoted values, escaped quotes, CRLF, and UTF-8 encoding. Includes strict directory traversal defense (`ResolveSafeEvidencePath`).
- **`IdentityResolverService`**: Inverted index recovering current and historical user identities (user ID, emails, phones, email hashes) from `user_identities.csv` and `user_audit_log.csv`.
- **`RequestClassifierService`**: High-accuracy multilingual intent classifier distinguishing deletion requests from opt-out, access, correction, and invoice queries.
- **`RuleEngineService`**: Rule engine encoding the 15 challenge data categories and required operations (`DELETE`, `REDACT`, `ANONYMIZE`, `RETAIN`, `DISABLE`, `BLOCK`).
- **`StepBuilderService`**: Directed graph step planner ensuring phase monotonicity (`FINDING` → `REMEDIATION` → `VERIFICATION`), remediation safety (flows and restore risks addressed before dependent records), and evidence referencing rules (`evidence_id` populated for findings/complete checks, empty string for future remediation).
- **`OutputWriterService`**: RFC-compliant CSV generator formatting valid JSON without BOM (`new UTF8Encoding(false)`).
- **`AuditOrchestratorService`**: Central engine correlating temporal audit windows, system activity events, and evidence files.

---

## Quick Start & Execution

### Prerequisites

- **.NET 10.0 SDK** (or .NET 9.0+)
- **Python 3.8+** (for standard wrapper entry point)

### Option 1: Run via Python Wrapper Entry Point (Recommended)

To run the standard repository entry point:

```bash
python3 code/main.py
```

This compiles (if needed) and invokes the .NET audit engine, generating `output.csv` with all 518 evaluation cases at the repository root.

### Option 2: Run via .NET CLI Directly

To run the .NET engine directly from the command line:

```bash
dotnet run --project code/GhostDataAudit.Api -- --generate-output
```

### Option 3: Run as an ASP.NET Core Web API Server

To start the interactive HTTP service:

```bash
dotnet run --project code/GhostDataAudit.Api
```

- **Interactive Swagger UI**: `GET http://localhost:5000/swagger`
- **OpenAPI v1 JSON**: `GET http://localhost:5000/swagger/v1/swagger.json`
- **Health Endpoint**: `GET http://localhost:5000/`
- **Dataset Diagnostics**: `GET http://localhost:5000/api/audit/diagnostics`
- **Single Case Audit**: `POST http://localhost:5000/api/audit/audit-case`

---

## Running Automated Tests

To run the full unit and integration test suite (21 passing tests):

```bash
dotnet test code/GhostDataAudit.slnx
```

Test coverage includes:
- RFC 4180 CSV parsing edge cases, quoted fields, and encoding.
- Directory traversal security defenses.
- Integrity verification of all 7 dataset files (518 requests, 850 users, 306 audits, 4,011 activities, 15 rules, 94 evidence files).
- Intent classification across English, German, French, and Spanish.
- Strict phase sequencing and remediation precedence ordering.
- Evidence reference verification.

---

## Dataset Structure

All participant-facing input files live in `dataset/`:

| File | Contents |
| --- | --- |
| `deletion_requests.csv` | 518 requests to evaluate (`case_33` through `case_550`) |
| `sample_deletion_requests.csv` | 32 reference requests |
| `sample_output.csv` | Correct reference answers |
| `user_identities.csv` | Current user snapshots |
| `user_audit_log.csv` | Historical identity mutations |
| `system_activity.csv` | Activity event stream across platform services |
| `deletion_rules.csv` | 15 data category deletion rules |
| `evidence_files.csv` | Manifest of evidence files under `dataset/evidence/` |
| `output.csv` | Blank output template |

---

## Output Contract

The generated `output.csv` contains exact columns:

```text
case_id,deletion_status,steps_json
```

- Exactly 518 data rows sorted by `case_id`.
- Permitted statuses: `COMPLETE`, `INCOMPLETE`, `NOT_APPLICABLE`.
- Valid JSON array `steps_json` numbered from 1.
- Findings cite participant evidence IDs; future remediation and verification use `""`.

---

## Packaging `code.zip`

To create the final clean submission archive excluding build caches, binaries, and git tracking:

```bash
zip -r code.zip README.md code evaluation -x "*/bin/*" "*/obj/*" "*/__pycache__/*" "*/.git/*" "*/.DS_Store" "*/.local/*" "*/Library/*"
```

Verify the archive contents before submitting:

```bash
unzip -l code.zip | head -n 30
```

---

## Submission Deliverables Checklist

- [x] **`output.csv`**: Exactly 518 rows sorted by `case_id` with valid schema and JSON.
- [x] **`code.zip`**: Runnable .NET 10 solution, `README.md`, `code/evaluation/evaluation_report.md`, and `code/evaluation/usage_report.md`.
- [x] **`log.txt`**: Complete per-turn development transcript matching `AGENTS.md` format.
- [x] **Git Repository**: Remote synchronised at `https://github.com/kiranpinisetty/ghost-data-audit.git`.

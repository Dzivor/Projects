# Database Schema (Current EF Core Model)

This document describes the current relational schema implied by the EF Core models and migrations in `Backend/BankStatementAPI`.

> Tech: ASP.NET Core + EF Core + SQL Server

---

## Tables

### `Users`

- **Columns**
  - `Id` (int, identity, PK)
  - `Username` (nvarchar(450), required, unique)
  - `FullName` (nvarchar(max), required)
  - `Email` (nvarchar(max), required)
  - `IsActive` (bit, not null)
  - `IsAdmin` (bit, not null)
  - `CreatedAt` (datetime2, not null)
  - `AddedBy` (nvarchar(max), required)

---

### `AuditLogs`

Represents audit information for statement generation / charge actions.

- **Columns**
  - `Id` (int, identity, PK)
  - `UserId` (int, required, FK → `Users(Id)`)
  - `AccountNumber` (nvarchar(max), required)
  - `AccountHolderName` (nvarchar(max), required)
  - `StartDate` (date, required)
  - `EndDate` (date, required)
  - `ChannelUsed` (nvarchar(max), required)
  - `NumberOfPages` (int, not null)
  - `AmountCharged` (decimal(18,2), required)
  - `AccountCharged` (nvarchar(max), required)
  - `WasWaived` (bit, not null)
  - `BankTransactionReference` (nvarchar(max), nullable)
  - `GeneratedAt` (datetime2, not null)

- **Relationships**
  - Many `AuditLogs` belong to one `User`.
  - One `AuditLog` has many `ChargeTransactions`.

---

### `ChargeTransactions`

Represents individual charge debit/fee attempts for a statement.

- **Columns**
  - `Id` (int, identity, PK)
  - `DebitAccountNumber` (nvarchar(max), required)
  - `CreditAccountNumber` (nvarchar(max), required)
  - `Amount` (decimal(18,2), required)
  - `Channel` (nvarchar(max), required)
  - `StatementAccountNumber` (nvarchar(max), required)
  - `BankTransactionReference` (nvarchar(max), nullable)
  - `Status` (int, required)
    - `Pending`, `Success`, `Failed`
  - `ErrorMessage` (nvarchar(max), nullable)
  - `StaffUsername` (nvarchar(max), required)
  - `Narration` (nvarchar(max), required)
  - `CreatedAt` (datetime2, not null)
  - `CompletedAt` (datetime2, nullable)
  - `AuditLogId` (int, nullable FK → `AuditLogs(Id)`)

- **Relationships**
  - Each `ChargeTransaction` _may_ be linked to an `AuditLog` via `AuditLogId`.
  - `OnDelete(DeleteBehavior.Restrict)` means you cannot delete an `AuditLog` while it still has linked `ChargeTransactions`.

---

### `AppSettings`

Application configuration values.

- **Columns**
  - `Id` (int, identity, PK)
  - `Key` (nvarchar(450), required, unique)
  - `Value` (nvarchar(max), required)
  - `Description` (nvarchar(max), required)
  - `DataType` (nvarchar(max), required)
  - `LastUpdatedAt` (datetime2, not null)
  - `LastUpdatedBy` (nvarchar(max), required)

---

### `SettingsAuditLogs`

Audit trail of changes to `AppSettings`.

- **Columns**
  - `Id` (int, identity, PK)
  - `SettingKey` (nvarchar(max), required)
  - `OldValue` (nvarchar(max), required)
  - `NewValue` (nvarchar(max), required)
  - `ChangedBy` (nvarchar(max), required)
  - `ChangedAt` (datetime2, not null)
  - `Reason` (nvarchar(max), nullable)
  - `AppSettingId` (int, required FK → `AppSettings(Id)`)

---

## How `ChargeTransactions` and `AuditLogs` are linked

### 1) Primary linkage: `ChargeTransactions.AuditLogId`

- `ChargeTransactions` has a nullable foreign key column:
  - `AuditLogId` (int?, nullable)
- Navigation properties:
  - `ChargeTransaction.AuditLog` (optional)
  - `AuditLog.ChargeTransactions` (collection)

**Meaning:**

- A charge transaction can exist _before_ the PDF/statement audit record is finalized.
- After statement generation, the system links the created charges to the corresponding `AuditLog` by setting `ChargeTransaction.AuditLogId`.

### 2) Additional bank reference fields (not the FK)

Both tables contain a bank transaction reference field, which can be used for reconciliation:

- `AuditLogs.BankTransactionReference` (string?, nullable)
- `ChargeTransactions.BankTransactionReference` (string?, nullable)

**Important:**

- These are **not** enforced as foreign keys.
- They are separate nullable columns intended for lookup/debugging/verification.

### 3) Cardinality

- `AuditLogs` → `ChargeTransactions`: **one-to-many**
- `ChargeTransactions` → `AuditLogs`: **many-to-one** (optional due to nullable FK)

---

## Front-end linking “as one” (implementation sketch: click charge → modal)

### Current limitation in the codebase

- `GET /api/admin/audit-logs` returns `AuditLogDTO[]` **only** (no charges nested).
- `ChargeTransactionDTO` contains `auditLogId`, so the join key exists.
- Therefore, the UI must link audit logs and charges either by:
  - client-side grouping (join on `auditLogId`), or
  - a backend endpoint that returns `{ auditLog, charges[] }`.

### Recommended UI model (client-side join)

Assume the UI loads:

- `auditLogs: AuditLogDTO[]`
- `charges: ChargeTransactionDTO[]`

Then build:

- `auditLogById: { [id: number]: AuditLogDTO }`
- `chargesByAuditLogId: { [auditLogId: number]: ChargeTransactionDTO[] }`

Filter charges before attaching:

- show only charges where:
  - `charge.auditLogId != null`
  - `charge.status === 'success'`
  - `charge.bankTransactionReference != null`

### Rendering

- Display one “Statement row” per `AuditLogDTO`.
- Under each statement row, render its attached (filtered) `ChargeTransactionDTO[]`.
- Make each charge row **clickable**.

### Modal behavior

State:

- `selectedCharge: ChargeTransactionDTO | null`
- derived: `selectedAuditLog = auditLogById[selectedCharge.auditLogId]`

When user clicks a charge:

1. set `selectedCharge`
2. open modal
3. modal shows:
   - audit log details (account number/name, staff, channel, period, generatedAt, waived/free)
   - charge details (amount, status, bankTransactionReference, narration, createdAt/completedAt)

### Backend option (more efficient later)

Create an endpoint returning nested DTOs:

- `AuditLogWithChargesDTO[]`
  - `auditLog: AuditLogDTO`
  - `charges: ChargeTransactionDTO[]` (already filtered)

This removes client-side joining complexity and makes modal data access trivial.

---

## Relevant EF Core sources (for traceability)

- Models:
  - `Backend/BankStatementAPI/Models/AuditLog.cs`
  - `Backend/BankStatementAPI/Models/ChargeTransaction.cs`
  - `Backend/BankStatementAPI/data/AppDbContext.cs`
- Migrations:
  - `Backend/BankStatementAPI/Migrations/20260609082935_AddTransactionReferenceToAuditLog.cs`

---

## Quick example (conceptual)

1. Create `AuditLog` for a statement action.
2. Create one or more `ChargeTransaction` rows (each may start with `AuditLogId = null`).
3. After the statement/PDF generation is complete, update each charge row to set:
   - `ChargeTransactions.AuditLogId = <AuditLog.Id>`
4. Use `BankTransactionReference` columns for reconciliation with bank responses.

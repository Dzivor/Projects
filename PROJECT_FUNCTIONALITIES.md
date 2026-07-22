# Project Functionality Overview (Backend + Frontend)

## 1) High-level architecture

- **Backend**: `Backend/BankStatementAPI` (ASP.NET Core Web API)
  - Exposes REST endpoints for authentication, account/statement lookup, statement preview & generation, and admin capabilities (users, settings, audit logs, exports).
  - Integrates with an external **Bank API** via `BankApiService`.
  - Generates PDFs via `PdfService`.
  - Performs charging/debit flows via `ChargingService`.
  - Persists statement/audit information via `AuditService`.

- **Frontend**: `umb-portal` (React + TypeScript)
  - Handles login, session storage, protected routes, and calls backend APIs through service modules.
  - Provides user-facing statement preview & generation UI.
  - Provides admin pages for dashboard stats, user management, audit logs, and settings.

- **Data layer**: EF Core models/migrations under `Backend/BankStatementAPI/Models`, `data`, and `Migrations`.

---

## 2) Backend functionalities

### 2.1 Authentication

- **Endpoint**: `POST /api/auth/login`
  - Implemented in: `Backend/BankStatementAPI/Controllers/AuthController.cs`
  - Input: `LoginRequestDTO` (username + password)
  - Behavior:
    - Validates request fields.
    - Delegates login to `AuthService.Login(request)`.
    - Always returns HTTP 200 with a `LoginResponseDTO` containing `Success` and `Message` (frontend uses `Success`).
    - On exception: returns `Success=false` with a generic error message and logs via `ILogger`.

### 2.2 Account lookup (bank verification)

- **Endpoint**: `GET /api/account/lookup/{accountNumber}?channel={VISA|ESB}`
  - Implemented in: `Backend/BankStatementAPI/Controllers/AccountController.cs`
  - Behavior:
    - Delegates to `BankApiService.GetAccountDetails(accountNumber, channel)`.
    - Returns:
      - `Ok(account)` when bank lookup succeeds.
      - `NotFound(...)` when account not found in selected/supported channel(s), including `selectedChannel` and `suggestedChannel`.
      - `503` when the bank cannot be reached or validation fails upstream.

### 2.3 Statement preview

- **Endpoint**: `POST /api/statement/preview`
  - Implemented in: `Backend/BankStatementAPI/Controllers/StatementController.cs`
  - Input: `StatementRequestDTO`
    - Includes: `accountNumber`, `startDate`, `endDate`, `channel`, charging options like `waiveCharge`, optional alternative account settings.
  - Behavior (core flow):
    1. Validates request (required fields + channel in VISA/ESB + alt-account rules).
    2. Parses date range (supports multiple formats).
    3. Fetches statement from bank API via `BankApiService.GetStatement(...)`.
    4. Creates a **rendered preview**:
       - Uses `ChargingService.PreviewCharge(...)` to compute charge preview totals.
       - Uses `PdfService.GenerateStatement(statement, chargePreview)`.
       - Iteratively re-renders up to 3 times until the rendered page count stabilizes.
    5. Counts pages with `PdfService.CountPages(previewPdf)`.
    6. Stores preview bytes + metadata in **in-memory cache** keyed by a generated `previewToken`.
    7. If configured to charge an alternative account, performs optional extra account lookup via `BankApiService.GetAccountDetails(...)` to show the alt account balance/name.
    8. Returns `PreviewResponseDTO`:
       - `previewToken`
       - `NumberOfPages`
       - `TotalCharge`, `AccountToCharge` + message
       - statement summary fields and balances

### 2.4 Statement generation (charge + PDF)

- **Endpoint**: `POST /api/statement/generate`
  - Implemented in: `Backend/BankStatementAPI/Controllers/StatementController.cs`
  - Input: `StatementRequestDTO` (same as preview, plus `previewToken`)
  - Behavior (core flow):
    1. Extracts `userId` from JWT claims (rejects if invalid) for audit logging.
    2. Validates request + parses date range.
    3. Fetches statement from bank API via `BankApiService.GetStatement(...)`.
    4. Uses `previewToken` to:
       - Load cached preview PDF and stable page count.
       - Verify a request “signature” (ensures details didn’t change after preview).
       - If preview token is missing/expired or signature mismatches: returns `BadRequest`.
    5. Charges the account via `ChargingService.ProcessCharge(request, numberOfPages)`.
       - If charge fails: returns `BadRequest` with `message` and `details`.
    6. Generates/chooses PDF bytes:
       - Uses cached preview PDF if available; otherwise generates fresh.
    7. Writes audit entry:
       - `AuditService.LogStatement(...)` inside try/catch.
       - Statement generation succeeds even if audit insert fails.
    8. Removes preview cache after successful generation.
    9. Returns PDF file download response (`application/pdf`).

### 2.5 Bank API integration

- **Service**: `Backend/BankStatementAPI/Services/BankApiService.cs`

#### 2.5.1 Account details

- Method: `GetAccountDetails(accountNumber, channel)`
- Bank calls:
  - Uses `GET {BankApi:BaseUrl}/party/umbGetAcctInfo/?accountNo={accountNumber}`
- Headers added automatically:
  - `credentials` (SignOn)
  - `companyId` (resolved by channel from configuration)
  - `Accept: application/json`
- Returns:
  - `AccountLookupResultDTO.Success=true` with account number/name/balance.
  - `AccountNotFound=true` when bank indicates the account is not found in that channel.
  - Generic failure message when bank errors occur.

#### 2.5.2 Statement fetch

- Method: `GetStatement(accountNumber, startDate, endDate, channel)`
- Bank calls:
  - `GET {BankApi:BaseUrl}/party/account/getAccountStatements.2.1.0?accountNumber=...&startDate=yyyyMMdd&endDate=yyyyMMdd`
  - Adds `disablePagination=true` header.
- Behavior:
  - Parses response and maps bank payload into internal `Statement` model.
  - Computes:
    - balances: opening/book/clear
    - totals: total debit/credit values and counts
    - transaction list with narrative composed from transaction type + normalized descriptions

#### 2.5.3 Statement charge (debit)

- Method: `DebitAccount(accountNumber, amount, channel)`
- Bank calls:
  - `POST {BankApi:BaseUrl}/party//account/statementCharge`
- Request payload:
  - transactionType: `ACST`
  - debitAccountId: accountNumber
  - debitCurrency: `GHS`
  - debitAmount: amount
  - creditAccountId: from `ChargeCollectionAccount` setting (via `SettingsService`)
- Response handling:
  - On non-2xx: extracts bank error message from JSON if possible.
  - On success response:
    - Checks `result.Header.Status == "success"`
    - Returns `DebitResult.Success=true` and `TransactionReference=result.Header.Id`

### 2.6 Admin features

- **Controller**: `Backend/BankStatementAPI/Controllers/AdminController.cs`

All admin endpoints:

- Validate admin privileges via `isAdmin` claim.
- Otherwise return `403` with `Access denied. Admin privileges required.`

Admin endpoints include:

1. `GET /api/admin/stats`
   - Dashboard stats for admin UI.
   - Delegates to `AdminService.GetDashboardStats()`.

2. `GET /api/admin/users?search=&status=`
   - Lists users with filters.
   - Delegates to `AdminService.GetAllUsers(search, status)`.

3. `GET /api/admin/users/ad-lookup/{username}`
   - Looks up AD user.
   - Delegates to `AdminService.LookupUserInAD(username)`.

4. `POST /api/admin/users`
   - Adds a user.
   - Delegates to `AdminService.AddUser(request, GetAdminUsername())`.

5. `PUT /api/admin/users/{id}/toggle`
   - Toggles enabled/disabled status.
   - Delegates to `AdminService.ToggleUserStatus(id, GetAdminUsername())`.

6. `GET /api/admin/audit-logs?...filters...`
   - Returns audit logs filtered by:
     - date range
     - staffUsername
     - channel
     - accountNumber
   - Delegates to `AdminService.GetAuditLogs(filter)`.

7. `GET /api/admin/settings`
   - Returns all app settings.
   - Delegates to `SettingsService.GetAllSettings()`.

8. `GET /api/admin/settings/history`
   - Returns settings history.
   - Delegates to `SettingsService.GetSettingsHistory()`.

9. `PUT /api/admin/settings/{key}`
   - Updates a setting value.
   - Delegates to `SettingsService.UpdateSetting(key, request, GetAdminUsername())`.

10. Export audit logs:

- `GET /api/admin/audit-logs/export/excel?...filters...`
  - Returns XLSX file bytes from `AdminService.ExportAuditLogsToExcel(filter)`.
- `GET /api/admin/audit-logs/export/pdf?...filters...`
  - Returns PDF bytes from `AdminService.ExportAuditLogsToPdf(filter, adminName)`.

---

## 3) Frontend (`umb-portal`) functionalities

### 3.1 Authentication UI + session

- Components:
  - `src/Components/Login.tsx`
  - `src/services/auth.ts`
  - `src/services/session.ts`
  - `src/services/requestManager.ts`

Functional behavior (typical based on structure):

- User submits credentials.
- Frontend calls backend `POST /api/auth/login`.
- Stores session data (likely JWT + userId + isAdmin) through `session.ts`.
- Uses `AuthMiddleware`-like approach on backend; frontend uses protected routes via `AdminRoute` and layout components.

### 3.2 Statement flow UI

- Components:
  - `src/Components/ESB-Statement.tsx` (ESB statement view)
  - `src/Components/Statement.tsx` (main statement interaction)
  - `src/Components/PreviewChargesModal.tsx`
  - `src/Components/Toast.tsx`, `ErrorModal.tsx`

- Services:
  - `src/services/statement.ts`

Functional behavior:

- Collects account number, date range, and channel.
- Calls backend preview endpoint (`/api/statement/preview`) to:
  - show computed charge totals and charge message
  - receive `previewToken`
- Calls backend generate endpoint (`/api/statement/generate`) with the `previewToken` to:
  - perform charging
  - generate and download PDF

### 3.3 Admin UI

- Layout:
  - `src/layouts/AdminLayout.tsx`

- Admin pages:
  - `src/pages/admin/AdminOverview.tsx`
  - `src/pages/admin/UserManagement.tsx`
  - `src/pages/admin/AuditLogs.tsx`
  - `src/pages/admin/Settings.tsx`

- Admin routing / guards:
  - `src/pages/admin/AdminRoute.tsx`
  - `src/services/adminService.ts`

Functional behavior:

- Dashboard shows statistics.
- User management includes listing, adding, and toggling users.
- Audit logs include filtering and export.
- Settings page loads settings + update UI.

---

## 4) Operational notes / non-functional behaviors

- **Request caching**: Statement preview is cached in memory for 10 minutes keyed by `previewToken`.
- **Race protection**: Generate step validates a signature to ensure request details match what was previewed.
- **Best-effort audit logging**: Statement generation returns success PDF even if audit logging fails.
- **Channel mapping**: Bank companyId resolution and suggested channel logic exist in `BankApiService`.

---

## 5) Explicitly documented pending work

- `TODO.md` describes a plan to implement **database-backed logging** for each bank statement-charge request (captures correlation id, user, debit payload, bank response, and success/failure).

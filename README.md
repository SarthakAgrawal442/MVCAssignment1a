# MVCSampleApp — Security Assignment

An ASP.NET Core MVC application demonstrating a full security implementation across authentication, authorization, encryption, non-repudiation, and file protection.

---

## Tech Stack

- **Framework:** ASP.NET Core MVC (.NET 8)
- **Database:** SQLite via Entity Framework Core
- **Auth:** ASP.NET Core Identity + Google SSO + mTLS
- **Encryption:** AES-256 (fields), ChaCha20 (files)
- **Signing:** RSA-2048 (audit log non-repudiation)

---

## Security Features

### 1. Field Encryption (AES-256-CBC)
All sensitive person fields are encrypted at rest in the database.

**Encrypted fields:**
- `FirstName`, `LastName`
- `Street`, `PostalCode`
- `DateOfBirth`
- `Income` (integer → encrypted string)

**Service:** `Services/FieldEncryptionService.cs`  
Key stored in `appsettings.json` → `Encryption:Key`

---

### 2. Non-Repudiation (RSA-2048 Audit Log)
Every Create, Edit, and Delete action on `Client` and `Employee` is recorded with an RSA-signed audit entry — proving who did what and when.

**Audit log fields:**
| Field | Description |
|---|---|
| `Action` | e.g. `Client.Create` |
| `UserName` | Authenticated user's email |
| `Timestamp` | UTC timestamp |
| `Summary` | Human-readable description |
| `Payload` | JSON of changed data |
| `Signature` | RSA-2048 signature over payload |

**Service:** `Services/AuditService.cs`  
RSA key pair stored in `appsettings.json` → `Audit:PrivateKey` / `Audit:PublicKey`

---

### 3. File Encryption (ChaCha20 + HMAC-SHA256)
Client photos are never stored as raw bytes. Every uploaded file is encrypted using a from-scratch ChaCha20 stream cipher implementation (RFC 8439) and stored in the `vault/` folder.

**File format on disk:**
```
[12-byte nonce][ciphertext][32-byte HMAC-SHA256 tag]
```

**Flow:**
- Upload → `FileVaultService.SaveAsync()` → encrypted `.vault` file written to `vault/`
- View → `GET /Clients/GetPhoto/{id}` → HMAC verified → decrypted → streamed to browser
- Delete → `FileVaultService.Delete()` → `.vault` file removed from disk

**Services:**
- `Services/ChaCha20Service.cs` — from-scratch quarter-round + block function
- `Services/FileVaultService.cs` — vault I/O, HMAC verification, key management

Keys stored in `appsettings.json` → `Vault:Key` / `Vault:HmacKey`  
> ⚠️ `vault/` is excluded from Git via `.gitignore`

---

### 4. Mutual TLS (mTLS)
The Kestrel server requires a valid client certificate on all HTTPS connections.

```csharp
options.ConfigureHttpsDefaults(https => {
    https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
});
```

Client certificate files (`client.crt`, `client.key`, `client.p12`) are excluded from Git via `.gitignore`.

---

### 5. IP Filtering Middleware
A custom middleware (`Middleware/IpFilterMiddleware.cs`) blocks requests from IPs not on the allowlist configured in `appsettings.json`.

---

### 6. Authentication & Authorization

| Feature | Detail |
|---|---|
| Local login | ASP.NET Core Identity with hashed passwords |
| Google SSO | OAuth2 via `AddGoogle()` |
| Two-Factor Auth | TOTP-based 2FA |
| Role-based auth | `[Authorize(Roles = "Admin")]` on all write actions |
| Access denied | Custom `AccessDenied` page |

---

## Models

```
Person (base)
├── Client    → Income (encrypted), PhotoVaultName, Services[]
└── Employee  → Salary (encrypted), Roles, MyService
```

---

## Running Locally

```bash
# 1. Restore dependencies
dotnet restore

# 2. Apply migrations
dotnet ef database update

# 3. Run
dotnet run
```

> `appsettings.Development.json` is excluded from Git. You must create it locally with your encryption keys, Google OAuth credentials, and connection string.

---

## Commit History (feature/security-auth)

| Commit | Feature |
|---|---|
| `a8097f0` | ChaCha20 file vault |
| `39da599` | RSA audit log (non-repudiation) |
| `789055d` | AES field encryption |
| `ea21c55` | mTLS client certificate support |
| `955da98` | Role-based authorization |
| `8ec1955` | 2FA, Google SSO, SSL, IP filtering |

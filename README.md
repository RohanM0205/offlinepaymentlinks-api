<div align="center">

# 🛡️ OfflinePaymentLinks — Backend API

### ASP.NET Core 8 · Entity Framework Core · SQL Server · JWT Auth

**A production-grade REST API powering an offline insurance payment portal for HFFC ARGO General Insurance.**  
Enables agents to generate secure, trackable payment links for customers — with full KYC verification, role-based access, and real-time payment lifecycle tracking.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/en-us/sql-server)
[![Entity Framework](https://img.shields.io/badge/EF_Core-8.0-512BD4?style=flat-square)](https://docs.microsoft.com/en-us/ef/core/)
[![ASP.NET Identity](https://img.shields.io/badge/ASP.NET_Identity-✓-512BD4?style=flat-square)](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)

[Features](#-features) · [Architecture](#-architecture) · [API Reference](#-api-reference) · [Setup](#-local-setup) · [Frontend Repo](https://github.com/RohanM0205/offlinepaymentlinks-ui)

</div>

---

## 📌 Project Context

HFFC ARGO agents traditionally handled insurance payments manually — no tracking, no audit trail, no customer-facing payment flow. This system digitises the entire process:

1. Agent fills a payment form → system generates a **unique short URL**
2. Customer opens the link → completes **KYC verification**
3. Customer pays → system records the **full payment lifecycle**
4. Agent tracks all links via a **personal dashboard**

---

## ✨ Features

### 🔐 Authentication & Authorization
- JWT-based stateless authentication with configurable expiry
- **ASP.NET Identity** for user management with custom `ApplicationUser` fields
- Role hierarchy: `SuperAdmin` → `Admin` → `Agent` → custom roles
- Self-registration with **admin approval workflow** before access is granted
- Pending / Active / Rejected user states

### 👥 Role-Based Access Control
- **SuperAdmin** — full system control, manages admins, roles, all users
- **Admin** — manages agents and approves registrations
- **Agent** — generates payment links, views personal dashboard
- Per-role **module permissions** (Send Payment Link, View Reports, Manage Users, etc.)
- Per-role **transaction type restrictions** stored as JSON in `RolePermissions` table

### 💳 Payment Link Engine
- Supports **7 transaction types**: New Business, Roll Over, Renewal, Endorsement, Shortfall, NCB Recovery, 2Rs Payment
- Generates unique `InvoiceNo`, `PaymentReferenceNo`, and `JobRequestId` per link
- Auto-generated **short URLs** (7-char alphanumeric) stored in `UrlMappings`
- Configurable **link expiry** (default 24 hours) using `DateTime.Now` for SQL Server timezone consistency
- Separate payment flows: KYC-required flow vs direct payment summary flow

### 🔍 KYC Verification
- Fetches KYC data by **KYC ID** or **PAN + Date of Birth**
- **Fuzzy name matching** using Levenshtein distance algorithm (≥60% = Approved)
- KYC status pre-check — blocks rejected KYC before name comparison
- Full audit trail in `NameMatchLogs` table (match percentage, KYC ID, timestamp)

### 💰 Payment Lifecycle Tracking
- `PrePaymentData` — captures all link generation data
- `PostPaymentData` — records full transaction result (success/failure, payment mode, instrument details, timestamps)
- Payment statuses: **Paid**, **Pending**, **Attempted** (failed), **Expired**
- Stores UPI ID, card last 4 digits, card network, bank name, wallet name per transaction

### 📊 User Dashboard API
- Per-agent stats: total links, paid, pending, attempted, expired, amount collected
- Paginated, filtered, sortable shared links endpoint
- Recent activity feed
- Excel-exportable data (consumed by frontend)

### 🔗 URL Resolution
- Public short URL resolver → checks expiry → redirects to correct flow
- Expired URL detection with timestamp returned to frontend
- Already-paid detection to prevent duplicate payments

---

## 🏗️ Architecture
OfflinePaymentLinks.API/
├── Controllers/
│   ├── AuthController.cs           # Login, register, approve users
│   ├── AdminController.cs          # User management, access requests
│   ├── RolesController.cs          # Role CRUD + permissions (SuperAdmin only)
│   ├── PaymentFormController.cs    # KYC fetch, policy search, link generation
│   ├── KycVerifyController.cs      # Public KYC compare (customer-facing)
│   ├── UrlResolverController.cs    # Public short URL resolver
│   ├── PaymentSummaryController.cs # Public payment summary (customer-facing)
│   ├── PostPaymentController.cs    # Record payment results
│   └── UserDashboardController.cs  # Agent dashboard stats + links
├── Models/
│   ├── ApplicationUser.cs          # Extended Identity user
│   ├── PrePaymentData.cs           # Payment link data
│   ├── PostPaymentData.cs          # Transaction records
│   ├── RolePermission.cs           # Per-role permission config
│   ├── UrlMapping.cs               # Short URL store
│   ├── KYC_Information.cs          # KYC master data
│   └── NameMatchLog.cs             # KYC verification audit log
├── Services/
│   ├── AuthService.cs              # JWT generation, login, registration
│   ├── NameMatchService.cs         # Levenshtein fuzzy matching
│   ├── GenericPaymentsFetchService.cs # KYC, policy, pincode lookups
│   ├── PaymentUtilityService.cs    # Unique ID generation
│   └── ShortCodeGenerator.cs      # 7-char short URL generator
├── Data/
│   └── ApplicationDbContext.cs     # EF Core DbContext + seeding
└── Helpers/
└── SeedData.cs                 # Default SuperAdmin + roles seed
---

## 🗄️ Database Schema

| Table | Purpose |
|---|---|
| `AspNetUsers` | Extended Identity users with approval fields |
| `AspNetRoles` | System + custom roles |
| `RolePermissions` | Per-role module flags + allowed transaction types (JSON) |
| `PrePaymentData` | All payment link data — 30+ columns |
| `PostPaymentData` | Full transaction records with payment instrument details |
| `UrlMappings` | Short URL → long URL with expiry |
| `NameMatchLogs` | KYC name comparison audit trail |
| `KYC_Information` | KYC master data |
| `PolicyInformation` | Policy master data |
| `Products` | Product catalogue with codes |
| `PinCodeData` | Pincode → city/state mapping |

---

## 🌐 API Reference

### Auth — `/api/auth`
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/login` | Public | JWT login |
| POST | `/register` | Public | Self-registration (pending approval) |
| GET | `/me` | Bearer | Current user info |

### Payment — `/api/payment`
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/my-permissions` | Bearer | Role-based permissions check |
| GET | `/kyc/{kycId}` | Bearer | Fetch KYC by ID |
| GET | `/kyc-by-pan-dob` | Bearer | Fetch KYC by PAN + DOB |
| GET | `/policy/{policyNumber}` | Bearer | Policy lookup |
| GET | `/shortfall-search` | Bearer | Shortfall search (3 modes) |
| GET | `/pincode/{pinCode}` | Bearer | Pincode → city/state |
| POST | `/process-and-send` | Bearer | Generate payment link |

### Customer-Facing (Public) — No Auth Required
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/resolve/{shortCode}` | Resolve short URL + expiry check |
| GET | `/api/kyc-verify` | Fetch masked proposer name |
| POST | `/api/kyc-verify/compare` | KYC name match + consent |
| GET | `/api/payment-summary` | Payment summary (paid/expired detection) |
| POST | `/api/post-payment/record` | Record payment result |

### Admin — `/api/admin` (Admin/SuperAdmin)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/access-requests` | Pending registrations |
| POST | `/approve-user` | Approve + assign role |
| POST | `/reject-user` | Reject registration |
| GET | `/admins` | List admin users (paginated) |
| DELETE | `/admins/{id}` | Delete admin |
| PUT | `/admins/{id}/role` | Change admin role |
| GET | `/users` | Approved non-admin users |
| GET | `/users/all` | All users with filters (SuperAdmin) |

### Roles — `/api/roles` (SuperAdmin only)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | All roles with permissions |
| POST | `/` | Create custom role |
| PUT | `/{id}/rename` | Rename role |
| DELETE | `/{id}` | Delete role |
| PUT | `/{id}/permissions` | Update module permissions |

---

## ⚙️ Local Setup

### Prerequisites
- .NET 8 SDK
- SQL Server (local or Docker)
- Visual Studio 2022 or VS Code

### Steps

```bash
# 1. Clone the repo
git clone https://github.com/RohanM0205/offlinepaymentlinks-api.git
cd OfflinePaymentLinks.API

# 2. Create appsettings.json from the example
cp appsettings.example.json appsettings.json
# Fill in your SQL Server connection string and JWT secret

# 3. Apply database migrations
dotnet ef database update

# 4. Run the API
dotnet run

# API will be available at https://localhost:7002
# Swagger UI at https://localhost:7002/swagger
```

### Default SuperAdmin Credentials
After running migrations, a default SuperAdmin is seeded:
Email:    superadmin@hffc.com
Password: SuperAdmin@123
> ⚠️ Change these immediately in production.

---

## 🔧 Key Technical Decisions

| Decision | Rationale |
|---|---|
| `DateTime.Now` over `DateTime.UtcNow` | SQL Server stores local time; using UTC caused 5.5hr offset issues in IST timezone |
| Levenshtein fuzzy match at 60% threshold | Handles common name variations and spelling differences in KYC records |
| Short URL in-house vs third-party | Full control over expiry, audit, and no external dependency |
| Anonymous endpoints for customer flow | Customers don't have accounts — payment links are the auth mechanism |
| JSON for AllowedTransactionTypes | Avoids a separate join table for a simple list of strings per role |

---

## 🤝 Connected Frontend

This API is consumed by **[offlinepaymentlinks-ui](https://github.com/RohanM0205/offlinepaymentlinks-ui)** — a React 19 + TanStack Router SPA.

---

<div align="center">
  <p>Built by <a href="https://github.com/RohanM0205">Rohan More</a></p>
</div>
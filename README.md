# 🛡️ SVSharp — Enterprise Cyber Assets & Vulnerability Management (ECVM)

A high-performance, secure, and scalable RESTful API designed for centralized management of IT assets and security vulnerabilities. Built with **.NET 8** (Long Term Support) following modern software engineering principles.

---

## 🏛️ Enterprise Architecture

The system follows a **Modular Monolith** approach with a clear separation of concerns, ensuring high maintainability and ease of testing.

- **Presentation Layer (Controllers):** RESTful endpoints with standard HTTP status codes, structured JSON responses, and Swagger/OpenAPI 3.0 documentation.
- **Application Layer (Services):** Contains all business logic, orchestrating data flow between DTOs and the persistence layer. Uses **Dependency Injection (Scoped)** for lifetime management.
- **Data Access Layer (EF Core):** Optimized persistence using Entity Framework Core with **Npgsql** for PostgreSQL.
- **Domain Layer (Entities):** Robust models with inheritance from `BaseEntity` to ensure cross-cutting auditability.

---

## 🛠️ Technology Stack & Decisions

- **Framework:** .NET 9.0 (ASP.NET Core Web API).
- **Database:** PostgreSQL (Cloud-native, ACID compliant).
- **ORM:** Entity Framework Core (Code-First approach).
- **Security:** JWT (JSON Web Tokens) with HS256 algorithm.
- **Serialization:** System.Text.Json with `JsonStringEnumConverter` for seamless Frontend-Backend Enum synchronization.
- **Documentation:** Swagger UI (Swashbuckle) with JWT Authorization integration.

---

## 🔐 Security & Governance (CIO/CISO Focus)

The SVSharp API prioritizes the **CIA Triad** (Confidentiality, Integrity, and Availability):

1. **Authentication & Authorization:**
   - Stateless JWT-based authentication.
   - Robust Bearer token validation with Issuer/Audience checks.
2. **Data Integrity & Auditability:**
   - **Automatic Auditing:** All entities automatically track `CreatedAt` and `UpdatedAt` timestamps via `AppDbContext` overrides.
   - **Soft Delete Pattern:** Implemented via a `DeletedAt` timestamp and **EF Core Global Query Filters**. This ensures data is never physically removed without authorization, preserving the audit trail for SOC2/ISO 27001 compliance.
3. **Password Security:**
   - Industry-standard hashing using **BCrypt.Net-Next**.
4. **Resilience:**
   - Global Exception Handling (via Services/Controllers).
   - Health Check endpoints (`/health`) for real-time monitoring by orchestrators (Render, K8s).

---

## 🛰️ Integration & Features

### 1. Asset Lifecycle Management
Comprehensive management of IT assets (Operating Systems, WebApps, Databases, APIs, Networks).
- **Status:** Online/Offline tracking.
- **Archiving:** Soft-archive logic for lifecycle decommissioning.

### 2. Vulnerability Intelligence
Detailed tracking of security flaws with severity levels (Critical, High, Medium, Low) and status (Active, Resolved, Archived).

### 3. N:N Asset-Vulnerability Mapping
Advanced relationship management allowing the mapping of specific vulnerabilities to multiple assets. 
- **Endpoint:** `POST /api/assets/{id}/vulns/{vulnId}`
- **Data Integrity:** Cascading rules defined via Fluent API to prevent orphaned records.

---

## 📦 Deployment & CI/CD

- **Containerization:** Ready-to-use `Dockerfile` for standardized environments.
- **Environment Management:** Configuration via Environment Variables (`Jwt__Key`, `ConnectionStrings__DefaultConnection`).
- **Database Migrations:** Automated synchronization during startup `context.Database.Migrate()`.

---

## 📈 Roadmap

- [ ] Implementation of Role-Based Access Control (RBAC).
- [ ] Integration with automated vulnerability scanners (Tenable, Nessus).
- [ ] Advanced reporting engine with PDF/Excel export.
- [ ] Multi-tenant support.

---

**CISO/CTO Note:** *SVSharp is built to be the "Source of Truth" for your security posture, ensuring that every asset and its associated risks are documented and trackable.*

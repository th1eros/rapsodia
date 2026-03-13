# 🛡️ SVsharp — Cyber Assets & Vulnerability Management API

API REST em .NET 8 para gestão centralizada de ativos tecnológicos e vulnerabilidades.

---

## 🚀 Stack
- **Framework:** .NET 8 / ASP.NET Core
- **Banco:** PostgreSQL via Npgsql + Entity Framework Core
- **Auth:** JWT Bearer + BCrypt
- **Deploy:** Docker → Render
- **Frontend:** React + TypeScript + Vite

---

## 🔐 Variáveis de Ambiente (obrigatórias no Render)

| Variável                              | Descrição                          |
|---------------------------------------|------------------------------------|
| `ConnectionStrings__DefaultConnection`| String de conexão PostgreSQL       |
| `Jwt__Key`                            | Chave secreta para assinar o JWT   |
| `CORS_ALLOWED_ORIGIN`                 | URL do frontend (ex: https://app.vercel.app) |

> ⚠️ **Nunca commitar valores reais no `appsettings.json`.** O arquivo está intencionalmente vazio.

---

## 🗄️ Banco de Dados

### Rodando as migrations
```bash
dotnet ef database update
```

### ⚠️ Migration obrigatória após refactor de AssetVuln e Asset
Se você aplicou o refactor dos enums de `Asset` e a remoção do `BaseEntity` do `AssetVuln`:
```bash
dotnet ef migrations add RefactorAssetEnumsAndAssetVuln
dotnet ef database update
```

---

## 🏗️ Arquitetura

```
Controllers  →  Services  →  AppDbContext  →  PostgreSQL
                    ↕
                  DTOs
```

- **Soft Delete:** `DeletedAt` + Global Query Filters (invisível nas listagens padrão).
- **Auditoria:** `CreatedAt` / `UpdatedAt` preenchidos automaticamente no `SaveChanges`.
- **Enums como string:** armazenados e serializados como texto (legível no banco e no frontend).

---

## 🧪 Executando Localmente

### Pré-requisitos
- SDK .NET 8
- PostgreSQL local ou connection string remota

### Passos
```bash
# 1. Clone o repositório
git clone <repo>

# 2. Configure suas variáveis (use user-secrets ou appsettings.Development.json)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;..."
dotnet user-secrets set "Jwt:Key" "sua_chave_aqui"

# 3. Rode as migrations
dotnet ef database update

# 4. Suba a API
dotnet run
```

Acesse a documentação em: `http://localhost:5073/swagger`

---

## 🩺 Health Check
`GET /health` — usado pelo Render para verificar se o container está vivo.

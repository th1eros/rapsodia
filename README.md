# api-svsharp
API REST em ASP.NET Core com EF Core, Soft Delete e relacionamento N:N entre Asset e Vulnerability.
---

## 📌 Tecnologias

- .NET (ASP.NET Core Web API)
- Entity Framework Core
- SQL Server Express
- Swagger / OpenAPI
- Git

---

## 📌 Arquitetura

- Estrutura em camadas:
  - Controllers
  - Services
  - DTOs
  - Models
  - Data (DbContext)
- Injeção de Dependência nativa
- Separação de Responsabilidades (SRP)
- Uso de interfaces
- Soft Delete
- Padrão de resposta genérico

---

## 📌 Banco de Dados

- SQL Server
- Migrations habilitadas
- Relacionamento N:N entre Asset e Vuln
- Chave composta na tabela intermediária

---

## 📌 Soft Delete

- Campo `DeletedAt`
- Exclusão lógica
- Recuperação via reativação
- Global Query Filter aplicado

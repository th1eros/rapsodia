# 🛡️ SVsharp - Cyber Assets & Vulnerability Management

SVsharp é uma API REST de nível corporativo projetada para o gerenciamento centralizado de ativos tecnológicos e suas vulnerabilidades. Inspirada nas melhores práticas de segurança do Vale do Silício, a aplicação utiliza uma arquitetura robusta para garantir integridade, auditoria e escalabilidade.

---

## 🚀 Tecnologias Core
* **Framework:** .NET 8 / ASP.NET Core
* **Linguagem:** C#
* **Banco de Dados:** PostgreSQL (com Entity Framework Core)
* **Segurança:** JWT Bearer Authentication & BCrypt Password Hashing
* **Documentação:** Swagger (OpenAPI 3.0)

---

## 🏗️ Arquitetura e Padrões
A API segue uma abordagem de **Separação de Preocupações (SoC)**:
* **Controllers:** Interface de entrada/saída HTTP.
* **Services:** Camada de lógica de negócio e regras de segurança.
* **Data/DbContext:** Persistência de dados com auditoria automatizada.
* **DTOs:** Contratos de dados para desacoplamento da camada de domínio.
* **Soft Delete:** Implementado via `DeletedAt` com Global Query Filters.

---

## 🔐 Segurança & Auditoria (CISO Compliance)
* **Authentication:** Fluxo completo de JWT com validação de Issuer/Audience.
* **Encryption:** Armazenamento de credenciais utilizando algoritmos de Hash BCrypt.
* **CORS:** Configurado para integração segura com o Frontend (React/Vite).
* **Audit Trail:** Rastreabilidade nativa de criação (`CreatedAt`), atualização (`UpdatedAt`) e arquivamento (`DeletedAt`).

---

## 🗄️ Estrutura do Banco de Dados
A modelagem suporta relacionamentos complexos:
* **Asset ↔ Vulnerability:** Relacionamento N:N (Many-to-Many).
* **Filtros Globais:** Dados "arquivados" são omitidos automaticamente das consultas.



---

## 🧪 Como Executar

### Pré-requisitos
* SDK .NET 8.0
* Instância PostgreSQL ativa

### Configuração
1. Clone o repositório.
2. Configure a string de conexão no `appsettings.json` ou via Variável de Ambiente:
   `ConnectionStrings__DefaultConnection`
3. Execute as migrations:
   ```bash
   dotnet ef database update
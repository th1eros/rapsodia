SVsharp 

📌 Visão do Projeto
SVsharp é uma API REST corporativa para:
Gestão de Assets
Gestão de Vulnerabilidades
Relacionamento N:N entre entidades
Controle por Ambiente (DEV / HML / PROD)
Autenticação e Autorização via JWT
Soft Delete
Auditoria automática
Pronta para integração com Front-end moderno

🏗 Arquitetura
Camadas:
Controllers → Interface HTTP
Services → Regras de negócio
Data → Persistência
DTOs → Contratos de entrada/saída
Models → Entidades de domínio

🔐 Segurança
Autenticação JWT (Bearer)
Validação de Issuer e Audience
Expiração configurável
ClockSkew = 0
HTTPS habilitado
Variáveis de ambiente para chave secreta
Estrutura preparada para Roles/Claims

🗄 Banco de Dados
Tecnologias:
SQL Server
Entity Framework Core
Migrations
Recursos implementados:
Relacionamento N:N (Asset ↔ Vuln)
Soft Delete (DeletedAt)
Global Query Filters

Auditoria automática:
CreatedAt
UpdatedAt
DeletedAt
Enums armazenados como string

📂 Estrutura do Projeto

Organização:

API_SVsharp
│
├── Controllers
├── Services
│   ├── Assets
│   ├── Vulns
│   └── Auth
├── Data
├── DTO
├── Models
└── Mapping

🚀 Funcionalidades
Assets
Criar
Editar
Listar
Buscar por ID
Arquivar
Restaurar
Vincular Vulnerabilidade
Remover vínculo

Vulnerabilidades
Criar
Editar
Listar
Buscar por ID
Arquivar
Restaurar

🔑 Configuração JWT
Exemplo appsettings.json:
"Jwt": {
  "Key": "CHAVE_COM_MINIMO_32_CARACTERES",
  "Issuer": "API_SVsharp",
  "Audience": "API_SVsharp_Clients",
  "ExpiresInMinutes": 60
}

Ou via variável de ambiente:
Jwt__Key
🧪 Execução
dotnet restore
dotnet build
dotnet run

Swagger:
http://localhost:5073/swagger/index.html

🧭 Roadmap
 Arquitetura base 100%
 Soft Delete 100%
 Auditoria automática 100%
 JWT Authentication 100%
 Middleware global de exceção 20%...
 AutoMapper
 Logging estruturado
 Testes automatizados
 Front-end simples
 Hardening final

📊 Status Atual
Backend estável.
Preparado para integração com Front-end.
Estrutura pronta para evolução corporativa.

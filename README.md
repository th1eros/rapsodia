# 🛡️ Malebolge — Gestão Empresarial de Ativos e Vulnerabilidades Cibernéticas (ECVM)

API RESTful de alto desempenho, segura e escalável, projetada para o gerenciamento centralizado de ativos de TI e vulnerabilidades de segurança. Desenvolvida em **.NET 8** com foco em arquitetura modular, conteinerização e prontidão para ambientes de nuvem híbrida.

---

## 🏛️ Arquitetura Corporativa

O sistema adota uma abordagem de **Monólito Modular** com clara separação de responsabilidades, facilitando a manutenção, a testabilidade e a evolução controlada.

- **Camada de Apresentação (Controllers):** Endpoints RESTful com códigos de estado HTTP padronizados, respostas JSON estruturadas e documentação interativa via Swagger/OpenAPI 3.0 (ativação condicional por ambiente).
- **Camada de Aplicação (Services):** Contém toda a lógica de negócio, orquestrando o fluxo de dados entre DTOs e a camada de persistência. Utiliza **Injeção de Dependência (Scoped)** para gerenciamento do ciclo de vida.
- **Camada de Acesso a Dados (EF Core):** Persistência otimizada com Entity Framework Core e provider **Npgsql** para PostgreSQL (cloud‑native ou on‑premise).
- **Camada de Domínio (Entities):** Modelos robustos com herança de `BaseEntity`, garantindo auditabilidade transversal.
- **Infraestrutura (Cross‑Cutting):** Execução automática de migrations na inicialização; persistência do anel de chaves do Data Protection; ativação condicional da interface do Swagger.

---

## 🛠️ Stack Tecnológica

- **Framework:** .NET 8.0 (ASP.NET Core Web API)
- **Banco de Dados:** PostgreSQL (Supabase gerenciado para ambientes superiores; instância local em contêiner para desenvolvimento)
- **ORM:** Entity Framework Core (Code‑First)
- **Segurança:** JWT (HS256) e criptografia de campos sensíveis
- **Conteinerização:** Docker com imagens otimizadas (Distroless/Chiseled em produção), arquivos de override por ambiente
- **Proxy Reverso & TLS:** Nginx + Certbot (renovação automática Let’s Encrypt) para domínios de teste; Cloudflare para proteção adicional e SSL no domínio principal
- **Documentação:** Swagger UI (Swashbuckle) com suporte a autenticação JWT

---

## 🔐 Segurança e Governança (Foco CISO/CTO)

A API **Malebolge** foi concebida para atender aos pilares **CID** (Confidencialidade, Integridade e Disponibilidade):

1. **Autenticação e Autorização:**
   - Autenticação stateless baseada em JWT.
   - Validação rigorosa de `Issuer` e `Audience` nos tokens Bearer.

2. **Integridade e Auditabilidade:**
   - **Auditoria Automática:** Todas as entidades herdam os campos `CreatedAt` e `UpdatedAt`, gerenciados pelo `AppDbContext`.
   - **Soft Delete:** Exclusão lógica implementada por meio da coluna `DeletedAt` e **Global Query Filters** do EF Core. Nenhum registro é removido fisicamente, preservando a trilha de auditoria exigida por normas como SOC2 e ISO 27001.

3. **Segurança de Credenciais:**
   - Hashing de senhas com algoritmo **BCrypt.Net‑Next** (padrão da indústria).

4. **Proteção da Infraestrutura:**
   - **Nenhum segredo em imagens ou arquivos compose:** Todas as configurações sensíveis são injetadas exclusivamente via variáveis de ambiente e arquivos `.env` externos.
   - **Imagem de produção hardened:** Imagem Chiseled (sem shell, sem gerenciador de pacotes), reduzindo drasticamente a superfície de ataque.
   - **Isolamento de rede:** Redes Docker customizadas (`malebolge_network`, `abitat_network`) com políticas de acesso controlado.

5. **Resiliência:**
   - Tratamento global de exceções.
   - Endpoint de verificação de saúde (`/health`) para monitoramento em tempo real.
   - Migrações automáticas do banco de dados executadas na inicialização, garantindo consistência do esquema.

---

## 🛰️ Funcionalidades

### 1. Gestão do Ciclo de Vida de Ativos
Cadastro completo de ativos de TI (Sistemas Operacionais, Aplicações Web, Bancos de Dados, APIs, Redes).
- **Status:** Monitoramento Online/Offline.
- **Arquivamento:** Lógica de *soft‑archive* para descomissionamento controlado.

### 2. Inteligência de Vulnerabilidades
Registro detalhado de falhas de segurança com níveis de severidade (Crítica, Alta, Média, Baixa) e status (Ativa, Resolvida, Arquivada).

### 3. Mapeamento N:N Ativo‑Vulnerabilidade
Relacionamento avançado que permite associar uma vulnerabilidade a múltiplos ativos.
- **Endpoint:** `POST /api/assets/{id}/vulns/{vulnId}`
- **Integridade:** Regras de integridade referencial definidas via Fluent API, prevenindo registros órfãos.

---

## 🌐 Estratégia Multi‑Ambiente

A solução é executada em ambientes isolados, cada um com sua própria base de dados, configuração e terminação TLS, garantindo a segregação adequada entre desenvolvimento, teste e produção.

| Ambiente       | Finalidade                          | Acesso                         |
|----------------|-------------------------------------|--------------------------------|
| **Sandbox**    | Desenvolvimento ativo e testes      | `sandbox.th1eros.dev` (HTTPS)  |
| **Staging**    | Validação pré‑produção (QA)         | `staging.th1eros.dev` (HTTPS)  |
-----------------------------------------------------------------------------------------

### Principais características operacionais:
- **Orquestração:** Duas stacks Docker Compose independentes (uma para sandbox/staging, outra exclusiva para produção).
- **Configuração por ambiente:** Arquivos `.env` mantidos fora do repositório, com valores específicos por ambiente.
- **Proxy reverso:** Nginx roteia o tráfego para as portas internas; Certbot gerencia automaticamente certificados TLS para os domínios de desenvolvimento; Cloudflare provê proteção adicional contra DDoS e SSL para o domínio principal.
- **Migrações:** Executadas automaticamente na inicialização da aplicação, com resiliência de conexão através do pooler do Supabase (porta `6543`) nos ambientes de staging e produção.
- **Chaves de proteção de dados:** Persistidas via volumes Docker, preservando tokens de autenticação entre reinicializações.
- Implementação de controle de acesso baseado em papéis (RBAC).
- Painel de visualização para gestão de ativos e vulnerabilidades (front‑end dedicado).
- Integração com scanners automatizados de vulnerabilidades (Tenable, Nessus).
- Pipeline de CI/CD com compilação automatizada de imagens e deploy multi‑ambiente.

---

**Nota para CISO/CTO:** *O Malebolge foi projetado para ser a "Fonte da Verdade" da sua postura de segurança, garantindo que cada ativo e seus riscos associados estejam documentados, rastreáveis e protegidos por uma arquitetura de defesa em profundidade.*
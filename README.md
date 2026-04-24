# 🏢 aBitat — Ambiente de Produção (Rapsodia)

Plataforma de produção isolada da API Rapsodia, executada em contêiner **Docker** sobre **.NET 8.0 (LTS)**, com imagem ultra‑enxuta baseada em **Chiseled Ubuntu**. Projetada para oferecer máxima estabilidade, segurança e eficiência operacional em cargas de trabalho reais.

---

## 📦 Stack de Produção

| Componente               | Escolha Técnica                                   |
|--------------------------|---------------------------------------------------|
| **Runtime**              | .NET 8.0.125 (LTS)                                |
| **Banco de Dados**       | PostgreSQL gerenciado (Supabase)                  |
| **Containerização**      | Docker `Chiseled`                                 |
| **Proxy Reverso**        | Nginx (local) + Cloudflare (CDN e DDoS)           |
| **Certificação SSL**     | Let's Encrypt (Certbot) no domínio `.com`         |
| **Rede**                 | Rede Docker isolada                               |
| **Persistência Chaves**  | Volume Docker para Data Protection                |

---

## 🚀 Características Técnicas

### 🎯 Isolamento Total
O ambiente de produção roda em uma **stack Docker Compose dedicada**, completamente independente dos ambientes de desenvolvimento e testes. Isso garante que atualizações ou falhas em outros ambientes jamais afetem o serviço em produção.

### 🐚 Imagem Chiseled – O Estado da Arte em Containerização
A imagem do contêiner é construída a partir da variante **Chiseled** do Ubuntu fornecida pela Microsoft. As imagens Chiseled:

- **Não contêm shell** (`sh`, `bash`, etc.) nem gerenciador de pacotes.
- Incluem **apenas os binários e bibliotecas estritamente necessários** para executar uma aplicação .NET.
- **Reduzem drasticamente a superfície de ataque**, eliminando vetores comuns de intrusão.
- São **significativamente menores**, acelerando o *pull*, a inicialização e a distribuição.

Essa escolha de design torna o ambiente de produção **hardened por padrão**, exigindo que toda a configuração seja feita externamente — seja via arquivos de ambiente, seja por meio de volumes montados.

### ⚙️ Configuração Externalizada
Nenhuma informação sensível (senhas, chaves JWT, strings de conexão) é armazenada na imagem ou no código fonte. Toda a configuração é injetada em tempo de execução através de:

- **Arquivo de ambiente dedicado** (`.env`), localizado fora do repositório.
- **Variáveis de ambiente** definidas no `docker-compose.yml` a partir do arquivo `.env`.

Exemplo de variáveis gerenciadas:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key`
- `Security__FieldEncryptionKey`
- `AppSettings__Domain`, `AppSettings__Port`
- `ASPNETCORE_URLS` e `ASPNETCORE_ENVIRONMENT`

### 🛡️ Comunicação Segura e Balanceamento
- O tráfego externo chega via **Cloudflare**, que fornece CDN e mitigação de DDoS.
- O Cloudflare se comunica com o servidor de origem por HTTPS, utilizando um certificado **Let's Encrypt** válido (renovado automaticamente pelo Certbot).
- Internamente, o Nginx atua como proxy reverso, encaminhando as requisições para o contêiner da aplicação na porta `*`.

### 🔄 Migrações Automáticas
Ao iniciar, a aplicação executa automaticamente as migrações pendentes do Entity Framework Core no banco de dados Supabase, garantindo que o esquema esteja sempre atualizado sem intervenção manual.

### ✳️ Resiliência e Monitoramento
- Endpoint de saúde (`/health`) exposto para monitoramento externo.
- Política de reinício `unless-stopped` garante recuperação automática em caso de falha.
- Logs estruturados em formato JSON, rotacionados automaticamente (limite de 10 MB por arquivo, até 3 arquivos).

---
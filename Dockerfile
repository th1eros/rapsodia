# 🐳 Dockerfile Otimizado para Render
# Este arquivo garante que a aplicação inicie rapidamente e responda aos health checks

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# [CTO] Copiar arquivo de projeto primeiro (cache layer optimization)
COPY ["API_SVsharp.csproj", "./"]
RUN dotnet restore "./API_SVsharp.csproj"

# [CTO] Copiar código-fonte
COPY . .

# [CTO] Publicar em Release (otimizado e sem debug symbols)
RUN dotnet publish "API_SVsharp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =====================================
# Runtime Stage
# =====================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# [CISO] Criar usuário não-root para segurança
RUN useradd -m -u 1000 appuser
USER appuser

# [CTO] Copiar arquivos publicados
COPY --from=build /app/publish .

# [CIO] Variáveis de Ambiente para Render
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# [CIO] Health Check que o Render vai executar
# Timeout: 10 segundos por check
# Intervalo: a cada 10 segundos durante startup
# Tentativas: 3 falhas = container unhealthy
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# [CTO] Expor porta 8080 (Render redireciona tráfego aqui)
EXPOSE 8080

# [CIO] Entrypoint com melhor tratamento de sinais
ENTRYPOINT ["dotnet", "API_SVsharp.dll"]

# =============================================================================
# Rapsodia — Dockerfile Unificado (Multi-stage)
# =============================================================================
# Targets disponíveis:
#   --target staging    → Malebolge (Staging)  — porta 10001, jammy completo
#   --target production → Abitat (Production)  — porta 10000, jammy-chiseled
#
# Build args:
#   USER_ID   (default: 1654) — UID do usuário no host OCI
#   GROUP_ID  (default: 1654) — GID do grupo no host OCI
# =============================================================================

# ── Stage 1: Restore de dependências (cache layer separada) ─────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY ["Rapsodia.csproj", "./"]
RUN dotnet restore "Rapsodia.csproj" --locked-mode

# ── Stage 2: Build & Publish ─────────────────────────────────────────────────
FROM restore AS build
COPY . .
RUN dotnet publish "Rapsodia.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:TreatWarningsAsErrors=false

# ── Stage 3: STAGING (Malebolge) ─────────────────────────────────────────────
# Imagem completa jammy — mais ferramentas para debugging em staging
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS staging

ARG USER_ID=1654
ARG GROUP_ID=1654

WORKDIR /app
COPY --from=build /app/publish .

RUN groupadd -g ${GROUP_ID} appgroup && \
    useradd -m -u ${USER_ID} -g appgroup appuser && \
    chown -R appuser:appgroup /app

USER appuser

ENV ASPNETCORE_URLS=http://+:10001
EXPOSE 10001
ENTRYPOINT ["dotnet", "Rapsodia.dll"]

# ── Stage 4: PRODUCTION (Abitat) ──────────────────────────────────────────────
# Imagem chiseled — mínima, sem shell, superfície de ataque reduzida
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS production

ARG USER_ID=1654
ARG GROUP_ID=1654

WORKDIR /app
COPY --from=build --chown=${USER_ID}:${GROUP_ID} /app/publish .

USER ${USER_ID}:${GROUP_ID}

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Rapsodia.dll"]
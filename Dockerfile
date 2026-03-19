Copiar

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
 
# Copiar arquivo de projeto
COPY ["API_SVsharp.csproj", "./"]
 
# Restaurar dependências
RUN dotnet restore "./API_SVsharp.csproj"
 
# Copiar código fonte
COPY . .
 
# Publicar em Release
RUN dotnet publish "API_SVsharp.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:DebugType=embedded
 
# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
 
# Definir diretório de trabalho
WORKDIR /app
 
# Copiar publicação do stage anterior
COPY --from=build /app/publish .
 
# [CISO] Criar usuário não-root por segurança
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser
 
# Expor porta (Render pode usar outra)
EXPOSE 8080
 
# [CTO] Health check para Render monitorar
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD dotnet API_SVsharp.dll --version || exit 1
 
# Variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
 
# Executar aplicação
ENTRYPOINT ["dotnet", "API_SVsharp.dll"]
 
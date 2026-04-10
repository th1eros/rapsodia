# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Rapsodia.csproj", "./"]
RUN dotnet restore "Rapsodia.csproj"

COPY . .
# Removida a linha que sobrescrevia o appsettings.json
RUN dotnet publish "Rapsodia.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime'
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Segurança: Hardening de usuário
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Rapsodia.dll"]
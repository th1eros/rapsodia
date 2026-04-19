FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Rapsodia.csproj", "./"]
RUN dotnet restore "Rapsodia.csproj"
COPY . .
RUN dotnet publish "Rapsodia.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- AMBIENTE MALEBOLGE (Staging/Warzone) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS staging
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10001
EXPOSE 10001
ENTRYPOINT ["dotnet", "Rapsodia.dll"]

# --- AMBIENTE ABITAT (Production) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS production
WORKDIR /app
COPY --from=build --chown=1654:1654 /app/publish .
USER 1654
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Rapsodia.dll"]
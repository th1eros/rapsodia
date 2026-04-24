# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Rapsodia.csproj", "./"]
RUN dotnet restore "Rapsodia.csproj"
COPY . .
RUN dotnet publish "Rapsodia.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (Chiseled)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS production
WORKDIR /app
COPY --from=build /app/publish .
USER 1654
ENTRYPOINT ["dotnet", "Rapsodia.dll"]
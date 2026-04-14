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
COPY --from=build --chown=appuser:appgroup /app/publish .

ARG USER_ID=1000
ARG GROUP_ID=1000

RUN groupadd -g ${GROUP_ID} appgroup && \
    useradd -m -u ${USER_ID} -g appgroup appuser && \
    chown -R appuser:appgroup /app

USER appuser
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Rapsodia.dll"]
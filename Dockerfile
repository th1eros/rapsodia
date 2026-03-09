# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura as dependências
COPY ["API_SVsharp.csproj", "./"]
RUN dotnet restore "./API_SVsharp.csproj"

# Copia o restante do código e compila
COPY . .
RUN dotnet publish "API_SVsharp.csproj" -c Release -o /app/publish

# Estágio Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Comando para rodar a aplicação
ENTRYPOINT ["dotnet", "API_SVsharp.dll"]
# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro (cache de camadas), depois publica.
COPY src/ChatDistribuido/ChatDistribuido.csproj src/ChatDistribuido/
RUN dotnet restore src/ChatDistribuido/ChatDistribuido.csproj
COPY src/ src/
RUN dotnet publish src/ChatDistribuido/ChatDistribuido.csproj -c Release -o /app

# ---- runtime (sem SDK/ferramentas de build) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Catálogos específicos de container (config estática lida na inicialização).
COPY docker/nos.docker*.json ./

ENTRYPOINT ["dotnet", "ChatDistribuido.dll"]

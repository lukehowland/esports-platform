FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para cachear el restore
COPY shared/Esports.Auth.Shared/Esports.Auth.Shared.csproj shared/Esports.Auth.Shared/
COPY services/auth/Esports.Auth.Api/Esports.Auth.Api.csproj services/auth/Esports.Auth.Api/

RUN dotnet restore services/auth/Esports.Auth.Api/Esports.Auth.Api.csproj

# Copiar el código fuente
COPY shared/ shared/
COPY services/auth/ services/auth/

RUN dotnet publish services/auth/Esports.Auth.Api/Esports.Auth.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl para el healthcheck del compose
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Esports.Auth.Api.dll"]

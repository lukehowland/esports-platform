FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para cachear el restore
COPY shared/Esports.Shared/Esports.Shared.csproj shared/Esports.Shared/
COPY services/teams/Esports.Teams.Api/Esports.Teams.Api.csproj services/teams/Esports.Teams.Api/

RUN dotnet restore services/teams/Esports.Teams.Api/Esports.Teams.Api.csproj

# Copiar el código fuente
COPY shared/ shared/
COPY services/teams/ services/teams/

RUN dotnet publish services/teams/Esports.Teams.Api/Esports.Teams.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl para el healthcheck del compose
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Esports.Teams.Api.dll"]

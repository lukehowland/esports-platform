FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Esports.Shared/Esports.Shared.csproj shared/Esports.Shared/
COPY shared/Esports.Auth.Shared/Esports.Auth.Shared.csproj shared/Esports.Auth.Shared/
COPY services/tournaments/Esports.Tournaments.Api/Esports.Tournaments.Api.csproj services/tournaments/Esports.Tournaments.Api/
RUN dotnet restore services/tournaments/Esports.Tournaments.Api/Esports.Tournaments.Api.csproj

COPY shared/ shared/
COPY services/tournaments/ services/tournaments/
RUN dotnet publish services/tournaments/Esports.Tournaments.Api/Esports.Tournaments.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Esports.Tournaments.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Esports.Shared/Esports.Shared.csproj shared/Esports.Shared/
COPY services/ranking/Esports.Ranking.Api/Esports.Ranking.Api.csproj services/ranking/Esports.Ranking.Api/
RUN dotnet restore services/ranking/Esports.Ranking.Api/Esports.Ranking.Api.csproj

COPY shared/ shared/
COPY services/ranking/ services/ranking/
RUN dotnet publish services/ranking/Esports.Ranking.Api/Esports.Ranking.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Esports.Ranking.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Esports.Shared/Esports.Shared.csproj shared/Esports.Shared/
COPY services/matches/Esports.Matches.Api/Esports.Matches.Api.csproj services/matches/Esports.Matches.Api/
RUN dotnet restore services/matches/Esports.Matches.Api/Esports.Matches.Api.csproj

COPY shared/ shared/
COPY services/matches/ services/matches/
RUN dotnet publish services/matches/Esports.Matches.Api/Esports.Matches.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Esports.Matches.Api.dll"]

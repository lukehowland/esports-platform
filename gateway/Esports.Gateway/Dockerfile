FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY gateway/Esports.Gateway/Esports.Gateway.csproj gateway/Esports.Gateway/
RUN dotnet restore gateway/Esports.Gateway/Esports.Gateway.csproj

COPY gateway/ gateway/
RUN dotnet publish gateway/Esports.Gateway/Esports.Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Esports.Gateway.dll"]

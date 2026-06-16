FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY tools/Esports.Seeder/Esports.Seeder.csproj tools/Esports.Seeder/
RUN dotnet restore tools/Esports.Seeder/Esports.Seeder.csproj

COPY tools/Esports.Seeder/ tools/Esports.Seeder/
RUN dotnet publish tools/Esports.Seeder/Esports.Seeder.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Esports.Seeder.dll"]

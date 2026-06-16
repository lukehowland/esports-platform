FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY tests/Esports.Gateway.Tests/Esports.Gateway.Tests.csproj tests/Esports.Gateway.Tests/
RUN dotnet restore tests/Esports.Gateway.Tests/Esports.Gateway.Tests.csproj

COPY tests/Esports.Gateway.Tests/ tests/Esports.Gateway.Tests/

WORKDIR /src/tests/Esports.Gateway.Tests
ENTRYPOINT ["dotnet", "test", "--logger", "console;verbosity=normal", "--no-restore"]

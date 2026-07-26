FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/WhatsEnvio.Core/WhatsEnvio.Core.csproj src/WhatsEnvio.Core/
COPY src/WhatsEnvio.Worker/WhatsEnvio.Worker.csproj src/WhatsEnvio.Worker/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Contracts/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Contracts/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Domain/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Domain/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Application/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Application/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Infrastructure/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Infrastructure/

RUN dotnet restore src/WhatsEnvio.Worker/WhatsEnvio.Worker.csproj

COPY src/ src/
RUN dotnet publish src/WhatsEnvio.Worker/WhatsEnvio.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WhatsEnvio.Worker.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/WhatsEnvio.Core/WhatsEnvio.Core.csproj src/WhatsEnvio.Core/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.csproj src/Modules/Tenancy/
COPY src/WhatsEnvio.Worker/WhatsEnvio.Worker.csproj src/WhatsEnvio.Worker/

RUN dotnet restore src/WhatsEnvio.Worker/WhatsEnvio.Worker.csproj

COPY src/ src/
RUN dotnet publish src/WhatsEnvio.Worker/WhatsEnvio.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WhatsEnvio.Worker.dll"]

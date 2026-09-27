FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/WhatsEnvio.Core/WhatsEnvio.Core.csproj src/WhatsEnvio.Core/
COPY src/WhatsEnvio.Api/WhatsEnvio.Api.csproj src/WhatsEnvio.Api/
COPY src/Modules/Identity/WhatsEnvio.Modules.Identity.Contracts/*.csproj src/Modules/Identity/WhatsEnvio.Modules.Identity.Contracts/
COPY src/Modules/Identity/WhatsEnvio.Modules.Identity.Domain/*.csproj src/Modules/Identity/WhatsEnvio.Modules.Identity.Domain/
COPY src/Modules/Identity/WhatsEnvio.Modules.Identity.Application/*.csproj src/Modules/Identity/WhatsEnvio.Modules.Identity.Application/
COPY src/Modules/Identity/WhatsEnvio.Modules.Identity.Infrastructure/*.csproj src/Modules/Identity/WhatsEnvio.Modules.Identity.Infrastructure/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Contracts/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Contracts/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Domain/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Domain/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Application/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Application/
COPY src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Infrastructure/*.csproj src/Modules/Tenancy/WhatsEnvio.Modules.Tenancy.Infrastructure/

RUN dotnet restore src/WhatsEnvio.Api/WhatsEnvio.Api.csproj

COPY src/ src/
RUN dotnet publish src/WhatsEnvio.Api/WhatsEnvio.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WhatsEnvio.Api.dll"]

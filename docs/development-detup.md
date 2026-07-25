# Development setup

## Required tools

- Git
- Windows PowerShell 5.1 or PowerShell 7 or newer
- .NET SDK 10.0.302
- Docker Desktop with Linux containers
- Docker Compose 2.39 or newer

## First setup

From the repository root:

```powershell
dotnet --version
docker compose --env-file deploy/compose/.env.example -f deploy/compose/compose.yml up -d --wait
& ./scripts/Test-DevelopmentEnvironment.ps1
```

The selected SDK must be `10.0.302`, and the final command must report that the environment is ready for Milestone 02.

## Local database

PostgreSQL is available on `localhost:5432`. Local credentials are defined in `deploy/compose/.env.example` and must not be reused outside development.

Operational commands, including the destructive local-data reset command, are documented in `deploy/compose/README.md`.

## Project rules

- Follow the active implementation plan one task at a time.
- Run relevant verification before each commit.
- Never commit real credentials or customer data.
- Application timestamps are persisted in UTC; tenant time zones use IANA identifiers.
- Product and architecture specifications are internal documents kept outside this repository.

# Local PostgreSQL

The Compose stack provides PostgreSQL exclusively for local development.

## Start

```powershell
docker compose --env-file deploy/compose/.env.example -f deploy/compose/compose.yml up -d --wait
```

## Inspect

```powershell
docker compose --env-file deploy/compose/.env.example -f deploy/compose/compose.yml ps
docker compose --env-file deploy/compose/.env.example -f deploy/compose/compose.yml logs postgres
```

## Stop without deleting data

```powershell
docker compose --env-file deploy/compose/.env.example -f deploy/compose/compose.yml down
```

## Reset local data

The following command permanently deletes only the local Compose database volume. Confirm that no required local data remains before running it.

```powershell
docker compose --env-file deploy/compose/.env.example -f deploy/compose/compose.yml down --volumes
```

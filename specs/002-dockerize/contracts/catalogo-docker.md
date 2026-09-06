# Contract — Catálogo de nós para containers

Mesmo formato do catálogo local, mas com `host` = nome do serviço Compose (resolvido por
DNS interno). Lido na inicialização; NÃO é canal de coordenação (Constituição).

## Formato

```json
[
  { "id": 1, "host": "no1", "portaTcp": 5001 },
  { "id": 2, "host": "no2", "portaTcp": 5002 },
  { "id": 3, "host": "no3", "portaTcp": 5003 }
]
```

## Invariantes

- **CAT-1**: `host` é o nome do serviço do nó no Compose (`no{id}`).
- **CAT-2**: `portaTcp = 5000 + id`.
- **CAT-3**: O conjunto de ids/hosts coincide exatamente com os serviços do arquivo Compose
  da mesma escala (FR-007).
- **CAT-4**: Existe um catálogo por escala: `nos.docker.json` (3), `nos.docker.8.json` (8),
  `nos.docker.15.json` (15).

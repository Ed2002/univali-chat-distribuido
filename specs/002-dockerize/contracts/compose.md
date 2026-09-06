# Contract — Definição de cluster (Docker Compose)

Um arquivo por escala. Cada serviço é um nó; todos usam a mesma imagem e a mesma rede.

## Forma de cada serviço

```yaml
services:
  no1:
    image: chat-distribuido
    build: .                       # apenas no primeiro serviço; os demais reusam a imagem
    command: ["--id", "1", "--catalogo", "/app/nos.docker.json"]
    ports:
      - "8001:8001"               # painel do nó no host
    networks: [chat]
    restart: unless-stopped
  # no2, no3, ... análogos (id, porta 8000+id)
networks:
  chat:
    driver: bridge
```

## Invariantes

- **CMP-1**: Existe exatamente um serviço por id do catálogo da escala (FR-002, FR-007).
- **CMP-2**: Todos os serviços compartilham a rede `chat` (único canal entre nós — FR-003).
- **CMP-3**: Cada serviço publica somente sua porta web `8000+id`; portas TCP `5000+id` NÃO
  são publicadas ao host (tráfego interno — FR-005).
- **CMP-4**: Nenhum volume é usado como canal de coordenação (Constituição I). O catálogo é
  embutido na imagem (config read-only).
- **CMP-5**: `docker compose -f <arquivo> up` sobe todo o cluster; `... down` encerra tudo
  de forma limpa (FR-001, FR-008).
- **CMP-6**: Sem `depends_on` bloqueante entre nós; a reconexão sob demanda cobre a ordem de
  subida (FR-009).

## Arquivos por escala

| Arquivo | Escala | Catálogo |
|---------|--------|----------|
| `compose.yaml` | 3 nós | `docker/nos.docker.json` |
| `compose.8.yaml` | 8 nós | `docker/nos.docker.8.json` |
| `compose.15.yaml` | 15 nós | `docker/nos.docker.15.json` |

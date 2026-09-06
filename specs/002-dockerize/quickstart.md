# Quickstart — Chat Distribuído em Containers

Guia de execução e validação. Detalhes em [data-model.md](./data-model.md) e
[contracts/](./contracts/).

## Pré-requisitos

- Docker Engine + Docker Compose v2. Nenhum runtime .NET no host é necessário.

## Subir o cluster (3 nós)

```bash
docker compose up --build
```

Painéis no host:

- Nó 1 → http://127.0.0.1:8001
- Nó 2 → http://127.0.0.1:8002
- Nó 3 → http://127.0.0.1:8003

Parar tudo:

```bash
docker compose down
```

## Escalar para 8 ou 15 nós (sem alterar código)

```bash
docker compose -f compose.8.yaml up --build     # 8 nós  → painéis 8001..8008
docker compose -f compose.15.yaml up --build    # 15 nós → painéis 8001..8015
```

## Cenários de validação

### V1 — Subida com um comando (SC-001)

1. Em máquina só com Docker, rodar `docker compose up --build`.
2. **Esperado**: 3 containers de nó sobem e formam o chat.

### V2 — Ordem total no cluster conteinerizado (SC-004)

1. Difundir mensagens de grupo de nós diferentes pelos painéis.
2. **Esperado**: a ordem global de entrega é idêntica em todos os painéis.

### V3 — Painéis acessíveis pelo host (SC-003)

1. Abrir cada painel em `http://127.0.0.1:800{id}`.
2. **Esperado**: cada painel exibe e atualiza o estado do próprio nó em tempo real.

### V4 — Escala 3/8/15 sem código (SC-002)

1. Repetir V1–V2 com `compose.8.yaml` e `compose.15.yaml`.
2. **Esperado**: comportamento idêntico em todas as escalas.

### V5 — Resiliência à queda de container (SC-005)

1. Com o cluster no ar, parar o container do líder: `docker stop <projeto>-no3-1`.
2. **Esperado**: os demais elegem novo líder e a sequência continua sem lacunas.

### V6 — Build reproduzível / imagem enxuta (SC-006, FR-011)

1. `docker compose build` e depois inspecionar a imagem.
2. **Esperado**: a imagem sobe um nó funcional e não contém o SDK/ferramentas de build.

## Nota sobre conflito de portas

Se uma porta `800{id}` já estiver em uso no host, o `up` falha com erro explícito. Libere a
porta ou ajuste o mapeamento no arquivo Compose.

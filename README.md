# Chat Distribuído com Ordem Total

Sistema de comunicação de grupo com múltiplos nós independentes que garante **ordem total**
das mensagens difundidas, captura de **estado global consistente** (Chandy-Lamport) e
**eleição de líder** com reeleição automática (Bully). Os nós se comunicam **exclusivamente
por rede** (TCP + JSON); nenhum estado é compartilhado fora do canal de rede.

Especificação completa em [`specs/001-chat-distribuido/`](specs/001-chat-distribuido/).

## Arquitetura (camadas)

- **Rede** (`src/ChatDistribuido/Rede`) — transporte TCP com enquadramento length-prefix +
  serialização JSON. Único canal de coordenação entre nós.
- **Núcleo** (`src/ChatDistribuido/Nucleo`) — relógio vetorial, sequenciador de ordem total,
  buffer de reordenação, eleição (Bully) e snapshot (Chandy-Lamport). Independente de
  sockets e da UI.
- **Serviços** (`src/ChatDistribuido/Servicos`) — `NodeService` (fachada/orquestração) e
  `NodeState` (estado observável do nó).
- **Components** (`src/ChatDistribuido/Components`) — painel Blazor Server do nó (tempo real).

Endereçamento: TCP `5000 + id` (entre nós), web `8000 + id` (painel local), host `127.0.0.1`.

## Pré-requisitos

- SDK do .NET 10.

## Executar (3 nós)

Um processo por nó, cada um em um terminal:

```bash
dotnet run --project src/ChatDistribuido -- --id 1
dotnet run --project src/ChatDistribuido -- --id 2
dotnet run --project src/ChatDistribuido -- --id 3
```

Painéis: http://127.0.0.1:8001, http://127.0.0.1:8002, http://127.0.0.1:8003

## Escalar para 8 ou 15 nós (sem alterar código)

Aponte para o catálogo desejado e suba um processo por id listado:

```bash
dotnet run --project src/ChatDistribuido -- --id 1 --catalogo src/ChatDistribuido/nos.8.json
# ... um processo por id de 1 a 8
```

Catálogos prontos: `nos.json` (3), `nos.8.json` (8), `nos.15.json` (15).

## Executar com Docker

Não é necessário ter o .NET no host — apenas Docker Engine + Docker Compose v2.

Subir o cluster de 3 nós:

```bash
docker compose up --build
```

Painéis no host: http://127.0.0.1:8001, http://127.0.0.1:8002, http://127.0.0.1:8003

Escalar para 8 ou 15 nós (sem alterar código), escolhendo o arquivo Compose:

```bash
docker compose -f compose.8.yaml up --build      # 8 nós  → painéis 8001..8008
docker compose -f compose.15.yaml up --build     # 15 nós → painéis 8001..8015
```

Parar tudo:

```bash
docker compose down
```

Cada nó roda em um container independente; os nós se comunicam apenas pela rede bridge
`chat`. O catálogo (`docker/nos.docker*.json`, com `host` = nome do serviço) é config estática
embutida na imagem, não canal de coordenação. Só as portas de painel (`8000+id`) são
publicadas ao host; o tráfego TCP entre nós (`5000+id`) fica interno.

> **Conflito de portas**: se uma porta `800{id}` já estiver em uso no host, o `up` falha com
> erro explícito. Libere a porta ou ajuste o mapeamento no arquivo Compose.

Detalhes e cenários de validação em
[`specs/002-dockerize/quickstart.md`](specs/002-dockerize/quickstart.md).

## Testes

```bash
dotnet test
```

Cobre ordem total idêntica entre nós, unicast, snapshot consistente sob tráfego, reeleição
sem lacunas de sequência e escala com 8 e 15 nós.

## Cenários de validação

Ver [`specs/001-chat-distribuido/quickstart.md`](specs/001-chat-distribuido/quickstart.md)
(V1–V6).

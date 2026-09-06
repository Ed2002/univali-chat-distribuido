# Phase 0 — Research: Conteinerização do Chat Distribuído

## 1. Imagem: build multi-stage (.NET 10)

- **Decisão**: `Dockerfile` multi-stage — estágio de build com `sdk:10.0` (`dotnet publish
  -c Release`) e estágio de runtime com `aspnet:10.0` contendo só o publish.
- **Rationale**: Imagem final enxuta e reproduzível, sem ferramentas de build (FR-011).
  `aspnet` já traz o runtime ASP.NET Core necessário ao Blazor Server.
- **Alternativas descartadas**: imagem única com SDK (grande, contém build tools);
  self-contained/AOT (ganho irrelevante e mais complexo para este escopo).

## 2. Orquestração: Docker Compose, um serviço por nó

- **Decisão**: Um serviço Compose por nó, todos na mesma rede bridge definida pelo Compose.
  Cada serviço roda a mesma imagem com `command`/`environment` definindo `--id` e o
  `--catalogo`.
- **Rationale**: Um container por nó satisfaz "processo independente" (FR-002); a rede
  bridge é o único canal entre nós (FR-003). Compose sobe/derruba tudo com um comando
  (FR-001, FR-008).
- **Alternativas descartadas**: `docker run` manual por nó (não é um comando único);
  `deploy.replicas` (réplicas anônimas não têm id/endereço estável exigido pelo catálogo).

## 3. Escala 3/8/15 sem alterar código

- **Decisão**: Um arquivo Compose por escala (`compose.yaml` = 3, `compose.8.yaml`,
  `compose.15.yaml`), cada um apontando para o catálogo correspondente em `docker/`.
- **Rationale**: Simples e explícito; selecionar a escala é `-f <arquivo>` (FR-004, SC-002).
  Nenhuma mudança de código.
- **Alternativas descartadas**: gerar o Compose dinamicamente por script (mais partes
  móveis); `--scale` (incompatível com ids/endereços estáveis).

## 4. Endereçamento entre containers (mudança de bind necessária)

- **Problema encontrado**: o código atual liga o listener TCP a `IPAddress.Parse(
  catalogo[id].Host)` e o host web a `http://127.0.0.1:{8000+id}`. Em containers, o host do
  par é um **nome de serviço** (DNS do Compose), não um IP, e o loopback não é acessível pelo
  mapeamento de portas do host.
- **Decisão**: Ligar o listener TCP a `IPAddress.Any` (`0.0.0.0`) e o host web a
  `http://0.0.0.0:{8000+id}`. A conexão de saída continua usando o host do catálogo
  (`ConnectAsync(host, porta)` já resolve nomes de serviço por DNS).
- **Rationale**: Mudança de **endereço de escuta**, não de canal — mantém "comunicação só
  por rede" (Constituição I) e não altera a lógica distribuída. Funciona igualmente local
  (bind em 0.0.0.0 aceita conexões de loopback) e em container.
- **Alternativas descartadas**: `network_mode: host` (não isola portas, frágil no Windows/
  macOS); manter 127.0.0.1 e usar apenas um container (viola "um container por nó").

## 5. Catálogo para containers

- **Decisão**: Catálogos específicos em `docker/nos.docker*.json` com `host` = nome do
  serviço (`no1`, `no2`, …) e `portaTcp` = `5000 + id`. O `nos.json` local (host
  `127.0.0.1`) permanece para execução fora de container.
- **Rationale**: Cada container tem seu próprio namespace de rede, então a porta TCP pode
  repetir; o nome de serviço identifica o destino. Mantém o catálogo consistente com os
  serviços (FR-007).
- **Alternativas descartadas**: reescrever `nos.json` local (quebraria a execução local e os
  testes de integração).

## 6. Exposição dos painéis ao host

- **Decisão**: Publicar a porta web de cada nó (`8000 + id`) para a mesma porta no host
  (`"8001:8001"`, …). Portas TCP entre nós **não** são publicadas (só tráfego interno).
- **Rationale**: Endereço previsível e distinto por nó a partir do host (FR-005, SC-003);
  menor superfície exposta.
- **Ponto resolvido — conflito de portas**: se `8000+id` estiver ocupada no host, o Compose
  falha com erro claro; documentado no quickstart.

## 7. Resiliência e ordem de inicialização

- **Decisão**: Sem `depends_on` bloqueante entre nós; a reconexão TCP sob demanda já lida com
  pares que sobem depois. `restart: unless-stopped` mantém nós vivos após falhas transitórias.
- **Rationale**: Nós que iniciam antes dos pares não falham permanentemente (edge case);
  queda de um container é tratada como queda de nó → reeleição (FR-009, SC-005).
- **Alternativas descartadas**: `depends_on` com healthcheck encadeado (não há ordem correta
  num grafo totalmente conectado; adiciona complexidade sem ganho).

## Resumo de pontos resolvidos

| Pergunta | Resolução |
|----------|-----------|
| Bind entre containers | Listener TCP e web em `0.0.0.0`; conexão de saída por nome de serviço |
| Escala sem código | Um arquivo Compose por escala + catálogo correspondente |
| Catálogo de container | `docker/nos.docker*.json` com host = nome de serviço |
| Painéis no host | Publicar `8000+id`; TCP interno não publicado |
| Ordem de subida | Sem depends_on bloqueante; reconexão sob demanda + restart |

Nenhum item permanece como NEEDS CLARIFICATION.

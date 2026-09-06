# Implementation Plan: Conteinerização do Chat Distribuído

**Branch**: `002-dockerize` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-dockerize/spec.md`

## Summary

Empacotar a aplicação do nó em uma imagem de container reproduzível (build multi-stage) e
orquestrar o cluster com Docker Compose, um container por nó. A escala (3, 8, 15 nós) é
escolhida por configuração de execução (um arquivo Compose por escala), sem alterar o código.
Cada nó se comunica apenas pela rede de containers; o `nos.json` é fornecido como
configuração estática lida na inicialização. Os painéis de cada nó são publicados em portas
distintas do host. São necessárias duas pequenas mudanças de bind no código existente (ligar
o listener TCP e o host web a `0.0.0.0` em vez de `127.0.0.1`) para funcionar entre
containers — sem mudar a lógica distribuída nem violar a constituição.

## Technical Context

**Language/Version**: C# / .NET 10 (aplicação existente, inalterada em lógica)

**Primary Dependencies**: Docker Engine + Docker Compose v2; imagens base oficiais
`mcr.microsoft.com/dotnet/sdk:10.0` (build) e `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)

**Storage**: N/A — estado efêmero em memória por container; nenhum volume de coordenação

**Testing**: Validação por scripts de subida do cluster (3/8/15) + os testes de integração
existentes continuam válidos localmente; verificação de acesso aos painéis pelo host

**Target Platform**: Um único host com Docker (Linux containers); portas de painel mapeadas
ao host

**Project Type**: Adição de infraestrutura/empacotamento a uma aplicação .NET existente

**Performance Goals**: Paridade funcional com a execução local; subida do cluster de 3 nós
em segundos; até 15 containers em um host

**Constraints**: Comunicação exclusivamente pela rede de containers; sem volume/estado
compartilhado como coordenação; catálogo estático coerente com os containers iniciados;
imagem final sem ferramentas de build

**Scale/Scope**: 3, 8 e 15 nós por configuração; execução em host único (orquestração
multi-máquina fora de escopo)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Princípio | Como o plano atende | Status |
|---|-----------|---------------------|--------|
| I | Comunicação Exclusiva por Mensagens de Rede | Nós falam só pela rede bridge do Compose; `nos.json` é config read-only (catálogo), não coordenação; sem volumes de estado compartilhado | ✅ PASS |
| II | Processos Independentes com Estado Privado | Um container por nó, processo e memória isolados; id exclusivo por container | ✅ PASS |
| III | Ordem Total via Sequenciador Eleito | Lógica inalterada; apenas empacotamento | ✅ PASS |
| IV | Corretude dos Algoritmos Distribuídos | Queda de container = queda de nó → reeleição; lógica inalterada | ✅ PASS |
| V | Núcleo Independente da Interface e do Transporte | Bind passa a `0.0.0.0` (endereço, não canal novo); painel continua local ao processo do container | ✅ PASS |

**Resultado**: Todos os gates passam. As mudanças de bind são de endereço de escuta, não
introduzem canal de coordenação — nenhuma violação a justificar.

## Project Structure

### Documentation (this feature)

```text
specs/002-dockerize/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── dockerfile.md         # Contrato da imagem (multi-stage)
│   ├── compose.md            # Contrato dos serviços/rede/portas
│   └── catalogo-docker.md    # Formato do catálogo para containers
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
Dockerfile                      # Build multi-stage da imagem do nó
.dockerignore                   # Exclui bin/obj/specs do contexto de build
compose.yaml                    # Cluster de 3 nós (padrão)
compose.8.yaml                  # Cluster de 8 nós
compose.15.yaml                 # Cluster de 15 nós
docker/
├── nos.docker.json             # Catálogo (hosts = nomes de serviço) para 3 nós
├── nos.docker.8.json           # Catálogo para 8 nós
└── nos.docker.15.json          # Catálogo para 15 nós

src/ChatDistribuido/
├── Rede/TcpTransport.cs        # (ajuste) bind do listener em 0.0.0.0
└── Program.cs                  # (ajuste) host web em 0.0.0.0:{8000+id}
```

**Structure Decision**: A feature adiciona artefatos de empacotamento na raiz do repositório
(`Dockerfile`, `.dockerignore`, arquivos `compose*.yaml`) e catálogos específicos de
container em `docker/`. O código-fonte recebe apenas dois ajustes mínimos de endereço de
bind; a arquitetura de camadas e a lógica distribuída permanecem intactas.

## Complexity Tracking

> Sem violações de constituição — nenhuma justificativa necessária.

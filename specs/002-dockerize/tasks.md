---
description: "Task list for Conteinerização do Chat Distribuído"
---

# Tasks: Conteinerização do Chat Distribuído

**Input**: Design documents from `/specs/002-dockerize/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Sem tarefas de teste automatizado — a validação é de runtime (requer Docker) e
segue os cenários V1–V6 do `quickstart.md`. Os testes .NET existentes continuam válidos para
a execução local e não são afetados.

**Organization**: Tarefas agrupadas por user story para implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências pendentes)
- **[Story]**: User story à qual a tarefa pertence (US1..US4)

## Path Conventions

- Artefatos de container na raiz do repositório; catálogos de container em `docker/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Estrutura básica de arquivos de conteinerização

- [X] T001 [P] Criar a pasta `docker/` para os catálogos de container
- [X] T002 [P] Criar `.dockerignore` na raiz excluindo `bin/`, `obj/`, `.git/`, `specs/`,
  `tests/` do contexto de build (IMG-4)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Ajustes de bind e imagem base — sem eles nenhum cluster conteinerizado funciona

**⚠️ CRITICAL**: Nenhuma user story começa antes desta fase concluir

- [X] T003 Ajustar o bind do listener TCP em `src/ChatDistribuido/Rede/TcpTransport.cs` para
  `IPAddress.Any` (0.0.0.0), mantendo `ConnectAsync(host, porta)` por nome de serviço na saída
- [X] T004 Ajustar o host web em `src/ChatDistribuido/Program.cs` para
  `http://0.0.0.0:{8000+id}` (em vez de `127.0.0.1`), preservando a lógica de id/catálogo
- [X] T005 Confirmar que os testes locais continuam verdes após os ajustes de bind:
  `dotnet test` (execução em `127.0.0.1` deve continuar passando)
- [X] T006 Criar o `Dockerfile` multi-stage na raiz conforme `contracts/dockerfile.md`
  (build `sdk:10.0` → `dotnet publish -c Release`; runtime `aspnet:10.0`;
  `ENTRYPOINT ["dotnet","ChatDistribuido.dll"]`) (depende de T003, T004)
- [X] T007 [P] Criar o catálogo de 3 nós `docker/nos.docker.json` (host = `no{id}`,
  portaTcp = `5000+id`) conforme `contracts/catalogo-docker.md`
- [X] T008 Garantir que os catálogos de container sejam incluídos na imagem (copiar
  `docker/nos.docker*.json` para `/app` no `Dockerfile`) (depende de T006, T007)

**Checkpoint**: Imagem construível e catálogos disponíveis — user stories podem começar

---

## Phase 3: User Story 1 - Subir o cluster em containers com um comando (Priority: P1) 🎯 MVP

**Goal**: `docker compose up --build` sobe o cluster de 3 nós, que forma o chat com ordem total.

**Independent Test**: Em máquina só com Docker, `docker compose up --build` sobe 3 containers
que formam o chat; mensagens de grupo entram em ordem total idêntica (SC-001, SC-004).

### Implementation for User Story 1

- [X] T009 [US1] Criar `compose.yaml` na raiz com 3 serviços (`no1`..`no3`) na rede bridge
  `chat`, cada um com `command` `--id {id} --catalogo /app/nos.docker.json`,
  `restart: unless-stopped`, sem `depends_on` bloqueante, conforme `contracts/compose.md`
  (CMP-1, CMP-2, CMP-6)
- [X] T010 [US1] Definir `build: .` (imagem `chat-distribuido`) no primeiro serviço e reuso
  da mesma imagem nos demais em `compose.yaml`
- [X] T011 [US1] Validar V1/V2 do `quickstart.md`: `docker compose up --build`, difundir
  mensagens e confirmar ordem global idêntica; `docker compose down` encerra tudo (FR-008)

**Checkpoint**: MVP — cluster de 3 nós conteinerizado operando com ordem total

---

## Phase 4: User Story 2 - Escalar 3/8/15 nós sem alterar código (Priority: P2)

**Goal**: Selecionar a escala por arquivo Compose, sem tocar no código.

**Independent Test**: Subir com `compose.8.yaml` e `compose.15.yaml` e confirmar que 8 e 15
containers formam o chat com ordem total (SC-002).

### Implementation for User Story 2

- [X] T012 [P] [US2] Criar o catálogo de 8 nós `docker/nos.docker.8.json` (CAT-3, CAT-4)
- [X] T013 [P] [US2] Criar o catálogo de 15 nós `docker/nos.docker.15.json` (CAT-3, CAT-4)
- [X] T014 [US2] Criar `compose.8.yaml` com 8 serviços apontando para
  `/app/nos.docker.8.json` (depende de T012)
- [X] T015 [US2] Criar `compose.15.yaml` com 15 serviços apontando para
  `/app/nos.docker.15.json` (depende de T013)
- [X] T016 [US2] Validar V4 do `quickstart.md`: subir com `compose.8.yaml` e `compose.15.yaml`
  e confirmar ordem total idêntica em ambas as escalas

**Checkpoint**: US1 e US2 funcionam — escala escolhida por configuração

---

## Phase 5: User Story 3 - Acessar os painéis a partir do host (Priority: P2)

**Goal**: Cada painel de nó acessível no host em porta distinta e previsível.

**Independent Test**: Abrir `http://127.0.0.1:800{id}` de cada nó e confirmar atualização em
tempo real do estado do próprio nó (SC-003).

### Implementation for User Story 3

- [X] T017 [US3] Publicar a porta web `8000+id` de cada serviço (`"{8000+id}:{8000+id}"`) em
  `compose.yaml`, `compose.8.yaml` e `compose.15.yaml`, sem publicar as portas TCP `5000+id`
  (CMP-3, FR-005)
- [X] T018 [US3] Validar V3 do `quickstart.md`: abrir o painel de cada nó pelo host e
  confirmar tempo real e isolamento por nó

**Checkpoint**: US1–US3 funcionais — painéis observáveis pelo host

---

## Phase 6: User Story 4 - Build reproduzível e imagem enxuta (Priority: P3)

**Goal**: Imagem construída do código-fonte sem passos manuais e sem ferramentas de build.

**Independent Test**: `docker compose build` produz imagem que sobe um nó funcional; a imagem
final não contém o SDK (SC-006, FR-011).

### Implementation for User Story 4

- [X] T019 [US4] Validar V6 do `quickstart.md`: `docker compose build`, iniciar um único nó a
  partir da imagem e confirmar que ele ingressa no chat; inspecionar a imagem para confirmar
  ausência do SDK/ferramentas de build (IMG-1)

**Checkpoint**: Todas as user stories funcionais

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentação e validação final

- [X] T020 [P] Adicionar uma seção "Executar com Docker" ao `README.md` (subir 3/8/15,
  acessar painéis, parar) alinhada ao `quickstart.md`
- [X] T021 Validar V5 do `quickstart.md`: parar o container do líder e confirmar reeleição e
  sequência contínua sem lacunas (FR-009, SC-005)
- [X] T022 Documentar no `README.md` a nota sobre conflito de portas do host (edge case)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA todas as user stories
- **User Stories (Phase 3–6)**: dependem da Fundação; depois podem seguir prioridade
  (US1 → US2/US3 → US4)
- **Polish (Phase 7)**: depende das stories desejadas

### User Story Dependencies

- **US1 (P1)**: após Fundação (imagem + bind + catálogo de 3 nós). Sem dependência de outras.
- **US2 (P2)**: após Fundação; reusa o padrão do `compose.yaml` de US1.
- **US3 (P2)**: após Fundação (bind web); adiciona mapeamento de portas aos arquivos Compose.
- **US4 (P3)**: após Fundação (Dockerfile); é validação de qualidade da imagem.

### Parallel Opportunities

- T001/T002 (Setup); T007 na Fundação; T012/T013 (catálogos 8/15) são [P]
- US2 e US3 podem ser trabalhadas em paralelo após a Fundação (arquivos distintos, exceto o
  mapeamento de portas que toca os mesmos Compose — coordenar T014/T015 com T017)

---

## Parallel Example: User Story 2

```bash
# Catálogos de escala juntos (arquivos distintos):
Task: "Criar docker/nos.docker.8.json"
Task: "Criar docker/nos.docker.15.json"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Setup → 2. Foundational (bind + Dockerfile + catálogo 3) → 3. US1 (`compose.yaml`)
4. **PARAR e VALIDAR**: `docker compose up --build` forma o chat com ordem total → demo

### Incremental Delivery

Fundação → US1 (MVP) → US2 (escala) → US3 (painéis) → US4 (imagem) → Polish.

---

## Notes

- [P] = arquivos diferentes, sem dependências pendentes
- Os ajustes de bind (T003/T004) são de endereço de escuta — não alteram a lógica
  distribuída nem violam a constituição (comunicação só por rede)
- Nenhum volume é canal de coordenação; o catálogo é config read-only embutida na imagem
- Validações V1–V6 exigem Docker no ambiente de execução

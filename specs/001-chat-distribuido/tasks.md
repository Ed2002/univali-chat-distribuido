---
description: "Task list for Chat Distribuído com Ordem Total"
---

# Tasks: Chat Distribuído com Ordem Total

**Input**: Design documents from `/specs/001-chat-distribuido/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/wire-protocol.md

**Tests**: Test tasks são incluídos — o plano define um projeto de testes e os critérios de
sucesso (ordem total idêntica, snapshot consistente, reeleição sem lacunas) são,
essencialmente, cenários verificáveis.

**Organization**: Tarefas agrupadas por user story para implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências pendentes)
- **[Story]**: User story à qual a tarefa pertence (US1..US5)

## Path Conventions

- Projeto único .NET: `src/ChatDistribuido/`, testes em `tests/ChatDistribuido.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicialização do projeto e estrutura básica

- [X] T001 Criar solução e estrutura de projetos: `ChatDistribuido.sln`, projeto web
  `src/ChatDistribuido/ChatDistribuido.csproj` (Blazor Server, `net10.0`) e projeto de teste
  `tests/ChatDistribuido.Tests/ChatDistribuido.Tests.csproj` (xUnit), com as pastas
  `src/ChatDistribuido/{Rede,Nucleo,Servicos,Components}` e `tests/ChatDistribuido.Tests/{unit,integration}`
- [X] T002 Configurar `src/ChatDistribuido/ChatDistribuido.csproj` para ASP.NET Core + Blazor
  Server e habilitar `System.Text.Json`; referenciar o projeto no `.sln`
- [X] T003 [P] Adicionar `.editorconfig` e habilitar analyzers/nullable em ambos os projetos

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infra de rede, serialização, catálogo e núcleo base que TODAS as stories usam

**⚠️ CRITICAL**: Nenhuma user story começa antes desta fase concluir

- [X] T004 [P] Definir tipos de mensagem e envelope em `src/ChatDistribuido/Rede/Envelope.cs`
  conforme `contracts/wire-protocol.md` (campos `tipo`, `de`, `para`, `vc`, `payload` e enum
  de tipos: UNICAST, GROUP, SEQUENCED, HEARTBEAT, ELECTION, OK, COORDINATOR, LAST_SEQ_QUERY,
  LAST_SEQ_REPLY, MARKER)
- [X] T005 [P] Configurar serialização JSON em `src/ChatDistribuido/Rede/JsonSerializerConfig.cs`
  (opções `System.Text.Json` compartilhadas para envelopes)
- [X] T006 [P] Modelar o catálogo e o carregador em `src/ChatDistribuido/Nucleo/CatalogoNos.cs`
  (registro `id/host/portaTcp`) que lê `nos.json` na inicialização
- [X] T007 Implementar transporte TCP em `src/ChatDistribuido/Rede/IMessageChannel.cs` e
  `src/ChatDistribuido/Rede/TcpTransport.cs`: listener em `5000+id`, conexões cliente por par,
  enquadramento length-prefix (int32) + payload JSON, entrega FIFO por canal, envio unicast e
  evento de recepção de envelopes (depende de T004, T005)
- [X] T008 [P] Implementar relógio vetorial em `src/ChatDistribuido/Nucleo/VectorClock.cs`
  (incremento local, merge por máximo, tamanho N do catálogo)
- [X] T009 Criar estado observável do nó em `src/ChatDistribuido/Servicos/NodeState.cs` (ordem
  local de emissão, ordem global de entrega, VC, buffer, líder atual) com notificação de
  mudança para a UI
- [X] T010 Criar `src/ChatDistribuido/Servicos/NodeService.cs` (esqueleto): injeta transporte +
  catálogo, mantém `NodeState`, roda o laço de despacho por `tipo` de envelope e expõe
  eventos para o painel (depende de T007, T008, T009)
- [X] T011 Compor a inicialização em `src/ChatDistribuido/Program.cs`: ler `--id`, carregar
  `nos.json`, subir TCP em `5000+id` e o host web/Blazor em `8000+id` (depende de T010)
- [X] T012 [P] Criar catálogo de exemplo `src/ChatDistribuido/nos.json` (3 nós) conforme
  `quickstart.md`

**Checkpoint**: Fundação pronta — user stories podem começar

---

## Phase 3: User Story 1 - Difusão em ordem total (Priority: P1) 🎯 MVP

**Goal**: Toda mensagem de grupo é entregue na mesma sequência (ordem total) em todos os nós.

**Independent Test**: Difundir várias mensagens de grupo de nós diferentes e confirmar que a
ordem global de entrega é idêntica em todos os nós, sem lacunas nem duplicatas (SC-001).

### Tests for User Story 1 ⚠️

- [X] T013 [P] [US1] Teste unitário do buffer de reordenação em
  `tests/ChatDistribuido.Tests/unit/DeliveryBufferTests.cs` (entrega estrita por seq, retém
  seq futuro, sem lacunas/duplicatas)
- [X] T014 [P] [US1] Teste unitário do sequenciador em
  `tests/ChatDistribuido.Tests/unit/TotalOrderSequencerTests.cs` (seq monotônico único)
- [X] T015 [US1] Teste de integração multi-nó em
  `tests/ChatDistribuido.Tests/integration/OrdemTotalTests.cs`: sobe 3 nós em `127.0.0.1`,
  difunde mensagens concorrentes e verifica filas de entrega idênticas (SC-001)

### Implementation for User Story 1

- [X] T016 [P] [US1] Implementar `src/ChatDistribuido/Nucleo/DeliveryBuffer.cs` (mapa
  seq→mensagem, `proximoSeqEsperado`, entrega quando contíguo)
- [X] T017 [US1] Implementar `src/ChatDistribuido/Nucleo/TotalOrderSequencer.cs`: atribuição de
  `seq` pelo líder com bootstrap `líderInicial = maior id do catálogo`; expõe `proximoSeq`
  (depende de T016)
- [X] T018 [US1] Implementar o fluxo de difusão de grupo em
  `src/ChatDistribuido/Servicos/NodeService.cs`: enviar `GROUP` ao líder, líder atribuir `seq`
  e redifundir `SEQUENCED` a todos (depende de T017)
- [X] T019 [US1] Implementar a entrega ordenada em `src/ChatDistribuido/Servicos/NodeService.cs`:
  ao receber `SEQUENCED`, bufferizar, entregar em ordem de `seq`, atualizar VC e anexar em
  `NodeState.OrdemGlobalEntrega` (depende de T018, T008)

**Checkpoint**: US1 funcional e testável de forma independente (MVP)

---

## Phase 4: User Story 2 - Mensagem privada (unicast) (Priority: P1)

**Goal**: Enviar mensagem privada a um nó específico pelo id, entregue só ao destinatário.

**Independent Test**: Enviar unicast de um nó a outro e confirmar que apenas o destinatário
recebe, preservando FIFO por canal (SC-002).

### Tests for User Story 2 ⚠️

- [X] T020 [P] [US2] Teste de integração em
  `tests/ChatDistribuido.Tests/integration/UnicastTests.cs`: unicast do nó 1 ao nó 3 chega só
  ao nó 3; múltiplos unicasts preservam ordem de emissão (SC-002)

### Implementation for User Story 2

- [X] T021 [US2] Implementar envio e recepção de `UNICAST` em
  `src/ChatDistribuido/Servicos/NodeService.cs`: enviar ao `para`, registrar em
  `NodeState.OrdemLocalEmissao` e entregar apenas ao destinatário

**Checkpoint**: US1 e US2 funcionam de forma independente

---

## Phase 5: User Story 3 - Painel de observação do nó (Priority: P2)

**Goal**: Painel Blazor por nó, em tempo real, mostrando o estado apenas do próprio processo.

**Independent Test**: Emitir/entregar mensagens e confirmar que cada painel reflete em tempo
real ordem local, ordem global, VC e buffer, sem cruzar estado entre nós (SC-006).

### Implementation for User Story 3

- [X] T022 [P] [US3] Criar shell da UI em `src/ChatDistribuido/Components/App.razor` e roteamento
  Blazor Server
- [X] T023 [US3] Criar `src/ChatDistribuido/Components/Pages/Painel.razor`: controles para envio
  unicast (por id) e envio de grupo, e listas de ordem local de emissão e ordem global de
  entrega, assinando `NodeState` via eventos (depende de T009)
- [X] T024 [US3] Exibir no `Painel.razor` o relógio vetorial atual e o buffer de mensagens
  pendentes, atualizando em tempo real (depende de T023)

**Checkpoint**: US1–US3 funcionais de forma independente

---

## Phase 6: User Story 4 - Captura de estado global consistente (Priority: P2)

**Goal**: Capturar sob demanda um retrato global consistente sem parar o chat (Chandy-Lamport).

**Independent Test**: Disparar a captura com mensagens em trânsito e confirmar retrato
consistente enquanto o chat segue operando (SC-003).

### Tests for User Story 4 ⚠️

- [X] T025 [P] [US4] Teste de integração em
  `tests/ChatDistribuido.Tests/integration/SnapshotTests.cs`: dispara snapshot sob tráfego e
  valida corte consistente (nenhum efeito sem causa) e continuidade do chat (SC-003)

### Implementation for User Story 4

- [X] T026 [P] [US4] Implementar `src/ChatDistribuido/Nucleo/ChandyLamportSnapshot.cs`: gravação
  de estado local, marcadores por canal, gravação de estado dos canais, conclusão ao receber
  todos os marcadores
- [X] T027 [US4] Integrar `MARKER` no `src/ChatDistribuido/Servicos/NodeService.cs`: iniciar
  snapshot, propagar marcadores por todos os canais de saída e coletar estados (depende de
  T026)
- [X] T028 [US4] Adicionar disparo da captura e exibição do resultado no
  `src/ChatDistribuido/Components/Pages/Painel.razor` (depende de T027, T023)

**Checkpoint**: US1–US4 funcionais de forma independente

---

## Phase 7: User Story 5 - Eleição de líder e reeleição (Priority: P2)

**Goal**: Manter um único líder; ao cair, eleger novo líder (Bully) e retomar `seq` sem lacunas.

**Independent Test**: Encerrar o líder sob tráfego e confirmar novo líder eleito e `seq`
contínuo sem lacunas nem duplicatas, com no máximo um líder (SC-004).

### Tests for User Story 5 ⚠️

- [X] T029 [P] [US5] Teste de integração em
  `tests/ChatDistribuido.Tests/integration/ReeleicaoTests.cs`: mata o líder sob tráfego,
  verifica novo líder (maior id ativo) e `seq` sem lacunas/duplicatas (SC-004)

### Implementation for User Story 5

- [X] T030 [P] [US5] Implementar `src/ChatDistribuido/Nucleo/BullyElection.cs`: fluxo
  ELECTION/OK/COORDINATOR convergindo para o maior id ativo
- [X] T031 [US5] Implementar heartbeat do líder e detecção por timeout no
  `src/ChatDistribuido/Servicos/NodeService.cs` (envio periódico de `HEARTBEAT`; timeout dispara
  eleição) (depende de T030)
- [X] T032 [US5] Implementar retomada de sequência pós-reeleição no
  `src/ChatDistribuido/Servicos/NodeService.cs`: novo líder envia `LAST_SEQ_QUERY`, coleta
  `LAST_SEQ_REPLY` e define `proximoSeq = max(últimoSeqEntregue) + 1` (depende de T031, T017)
- [X] T033 [US5] Exibir o líder atual e o estado de eleição no
  `src/ChatDistribuido/Components/Pages/Painel.razor` (depende de T031, T023)

**Checkpoint**: Todas as user stories funcionais de forma independente

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Escala, documentação e validação final

- [X] T034 [P] Adicionar catálogos de exemplo para 8 e 15 nós
  (`src/ChatDistribuido/nos.8.json`, `src/ChatDistribuido/nos.15.json`) e documentar seleção
- [X] T035 [P] Teste de integração de escala em
  `tests/ChatDistribuido.Tests/integration/EscalaTests.cs`: repetir ordem total com 8 e 15 nós
  (SC-005)
- [X] T036 [P] Adicionar `README.md` com instruções de execução alinhadas ao `quickstart.md`
- [X] T037 Endurecer tratamento de erros/log de rede (par indisponível não bloqueia entrega aos
  ativos — FR-016) em `src/ChatDistribuido/Rede/TcpTransport.cs` e
  `src/ChatDistribuido/Servicos/NodeService.cs`
- [X] T038 Executar a validação do `quickstart.md` (cenários V1–V6)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA todas as user stories
- **User Stories (Phase 3–7)**: dependem da Fundação; depois podem ser paralelas ou seguir
  prioridade (US1/US2 P1 → US3/US4/US5 P2)
- **Polish (Phase 8)**: depende das stories desejadas

### User Story Dependencies

- **US1 (P1)**: após Fundação. Usa bootstrap `líder = maior id` (não depende de US5).
- **US2 (P1)**: após Fundação. Independente de US1.
- **US3 (P2)**: após Fundação; observa `NodeState` (mais rico com US1/US2, mas testável só).
- **US4 (P2)**: após Fundação. Independente das demais.
- **US5 (P2)**: após Fundação; a retomada de `seq` (T032) integra com o sequenciador de US1.

### Within Each User Story

- Testes escritos e falhando antes da implementação
- Núcleo (models) antes de serviços; serviços antes de UI

### Parallel Opportunities

- T003 no Setup; T004/T005/T006/T008/T012 na Fundação (arquivos distintos)
- Concluída a Fundação, US1–US5 podem ser desenvolvidas em paralelo por pessoas diferentes
- Testes marcados [P] de uma story rodam juntos (T013/T014; etc.)

---

## Parallel Example: User Story 1

```bash
# Testes de US1 juntos:
Task: "Teste unitário do buffer em tests/.../unit/DeliveryBufferTests.cs"
Task: "Teste unitário do sequenciador em tests/.../unit/TotalOrderSequencerTests.cs"

# Núcleo de US1 (arquivos distintos):
Task: "Implementar DeliveryBuffer em src/ChatDistribuido/Nucleo/DeliveryBuffer.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational, crítica) → 3. Phase 3 (US1)
4. **PARAR e VALIDAR**: filas de entrega idênticas em todos os nós → demo do MVP

### Incremental Delivery

Fundação → US1 (MVP) → US2 → US3 → US4 → US5 → Polish, cada story testada isoladamente.

### Parallel Team Strategy

Após a Fundação: Dev A → US1, Dev B → US2/US3, Dev C → US4, Dev D → US5.

---

## Notes

- [P] = arquivos diferentes, sem dependências pendentes
- Bootstrap de líder pelo maior id mantém US1 independente de US5
- Comunicação exclusivamente por rede (Constituição, Princípio I) — sem estado compartilhado
- Commit após cada tarefa ou grupo lógico; validar cada story no checkpoint

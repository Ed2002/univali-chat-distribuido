---
description: "Task list for Interface de Chat com Balões e Conversas Privadas"
---

# Tasks: Interface de Chat com Balões e Conversas Privadas

**Input**: Design documents from `/specs/003-chat-bubbles-ui/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-chat.md

**Tests**: Inclui um teste unitário para o agrupamento de conversas privadas (`ConversasTests`);
a validação visual segue os cenários V1–V6 do `quickstart.md`. Os testes existentes continuam
válidos — a feature não altera a semântica de entrega.

**Organization**: Tarefas agrupadas por user story para implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências pendentes)
- **[Story]**: User story à qual a tarefa pertence (US1..US4)

## Path Conventions

- App .NET existente: UI em `src/ChatDistribuido/Components`, estilos em
  `src/ChatDistribuido/wwwroot`, estado em `src/ChatDistribuido/Servicos`

---

## Phase 1: Setup

**Purpose**: Baseline antes das mudanças de UI

- [X] T001 Confirmar baseline verde antes de alterar a UI: `dotnet build ChatDistribuido.slnx`
  e `dotnet test` (14 testes existentes devem passar)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Auxiliares de leitura e o esqueleto do layout de chat — base de US1 e US2

**⚠️ CRITICAL**: Nenhuma user story começa antes desta fase concluir

- [X] T002 [P] Adicionar auxiliar `PrivadasCom(int peer)` em
  `src/ChatDistribuido/Servicos/NodeState.cs` (filtra `Privadas()` pelo par
  `Enviada ? Para : De`, na ordem de registro)
- [X] T003 [P] Expor os ids dos outros nós em `src/ChatDistribuido/Servicos/NodeService.cs`
  (ex.: `IReadOnlyList<int> OutrosNos` derivado do catálogo, exceto o próprio) para a lista
  de conversas
- [X] T004 Reescrever o esqueleto do layout em `src/ChatDistribuido/Components/Pages/Painel.razor`:
  duas áreas (lista de conversas à esquerda, conversa ativa à direita com composer) e uma
  gaveta "Observação técnica" recolhível; manter a assinatura de `NodeState.Changed`
- [X] T005 Adicionar estilos base de chat em `src/ChatDistribuido/wwwroot/app.css` (layout de
  duas colunas, área de mensagens rolável, composer, gaveta técnica), reusando os tokens do
  tema escuro existente

**Checkpoint**: Esqueleto de chat pronto — user stories podem começar

---

## Phase 3: User Story 1 - Chat de grupo com balões (Priority: P1) 🎯 MVP

**Goal**: A conversa "Grupo" exibe as mensagens de grupo como balões com o remetente, na
ordem total, com rolagem automática.

**Independent Test**: Difundir mensagens de nós diferentes e ver balões identificando o
remetente, na mesma ordem em todos os nós (SC-001, SC-003).

### Implementation for User Story 1

- [X] T006 [US1] Renderizar a conversa "Grupo" em
  `src/ChatDistribuido/Components/Pages/Painel.razor` a partir de `OrdemGlobal()` ordenada por
  `seq`, cada mensagem como um balão com `DeOriginal` como remetente (UI-3, UI-6)
- [X] T007 [US1] Ligar o composer ao envio de grupo (`Node.EnviarGrupo`) quando a conversa
  ativa é o Grupo, em `src/ChatDistribuido/Components/Pages/Painel.razor` (UI-10)
- [X] T008 [US1] Implementar rolagem automática para a última mensagem da conversa ativa ao
  atualizar/enviar (via JS interop mínimo em `Painel.razor`) (UI-9, FR-008)

**Checkpoint**: MVP — chat de grupo com balões funcionando

---

## Phase 4: User Story 2 - Conversas privadas separadas do grupo (Priority: P1)

**Goal**: Lista de conversas (Grupo + um PV por nó); PVs mostram só suas diretas; direta
nunca aparece no Grupo.

**Independent Test**: Enviar direta de A para B → aparece só no PV A↔B (nos dois lados) e
nunca no Grupo (SC-002).

### Tests for User Story 2 ⚠️

- [X] T009 [P] [US2] Teste unitário em
  `tests/ChatDistribuido.Tests/unit/ConversasTests.cs`: registrar privadas enviadas/recebidas
  e de grupo; verificar que `PrivadasCom(peer)` agrupa corretamente por par e que mensagens
  de grupo não entram nas privadas (separação Grupo × Privado)

### Implementation for User Story 2

- [X] T010 [US2] Construir a lista de conversas em
  `src/ChatDistribuido/Components/Pages/Painel.razor`: "Grupo" + um PV por id de
  `Node.OutrosNos`, com seleção da conversa ativa (UI-1, UI-2)
- [X] T011 [US2] Renderizar a conversa privada ativa a partir de `PrivadasCom(peer)` como
  balões, em `src/ChatDistribuido/Components/Pages/Painel.razor` (UI-4, UI-5)
- [X] T012 [US2] Rotear o composer conforme a conversa ativa: Grupo → difusão; PV →
  `Node.EnviarUnicast(peer, ...)`, em `src/ChatDistribuido/Components/Pages/Painel.razor`
  (UI-11)
- [X] T013 [US2] Impedir PV/envio ao próprio nó em
  `src/ChatDistribuido/Components/Pages/Painel.razor` (UI-12, FR-013)

**Checkpoint**: US1 e US2 funcionais — grupo e PVs separados

---

## Phase 5: User Story 3 - Distinção visual do remetente (Priority: P2)

**Goal**: Balões próprios distintos dos recebidos, com autoria sempre visível.

**Independent Test**: Enviar e receber; balões próprios têm alinhamento/estilo distinto e os
recebidos mostram o remetente (SC-004).

### Implementation for User Story 3

- [X] T014 [US3] Marcar cada balão como próprio/recebido em
  `src/ChatDistribuido/Components/Pages/Painel.razor` (`DeOriginal==Id` no Grupo; `Enviada` no
  PV) e exibir rótulo do remetente nos recebidos, com o relógio vetorial discreto (UI-7, UI-8)
- [X] T015 [US3] Estilizar balões próprios × recebidos em
  `src/ChatDistribuido/wwwroot/app.css` (alinhamento, cor de acento para os próprios, rótulo
  do remetente)

**Checkpoint**: US1–US3 funcionais — chat com autoria clara

---

## Phase 6: User Story 4 - Aviso de não lida em conversa não ativa (Priority: P3)

**Goal**: Conversa não ativa que recebe mensagem mostra indicador; abri-la limpa.

**Independent Test**: Estar no Grupo e receber direta → PV daquele nó marca não lida; abrir
limpa (SC-006).

### Implementation for User Story 4

- [X] T016 [US4] Manter estado local de não lida por conversa em
  `src/ChatDistribuido/Components/Pages/Painel.razor` (contagem vista por conversa; marca ao
  chegar mensagem em conversa não ativa; limpa ao abrir) (UI-13..UI-15, FR-010/FR-011)
- [X] T017 [US4] Exibir o badge de não lida na lista de conversas em
  `src/ChatDistribuido/wwwroot/app.css` e no markup da lista em `Painel.razor`

**Checkpoint**: Todas as user stories funcionais

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Observação técnica (constituição), verificação e documentação

- [X] T018 Preencher a gaveta "Observação técnica" em
  `src/ChatDistribuido/Components/Pages/Painel.razor` com ordem local de emissão, relógio
  vetorial atual, buffer de seqs pendentes e o disparo/exibição de captura de estado global
  (FR-012, SC-007, UI-16)
- [X] T019 Rodar `dotnet build` e `dotnet test` e garantir tudo verde (novo `ConversasTests`
  + os 14 existentes)
- [X] T020 [P] Atualizar o `README.md` com a descrição da nova experiência de chat (grupo,
  PVs, observação técnica secundária)
- [X] T021 Validar os cenários V1–V6 do `quickstart.md` (subir 3 nós e conferir grupo, PVs,
  distinção, não lida e observação técnica)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA as user stories
- **User Stories (Phase 3–6)**: dependem da Fundação; US1 → US2 → US3 → US4 por prioridade
- **Polish (Phase 7)**: depende das stories desejadas

### User Story Dependencies

- **US1 (P1)**: após Fundação; usa o esqueleto com a conversa Grupo como padrão. MVP.
- **US2 (P1)**: após Fundação; adiciona lista de conversas, PVs e roteamento. Usa `PrivadasCom`.
- **US3 (P2)**: refina a apresentação dos balões de US1/US2.
- **US4 (P3)**: adiciona não lida sobre a lista de conversas de US2.

### Parallel Opportunities

- T002 (NodeState) e T003 (NodeService) são [P] — arquivos distintos.
- T009 (ConversasTests) é [P] — arquivo de teste próprio.
- A maioria das tarefas de UI toca `Painel.razor`/`app.css` → executar em sequência para
  evitar conflitos no mesmo arquivo.

---

## Parallel Example: Foundational

```bash
# Auxiliares de leitura (arquivos distintos):
Task: "Adicionar PrivadasCom(peer) em src/ChatDistribuido/Servicos/NodeState.cs"
Task: "Expor OutrosNos em src/ChatDistribuido/Servicos/NodeService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Setup → 2. Foundational (auxiliares + esqueleto) → 3. US1 (chat de grupo com balões)
4. **PARAR e VALIDAR**: balões de grupo na ordem total → demo do MVP

### Incremental Delivery

Fundação → US1 (MVP) → US2 (PVs) → US3 (estilo) → US4 (não lida) → Polish (técnico + docs).

---

## Notes

- [P] = arquivos diferentes, sem dependências pendentes
- Feature de apresentação: nenhuma mudança em `Rede/` ou `Nucleo/`, nem na semântica de entrega
- Separação Grupo × Privado é garantida pela estrutura do estado (coleções distintas)
- "Não lida" e conversa ativa são estado local do painel — não trafegam entre nós

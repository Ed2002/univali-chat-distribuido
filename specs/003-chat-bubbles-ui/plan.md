# Implementation Plan: Interface de Chat com Balões e Conversas Privadas

**Branch**: `003-chat-bubbles-ui` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-chat-bubbles-ui/spec.md`

## Summary

Reformular o painel Blazor do nó em uma experiência de **chat**: uma lista de conversas
(Grupo + um PV por nó do catálogo), a conversa ativa exibida como **balões** que identificam
o remetente, mensagens do próprio nó distintas das recebidas, rolagem automática e indicador
de não lida por conversa. Mensagens de grupo aparecem só no Grupo (na ordem total);
mensagens diretas aparecem só no PV do nó correspondente e nunca no Grupo. A observação
técnica (ordem local, relógio vetorial, buffer, snapshot) fica acessível de forma secundária,
como exige a constituição. É uma feature de **apresentação**: nenhuma mudança na lógica
distribuída (unicast, ordem total, eleição, snapshot). Os dados já existem no `NodeState`
(`OrdemGlobal` = grupo; `Privadas` com direção = PVs); acrescenta-se apenas um pequeno
auxiliar de agrupamento por par e o estado de "não lida", ambos locais ao painel.

## Technical Context

**Language/Version**: C# / .NET 10 (aplicação existente)

**Primary Dependencies**: Blazor Server (SignalR), `NodeState`/`NodeService` já existentes

**Storage**: N/A — estado em memória; histórico de chat reflete a sessão do processo; "não
lida" é estado efêmero local ao painel

**Testing**: xUnit para o auxiliar de agrupamento de conversas (`PrivadasCom`); validação de
UI pelos cenários V1–V6 do quickstart; os testes de integração existentes seguem válidos

**Target Platform**: Painel web por nó (Blazor Server), local ao processo do nó

**Project Type**: Alteração de UI/apresentação sobre a aplicação .NET existente

**Performance Goals**: Atualização de chat em tempo real ao entregar/enviar; suportar até 15
conversas (Grupo + 14 pares) sem degradar a navegação

**Constraints**: Painel fala apenas com o próprio processo; nenhum estado cruza entre nós
fora do canal de rede; a semântica de entrega não muda; ordem total preservada na exibição
do Grupo

**Scale/Scope**: Um painel por nó; até 15 conversas por painel

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Princípio | Como o plano atende | Status |
|---|-----------|---------------------|--------|
| I | Comunicação Exclusiva por Mensagens de Rede | Feature só de UI; envio de grupo/unicast continua pela mesma via de rede; "não lida" é local | ✅ PASS |
| II | Processos Independentes com Estado Privado | Nenhuma mudança; painel lê o `NodeState` do próprio processo | ✅ PASS |
| III | Ordem Total via Sequenciador Eleito | Grupo exibido a partir de `OrdemGlobal` (ordem total); exibição não altera a ordem | ✅ PASS |
| IV | Corretude dos Algoritmos Distribuídos | Sem mudança na lógica de eleição/snapshot | ✅ PASS |
| V | Núcleo Independente da Interface e do Transporte | Observação técnica mantida (FR-012); painel só fala com o próprio processo; núcleo intocado | ✅ PASS |

**Resultado**: Todos os gates passam. Feature puramente de apresentação — nenhuma violação.

## Project Structure

### Documentation (this feature)

```text
specs/003-chat-bubbles-ui/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (modelo de visão da UI)
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── ui-chat.md        # Contrato da UI: conversas, balões, roteamento de envio, não lida
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
src/ChatDistribuido/
├── Servicos/NodeState.cs           # (+) auxiliares de leitura: pares em conversa, PrivadasCom(peer)
└── Components/Pages/Painel.razor    # (reescrita) layout de chat: lista de conversas + balões + técnico
src/ChatDistribuido/wwwroot/app.css  # (+) estilos de chat (balões, lista de conversas, badges de não lida)

tests/ChatDistribuido.Tests/
└── unit/ConversasTests.cs          # (+) agrupamento privado por par e separação do grupo
```

**Structure Decision**: A feature concentra-se em `Painel.razor` (reescrita para layout de
chat) e `app.css` (estilos de balões/lista de conversas), com um pequeno auxiliar de leitura
em `NodeState` para agrupar mensagens privadas por par sem espalhar lógica na view. O núcleo
distribuído e o transporte permanecem intocados.

## Complexity Tracking

> Sem violações de constituição — nenhuma justificativa necessária.

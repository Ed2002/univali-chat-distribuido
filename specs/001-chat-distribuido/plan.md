# Implementation Plan: Chat Distribuído com Ordem Total

**Branch**: `001-chat-distribuido` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-chat-distribuido/spec.md`

## Summary

Sistema de comunicação de grupo com N processos independentes (nós) que trocam mensagens
exclusivamente por rede. Cada nó envia mensagens privadas (unicast) e de grupo (broadcast),
e todas as mensagens de grupo são entregues em **ordem total** idêntica em todos os nós. A
ordem total é garantida por um **sequenciador eleito** (líder) que atribui números de
sequência globais; um **relógio vetorial** é mantido em paralelo para registrar
causalidade. A eleição usa o **algoritmo do Valentão (Bully)** com heartbeat/timeout e
reeleição automática. O **estado global consistente** é capturado sob demanda pelo
algoritmo de **Chandy-Lamport**. Cada nó expõe um painel web local em tempo real que
observa apenas o seu próprio estado. O sistema sobe com 3, 8 e 15 nós a partir de um
catálogo estático `nos.json`, sem alteração de código.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core, Blazor Server (painel em tempo real via SignalR),
`System.Net.Sockets` (TCP), `System.Text.Json` (serialização de mensagens)

**Storage**: N/A — estado apenas em memória privada de cada processo (persistência entre
execuções está fora de escopo; nenhum armazenamento compartilhado é permitido)

**Testing**: xUnit para o núcleo (relógio vetorial, ordem total, eleição, snapshot) de
forma determinística; testes de integração multi-nó em `127.0.0.1` validando ordem total,
snapshot e reeleição

**Target Platform**: Processo de console/servidor .NET multiplataforma (validado em
Windows); um processo por nó em `127.0.0.1`

**Project Type**: Aplicação distribuída multi-processo com UI web local por nó (arquitetura
em camadas: Rede · Núcleo · Serviços · Components)

**Performance Goals**: Escala-alvo de até 15 nós; latência de entrega adequada para
demonstração interativa (ordem de dezenas a centenas de ms por difusão); a corretude
(ordem total sem lacunas) prevalece sobre throughput

**Constraints**: Comunicação exclusivamente por mensagens de rede; sem memória
compartilhada, banco, arquivo compartilhado, variável global entre processos ou serviço
externo como canal de coordenação; endereçamento TCP `5000 + id`, web `8000 + id`

**Scale/Scope**: Configurações de 3, 8 e 15 nós a partir de `nos.json`, sem alteração de
código; falhas do tipo parada (crash-stop), sem falhas bizantinas; catálogo estático (sem
ingresso dinâmico nesta versão)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Princípio | Como o plano atende | Status |
|---|-----------|---------------------|--------|
| I | Comunicação Exclusiva por Mensagens de Rede | Único canal entre nós é TCP+JSON; broadcast = unicast a cada nó do catálogo; nenhum estado compartilhado | ✅ PASS |
| II | Processos Independentes com Estado Privado | Um processo por nó, estado só em memória; `nos.json` lido só na inicialização, nunca como coordenação | ✅ PASS |
| III | Ordem Total via Sequenciador Eleito | Líder atribui seq global monotônico; entrega estrita por seq com buffer de reordenação; relógio vetorial em paralelo | ✅ PASS |
| IV | Corretude dos Algoritmos Distribuídos | Bully (heartbeat/timeout + reeleição) e Chandy-Lamport (marcadores por canal) seguem definições canônicas | ✅ PASS |
| V | Núcleo Independente da Interface e do Transporte | Núcleo puro (sem sockets/UI), acessado pela camada de Serviços; painel Blazor fala só com o próprio processo | ✅ PASS |

**Resultado**: Todos os gates passam. Nenhuma violação a justificar em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-chat-distribuido/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── wire-protocol.md  # Contrato das mensagens de rede (envelope + tipos)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
└── ChatDistribuido/
    ├── Program.cs                 # Composição: lê nos.json + id, sobe TCP e web
    ├── nos.json                   # Catálogo estático de nós (id → host:portaTCP)
    ├── Rede/                      # Camada de transporte
    │   ├── TcpTransport.cs        # Servidor/cliente TCP, um canal FIFO por par de nós
    │   ├── JsonSerializerConfig.cs
    │   └── IMessageChannel.cs     # Envio unicast + recepção de envelopes
    ├── Nucleo/                    # Núcleo independente de rede e UI
    │   ├── VectorClock.cs         # Relógio vetorial
    │   ├── TotalOrderSequencer.cs # Atribuição de seq (líder) + fila de entrega
    │   ├── DeliveryBuffer.cs      # Buffer de reordenação por seq
    │   ├── BullyElection.cs       # Eleição do Valentão + heartbeat/timeout
    │   └── ChandyLamportSnapshot.cs # Marcadores, gravação de estado e de canais
    ├── Servicos/                  # Fachada do núcleo + ponte de eventos p/ UI
    │   ├── NodeService.cs         # Orquestra Rede + Nucleo; expõe operações e eventos
    │   └── NodeState.cs           # Snapshot observável do estado do nó p/ o painel
    └── Components/                # Painel Blazor Server do nó
        ├── App.razor
        └── Pages/
            └── Painel.razor       # Envio unicast/grupo, ordem local/global, VC, buffer

tests/
└── ChatDistribuido.Tests/
    ├── unit/                      # Núcleo determinístico (VC, sequenciador, buffer)
    └── integration/              # Multi-nó em 127.0.0.1 (ordem total, snapshot, reeleição)
```

**Structure Decision**: Projeto único .NET (`src/ChatDistribuido`) organizado nas quatro
camadas da constituição — **Rede**, **Núcleo**, **Serviços**, **Components** — com o Núcleo
livre de dependências de sockets e UI, permitindo testes unitários determinísticos. Os
testes de integração sobem múltiplos processos/instâncias em `127.0.0.1` para validar os
critérios de sucesso ponta a ponta.

## Complexity Tracking

> Sem violações de constituição — nenhuma justificativa necessária.

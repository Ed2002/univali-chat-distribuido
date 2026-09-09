# Phase 1 — Data Model: Interface de Chat com Balões e Conversas Privadas

Modelo de **visão** da UI. Deriva do estado já existente (`NodeState`); nada novo é
persistido nem trafega na rede.

## Entidades de visão

### Conversa

Um canal exibível na interface.

| Campo | Tipo | Descrição / Regra |
|-------|------|--------------------|
| `Tipo` | enum {Grupo, Privado} | Grupo (todas as de grupo) ou PV com um nó. |
| `PeerId` | int? | Nulo no Grupo; id do outro nó no PV. |
| `Titulo` | string | "Grupo" ou "Nó {peer}". |
| `NaoLida` | bool (local) | Há mensagem nova não vista nesta conversa. |

Regras: a lista é `Grupo` + uma `Privado` por nó do catálogo, exceto o próprio (FR-009,
FR-013). Ordem estável por id.

### Mensagem de chat (balão)

Projeção de uma mensagem para exibição.

| Campo | Tipo | Descrição / Regra |
|-------|------|--------------------|
| `RemetenteId` | int | Quem enviou (grupo: `DeOriginal`; PV: `De`/`self`). |
| `Propria` | bool | `RemetenteId == NodeState.Id`. Alinhamento/estilo do balão. |
| `Conteudo` | string | Texto da mensagem. |
| `Ordem` | long/int | Grupo: `seq` (ordem total). PV: ordem de registro. |
| `Meta` | string | Relógio vetorial da mensagem (exibição discreta). |

Regras: no Grupo, mensagens vêm de `OrdemGlobal()` ordenadas por `seq` (FR-002); no PV, de
`Privadas()` do par, na ordem de registro. Mensagens diretas nunca aparecem no Grupo
(FR-003).

### Estado de conversas (local ao painel)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `ConversaAtiva` | Conversa | A que está sendo exibida. |
| `VistoPorConversa` | mapa chave→contagem | Contagem já vista por conversa, base do "não lida". |

Regra: efêmero e local; nunca cruza entre nós (FR-011).

## Fonte no `NodeState` (existente + auxiliares)

| Necessidade da UI | Fonte |
|-------------------|-------|
| Mensagens do Grupo | `OrdemGlobal()` (já existe) |
| Mensagens de um PV | `Privadas()` filtradas por par — **auxiliar novo** `PrivadasCom(int peer)` |
| Pares possíveis | catálogo (ids exceto o próprio) — auxiliar de leitura |
| Id próprio | `NodeState.Id` (já existe) |
| Relógio vetorial atual | `NodeState.Vc` (já existe) |
| Observação técnica | `OrdemLocal()`, `BufferSeqs`, `UltimoSnapshot` (já existem) |

Chave de par em `Privadas()`: `peer = Enviada ? Para : De`.

## Ajustes de código (mínimos, sem tocar no núcleo)

| Arquivo | Mudança |
|---------|---------|
| `Servicos/NodeState.cs` | (+) `PrivadasCom(int peer)` e exposição dos ids de pares para a lista de conversas |
| `Components/Pages/Painel.razor` | Reescrita para layout de chat (lista + balões + técnico) |
| `wwwroot/app.css` | (+) estilos de balões, lista de conversas e badge de não lida |

Nenhuma mudança em `Rede/`, `Nucleo/` ou na semântica de entrega.

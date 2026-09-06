# Phase 1 — Data Model: Chat Distribuído com Ordem Total

Modela as entidades do domínio (estado em memória por processo). Nenhuma persistência; tudo
vive na memória privada de cada nó.

## Entidades

### Nó (Node)

Representa um processo participante.

| Campo | Tipo | Descrição / Regras |
|-------|------|--------------------|
| `Id` | int | Identificador único (chave do catálogo). Define porta TCP `5000+Id` e web `8000+Id`. |
| `Estado` | enum {Ativo, Caido} | Estado observado localmente dos pares. |
| `VectorClock` | int[] | Índice por id de nó; ver Relógio Vetorial. |
| `OrdemLocalEmissao` | lista | Sequência de eventos de emissão deste nó (ordem de saída). |
| `OrdemGlobalEntrega` | lista | Fila de entrega em ordem total (por `Seq`). Critério de sucesso: idêntica entre nós. |
| `Buffer` | mapa Seq→Mensagem | Mensagens de grupo recebidas mas ainda não entregáveis. |
| `LiderAtual` | int? | Id do líder reconhecido; único após estabilização. |

### Catálogo de Nós (nos.json)

Lista estática lida na inicialização. **Não** é canal de coordenação em runtime.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `id` | int | Id do nó. |
| `host` | string | Host (testes: `127.0.0.1`). |
| `portaTcp` | int | Porta TCP (convenção `5000 + id`). |

### Mensagem Privada (unicast)

| Campo | Tipo | Descrição / Regras |
|-------|------|--------------------|
| `De` | int | Id remetente. |
| `Para` | int | Id destinatário único. |
| `Conteudo` | string | Texto. |
| `VectorClock` | int[] | Carimbo causal do remetente no envio. |

Regra: entregue apenas ao destinatário; preserva FIFO por canal.

### Mensagem de Grupo (broadcast)

| Campo | Tipo | Descrição / Regras |
|-------|------|--------------------|
| `De` | int | Id remetente original. |
| `Seq` | long? | Número de sequência global atribuído pelo líder. Nulo antes da ordenação. |
| `Conteudo` | string | Texto. |
| `VectorClock` | int[] | Carimbo causal no envio. |

Regra de entrega: só entregável quando `Seq == próximoSeqEsperado`; caso contrário aguarda
no buffer. Entrega estrita e monotônica, sem lacunas nem duplicatas.

### Número de Sequência Global

- Monotônico, atribuído exclusivamente pelo líder.
- Estado do sequenciador no líder: `proximoSeq` (próximo a atribuir).
- Cada nó mantém `proximoSeqEsperado` (próximo a entregar).
- **Pós-reeleição**: novo líder define `proximoSeq = max(últimoSeqEntregue dos ativos) + 1`.

### Relógio Vetorial (VectorClock)

- Vetor `VC` de tamanho N (número de nós no catálogo).
- Evento local (envio): `VC[self] += 1`.
- Recepção/entrega: `VC[i] = max(VC[i], msg.VC[i]) ∀i`; depois `VC[self] += 1`.
- Exibido no painel junto de cada mensagem, para causalidade.

### Estado Global / Snapshot (Chandy-Lamport)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `IniciadorId` | int | Nó que disparou o snapshot. |
| `EstadoPorNo` | mapa id→estado | Estado gravado de cada nó (VC, próximos seq, tamanho do buffer). |
| `EstadoDosCanais` | mapa (i→j)→lista | Mensagens em trânsito capturadas por canal. |
| `Concluido` | bool | Verdadeiro quando todos os marcadores foram recebidos. |

Regra: produz um corte consistente (nenhum efeito sem causa) sem interromper o chat.

### Líder / Estado de Eleição (Bully)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `LiderAtual` | int? | Id do líder (maior id ativo). |
| `EmEleicao` | bool | Verdadeiro durante uma eleição em curso. |
| `UltimoHeartbeat` | timestamp | Último heartbeat recebido do líder; base do timeout. |

Regras: heartbeat periódico do líder; timeout dispara `ELECTION`; converge para o maior id;
no máximo um líder após estabilização.

## Transições de estado relevantes

- **Difusão de grupo**: `emitida` → (enviada ao líder) → `sequenciada (Seq)` → (redifundida)
  → `bufferizada` → `entregue` (quando Seq == esperado).
- **Detecção de falha do líder**: `líder ativo` → (timeout de heartbeat) → `em eleição` →
  `novo líder` → `sequenciamento retomado`.
- **Snapshot**: `ocioso` → (grava estado + envia marcadores) → `gravando canais` →
  (todos os marcadores recebidos) → `concluído`.

## Relações

- Um `Nó` conhece todos os outros pelo `Catálogo`.
- Uma `Mensagem de Grupo` recebe seu `Seq` do `Líder` e é ordenada na
  `OrdemGlobalEntrega` de cada `Nó`.
- O `Snapshot` agrega o estado de todos os `Nós` e dos canais entre eles.

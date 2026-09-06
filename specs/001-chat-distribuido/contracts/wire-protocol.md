# Contract — Protocolo de Mensagens de Rede (Wire Protocol)

Este é o **único** contrato entre nós. Toda coordenação acontece por estas mensagens sobre
TCP; nenhum outro canal é permitido (Constituição, Princípio I).

## Enquadramento

Cada mensagem no stream TCP é: `[comprimento: int32 big-endian][payload: JSON UTF-8]`.
O payload é um **envelope** com um campo discriminador `tipo`.

## Envelope

```json
{
  "tipo": "GROUP | UNICAST | SEQUENCE_REQUEST | SEQUENCED | HEARTBEAT | ELECTION | OK | COORDINATOR | LAST_SEQ_QUERY | LAST_SEQ_REPLY | MARKER | ...",
  "de": 1,
  "para": 2,
  "vc": [0, 0, 0],
  "payload": { }
}
```

- `de` (int): id do remetente.
- `para` (int): id do destinatário (unicast). Ausente/ignorado em difusões redistribuídas.
- `vc` (int[]): relógio vetorial do remetente no instante do envio.
- `payload` (objeto): campos específicos do `tipo`.

## Tipos de mensagem

### Comunicação de usuário

| tipo | Direção | payload | Semântica |
|------|---------|---------|-----------|
| `UNICAST` | nó → nó | `{ "conteudo": string }` | Mensagem privada entregue só ao `para`. |
| `GROUP` | nó → líder | `{ "conteudo": string, "msgId": string }` | Pedido de difusão; o líder atribuirá `seq`. |
| `SEQUENCED` | líder → todos | `{ "conteudo": string, "msgId": string, "seq": long, "deOriginal": int }` | Mensagem de grupo já numerada; entregue em ordem de `seq`. |

### Ordem total (sequenciador)

| tipo | Direção | payload | Semântica |
|------|---------|---------|-----------|
| `LAST_SEQ_QUERY` | novo líder → todos | `{}` | Após reeleição, pergunta o último seq entregue. |
| `LAST_SEQ_REPLY` | nó → líder | `{ "ultimoSeqEntregue": long }` | Resposta usada para retomar `proximoSeq`. |

Regra: cada nó entrega `SEQUENCED` estritamente por `seq` crescente sem lacunas; mensagens
com `seq` futuro aguardam no buffer.

### Eleição (Bully)

| tipo | Direção | payload | Semântica |
|------|---------|---------|-----------|
| `HEARTBEAT` | líder → todos | `{}` | Sinal periódico de vida do líder. |
| `ELECTION` | nó → ids maiores | `{}` | Inicia eleição. |
| `OK` | id maior → iniciador | `{}` | "Assumo a partir daqui"; iniciador recua. |
| `COORDINATOR` | novo líder → todos | `{ "liderId": int }` | Anuncia o novo líder. |

Regras: o maior id ativo vence; ausência de `HEARTBEAT` além do timeout dispara `ELECTION`;
eleições concorrentes convergem para um único líder.

### Estado global (Chandy-Lamport)

| tipo | Direção | payload | Semântica |
|------|---------|---------|-----------|
| `MARKER` | nó → todos (por canal) | `{ "snapshotId": string, "iniciadorId": int }` | Marcador de snapshot. |

Regras:
1. Ao **iniciar** ou receber o **primeiro** `MARKER` de um `snapshotId`, o nó grava seu
   estado local e envia `MARKER` por todos os canais de saída.
2. O canal por onde chegou o primeiro marcador é gravado como **vazio**.
3. Nos demais canais, todas as mensagens recebidas entre a gravação do estado e a chegada do
   respectivo `MARKER` compõem o **estado do canal**.
4. O snapshot conclui quando o nó recebeu `MARKER` por todos os canais de entrada.

## Invariantes do contrato

- **INV-1**: Ordem FIFO por canal é preservada (garantida pelo TCP).
- **INV-2**: Nenhuma entrega de `SEQUENCED` fora da ordem de `seq`.
- **INV-3**: `seq` é único e monotônico por execução, sem duplicatas mesmo após reeleição.
- **INV-4**: `UNICAST` nunca é entregue a nó diferente de `para`.
- **INV-5**: Nenhuma informação de coordenação trafega fora deste protocolo.

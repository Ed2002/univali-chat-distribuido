# Quickstart — Chat Distribuído com Ordem Total

Guia de execução e validação. Prova os critérios de sucesso ponta a ponta. Detalhes de
entidades e mensagens estão em [data-model.md](./data-model.md) e
[contracts/wire-protocol.md](./contracts/wire-protocol.md).

## Pré-requisitos

- SDK do .NET 10 instalado.
- Um arquivo de catálogo `nos.json` com o conjunto de nós desejado (3, 8 ou 15 entradas).
- Host de testes `127.0.0.1`. Portas: TCP `5000 + id`, web `8000 + id`.

## Catálogo de exemplo (3 nós)

```json
[
  { "id": 1, "host": "127.0.0.1", "portaTcp": 5001 },
  { "id": 2, "host": "127.0.0.1", "portaTcp": 5002 },
  { "id": 3, "host": "127.0.0.1", "portaTcp": 5003 }
]
```

## Subir o sistema

Um processo por nó (uma janela de terminal por id):

```bash
dotnet run --project src/ChatDistribuido -- --id 1
dotnet run --project src/ChatDistribuido -- --id 2
dotnet run --project src/ChatDistribuido -- --id 3
```

Abra os painéis:

- Nó 1 → http://127.0.0.1:8001
- Nó 2 → http://127.0.0.1:8002
- Nó 3 → http://127.0.0.1:8003

Para 8 ou 15 nós, use um `nos.json` com 8/15 entradas e suba um processo por id — **sem
alterar código**.

## Cenários de validação

### V1 — Ordem total (SC-001)

1. De nós diferentes, difunda várias mensagens de grupo quase simultaneamente.
2. **Esperado**: a lista "ordem global de entrega" é **idêntica** em todos os painéis, sem
   lacunas nem duplicatas de `seq`.

### V2 — Mensagem privada (SC-002)

1. Do nó 1, envie mensagem privada ao nó 3 pelo id.
2. **Esperado**: apenas o painel do nó 3 exibe a mensagem; nós 1 e 2 não a recebem.

### V3 — Painel em tempo real (SC-006)

1. Emita e entregue mensagens.
2. **Esperado**: cada painel atualiza em tempo real ordem local de emissão, ordem global de
   entrega, relógio vetorial e buffer — refletindo **somente** o próprio nó.

### V4 — Estado global consistente (SC-003)

1. Com mensagens em trânsito, dispare a captura de estado global em um nó.
2. **Esperado**: o retrato produzido é consistente (nenhum efeito sem causa) e o chat
   continua funcionando durante e após a captura.

### V5 — Reeleição de líder (SC-004)

1. Identifique o líder (maior id ativo) e encerre seu processo.
2. **Esperado**: os demais detectam a ausência de heartbeat, elegem um novo líder (próximo
   maior id) e a numeração de `seq` continua sem lacunas nem duplicatas; após estabilizar,
   há no máximo um líder.

### V6 — Escala 3/8/15 (SC-005)

1. Repita V1–V5 com catálogos de 3, 8 e 15 nós.
2. **Esperado**: comportamento idêntico em todas as escalas, sem alteração de código.

## Testes automatizados

```bash
dotnet test tests/ChatDistribuido.Tests
```

- **unit**: relógio vetorial, atribuição de seq, buffer de reordenação (determinísticos).
- **integration**: múltiplas instâncias em `127.0.0.1` cobrindo V1, V4 e V5.

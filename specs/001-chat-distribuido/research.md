# Phase 0 — Research: Chat Distribuído com Ordem Total

Consolida as decisões técnicas. A constituição já fixou a maior parte das escolhas; este
documento registra o *porquê* e as alternativas descartadas, e resolve os pontos em aberto.

## 1. Transporte: TCP unicast + JSON

- **Decisão**: Um canal TCP por par de nós, com serialização de mensagens em JSON
  (`System.Text.Json`). "Grupo" = iterar o catálogo e enviar unicast a cada nó.
- **Rationale**: TCP dá entrega confiável e ordem FIFO por canal (premissa da spec), sem a
  complexidade de multicast IP. JSON é legível, simples de depurar e alinhado à
  observabilidade exigida pelo painel.
- **Alternativas descartadas**: UDP/multicast (não confiável, sem FIFO, exige camada extra
  de confirmação); gRPC/protobuf (ganho de performance irrelevante na escala de 15 nós e
  contraria a legibilidade JSON da constituição).
- **Detalhe**: Enquadramento de mensagens por prefixo de comprimento (length-prefix, inteiro
  de 4 bytes) antes de cada payload JSON, para delimitar mensagens no stream TCP.

## 2. Ordem total: sequenciador eleito

- **Decisão**: O líder é o **sequenciador**. Ao difundir, um nó envia a mensagem ao líder,
  que atribui um número de sequência global monotônico e a redifunde a todos; cada nó
  entrega estritamente na ordem do seq, retendo no buffer o que chegar fora de ordem.
- **Rationale**: Sequenciador único é a forma mais simples e verificável de ordem total, e
  casa diretamente com a eleição de líder já exigida. Produz filas de entrega idênticas.
- **Alternativas descartadas**: Ordem total por acordo (tipo ABcast com relógios lógicos e
  ACKs de todos) — mais mensagens e mais complexo; consenso (Raft/Paxos) — excessivo para o
  escopo pedagógico.
- **Ponto resolvido — pós-reeleição**: O novo líder retoma a numeração a partir do **maior
  seq já entregue conhecido + 1**. Para evitar lacunas/duplicatas, o líder recém-eleito
  consulta os nós ativos pelo último seq entregue (via mensagem de rede) e continua a partir
  do máximo. Ver [data-model.md](./data-model.md) (estado do sequenciador).

## 3. Relógio vetorial (causalidade em paralelo)

- **Decisão**: Cada nó mantém um vetor `VC[id] → contador`. Incrementa o próprio índice a
  cada evento local (envio); ao entregar, faz merge (máximo componente a componente) e
  incrementa o próprio índice. Registrado no painel junto de cada mensagem.
- **Rationale**: Fornece a relação causal exigida pela spec, sem substituir a ordem total do
  sequenciador (que é a garantia de entrega). Os dois convivem: seq = ordem de entrega, VC =
  causalidade observável.
- **Alternativas descartadas**: Relógio de Lamport escalar (não captura concorrência tão bem
  quanto o vetorial para exibição no painel).

## 4. Eleição de líder: Bully + heartbeat/timeout

- **Decisão**: Algoritmo do Valentão. O nó de **maior id** ativo torna-se líder. Detecção de
  falha do líder por heartbeat periódico com timeout; ao expirar, o detector inicia eleição
  enviando ELECTION aos ids maiores, recebendo OK e, na ausência de resposta, anuncia-se
  COORDINATOR.
- **Rationale**: Determinístico, simples de demonstrar e diretamente ligado ao id do
  catálogo. Reeleição automática ao cair o líder é natural.
- **Pontos resolvidos**:
  - **Heartbeat**: líder envia HEARTBEAT a todos periodicamente (ex.: a cada 1s).
  - **Timeout de detecção**: ausência de heartbeat por um múltiplo do período (ex.: 3s)
    dispara eleição.
  - **Eleições concorrentes**: convergem para o maior id porque ids menores recuam ao
    receber OK/COORDINATOR de um id maior.

## 5. Estado global: Chandy-Lamport

- **Decisão**: Snapshot de Chandy-Lamport. O nó iniciador grava seu estado e envia um
  **marcador** por todos os canais de saída. Ao receber o primeiro marcador em um canal, o
  nó grava seu estado, marca esse canal como vazio e propaga marcadores; nos demais canais,
  grava as mensagens recebidas entre a gravação do estado e a chegada do marcador (estado
  do canal).
- **Rationale**: Algoritmo canônico para corte consistente sem parar o sistema — exatamente
  o critério de sucesso. Casa com canais FIFO confiáveis (premissa TCP).
- **Alternativas descartadas**: "Parar o mundo" e coletar estados (viola "sem interromper a
  operação"); snapshots baseados em relógio vetorial (mais complexo de reconstruir corte
  consistente para exibição).

## 6. Painel por nó: Blazor Server + SignalR

- **Decisão**: Cada processo hospeda um painel Blazor Server (porta web `8000 + id`) que
  observa o `NodeState` via eventos empurrados por SignalR. O painel só fala com o próprio
  processo.
- **Rationale**: Blazor Server dá atualização em tempo real com pouco código e roda no mesmo
  processo do nó, garantindo que nenhum estado cruze entre nós fora do TCP.
- **Alternativas descartadas**: SPA separada consumindo API REST (adiciona um segundo canal
  e risco de virar coordenação implícita); console TUI (menos demonstrável).

## 7. Execução e catálogo

- **Decisão**: `nos.json` mapeia `id → host:portaTCP` para todos os nós. Cada processo
  recebe o próprio `id` (argumento/variável) na inicialização, lê o catálogo, abre o
  servidor TCP em `5000 + id` e o painel em `8000 + id`. Host de testes `127.0.0.1`.
- **Rationale**: Catálogo estático permite subir 3, 8 ou 15 nós trocando apenas o arquivo,
  sem alterar código. Endereçamento derivado do id elimina configuração por porta.
- **Ponto resolvido — 3/8/15 nós**: Basta fornecer três variantes de `nos.json` (ou um
  arquivo com o número desejado de entradas) e iniciar um processo por id listado.

## Resumo de pontos em aberto

| Pergunta | Resolução |
|----------|-----------|
| Retomada de seq após reeleição | Novo líder consulta último seq entregue dos ativos e continua do máximo + 1 |
| Período de heartbeat / timeout | Heartbeat ~1s, timeout de detecção ~3s (ajustável) |
| Enquadramento no stream TCP | Length-prefix (4 bytes) + payload JSON |
| Suporte 3/8/15 nós | Variantes de `nos.json`; um processo por id |

Nenhum item permanece como NEEDS CLARIFICATION.

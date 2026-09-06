<!--
Sync Impact Report
==================
Version change: TEMPLATE (unfilled) → 1.0.0
Bump rationale: Initial ratification of the project constitution (first concrete version).

Principles defined:
  I.   Comunicação Exclusiva por Mensagens de Rede (NÃO-NEGOCIÁVEL)
  II.  Processos Independentes com Estado Privado
  III. Ordem Total via Sequenciador Eleito
  IV.  Corretude dos Algoritmos Distribuídos
  V.   Núcleo Independente da Interface e do Transporte

Added sections:
  - Restrições Técnicas e de Execução (Section 2)
  - Critérios de Sucesso e Verificação (Section 3)
  - Governance

Removed sections: none (template placeholders replaced).

Follow-up TODOs: none. RATIFICATION_DATE set to first adoption date (2026-09-06).
-->

# Chat Distribuído Constitution

## Core Principles

### I. Comunicação Exclusiva por Mensagens de Rede (NÃO-NEGOCIÁVEL)

Os nós MUST se comunicar exclusivamente por troca de mensagens de rede sobre TCP com
serialização JSON. É PROIBIDO usar memória compartilhada, banco de dados comum, arquivo
compartilhado em disco, variável global entre processos ou qualquer serviço externo como
canal de coordenação em tempo de execução. A comunicação de grupo MUST ser realizada por
unicast confiável e FIFO por canal, iterando o catálogo de nós e enviando a cada
destinatário.

Rationale: O propósito do sistema é demonstrar coordenação distribuída real; qualquer
atalho por estado compartilhado invalida o objetivo pedagógico e os critérios de sucesso.

### II. Processos Independentes com Estado Privado

Cada nó MUST ser um processo independente do sistema operacional, com estado próprio em
memória privada. Um processo NÃO pode acessar o estado de outro senão por mensagens. O
catálogo `nos.json` é apenas uma lista estática de endereços lida na inicialização e
NÃO pode ser usado como canal de coordenação em tempo de execução. O sistema MUST subir
com 3, 8 e 15 nós a partir do catálogo, sem alteração de código.

Rationale: Independência de processos garante que a coordenação observada seja fruto do
protocolo e não de acoplamento oculto, e permite escala configurável.

### III. Ordem Total via Sequenciador Eleito

Toda mensagem de grupo MUST ser entregue na mesma ordem em todos os nós. A ordem total
MUST ser obtida por um sequenciador (nó líder eleito) que atribui número de sequência
global a cada mensagem de grupo; todos os nós entregam estritamente na ordem do seq, sem
buracos nem seqs duplicados. Um relógio vetorial MUST ser mantido e registrado em
paralelo para capturar causalidade, sem substituir a ordem total do sequenciador.

Rationale: Ordem total idêntica em todos os nós é o critério central de corretude do
chat; separar seq (ordem) de relógio vetorial (causalidade) mantém ambas as garantias
observáveis.

### IV. Corretude dos Algoritmos Distribuídos

Os algoritmos distribuídos MUST seguir suas definições canônicas: eleição de líder pelo
Algoritmo do Valentão (Bully) com heartbeat/timeout e reeleição automática quando o líder
cai; captura de estado global pelo algoritmo de snapshot de Chandy-Lamport, produzindo um
retrato consistente sem parar o sistema. A queda do líder MUST disparar reeleição, e o
novo líder MUST retomar a sequência sem buracos nem duplicatas.

Rationale: Aderência aos algoritmos canônicos torna o comportamento verificável contra a
literatura e garante tolerância a falhas do líder.

### V. Núcleo Independente da Interface e do Transporte

O núcleo (relógio vetorial, ordem total, eleição e estado global) MUST ser independente
da interface web e dos detalhes de sockets, comunicando-se com as demais camadas apenas
por sua fachada de Serviços. O painel Blazor de cada nó MUST falar somente com o próprio
processo (UI local); nenhum estado pode cruzar entre nós fora do canal TCP.

Rationale: Isolar o núcleo permite testá-lo de forma determinística e impede que a
interface se torne um canal de coordenação implícito entre nós.

## Restrições Técnicas e de Execução

- **Stack**: C# (.NET 10), ASP.NET Core, Blazor Server (painel em tempo real via SignalR),
  sockets TCP, JSON via `System.Text.Json`.
- **Camadas**: Rede (transporte TCP + serialização) · Núcleo (algoritmos distribuídos) ·
  Serviços (fachada do núcleo + ponte de eventos para a UI) · Components (painel Blazor).
- **Endereçamento**: porta TCP `5000 + id` para a rede entre nós; porta web `8000 + id`
  para o painel local; host de testes `127.0.0.1`.
- **Execução**: um processo por nó; catálogo estático `nos.json`; suporte a 3, 8 e 15 nós
  sem alteração de código.
- **Painel de cada nó**: MUST exibir envio para nó específico (unicast), envio para grupo,
  ordem local de emissão, ordem global de entrega, relógio vetorial e buffer de mensagens,
  além de captura e exibição de estado global sob demanda.

## Critérios de Sucesso e Verificação

- Ao fim de uma simulação, as filas de delivery (ordem global) MUST ser idênticas em todos
  os nós.
- O snapshot de estado global MUST produzir um retrato consistente sem parar o sistema.
- A queda do líder MUST disparar reeleição, e o novo líder MUST retomar a sequência sem
  buracos nem seqs duplicados.
- O sistema MUST subir com 3, 8 e 15 nós a partir do catálogo, sem alteração de código.

## Governance

Esta constituição supersede quaisquer outras práticas do projeto. As Regras Invioláveis
(Princípios I e II) NÃO podem ser relaxadas por conveniência de implementação.

- **Emendas**: MUST ser documentadas, justificadas e registradas com um novo número de
  versão e data de emenda neste arquivo.
- **Versionamento** (semântico): MAJOR para remoções/redefinições incompatíveis de
  princípios ou governança; MINOR para novos princípios/seções ou expansão material de
  orientação; PATCH para esclarecimentos e correções não semânticas.
- **Conformidade**: toda revisão de código e de PR MUST verificar aderência aos princípios,
  em especial a proibição de canais de coordenação fora da rede. Complexidade adicional
  MUST ser justificada frente aos princípios.

**Version**: 1.0.0 | **Ratified**: 2026-09-06 | **Last Amended**: 2026-09-06

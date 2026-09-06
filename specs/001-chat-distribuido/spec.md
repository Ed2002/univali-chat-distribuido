# Feature Specification: Chat Distribuído com Ordem Total

**Feature Branch**: `001-chat-distribuido`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "gere as especificações com base na construção" (constituição do projeto Chat Distribuído)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Difusão de mensagens em ordem total (Priority: P1)

Um participante escreve uma mensagem no seu painel e a difunde para o grupo. A mensagem é
entregue a todos os participantes exatamente na mesma sequência em que é entregue nos
demais nós, independentemente de quem enviou ou de quando cada nó a recebeu pela rede.

**Why this priority**: É o coração do sistema — sem ordem total idêntica em todos os nós,
o chat não cumpre seu propósito. Todo o restante (snapshot, eleição) existe para sustentar
essa garantia.

**Independent Test**: Subir um conjunto de nós, enviar várias mensagens de grupo
concorrentemente de nós diferentes e comparar a fila de entrega global de cada nó ao
final — todas devem ser idênticas.

**Acceptance Scenarios**:

1. **Given** um grupo de nós ativos, **When** um participante difunde uma mensagem de
   grupo, **Then** todos os nós exibem essa mensagem na sua ordem global de entrega.
2. **Given** dois participantes difundem mensagens quase simultaneamente, **When** as
   mensagens são entregues, **Then** todos os nós apresentam as duas mensagens na mesma
   ordem relativa entre si.
3. **Given** uma sequência de N mensagens de grupo emitidas, **When** a simulação termina,
   **Then** a fila de entrega global é idêntica em todos os nós, sem lacunas nem duplicatas.

---

### User Story 2 - Mensagem privada para um nó específico (Priority: P1)

Um participante escolhe outro participante pelo seu identificador e envia uma mensagem
privada (unicast), que é entregue apenas ao destinatário.

**Why this priority**: É a forma mais simples de comunicação e a base sobre a qual a
difusão de grupo é construída (grupo = enviar a cada nó do catálogo). Entrega valor de
forma independente e é testável isoladamente.

**Independent Test**: Enviar uma mensagem privada de um nó a outro e verificar que apenas
o destinatário a recebe, preservando a ordem FIFO por canal.

**Acceptance Scenarios**:

1. **Given** dois nós ativos, **When** um envia mensagem privada ao outro pelo id, **Then**
   apenas o destinatário exibe a mensagem.
2. **Given** um remetente envia várias mensagens privadas ao mesmo destinatário, **When**
   elas chegam, **Then** o destinatário as exibe na mesma ordem em que foram emitidas.

---

### User Story 3 - Painel de observação do nó (Priority: P2)

Cada participante acompanha, em tempo real, o comportamento do seu próprio nó: mensagens
enviadas para um nó específico, mensagens enviadas para o grupo, a ordem local de emissão,
a ordem global de entrega, o relógio vetorial e o buffer de mensagens pendentes.

**Why this priority**: Torna o comportamento distribuído observável e verificável — é o que
permite demonstrar e auditar a ordem total e a causalidade. Sem ele o sistema funciona, mas
não é demonstrável.

**Independent Test**: Executar operações de envio e verificar que cada painel reflete
corretamente os eventos do seu próprio nó em tempo real.

**Acceptance Scenarios**:

1. **Given** um nó em atividade, **When** ele emite ou entrega uma mensagem, **Then** o
   painel atualiza em tempo real a ordem local de emissão e a ordem global de entrega.
2. **Given** mensagens ainda não entregáveis, **When** o painel é observado, **Then** ele
   mostra o buffer de mensagens pendentes e o relógio vetorial atual.
3. **Given** dois painéis de nós diferentes, **When** comparados, **Then** cada painel
   reflete somente o estado do seu próprio nó e nenhum estado cruza fora do canal de rede.

---

### User Story 4 - Captura de estado global consistente (Priority: P2)

Um participante solicita, sob demanda, a captura do estado global do sistema. O sistema
produz um retrato consistente do estado de todos os nós e dos canais entre eles, sem
interromper a operação do chat.

**Why this priority**: Permite inspecionar a consistência do sistema em execução — critério
de sucesso explícito. Depende da difusão já existir, por isso vem após P1.

**Independent Test**: Disparar a captura durante o tráfego de mensagens e verificar que o
retrato resultante é consistente (não contém efeitos sem causa) e que o chat continua
funcionando durante e após a captura.

**Acceptance Scenarios**:

1. **Given** o sistema em operação com mensagens em trânsito, **When** um nó dispara a
   captura de estado global, **Then** o sistema produz um retrato consistente do estado dos
   nós e dos canais.
2. **Given** uma captura em andamento, **When** os participantes continuam enviando
   mensagens, **Then** o chat continua funcionando normalmente, sem pausa.

---

### User Story 5 - Eleição de líder e reeleição automática (Priority: P2)

O grupo mantém um único líder responsável por ordenar as mensagens de grupo. Quando o
líder cai, o grupo detecta a falha e elege automaticamente um novo líder, que retoma a
ordenação sem quebrar a sequência.

**Why this priority**: Dá tolerância a falhas à garantia de ordem total. É essencial para a
robustez, mas o sistema já entrega valor com um líder estável, então vem após o núcleo P1.

**Independent Test**: Encerrar o líder atual durante o tráfego e verificar que um novo líder
é eleito e que a numeração de sequência continua sem lacunas nem duplicatas.

**Acceptance Scenarios**:

1. **Given** um grupo com um líder ativo, **When** o líder deixa de responder, **Then** o
   grupo detecta a ausência e inicia a eleição de um novo líder.
2. **Given** uma eleição concluída, **When** o novo líder assume, **Then** as mensagens de
   grupo continuam a ser ordenadas sem lacunas nem números de sequência duplicados.
3. **Given** o grupo em qualquer instante, **When** observado após estabilização, **Then**
   existe no máximo um líder reconhecido pelos nós ativos.

---

### Edge Cases

- **Destinatário indisponível**: o que acontece ao enviar mensagem privada ou de grupo a um
  nó que está fora do ar? A entrega aos nós ativos não deve ser bloqueada.
- **Queda do líder no meio de uma ordenação**: uma mensagem em processo de ordenação quando
  o líder cai não pode gerar lacuna nem número de sequência duplicado após a reeleição.
- **Mensagens fora de ordem no canal**: uma mensagem de grupo cujo número de sequência ainda
  não pode ser entregue deve aguardar no buffer até que os anteriores tenham sido entregues.
- **Eleições concorrentes**: se vários nós iniciarem eleição ao mesmo tempo, o resultado
  deve convergir para um único líder.
- **Escala variável**: o comportamento deve ser idêntico com 3, 8 e 15 nós.
- **Reingresso após captura**: uma captura de estado global disparada por mais de um nó não
  deve produzir retratos inconsistentes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir que um participante envie uma mensagem privada a um
  único destinatário identificado pelo seu id (unicast).
- **FR-002**: O sistema MUST permitir que um participante difunda uma mensagem para todos os
  participantes do grupo.
- **FR-003**: O sistema MUST entregar as mensagens de grupo em ordem total — a mesma
  sequência de entrega em todos os nós.
- **FR-004**: O sistema MUST preservar a ordem FIFO das mensagens dentro de cada canal entre
  dois nós.
- **FR-005**: O sistema MUST manter e registrar um relógio vetorial por nó para capturar a
  relação de causalidade entre eventos, em paralelo à ordem total.
- **FR-006**: O sistema MUST reter em um buffer as mensagens de grupo que ainda não podem ser
  entregues e entregá-las assim que a ordem permitir, sem lacunas.
- **FR-007**: Cada nó MUST oferecer um painel próprio que exiba, em tempo real: envio para nó
  específico, envio para grupo, ordem local de emissão, ordem global de entrega, relógio
  vetorial e buffer de mensagens.
- **FR-008**: O painel de um nó MUST refletir apenas o estado do próprio nó; nenhum estado
  pode cruzar entre nós fora do canal de rede.
- **FR-009**: O sistema MUST permitir a captura de um estado global consistente sob demanda,
  produzindo um retrato do estado dos nós e dos canais.
- **FR-010**: A captura de estado global MUST ocorrer sem interromper a operação do chat.
- **FR-011**: O sistema MUST manter exatamente um líder responsável por atribuir a ordem
  total das mensagens de grupo.
- **FR-012**: O sistema MUST detectar a queda do líder e eleger automaticamente um novo
  líder.
- **FR-013**: Após a reeleição, o novo líder MUST retomar a ordenação sem produzir lacunas
  nem números de sequência duplicados.
- **FR-014**: O sistema MUST operar com 3, 8 e 15 participantes a partir de um catálogo
  estático de nós, sem alteração de comportamento.
- **FR-015**: Os nós MUST se comunicar exclusivamente por mensagens de rede; nenhum canal de
  coordenação por estado compartilhado (memória, banco, arquivo, variável global, serviço
  externo) é permitido.
- **FR-016**: A entrega a nós indisponíveis NÃO pode bloquear a entrega aos demais
  participantes ativos.

### Key Entities *(include if feature involves data)*

- **Nó (participante)**: um processo independente identificado por um id único, com estado
  próprio (ordem local de emissão, ordem global de entrega, relógio vetorial, buffer).
- **Mensagem privada**: comunicação dirigida a um único destinatário identificado por id.
- **Mensagem de grupo**: comunicação difundida a todos os participantes, associada a um
  número de sequência global e a um carimbo de relógio vetorial.
- **Número de sequência global**: valor monotônico atribuído pelo líder que define a ordem
  total de entrega das mensagens de grupo.
- **Relógio vetorial**: estrutura por nó que registra causalidade entre eventos.
- **Catálogo de nós**: lista estática de participantes (ids e endereços) conhecida na
  inicialização.
- **Estado global (snapshot)**: retrato consistente do estado de todos os nós e dos canais
  entre eles em um instante lógico.
- **Líder**: o participante atualmente responsável por atribuir a ordem total.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ao fim de uma simulação, a fila de entrega global (ordem total) é idêntica em
  100% dos nós, sem lacunas nem duplicatas.
- **SC-002**: Uma mensagem privada é entregue apenas ao destinatário pretendido em 100% dos
  casos.
- **SC-003**: A captura de estado global produz um retrato consistente (nenhum efeito
  registrado sem sua causa) e o chat permanece operacional durante e após a captura.
- **SC-004**: Após a queda do líder, um novo líder é eleito e a numeração de sequência
  continua sem lacunas nem duplicatas, com no máximo um líder reconhecido após a
  estabilização.
- **SC-005**: O sistema é iniciado e opera corretamente com 3, 8 e 15 participantes a partir
  do catálogo, sem qualquer alteração de comportamento.
- **SC-006**: Cada painel reflete em tempo real os eventos do seu próprio nó, permitindo a um
  observador confirmar visualmente a ordem local, a ordem global, o relógio vetorial e o
  buffer.

## Assumptions

- Os participantes são executados como processos independentes em uma mesma rede local
  (ambiente de teste), com o catálogo de nós conhecido na inicialização.
- Os canais de comunicação são confiáveis e preservam a ordem FIFO por canal (não há perda
  nem reordenação dentro de um canal), embora nós individuais possam falhar (parar).
- A falha considerada é do tipo "parada" (o nó deixa de responder); não são tratadas falhas
  bizantinas.
- A escala-alvo é de até 15 participantes; escalas maiores estão fora do escopo desta versão.
- O catálogo de nós é estático durante uma execução — o ingresso dinâmico de novos
  participantes em tempo de execução está fora do escopo desta versão.
- Autenticação, criptografia de conteúdo e persistência de histórico de mensagens entre
  execuções estão fora do escopo desta versão.

# Feature Specification: Interface de Chat com Balões e Conversas Privadas

**Feature Branch**: `003-chat-bubbles-ui`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "eu gostaria que a ui fosse algo mais como um chat mesmo e cada
mensagem um balão de quem enviou e tbm se a mensagem foi direta para alguém então não aparece
no chat geral fica em um pv"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Chat de grupo com balões (Priority: P1)

O participante vê as mensagens de grupo como uma conversa de chat: cada mensagem é um balão
que identifica quem a enviou, exibidas na ordem em que o sistema as entrega (ordem total). É
a experiência principal ao abrir o painel do nó.

**Why this priority**: Transforma o painel técnico atual em um chat de verdade — é o cerne do
pedido. Sem isso, nada da nova experiência existe.

**Independent Test**: Difundir mensagens de grupo de nós diferentes e confirmar que cada nó
as vê como balões identificando o remetente, na mesma ordem em todos os nós.

**Acceptance Scenarios**:

1. **Given** o chat de grupo aberto, **When** um participante difunde uma mensagem, **Then**
   ela aparece como um balão com a identificação do remetente na conversa de grupo.
2. **Given** várias mensagens de grupo de remetentes diferentes, **When** elas são
   entregues, **Then** aparecem como balões na mesma ordem em todos os nós (ordem total
   preservada).
3. **Given** o chat com muitas mensagens, **When** chega uma nova, **Then** a conversa rola
   para mostrar a mensagem mais recente.

---

### User Story 2 - Conversas privadas separadas do chat geral (Priority: P1)

Quando um participante envia (ou recebe) uma mensagem direta a um nó específico, ela **não**
aparece no chat de grupo: fica em uma conversa privada (PV) com aquele nó. O participante
alterna entre o chat de grupo e cada conversa privada.

**Why this priority**: É a segunda metade explícita do pedido — separar o privado do geral.
Junto da US1 forma a experiência de chat completa.

**Independent Test**: Enviar uma mensagem direta do nó A ao nó B e confirmar que ela aparece
apenas na conversa privada A↔B (nos dois lados) e nunca no chat de grupo de nenhum nó.

**Acceptance Scenarios**:

1. **Given** um participante em uma conversa privada com o nó X, **When** ele envia uma
   mensagem, **Then** ela aparece somente nessa conversa privada e é entregue apenas ao nó X.
2. **Given** um participante recebe uma mensagem direta do nó Y, **When** ela chega, **Then**
   aparece na conversa privada com Y e não no chat de grupo.
3. **Given** o chat de grupo aberto, **When** qualquer mensagem direta trafega, **Then** ela
   nunca é exibida entre as mensagens de grupo.
4. **Given** o participante alterna entre o grupo e uma conversa privada, **When** seleciona
   uma conversa, **Then** vê apenas as mensagens daquela conversa.

---

### User Story 3 - Distinção visual do remetente (Priority: P2)

O participante distingue rapidamente suas próprias mensagens das dos outros: os balões do
próprio nó ficam de um lado/estilo e os dos demais de outro, com o nome/id de quem enviou
visível.

**Why this priority**: É o que faz "parecer um chat de verdade" e evita confusão sobre a
autoria. Melhora a US1/US2, mas elas já entregam valor sem o polimento.

**Independent Test**: Enviar e receber mensagens e confirmar que os balões do próprio nó são
visualmente diferentes (alinhamento/estilo) dos balões dos outros, com a autoria clara.

**Acceptance Scenarios**:

1. **Given** o próprio participante envia uma mensagem, **When** ela aparece, **Then** o
   balão é alinhado/estilizado como "minha mensagem".
2. **Given** uma mensagem de outro nó, **When** ela aparece, **Then** o balão mostra a
   identificação do remetente e é estilizado como "mensagem recebida".

---

### User Story 4 - Aviso de nova mensagem em conversa não ativa (Priority: P3)

Quando chega uma mensagem (privada ou de grupo) em uma conversa que não está aberta no
momento, o participante recebe um indicador de que há algo novo naquela conversa.

**Why this priority**: Conveniência que evita perder mensagens ao alternar conversas; não é
essencial para a experiência mínima.

**Independent Test**: Estar em uma conversa e receber mensagem em outra; confirmar que a
conversa não ativa mostra um indicador de novidade.

**Acceptance Scenarios**:

1. **Given** o participante está no chat de grupo, **When** chega uma mensagem privada de um
   nó, **Then** a conversa privada daquele nó exibe um indicador de mensagem não lida.
2. **Given** o participante abre a conversa com indicador, **When** ele a visualiza, **Then**
   o indicador é limpo.

---

### Edge Cases

- **Mensagem para um nó indisponível**: o envio direto a um nó fora do ar não deve travar a
  interface; a conversa reflete o que foi possível enviar.
- **Ordem total e a rolagem**: mensagens de grupo entregues fora de ordem de chegada devem
  aparecer na ordem total correta, não na ordem de recebimento pela rede.
- **Conversa privada consigo mesmo**: enviar direto ao próprio id não é uma interação de chat
  válida e deve ser evitado/desabilitado.
- **Muitas conversas (até 15 nós)**: a lista de conversas deve permanecer navegável com o
  número máximo de nós.
- **Distinção entre enviadas e recebidas no PV**: em uma conversa privada, deve ficar claro
  quais mensagens o próprio nó enviou e quais recebeu.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A interface MUST apresentar as mensagens como uma conversa de chat, cada
  mensagem em um balão identificando o remetente.
- **FR-002**: O chat de grupo MUST exibir as mensagens de grupo na ordem total de entrega
  (idêntica entre os nós).
- **FR-003**: Mensagens diretas (privadas) MUST NOT aparecer no chat de grupo.
- **FR-004**: Cada mensagem direta MUST ser exibida em uma conversa privada (PV) associada ao
  nó de origem/destino correspondente.
- **FR-005**: O participante MUST poder alternar entre o chat de grupo e cada conversa
  privada, vendo apenas as mensagens da conversa selecionada.
- **FR-006**: Ao enviar uma mensagem no chat de grupo, ela MUST ser difundida a todos; ao
  enviar em uma conversa privada, ela MUST ser entregue apenas ao nó daquela conversa.
- **FR-007**: Os balões das mensagens do próprio nó MUST ser visualmente distintos dos balões
  das mensagens recebidas, com a autoria sempre identificável.
- **FR-008**: A conversa aberta MUST rolar automaticamente para a mensagem mais recente ao
  chegar/enviar mensagens.
- **FR-009**: A interface MUST oferecer uma lista/seleção de conversas: o grupo e uma
  conversa por nó do catálogo.
- **FR-010**: Uma conversa não ativa que recebe nova mensagem MUST exibir um indicador de
  não lida, limpo ao abrir a conversa.
- **FR-011**: A interface MUST continuar refletindo apenas o estado do próprio nó (nenhum
  estado cruza entre nós fora do canal de rede).
- **FR-012**: A interface MUST manter acessível a observação técnica do nó (ordem local de
  emissão, ordem global de entrega, relógio vetorial, buffer e captura de estado global),
  ainda que de forma secundária à experiência de chat.
- **FR-013**: A interface MUST impedir (ou evitar) o envio de mensagem direta ao próprio nó.

### Key Entities *(include if feature involves data)*

- **Conversa**: um canal exibido na interface — o "Grupo" (todas as mensagens de grupo) ou um
  PV com um nó específico.
- **Mensagem de chat**: uma entrada exibida como balão, com remetente, conteúdo, indicação de
  enviada/recebida e a conversa à qual pertence.
- **Lista de conversas**: o conjunto navegável de conversas (Grupo + um PV por nó do
  catálogo), com estado de não lida por conversa.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ao observar o chat de grupo, o usuário identifica o remetente de 100% das
  mensagens sem ambiguidade.
- **SC-002**: 100% das mensagens diretas aparecem apenas na conversa privada correspondente e
  nunca no chat de grupo.
- **SC-003**: As mensagens de grupo aparecem na mesma ordem (ordem total) em todos os nós.
- **SC-004**: O usuário distingue suas próprias mensagens das recebidas em menos de 1 segundo
  de observação (distinção visual imediata).
- **SC-005**: O usuário alterna entre grupo e qualquer conversa privada e vê somente as
  mensagens daquela conversa, com o sistema suportando até 15 conversas.
- **SC-006**: Uma nova mensagem em conversa não ativa é sinalizada e o indicador é limpo ao
  abrir a conversa.
- **SC-007**: A observação técnica do nó (ordem local/global, relógio vetorial, buffer,
  estado global) permanece acessível a partir da mesma interface.

## Assumptions

- A funcionalidade é de apresentação/UX sobre o comportamento distribuído já existente
  (unicast, difusão em ordem total, relógio vetorial, snapshot, eleição); nenhuma mudança na
  semântica de entrega é pretendida.
- A lista de conversas privadas deriva do catálogo de nós conhecido na inicialização (uma
  conversa por nó, exceto o próprio).
- O escopo é a interface de um único nó (o painel local); a experiência é por nó.
- A distinção "minha mensagem" vs. "recebida" baseia-se no id do próprio nó.
- Persistência de histórico entre execuções permanece fora de escopo; as conversas refletem o
  que ocorreu na sessão atual do processo.
- O estado de "não lida" é local ao painel e não trafega entre nós.

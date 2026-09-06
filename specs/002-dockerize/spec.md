# Feature Specification: Conteinerização do Chat Distribuído

**Feature Branch**: `002-dockerize`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "dockerize o projeto"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Subir o cluster em containers com um comando (Priority: P1)

Um operador sobe o chat distribuído inteiro — vários nós — a partir de containers com um
único comando, sem instalar o runtime da aplicação na máquina. Cada nó roda em seu próprio
container isolado, com estado próprio, e os nós se comunicam apenas pela rede entre
containers.

**Why this priority**: É o valor central da conteinerização — reprodutibilidade e execução
sem preparar o ambiente local. Sem isso, nada mais da feature entrega valor.

**Independent Test**: Em uma máquina apenas com o runtime de containers instalado, executar
o comando de subida e confirmar que o cluster inicia e opera (mensagens de grupo entregues
em ordem total entre os nós).

**Acceptance Scenarios**:

1. **Given** uma máquina apenas com o runtime de containers, **When** o operador executa o
   comando de subida do cluster, **Then** todos os nós iniciam como containers independentes
   e formam o chat.
2. **Given** o cluster conteinerizado em operação, **When** mensagens de grupo são
   difundidas, **Then** elas são entregues em ordem total idêntica em todos os nós (mesmo
   comportamento da execução local).
3. **Given** o cluster em execução, **When** o operador emite o comando de parada, **Then**
   todos os containers são encerrados de forma limpa.

---

### User Story 2 - Escalar o número de nós sem alterar código (Priority: P2)

Um operador escolhe subir o cluster com 3, 8 ou 15 nós apenas por configuração de execução,
sem editar ou recompilar o código da aplicação.

**Why this priority**: Preserva o critério de sucesso do projeto (suporte a 3/8/15 nós) no
ambiente conteinerizado. Depende de US1 existir.

**Independent Test**: Subir o cluster nas três escalas (3, 8, 15) e confirmar que cada uma
forma o chat e entrega mensagens em ordem total, sem qualquer mudança no código.

**Acceptance Scenarios**:

1. **Given** a configuração para N nós (N ∈ {3, 8, 15}), **When** o operador sobe o cluster,
   **Then** exatamente N containers de nó iniciam e formam o chat.
2. **Given** uma escala diferente escolhida, **When** o cluster é ressubido, **Then** o
   comportamento (ordem total, eleição, snapshot) é idêntico em todas as escalas.

---

### User Story 3 - Acessar o painel de cada nó a partir do host (Priority: P2)

Um operador abre, no navegador da máquina host, o painel de qualquer nó do cluster
conteinerizado para observar ordem local, ordem global, relógio vetorial, buffer e estado
global.

**Why this priority**: A observabilidade é parte do valor do sistema; se os painéis não
forem acessíveis a partir do host, a demonstração conteinerizada perde utilidade.

**Independent Test**: Com o cluster no ar, acessar o painel de cada nó pelo host e confirmar
que cada painel reflete o estado do seu próprio nó em tempo real.

**Acceptance Scenarios**:

1. **Given** o cluster conteinerizado em execução, **When** o operador acessa o endereço do
   painel de um nó a partir do host, **Then** o painel daquele nó é exibido e atualiza em
   tempo real.
2. **Given** vários painéis abertos, **When** comparados, **Then** cada painel reflete
   apenas o estado do seu próprio nó.

---

### User Story 4 - Build reproduzível da imagem (Priority: P3)

Um desenvolvedor gera a imagem do aplicativo a partir do código-fonte de forma
reproduzível, obtendo a mesma imagem funcional independentemente da máquina de build.

**Why this priority**: Garante que a imagem usada em qualquer host seja consistente. É
suporte à US1, mas o cluster já entrega valor com uma imagem pré-construída.

**Independent Test**: Executar o processo de build em máquinas diferentes e confirmar que a
imagem resultante sobe um nó funcional em ambas.

**Acceptance Scenarios**:

1. **Given** o código-fonte do projeto, **When** o desenvolvedor executa o processo de
   build da imagem, **Then** uma imagem executável do nó é produzida sem passos manuais
   adicionais.
2. **Given** a imagem construída, **When** um container é iniciado a partir dela com um id
   de nó, **Then** o nó sobe e ingressa no chat.

---

### Edge Cases

- **Container de nó reiniciado ou parado**: a queda de um container deve ser tratada como a
  queda de um nó — os demais continuam e, se for o líder, ocorre reeleição.
- **Ordem de inicialização dos containers**: nós que iniciam antes dos pares ainda
  indisponíveis não podem falhar permanentemente; devem operar quando os pares surgirem.
- **Conflito de portas no host**: a exposição dos painéis ao host não pode colidir com
  portas já em uso, e a falha deve ser sinalizada claramente.
- **Escala inconsistente**: subir menos containers do que o catálogo declara não pode
  corromper a ordem total entre os nós efetivamente ativos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir subir todo o cluster de nós como containers com um
  único comando de orquestração.
- **FR-002**: Cada nó MUST rodar em um container independente, com estado próprio e
  identificador de nó exclusivo.
- **FR-003**: Os nós conteinerizados MUST se comunicar exclusivamente pela rede entre
  containers, sem qualquer canal de coordenação compartilhado (memória, volume de disco,
  banco ou serviço externo).
- **FR-004**: O sistema MUST permitir escolher a escala do cluster (3, 8 ou 15 nós) apenas
  por configuração de execução, sem alterar ou recompilar o código.
- **FR-005**: O painel de cada nó MUST ser acessível a partir da máquina host por um
  endereço previsível e distinto por nó.
- **FR-006**: O sistema MUST produzir a imagem executável do nó a partir do código-fonte de
  forma reproduzível, sem passos manuais adicionais.
- **FR-007**: O catálogo de nós usado pelos containers MUST ser consistente com o conjunto
  de containers efetivamente iniciados (mesmos ids e endereços).
- **FR-008**: A parada do cluster MUST encerrar todos os containers de forma limpa.
- **FR-009**: A queda de um container de nó MUST ser tratada como a queda daquele nó pelos
  demais (incluindo reeleição de líder quando aplicável), sem derrubar o cluster.
- **FR-010**: O comportamento funcional do sistema (ordem total, unicast, snapshot, eleição)
  conteinerizado MUST ser equivalente ao da execução local.
- **FR-011**: A imagem MUST conter apenas o necessário para executar um nó (sem ferramentas
  de build no artefato final).

### Key Entities *(include if feature involves data)*

- **Imagem do nó**: artefato executável reproduzível que contém a aplicação de um nó.
- **Container de nó**: instância em execução da imagem, representando um nó com um id
  exclusivo e estado próprio.
- **Definição de cluster**: descrição de orquestração que enumera os containers de nó, sua
  rede e a exposição de painéis ao host, parametrizada pela escala.
- **Catálogo de nós**: lista de ids e endereços dos nós, consistente com os containers
  iniciados.
- **Rede de containers**: canal de comunicação entre os nós conteinerizados.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em uma máquina apenas com o runtime de containers, o operador sobe o cluster
  completo com um único comando, sem instalar dependências da aplicação.
- **SC-002**: O cluster sobe corretamente nas três escalas (3, 8 e 15 nós) sem qualquer
  alteração de código, e cada escala forma o chat.
- **SC-003**: 100% dos painéis de nó ficam acessíveis a partir do host, cada um em um
  endereço distinto, refletindo o estado do próprio nó em tempo real.
- **SC-004**: Mensagens de grupo no cluster conteinerizado são entregues em ordem total
  idêntica em todos os nós (paridade com a execução local).
- **SC-005**: A queda de um container de nó não derruba o cluster; quando o líder cai, um
  novo líder é eleito e a sequência continua sem lacunas.
- **SC-006**: A imagem do nó é construída a partir do código-fonte sem passos manuais e sobe
  um nó funcional em máquinas diferentes.

## Assumptions

- O ambiente-alvo possui um runtime de containers e uma ferramenta de orquestração
  multi-container disponíveis (ambiente de execução local/desenvolvimento).
- O `nos.json` (catálogo estático) é fornecido aos containers como configuração lida na
  inicialização — não é canal de coordenação em tempo de execução (Constituição).
- A escala-alvo permanece de até 15 nós, coerente com o projeto atual.
- O endereçamento continua derivado do id do nó (portas de rede entre nós e de painel),
  mapeado para o host de forma previsível.
- Persistência de dados entre execuções permanece fora de escopo; o estado de cada nó é
  efêmero e vive apenas na memória do seu container.
- Orquestração em cluster multi-máquina (ex.: agendadores distribuídos) está fora do escopo
  desta versão; o foco é execução em um único host.

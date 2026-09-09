# Phase 0 — Research: Interface de Chat com Balões e Conversas Privadas

## 1. Origem dos dados (reuso do estado existente)

- **Decisão**: Reutilizar o `NodeState`. O Grupo vem de `OrdemGlobal()` (mensagens de grupo
  na ordem total, com `DeOriginal` = remetente). Os PVs vêm de `Privadas()` (registros com
  `De`, `Para`, `Enviada`), agrupados pelo **par** = `Enviada ? Para : De`.
- **Rationale**: Todos os dados necessários já existem; a feature é de apresentação. Evita
  qualquer mudança na lógica distribuída ou nova troca de mensagens.
- **Alternativas descartadas**: Criar uma nova store de "chat" (duplicaria estado e correria
  risco de divergir da ordem total/entrega reais).

## 2. Separação Grupo × Privado

- **Decisão**: São coleções distintas no `NodeState` — mensagens de grupo nunca entram em
  `Privadas`, e vice-versa. A UI mostra o Grupo só a partir de `OrdemGlobal` e cada PV só a
  partir das privadas daquele par. Assim, uma direta jamais aparece no Grupo (FR-003).
- **Rationale**: A separação já é estrutural no estado; a UI apenas a respeita. Garante
  SC-002 por construção.

## 3. Distinção "minha mensagem" × "recebida"

- **Decisão**: Comparar o remetente com `NodeState.Id`. No Grupo, `DeOriginal == Id` → minha;
  no PV, `Enviada == true` → minha. Balões próprios alinhados à direita/estilo primário;
  recebidos à esquerda com rótulo do remetente.
- **Rationale**: Convenção universal de apps de chat; imediata (SC-004). Baseia-se só em
  dados locais.

## 4. Lista de conversas

- **Decisão**: Derivar do catálogo: "Grupo" + uma conversa por nó do catálogo, exceto o
  próprio (FR-009, FR-013 impede PV consigo mesmo). Ordem estável por id.
- **Rationale**: Lista previsível e completa (até 15 conversas), independente de já existir
  mensagem. Simplifica a navegação.
- **Alternativas descartadas**: Mostrar PV só quando há mensagem (esconde destinos válidos e
  complica iniciar conversa).

## 5. Estado de "não lida"

- **Decisão**: Estado **local ao painel** (efêmero). O componente guarda, por conversa, a
  contagem de mensagens já vista; ao chegar mensagem em conversa não ativa, marca não lida;
  abrir a conversa limpa o indicador. Nada disso trafega entre nós.
- **Rationale**: Atende FR-010/SC-006 sem violar a constituição (Princípio V) e sem
  persistência.
- **Alternativas descartadas**: Rastrear "lido" no núcleo (acoplaria UI ao núcleo e cruzaria
  responsabilidades).

## 6. Ordem de exibição e rolagem

- **Decisão**: O Grupo é renderizado na **ordem total** (por `seq`), não na ordem de chegada
  pela rede. A conversa ativa rola automaticamente para a última mensagem ao atualizar.
- **Rationale**: Preserva a garantia central do sistema também na UI (edge case) e dá
  sensação de chat (FR-008).

## 7. Observação técnica secundária

- **Decisão**: Manter ordem local de emissão, relógio vetorial, buffer e captura de estado
  global acessíveis em uma área/painel técnico secundário (ex.: aba/gaveta), fora do fluxo
  principal do chat.
- **Rationale**: A constituição (Princípio V / critérios do projeto) exige que o painel
  exiba esses dados; a feature apenas os torna secundários à experiência de chat (FR-012,
  SC-007).

## 8. Atualização em tempo real

- **Decisão**: Continuar assinando `NodeState.Changed` e re-renderizar; a rolagem ao fim é
  aplicada após a atualização da conversa ativa.
- **Rationale**: Mecanismo já usado no painel atual; sem nova infraestrutura.

## Resumo de pontos resolvidos

| Pergunta | Resolução |
|----------|-----------|
| De onde vêm as mensagens | `OrdemGlobal` (grupo) e `Privadas` agrupadas por par (PV) |
| Como separar privado do grupo | Coleções já distintas no estado; UI respeita |
| Minha × recebida | `DeOriginal==Id` (grupo) / `Enviada` (PV) |
| Lista de conversas | Grupo + um PV por nó do catálogo (exceto o próprio) |
| Não lida | Estado local efêmero do painel |
| Ordem/rolagem | Grupo por `seq`; auto-scroll ao fim |

Nenhum item permanece como NEEDS CLARIFICATION.

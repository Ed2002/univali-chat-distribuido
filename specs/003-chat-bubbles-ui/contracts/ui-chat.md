# Contract — Interface de Chat (UI)

Contrato da experiência do painel. É uma UI de aplicação; o "contrato" descreve estrutura,
regras de renderização e roteamento das ações.

## Layout

```
┌───────────────────────────────────────────────┐
│ top-nav: marca · nó {id} · líder/eleição/VC    │
├───────────────┬───────────────────────────────┤
│ Conversas     │ Cabeçalho da conversa ativa    │
│ • Grupo    (●)│ ───────────────────────────────│
│ • Nó 2        │  [balões da conversa ativa]    │
│ • Nó 3     (●)│  ...                            │
│ • Nó 4        │ ───────────────────────────────│
│               │ [campo de texto] [enviar]      │
├───────────────┴───────────────────────────────┤
│ ▸ Observação técnica (secundária, recolhível)  │
└───────────────────────────────────────────────┘
```

## Regras de conversa

- **UI-1**: A lista contém `Grupo` + uma conversa por nó do catálogo, exceto o próprio.
- **UI-2**: Selecionar uma conversa exibe **somente** as mensagens dela (UI-3/UI-4).
- **UI-3**: A conversa `Grupo` exibe as mensagens de `OrdemGlobal()` ordenadas por `seq`.
- **UI-4**: Uma conversa `Privado(peer)` exibe as mensagens de `Privadas()` cujo par é
  `peer` (`Enviada ? Para : De`).
- **UI-5**: Mensagens diretas NUNCA aparecem no `Grupo`; mensagens de grupo NUNCA aparecem
  em um PV.

## Regras de balão

- **UI-6**: Cada mensagem é um balão com a identificação do remetente.
- **UI-7**: `Propria = RemetenteId == meuId` → balão alinhado/estilizado como "minha"; senão
  "recebida", com rótulo do remetente.
- **UI-8**: O relógio vetorial da mensagem é exibido de forma discreta no balão.
- **UI-9**: Ao chegar/enviar mensagem na conversa ativa, rolar para a última.

## Roteamento de envio

- **UI-10**: Enviar no `Grupo` → difusão a todos (comportamento de grupo existente).
- **UI-11**: Enviar em `Privado(peer)` → unicast ao `peer` (comportamento de unicast
  existente).
- **UI-12**: Não permitir enviar direto ao próprio nó (sem PV consigo mesmo).

## Regras de "não lida"

- **UI-13**: Chegar mensagem em conversa não ativa → marcar indicador de não lida naquela
  conversa.
- **UI-14**: Abrir a conversa → limpar o indicador.
- **UI-15**: O estado de não lida é local ao painel e não trafega entre nós.

## Observação técnica (constituição)

- **UI-16**: Ordem local de emissão, relógio vetorial atual, buffer de seqs pendentes e
  captura/exibição de estado global permanecem acessíveis, de forma secundária.

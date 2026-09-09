# Quickstart — Interface de Chat com Balões e Conversas Privadas

Guia de validação da UI. Detalhes em [data-model.md](./data-model.md) e
[contracts/ui-chat.md](./contracts/ui-chat.md).

## Pré-requisitos

- SDK do .NET 10 (ou Docker). Subir ao menos 3 nós para observar grupo e PVs.

```bash
dotnet run --project src/ChatDistribuido -- --id 1
dotnet run --project src/ChatDistribuido -- --id 2
dotnet run --project src/ChatDistribuido -- --id 3
```

Painéis: http://127.0.0.1:8001, 8002, 8003.

## Cenários de validação

### V1 — Chat de grupo com balões (SC-001, SC-003)

1. Na conversa "Grupo", enviar mensagens de nós diferentes.
2. **Esperado**: cada mensagem é um balão identificando o remetente; a ordem dos balões é a
   mesma (ordem total) em todos os nós.

### V2 — Minha × recebida (SC-004)

1. Enviar do próprio nó e receber de outro.
2. **Esperado**: balões próprios distintos (alinhamento/estilo) dos recebidos, autoria clara.

### V3 — Conversa privada separada do grupo (SC-002)

1. No nó 1, abrir a conversa "Nó 3" e enviar uma mensagem direta.
2. **Esperado**: a mensagem aparece só no PV 1↔3 (nos dois lados) e **não** no Grupo de
   nenhum nó.

### V4 — Alternância entre conversas (SC-005)

1. Alternar entre "Grupo" e cada PV.
2. **Esperado**: cada conversa mostra apenas suas mensagens; a lista suporta até 15 conversas.

### V5 — Indicador de não lida (SC-006)

1. Ficar no "Grupo" e receber uma direta de outro nó.
2. **Esperado**: o PV daquele nó mostra indicador de não lida; abri-lo limpa o indicador.

### V6 — Observação técnica acessível (SC-007)

1. Abrir a área de observação técnica.
2. **Esperado**: ordem local de emissão, relógio vetorial, buffer e captura de estado global
   permanecem acessíveis.

## Testes automatizados

```bash
dotnet test
```

- **unit** (`ConversasTests`): agrupamento de privadas por par e separação Grupo × Privado.
- Os testes de integração existentes (ordem total, unicast, snapshot, reeleição, escala)
  seguem válidos — a feature não altera a semântica de entrega.

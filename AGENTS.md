# AGENTS.md

Guidance for AI Coding Agents when working with code in this repository.

## Workflow Orchestration

### 1. Plan Mode Default

- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately — don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity

### 2. Subagent Strategy

- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

### 3. Verification Before Done

- Never mark a task complete without proving it works
- Run the full test suite before considering work done
- Verify your changes against the existing behavior
- Ask yourself: "Would a staff engineer approve this?"

### 4. Demand Elegance (Balanced)

- For nontrivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes — don't over-engineer
- Challenge your own work before presenting it

### 5. Autonomous Bug Fixing

- When given a bug report: just fix it. Don't ask for hand-holding
- Run tests to identify the root cause
- Zero context switching required from the user
- Go fix failing tests without being told how

## Decision Records (ADR) — Obrigatório

- **Toda mudança** (arquitetural, de dependência, de estrutura de pastas, de padrão de código ou de decisão relevante) **deve** executar a skill `create-architectural-decision-record` para gerar uma ADR.
- A ADR deve ser salva na pasta **`docs/adr/`**, numerada sequencialmente (`NNNN-titulo-em-kebab-case.md`).
- Nenhuma tarefa é considerada concluída sem a ADR correspondente registrada (ver "3. Verification Before Done").
- Consulte `docs/adr/README.md` para a convenção completa.


## Arquitetura

### Tecnologias

- **Linguagem:** C# (.NET 10)
- **Aplicação do nó:** ASP.NET Core
- **Interface (painel do nó):** Blazor Server (atualização em tempo real via SignalR)
- **Comunicação entre nós:** sockets TCP (`System.Net.Sockets`), unicast ponto a ponto
- **Serialização:** JSON (`System.Text.Json`)
- **Execução em background:** `BackgroundService` / Hosted Services
- **Composição:** injeção de dependência nativa do .NET
- **Configuração:** catálogo estático `nos.json` (lista de nós, lida na inicialização)
- **Orquestração (opcional):** Docker Compose (um serviço por nó)

### Organização de pastas

```
ChatDistribuido/
├── ChatDistribuido.sln
├── README.md
├── docker-compose.yml                    # opcional: um serviço por nó
│
├── config/                               # catálogo estático de endereços
│   ├── nos-3.json
│   ├── nos-8.json
│   └── nos-15.json
│
├── scripts/
│   ├── gerar-config.ps1                  # gera nos-N.json
│   └── lancar.ps1                        # sobe N processos com --id/--config
│
├── docs/
│   ├── relatorio.md
│   ├── arquitetura.md
│   └── evidencias/                       # prints das filas de delivery
│
└── src/
    └── ChatDistribuido/                  # 1 build, 1 processo = 1 nó
        ├── ChatDistribuido.csproj
        ├── Program.cs                    # ponto de entrada (--id, --config)
        ├── appsettings.json
        │
        ├── Configuracao/                 # modelo e carregamento do nos.json
        │   ├── ConfigNos.cs
        │   └── CarregadorConfig.cs
        │
        ├── Rede/                         # camada de transporte
        │   ├── Mensagem.cs               # DTO JSON: tipo, origem, destino, vetor, seq, payload
        │   ├── TipoMensagem.cs
        │   ├── ServidorTcp.cs
        │   └── ClienteTcp.cs
        │
        ├── Nucleo/                       # lógica de ordenação e estado
        │   ├── EstadoNo.cs
        │   ├── RelogioVetorial.cs
        │   ├── ProcessadorEntrega.cs
        │   ├── Sequenciador.cs
        │   ├── Eleicao.cs
        │   └── Snapshot.cs
        │
        ├── Servicos/                     # fachada entre núcleo e interface
        │   ├── NoHostedService.cs
        │   ├── NoService.cs
        │   └── EventosNo.cs
        │
        ├── Components/                   # camada de apresentação (Blazor)
        │   ├── App.razor
        │   ├── Routes.razor
        │   ├── _Imports.razor
        │   ├── Layout/
        │   │   └── MainLayout.razor
        │   └── Pages/
        │       ├── Painel.razor
        │       └── EstadoGlobal.razor
        │
        └── wwwroot/
            └── app.css
```

### Camadas

- **Rede** — transporte (sockets TCP + serialização JSON)
- **Núcleo** — relógio vetorial, ordenação total, eleição e estado global; sem dependência da interface nem dos sockets
- **Serviços** — fachada de acesso ao núcleo e ponte de eventos para a interface
- **Components** — camada de apresentação (Blazor)

### Endereçamento

- Cada nó é um processo independente com estado próprio em memória privada
- **Porta TCP (rede entre nós):** `5000 + id`
- **Porta web (painel local do nó):** `8000 + id`
- Host de testes: `127.0.0.1`

### Formato de mensagem

JSON de linha única, delimitado por `\n` no canal TCP. Campos:

| Campo | Descrição |
|-------|-----------|
| `tipo` | tipo da mensagem |
| `origem` | id do nó de origem |
| `destino` | id do nó de destino (unicast) |
| `vetor` | relógio vetorial da origem |
| `seq` | número de sequência global |
| `payload` | conteúdo |

## Agentes

Agentes especializados disponíveis neste repositório. Delegue sempre para o agente mais específico que se encaixa na tarefa.

### Agentes do Projeto

- **Expert React Frontend Engineer** — use ao construir ou alterar UI em React: componentes, hooks, Server Components, Actions/formulários, tipos TypeScript, performance e acessibilidade.
- **gem-browser-tester** — use para validar comportamento em navegador real: fluxos E2E, checagens de UI/UX e regressão visual, auditorias de acessibilidade. Apenas verificação — nunca edita código.
- **TDD Refactor Phase** — use depois que os testes estão verdes para melhorar qualidade, reforçar segurança e aprimorar o design sem alterar comportamento nem quebrar testes.

### Agentes de Orquestração (built-in)

- **Explore** — busca paralela e somente leitura no código quando o escopo é incerto (ver "2. Subagent Strategy").
- **Plan** — desenha a abordagem de implementação antes de escrever código em tarefas não triviais.

### Quando Usar

- Prefira o especialista ao generalista sempre que a tarefa mapear claramente para um.
- Uma tarefa focada por agente (alinhado com "2. Subagent Strategy").
- Encadeie-os: Explore → Plan → construir (agente React) → validar (browser-tester) → TDD Refactor.

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Minimal code impact.
- **No Laziness**: Find root causes. No temporary workarounds. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.
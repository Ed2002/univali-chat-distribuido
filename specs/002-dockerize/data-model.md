# Phase 1 — Data Model: Conteinerização do Chat Distribuído

A feature é de empacotamento/orquestração; as "entidades" são artefatos de configuração de
container, não dados de domínio persistidos.

## Entidades

### Imagem do nó

Artefato executável reproduzível produzido pelo build multi-stage.

| Campo | Descrição / Regra |
|-------|-------------------|
| Base de build | `mcr.microsoft.com/dotnet/sdk:10.0` |
| Base de runtime | `mcr.microsoft.com/dotnet/aspnet:10.0` |
| Conteúdo final | Apenas o publish Release do `src/ChatDistribuido` (sem SDK/build tools — FR-011) |
| Entrypoint | Executa a aplicação do nó, recebendo `--id` e `--catalogo` |

### Container de nó (serviço Compose)

Instância em execução da imagem, representando um nó.

| Campo | Descrição / Regra |
|-------|-------------------|
| Nome do serviço | `no{id}` (host DNS na rede de containers) |
| `--id` | Id exclusivo do nó (1..N), coerente com o catálogo (FR-002, FR-007) |
| `--catalogo` | Caminho do catálogo de container montado/embutido |
| Porta TCP (interna) | `5000 + id`, não publicada ao host |
| Porta web | `8000 + id`, publicada como `"{8000+id}:{8000+id}"` (FR-005) |
| Rede | Rede bridge única do Compose (único canal entre nós — FR-003) |
| `restart` | `unless-stopped` (resiliência — FR-009) |

### Definição de cluster (arquivo Compose)

Descreve os N serviços de nó, a rede e a exposição de painéis; um arquivo por escala.

| Campo | Descrição / Regra |
|-------|-------------------|
| Escala | Número de serviços de nó: 3, 8 ou 15 (FR-004) |
| Serviços | Um por id do catálogo, todos usando a mesma imagem |
| Rede | Bridge compartilhada por todos os serviços |
| Catálogo referenciado | `docker/nos.docker{,.8,.15}.json` correspondente à escala |

### Catálogo de nós (container)

Lista estática lida na inicialização (não é canal de coordenação).

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `id` | int | Id do nó |
| `host` | string | Nome do serviço (`no{id}`) resolvido por DNS do Compose |
| `portaTcp` | int | `5000 + id` |

Regra: o conjunto de entradas MUST coincidir com os serviços do arquivo Compose da escala
(mesmos ids e nomes de host) — FR-007.

## Relações

- Uma **Definição de cluster** referencia N **Containers de nó** e um **Catálogo** coerente.
- Cada **Container de nó** executa a mesma **Imagem do nó** com `--id` distinto.
- Todos os **Containers de nó** compartilham uma **Rede de containers** (único canal).

## Ajustes no código-fonte (mínimos)

| Arquivo | Mudança | Motivo |
|---------|---------|--------|
| `Rede/TcpTransport.cs` | Bind do listener em `IPAddress.Any` | Aceitar conexões de outros containers |
| `Program.cs` | Host web em `http://0.0.0.0:{8000+id}` | Painel acessível via mapeamento de porta |

Nenhuma mudança na lógica distribuída (ordem total, eleição, snapshot).

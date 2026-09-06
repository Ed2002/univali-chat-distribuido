# Contract — Imagem do nó (Dockerfile multi-stage)

Contrato do artefato de imagem. A imagem final DEVE conter apenas o runtime + publish
(FR-011).

## Estágios

1. **build** (`mcr.microsoft.com/dotnet/sdk:10.0`)
   - Copia o código e restaura/publica: `dotnet publish src/ChatDistribuido -c Release -o /app`.
2. **runtime** (`mcr.microsoft.com/dotnet/aspnet:10.0`)
   - Copia `/app` do estágio de build.
   - `ENTRYPOINT ["dotnet", "ChatDistribuido.dll"]`.

## Invariantes

- **IMG-1**: A imagem final NÃO contém o SDK nem ferramentas de build.
- **IMG-2**: O container aceita os argumentos `--id <n>` e `--catalogo <caminho>` (via
  `command` do serviço).
- **IMG-3**: O build é reproduzível a partir do código-fonte, sem passos manuais (FR-006).
- **IMG-4**: O `.dockerignore` exclui `bin/`, `obj/`, `specs/`, `.git/` do contexto de build.

## Parâmetros de execução

| Argumento | Exemplo | Efeito |
|-----------|---------|--------|
| `--id` | `--id 1` | Define o id do nó (porta TCP `5000+id`, web `8000+id`) |
| `--catalogo` | `--catalogo /app/nos.docker.json` | Catálogo estático lido na inicialização |

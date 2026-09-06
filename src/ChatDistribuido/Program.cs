using ChatDistribuido.Components;
using ChatDistribuido.Nucleo;
using ChatDistribuido.Rede;
using ChatDistribuido.Servicos;

var builder = WebApplication.CreateBuilder(args);

// ---- id do nó e catálogo (lido só na inicialização) ----
var id = LerId(args)
         ?? throw new InvalidOperationException("Informe o id do nó: --id <n>");
var caminhoCatalogo = LerOpcao(args, "--catalogo")
                      ?? Path.Combine(AppContext.BaseDirectory, "nos.json");
var catalogo = CatalogoNos.Carregar(caminhoCatalogo);
if (!catalogo.Contem(id))
    throw new InvalidOperationException($"Id {id} não consta no catálogo {caminhoCatalogo}.");

// Painel web em 8000+id. Liga a 0.0.0.0 para ser alcançável via mapeamento de porta do
// container; ainda assim, o painel só fala com o próprio processo do nó.
builder.WebHost.UseUrls($"http://0.0.0.0:{8000 + id}");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<IMessageChannel>(_ => new TcpTransport(id, catalogo));
builder.Services.AddSingleton(sp =>
    new NodeService(id, catalogo, sp.GetRequiredService<IMessageChannel>()));

var app = builder.Build();

// Sobe TCP em 5000+id e o núcleo do nó.
app.Services.GetRequiredService<NodeService>().Start();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

static int? LerId(string[] args)
{
    var v = LerOpcao(args, "--id");
    return v is not null && int.TryParse(v, out var n) ? n : null;
}

static string? LerOpcao(string[] args, string nome)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == nome) return args[i + 1];
    return null;
}

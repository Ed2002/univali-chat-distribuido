using Xunit;

namespace ChatDistribuido.Tests.Integration;

public class SnapshotTests
{
    [Fact]
    public async Task Captura_estado_global_sem_parar_o_chat()
    {
        using var cluster = new Cluster(3);
        await Cluster.Esperar(() => cluster.Nos.Values.All(n => n.State.LiderAtual == 3));

        // Tráfego antes da captura.
        cluster.Nos[1].EnviarGrupo("x");
        await Cluster.Esperar(() => cluster.Nos.Values.All(n => n.State.OrdemGlobal().Count == 1));

        // Dispara o snapshot sob tráfego.
        cluster.Nos[2].DispararSnapshot();
        cluster.Nos[3].EnviarGrupo("y");

        var capturou = await Cluster.Esperar(() => cluster.Nos[2].State.UltimoSnapshot is not null);
        Assert.True(capturou, "o iniciador não concluiu o snapshot global");

        // O snapshot menciona os 3 nós (retrato de todos).
        var texto = cluster.Nos[2].State.UltimoSnapshot!;
        Assert.Contains("nó 1", texto);
        Assert.Contains("nó 2", texto);
        Assert.Contains("nó 3", texto);

        // O chat continua funcionando após a captura.
        cluster.Nos[1].EnviarGrupo("z");
        var continua = await Cluster.Esperar(() =>
            cluster.Nos.Values.All(n => n.State.OrdemGlobal().Count == 3));
        Assert.True(continua, "o chat não continuou operando após a captura");
    }
}

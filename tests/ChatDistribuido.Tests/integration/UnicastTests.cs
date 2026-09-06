using Xunit;

namespace ChatDistribuido.Tests.Integration;

public class UnicastTests
{
    [Fact]
    public async Task Mensagem_privada_chega_apenas_ao_destinatario()
    {
        using var cluster = new Cluster(3);
        await Cluster.Esperar(() => cluster.Nos.Values.All(n => n.State.LiderAtual == 3));

        cluster.Nos[1].EnviarUnicast(3, "oi privado");

        var recebeu = await Cluster.Esperar(() =>
            cluster.Nos[3].State.Privadas().Any(p => !p.Enviada && p.Conteudo == "oi privado"));
        Assert.True(recebeu, "destinatário não recebeu a mensagem privada");

        // Nó 2 não deve receber nada privado.
        Assert.DoesNotContain(cluster.Nos[2].State.Privadas(), p => !p.Enviada);
    }

    [Fact]
    public async Task Unicasts_preservam_ordem_de_emissao()
    {
        using var cluster = new Cluster(3);
        await Cluster.Esperar(() => cluster.Nos.Values.All(n => n.State.LiderAtual == 3));

        cluster.Nos[1].EnviarUnicast(2, "um");
        cluster.Nos[1].EnviarUnicast(2, "dois");
        cluster.Nos[1].EnviarUnicast(2, "tres");

        await Cluster.Esperar(() => cluster.Nos[2].State.Privadas().Count(p => !p.Enviada) == 3);

        var recebidas = cluster.Nos[2].State.Privadas()
            .Where(p => !p.Enviada).Select(p => p.Conteudo).ToList();
        Assert.Equal(new[] { "um", "dois", "tres" }, recebidas);
    }
}

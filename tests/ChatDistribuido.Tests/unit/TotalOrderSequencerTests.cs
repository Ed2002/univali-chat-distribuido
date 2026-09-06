using ChatDistribuido.Nucleo;
using Xunit;

namespace ChatDistribuido.Tests.Unit;

public class TotalOrderSequencerTests
{
    [Fact]
    public void Atribui_seqs_monotonicos_a_partir_de_1()
    {
        var seq = new TotalOrderSequencer();

        Assert.Equal(1, seq.Atribuir());
        Assert.Equal(2, seq.Atribuir());
        Assert.Equal(3, seq.Atribuir());
        Assert.Equal(4, seq.ProximoSeq);
    }

    [Fact]
    public void Retomar_apos_reeleicao_continua_sem_lacunas()
    {
        var seq = new TotalOrderSequencer();
        seq.Retomar(maiorSeqEntregue: 7); // novo líder soube que 7 já foi entregue

        Assert.Equal(8, seq.Atribuir());
        Assert.Equal(9, seq.Atribuir());
    }

    [Fact]
    public void Retomar_nunca_retrocede_a_sequencia()
    {
        var seq = new TotalOrderSequencer();
        seq.Atribuir(); // 1
        seq.Atribuir(); // 2, proximo = 3

        seq.Retomar(maiorSeqEntregue: 1); // menor que o estado atual

        Assert.Equal(3, seq.Atribuir()); // não retrocede
    }
}

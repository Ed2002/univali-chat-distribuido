using ChatDistribuido.Nucleo;
using Xunit;

namespace ChatDistribuido.Tests.Unit;

public class DeliveryBufferTests
{
    private static MensagemSequenciada Msg(long seq) => new(seq, 1, $"m{seq}", null);

    [Fact]
    public void Entrega_em_ordem_quando_seqs_chegam_contiguos()
    {
        var buffer = new DeliveryBuffer();

        var e1 = buffer.Receber(Msg(1));
        var e2 = buffer.Receber(Msg(2));

        Assert.Equal(new long[] { 1 }, e1.Select(m => m.Seq));
        Assert.Equal(new long[] { 2 }, e2.Select(m => m.Seq));
        Assert.Equal(3, buffer.ProximoSeqEsperado);
    }

    [Fact]
    public void Retem_seq_futuro_e_libera_ao_preencher_a_lacuna()
    {
        var buffer = new DeliveryBuffer();

        var fora = buffer.Receber(Msg(3)); // chega antes de 1 e 2
        Assert.Empty(fora);
        Assert.Contains(3L, buffer.SeqsPendentes);

        buffer.Receber(Msg(1));
        var flush = buffer.Receber(Msg(2)); // agora 2 e 3 tornam-se entregáveis

        Assert.Equal(new long[] { 2, 3 }, flush.Select(m => m.Seq));
        Assert.Empty(buffer.SeqsPendentes);
        Assert.Equal(4, buffer.ProximoSeqEsperado);
    }

    [Fact]
    public void Descarta_duplicatas_de_seqs_ja_entregues()
    {
        var buffer = new DeliveryBuffer();
        buffer.Receber(Msg(1));

        var dup = buffer.Receber(Msg(1));

        Assert.Empty(dup);
        Assert.Equal(2, buffer.ProximoSeqEsperado);
    }

    [Fact]
    public void Nao_produz_lacunas_com_chegada_desordenada()
    {
        var buffer = new DeliveryBuffer();
        var entregues = new List<long>();

        foreach (var seq in new long[] { 2, 4, 1, 5, 3 })
            entregues.AddRange(buffer.Receber(Msg(seq)).Select(m => m.Seq));

        Assert.Equal(new long[] { 1, 2, 3, 4, 5 }, entregues);
    }
}

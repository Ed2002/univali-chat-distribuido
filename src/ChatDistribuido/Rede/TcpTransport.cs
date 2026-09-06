using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ChatDistribuido.Nucleo;

namespace ChatDistribuido.Rede;

/// <summary>
/// Transporte TCP: um listener em <c>5000+id</c> e uma conexão de saída por par.
/// Enquadramento por length-prefix (int32 big-endian) + payload JSON. A ordem FIFO por
/// canal é garantida pelo próprio TCP.
/// </summary>
public sealed class TcpTransport : IMessageChannel, IDisposable
{
    private readonly int _id;
    private readonly CatalogoNos _catalogo;
    private readonly TcpListener _listener;
    private readonly Dictionary<int, PeerSaida> _saidas = new();
    private readonly object _saidasGate = new();
    private volatile bool _rodando;

    public event Action<Envelope>? OnEnvelope;

    public TcpTransport(int id, CatalogoNos catalogo)
    {
        _id = id;
        _catalogo = catalogo;
        // Escuta em todas as interfaces (0.0.0.0) para aceitar conexões de outros containers;
        // a conexão de saída usa o host do catálogo (nome de serviço ou IP). A porta é a do
        // catálogo (fonte da verdade); a convenção 5000+id vive no nos.json.
        _listener = new TcpListener(IPAddress.Any, catalogo[id].PortaTcp);
        Pares = catalogo.Ids.Where(x => x != id).ToList();
    }

    public IReadOnlyList<int> Pares { get; }

    public void Start()
    {
        _rodando = true;
        _listener.Start();
        _ = Task.Run(AceitarLoopAsync);
    }

    private async Task AceitarLoopAsync()
    {
        while (_rodando)
        {
            TcpClient cliente;
            try
            {
                cliente = await _listener.AcceptTcpClientAsync();
            }
            catch when (!_rodando)
            {
                break;
            }
            catch
            {
                continue;
            }
            _ = Task.Run(() => LerLoopAsync(cliente));
        }
    }

    private async Task LerLoopAsync(TcpClient cliente)
    {
        using var _ = cliente;
        using var stream = cliente.GetStream();
        var header = new byte[4];
        while (_rodando)
        {
            try
            {
                if (!await LerExatoAsync(stream, header, 4)) break;
                var len = BinaryPrimitives.ReadInt32BigEndian(header);
                if (len <= 0 || len > 64 * 1024 * 1024) break;
                var buf = new byte[len];
                if (!await LerExatoAsync(stream, buf, len)) break;
                var env = JsonSerializer.Deserialize<Envelope>(buf, JsonSerializerConfig.Options);
                if (env is not null)
                    OnEnvelope?.Invoke(env);
            }
            catch
            {
                break;
            }
        }
    }

    private static async Task<bool> LerExatoAsync(NetworkStream stream, byte[] buffer, int total)
    {
        var lido = 0;
        while (lido < total)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(lido, total - lido));
            if (n == 0) return false;
            lido += n;
        }
        return true;
    }

    public async Task<bool> SendAsync(int destId, Envelope env)
    {
        if (!_catalogo.Contem(destId)) return false;
        PeerSaida peer;
        lock (_saidasGate)
        {
            if (!_saidas.TryGetValue(destId, out peer!))
            {
                peer = new PeerSaida(_catalogo[destId]);
                _saidas[destId] = peer;
            }
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(env, JsonSerializerConfig.Options);
            await peer.EnviarAsync(bytes);
            return true;
        }
        catch
        {
            // Destino indisponível: descarta a conexão e não bloqueia os demais (FR-016).
            lock (_saidasGate)
            {
                if (_saidas.TryGetValue(destId, out var p) && ReferenceEquals(p, peer))
                    _saidas.Remove(destId);
            }
            peer.Dispose();
            return false;
        }
    }

    public void Dispose()
    {
        _rodando = false;
        try { _listener.Stop(); } catch { }
        lock (_saidasGate)
        {
            foreach (var p in _saidas.Values) p.Dispose();
            _saidas.Clear();
        }
    }

    /// <summary>Conexão de saída para um par, com envio serializado (FIFO).</summary>
    private sealed class PeerSaida : IDisposable
    {
        private readonly NoInfo _info;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private TcpClient? _cliente;
        private NetworkStream? _stream;

        public PeerSaida(NoInfo info) => _info = info;

        public async Task EnviarAsync(byte[] payload)
        {
            await _gate.WaitAsync();
            try
            {
                if (_cliente is null || !_cliente.Connected)
                {
                    _cliente?.Dispose();
                    _cliente = new TcpClient();
                    await _cliente.ConnectAsync(_info.Host, _info.PortaTcp);
                    _stream = _cliente.GetStream();
                }

                var header = new byte[4];
                BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
                await _stream!.WriteAsync(header);
                await _stream!.WriteAsync(payload);
                await _stream!.FlushAsync();
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            try { _stream?.Dispose(); } catch { }
            try { _cliente?.Dispose(); } catch { }
        }
    }
}

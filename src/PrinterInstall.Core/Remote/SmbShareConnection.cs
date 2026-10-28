using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using PrinterInstall.Core.Auth;

namespace PrinterInstall.Core.Remote;

/// <summary>
/// Conecta a um compartilhamento SMB do Windows (ex: \\host\ADMIN$ ou \\host\IPC$) com credenciais explícitas e libera no dispose.
/// </summary>
public sealed class SmbShareConnection : IDisposable
{
    private const int ResourceTypeAny = 0x00000000;
    private const int ResourceTypeDisk = 0x00000001;

    private const int ErrorAlreadyAssigned = 85;
    private const int ErrorDeviceAlreadyRemembered = 1202;
    private const int ErrorSessionCredentialConflict = 1219;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> ServerAliases = new();

    private readonly string _remoteName;
    private bool _disposed;

    private SmbShareConnection(string remoteName)
    {
        _remoteName = remoteName;
    }

    /// <summary>
    /// Abre uma conexão para \\host\shareName (ex: shareName = ADMIN$ ou IPC$).
    /// </summary>
    public static SmbShareConnection Open(string host, string shareName, NetworkCredential credential)
    {
        var cleanHost = host.Trim();
        var cleanShare = shareName.Trim('\\', '/');
        var server = ResolveServerName(cleanHost);
        var remote = $@"\\{server}\{cleanShare}";
        var resourceType = cleanShare.Equals("IPC$", StringComparison.OrdinalIgnoreCase) ? ResourceTypeAny : ResourceTypeDisk;
        var user = CredentialHelper.BuildCredentialUserName(credential);
        var password = credential.Password ?? "";

        var code = Connect(remote, resourceType, user, password);

        if (code is ErrorSessionCredentialConflict or ErrorAlreadyAssigned or ErrorDeviceAlreadyRemembered)
        {
            PurgeHostConnections(server, remote);
            Thread.Sleep(100);

            code = Connect(remote, resourceType, user, password);
        }

        // Sessões abertas pelo Explorer ou por unidades mapeadas do usuário não aparecem como
        // conexões nossas e não podem ser derrubadas. O redirecionador SMB separa sessões pelo
        // nome do servidor, então outro nome para o mesmo destino aceita a conta informada.
        if (code == ErrorSessionCredentialConflict)
        {
            foreach (var alias in GetAlternateServerNames(cleanHost, Dns.GetHostEntry))
            {
                if (alias.Equals(server, StringComparison.OrdinalIgnoreCase))
                    continue;

                var aliasRemote = $@"\\{alias}\{cleanShare}";
                if (Connect(aliasRemote, resourceType, user, password) != 0)
                    continue;

                ServerAliases[NormalizeHostKey(cleanHost)] = alias;
                return new SmbShareConnection(aliasRemote);
            }
        }

        // Uma conexão existente com outra identidade não valida a conta informada.
        if (code != 0)
            throw new Win32Exception(code,
                $"Falha SMB em {remote} (Win32 {code}): {new Win32Exception(code).Message}");

        return new SmbShareConnection(remote);
    }

    /// <summary>
    /// Nome de servidor a usar em caminhos UNC para o host. Depois de um conflito de credenciais
    /// (Win32 1219) pode ser um nome alternativo do mesmo destino; caminhos montados com o nome
    /// original cairiam na sessão SMB da outra conta.
    /// </summary>
    public static string ResolveServerName(string host)
    {
        var cleanHost = host.Trim();
        return ServerAliases.TryGetValue(NormalizeHostKey(cleanHost), out var alias) ? alias : cleanHost;
    }

    internal static IReadOnlyList<string> GetAlternateServerNames(string host, Func<string, IPHostEntry> resolve)
    {
        var cleanHost = host.Trim().TrimEnd('.');
        var names = new List<string>();

        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (names.Contains(name, StringComparer.OrdinalIgnoreCase)) return;
            if (name.Equals(host.Trim(), StringComparison.OrdinalIgnoreCase)) return;
            names.Add(name);
        }

        IPHostEntry? entry = null;
        try { entry = resolve(cleanHost); }
        catch { /* Sem DNS reverso/direto ainda resta o nome com ponto final. */ }

        if (IPAddress.TryParse(cleanHost, out _))
        {
            var fqdn = entry?.HostName;
            if (!string.IsNullOrWhiteSpace(fqdn) && !IPAddress.TryParse(fqdn, out _))
            {
                Add(fqdn);
                Add(fqdn.TrimEnd('.') + ".");
                var shortName = fqdn.Split('.')[0];
                Add(shortName);
            }
        }
        else
        {
            Add(cleanHost + ".");
            if (entry is not null)
            {
                Add(entry.HostName);
                foreach (var address in entry.AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                    Add(address.ToString());
            }
        }

        return names;
    }

    private static int Connect(string remote, int resourceType, string? user, string password)
    {
        var netResource = new NetResource
        {
            ResourceType = resourceType,
            RemoteName = remote
        };
        return WNetAddConnection2(netResource, password, user, 0);
    }

    private static string NormalizeHostKey(string host) => host.Trim().TrimEnd('.').ToUpperInvariant();

    public static async Task<SmbShareConnection> OpenAsync(
        string host, string shareName, NetworkCredential credential, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pending = Task.Run(() => Open(host, shareName, credential), cancellationToken);
        try
        {
            return await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // WNetAddConnection2 não recebe CancellationToken. Não espera a negociação,
            // mas libera uma conexão que eventualmente termine depois do cancelamento.
            _ = pending.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully) t.Result.Dispose();
                else if (t.IsFaulted) _ = t.Exception;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
    }

    private static void PurgeHostConnections(string host, string remote)
    {
        _ = WNetCancelConnection2(remote, 0, true);
        _ = WNetCancelConnection2($@"\\{host}\IPC$", 0, true);
        _ = WNetCancelConnection2($@"\\{host}\ADMIN$", 0, true);
        _ = WNetCancelConnection2($@"\\{host}", 0, true);
    }

    public string RemoteRoot => _remoteName;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ = WNetCancelConnection2(_remoteName, 0, true);
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(NetResource netResource, string? password, string? username, int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class NetResource
    {
        public int Scope;
        public int ResourceType;
        public int DisplayType;
        public int Usage;
        public string? LocalName;
        public string? RemoteName;
        public string? Comment;
        public string? Provider;
    }
}

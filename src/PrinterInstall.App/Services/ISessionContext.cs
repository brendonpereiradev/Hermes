using System.Net;

namespace PrinterInstall.App.Services;

public interface ISessionContext
{
    NetworkCredential? Credential { get; set; }
    string DomainName { get; set; }

    /// <summary>
    /// Sessão aberta sem domínio (uso doméstico): só o computador local pode ser destino.
    /// </summary>
    bool IsLocalOnly { get; set; }
}

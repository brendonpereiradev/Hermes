using System.Text.RegularExpressions;
using PrinterInstall.Core.Catalog;
using PrinterInstall.Core.Models;

namespace PrinterInstall.Core.Validation;

public static partial class PrinterCompatibilityValidator
{
    private static readonly IReadOnlyDictionary<PrinterBrand, string[]> ApprovedModels = new Dictionary<PrinterBrand, string[]>
    {
        [PrinterBrand.Epson] = ["M1180", "WF-M5899", "WF-M5799", "WF-C5890", "WF-C5790"],
        // Famílias Lexmark (sem sufixo de configuração) listadas sem restrições no
        // Lexmark UPD Technical White Paper Rev. 3a (UPD 3.1.0.0).
        [PrinterBrand.Lexmark] =
        [
            // Laser mono
            "MS321", "MS331", "MS421", "MS431", "MS521", "MS531", "MS621", "MS622", "MS631", "MS632", "MS725",
            "MS821", "MS822", "MS823", "MS824", "MS825", "MS826",
            "B2338", "B2442", "B2546", "B2650", "B2865", "B3340", "B3442",
            // Multifuncional mono
            "MX321", "MX331", "MX421", "MX431", "MX432", "MX521", "MX522", "MX532", "MX622", "MX632",
            "MX721", "MX722", "MX725", "MX822", "MX824", "MX826", "MX931",
            "MB2338", "MB2442", "MB2546", "MB2650", "MB2770", "MB3442",
            // Laser colorida
            "CS331", "CS421", "CS431", "CS521", "CS531", "CS622", "CS632", "CS720", "CS725", "CS727", "CS728",
            "CS730", "CS735", "CS737", "CS820", "CS827", "CS921", "CS923", "CS927", "CS943", "CS963",
            "C2240", "C2326", "C2335", "C2425", "C2535", "C3224", "C3326", "C3426", "C4342", "C4352",
            // Multifuncional colorida
            "CX331", "CX421", "CX431", "CX522", "CX532", "CX622", "CX625", "CX635", "CX725", "CX727", "CX730",
            "CX735", "CX737", "CX820", "CX825", "CX827", "CX833", "CX860", "CX920", "CX921", "CX922", "CX923",
            "CX924", "CX927", "CX961", "CX962", "CX963",
            "MC2325", "MC2425", "MC2535", "MC2640", "MC3224", "MC3326", "MC3426"
        ],
        [PrinterBrand.Brother] = ["HL-L5212DW"],
        [PrinterBrand.Gainscha] = ["GA-2408T"]
    };

    [GeneratedRegex(@"^[A-Z]+\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LexmarkFamilyPattern();

    public static (bool Compatible, string Message) Check(PrinterIdentityResult identity, PrinterBrand selectedBrand)
    {
        if (!ApprovedModels.TryGetValue(selectedBrand, out var approvedModels))
            return (false, $"Marca selecionada inválida para a impressora em {identity.Host}.");

        if (identity.ConflictingEvidence)
            return (false, $"Identificação conflitante da impressora em {identity.Host}: {identity.Detail}");

        if (identity.Brand is null)
            return (false, $"Não foi possível identificar a impressora em {identity.Host}: {identity.Detail ?? "fabricante desconhecido"}");

        if (identity.Brand != selectedBrand)
            return (false, $"A impressora {identity.Host} foi identificada como {identity.Brand} {identity.Model}, enquanto o modelo selecionado é {selectedBrand}.");

        if (string.IsNullOrWhiteSpace(identity.Model))
            return (false, $"Não foi possível identificar o modelo da impressora {selectedBrand} em {identity.Host}.");

        // Lexmark varia o sufixo por configuração (MX622ade/adhe/adthe); compara-se a família.
        var model = selectedBrand == PrinterBrand.Lexmark
            ? LexmarkFamilyPattern().Match(identity.Model).Value
            : identity.Model;
        if (!approvedModels.Contains(model, StringComparer.OrdinalIgnoreCase))
            return (false, $"Modelo {identity.Model} em {identity.Host} não homologado para o driver {PrinterCatalog.GetExpectedDriverName(selectedBrand)}.");

        return (true, $"{identity.Brand} {identity.Model} em {identity.Host} identificado via {identity.Source}.");
    }
}

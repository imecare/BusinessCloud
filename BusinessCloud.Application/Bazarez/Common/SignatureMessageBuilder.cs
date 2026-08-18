namespace BusinessCloud.Application.Bazares.Common;

public static class SignatureMessageBuilder
{
    public static string Build(string? bazarName, string customerName, IReadOnlyList<string> proofUrls)
    {
        var bazar = string.IsNullOrWhiteSpace(bazarName) ? "el Bazar" : bazarName.Trim();
        var customer = string.IsNullOrWhiteSpace(customerName) ? "cliente" : customerName.Trim();
        var proofs = proofUrls.Count == 0
            ? "(Aún no hay imágenes de firma disponibles.)"
            : string.Join("\n", proofUrls);

        return $"Hola {customer} 👋\n\n" +
            "📦 *¡Tu paquete fue entregado!*\n" +
            "Consulta tu firma / comprobante de recibido en las siguientes imágenes:\n" +
            $"{proofs}\n\n" +
            $"¡Gracias por tu compra con {bazar}! 💛";
    }
}

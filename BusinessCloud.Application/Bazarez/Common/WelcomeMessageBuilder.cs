using System.Text;

namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Arma el mensaje de bienvenida que el bazar envía (semi-manual) a un cliente por WhatsApp
/// o Messenger. Explica cómo recibirá sus totales, qué encontrará en su comprobante y los
/// canales de contacto del bazar. El bloque "cómo recibirás tus totales" se adapta al canal:
/// por WhatsApp aclara que los avisos llegan del número del SISTEMA de Bazares (plataforma
/// compartida por varios bazares); por Messenger indica que llegarán por ese mismo chat.
/// El complemento configurable del bazar se inserta al final, justo antes del cierre.
/// </summary>
public static class WelcomeMessageBuilder
{
    public const string WhatsAppChannel = "whatsapp";
    public const string MessengerChannel = "messenger";

    public static string Build(
        string channel,
        string? bazarName,
        string customerName,
        string? systemNumber,
        string? bazarWhatsAppLink,
        string? bazarMessengerLink,
        string? complement)
    {
        var bazar = string.IsNullOrWhiteSpace(bazarName) ? "el Bazar" : bazarName.Trim();
        var customer = string.IsNullOrWhiteSpace(customerName) ? string.Empty : " " + customerName.Trim();
        var isMessenger = string.Equals(channel, MessengerChannel, StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.Append("*¡Bienvenido(a) a Bazar ").Append(bazar).AppendLine("! 👋*");
        sb.AppendLine();
        sb.Append("Hola").Append(customer)
          .AppendLine(", ¡gracias por comprar con nosotros! Te damos la bienvenida y en 1 minuto te explicamos cómo funciona 👇");
        sb.AppendLine();

        sb.AppendLine("📲 *¿Cómo recibirás tus totales?*");
        if (isMessenger)
        {
            sb.AppendLine("Te enviaremos el *total de tu compra* por este mismo Messenger cuando cerremos la venta. Revisa tus mensajes para no perderte tus avisos.");
            sb.AppendLine("📱 *Es necesario que nos envíes tu número de WhatsApp*, ya que es el *canal oficial* de envío de totales. Así te aseguras de recibir todos tus avisos a tiempo.");
        }
        else
        {
            sb.AppendLine("Al cerrar cada venta te enviaremos el *total de tu compra* por WhatsApp desde el *número del sistema de Bazares* (la plataforma que usamos varios bazares para enviarte tus avisos):");
            if (!string.IsNullOrWhiteSpace(systemNumber))
                sb.Append("📞 ").AppendLine(systemNumber.Trim());
            sb.AppendLine("⚠️ Es un *número automático de la plataforma*, no del bazar: no respondas ahí, nadie lo lee. Guárdalo como \"Bazares (avisos)\" — desde ese mismo número podrías recibir avisos de otros bazares que usen el sistema.");
        }
        sb.AppendLine();

        sb.AppendLine("🧾 *Tu enlace personal de comprobante*");
        sb.AppendLine("Cada total trae un enlace propio donde podrás:");
        sb.AppendLine("• Ver el *detalle y el estado* de tu compra");
        sb.AppendLine("• Consultar las *tarjetas / formas de pago*");
        sb.AppendLine("• *Subir tu comprobante* de pago");
        sb.AppendLine("• Ver la *firma o foto de recibido* de tu entrega");

        if (!string.IsNullOrWhiteSpace(bazarWhatsAppLink) || !string.IsNullOrWhiteSpace(bazarMessengerLink))
        {
            sb.AppendLine();
            sb.AppendLine("💬 *¿Dudas? Escríbenos directo:*");
            if (!string.IsNullOrWhiteSpace(bazarWhatsAppLink))
                sb.Append("• WhatsApp con ").Append(bazar).Append(": ").AppendLine(bazarWhatsAppLink.Trim());
            if (!string.IsNullOrWhiteSpace(bazarMessengerLink))
                sb.Append("• Messenger con ").Append(bazar).Append(": ").AppendLine(bazarMessengerLink.Trim());
        }

        var extra = complement?.Trim();
        if (!string.IsNullOrWhiteSpace(extra))
        {
            sb.AppendLine();
            sb.AppendLine(extra);
        }

        sb.AppendLine();
        sb.Append("¡Gracias por ser parte de ").Append(bazar).Append("! 💛");

        return sb.ToString();
    }
}

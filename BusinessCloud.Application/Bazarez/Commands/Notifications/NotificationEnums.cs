namespace BusinessCloud.Application.Bazares.Commands.Notifications;

public static class NotificationType
{
    public const int PaymentReminder = 1;
    public const int DueToday = 2;
    public const int SaleCancelled = 3;
    public const int ProofValidated = 4;
}

public static class NotificationChannelStrategy
{
    public const int OnlyWebPush = 1;
    public const int OnlyWhatsApp = 2;
    public const int Hybrid = 3;
}

public static class NotificationChannel
{
    public const int WebPush = 1;
    public const int WhatsApp = 2;
}

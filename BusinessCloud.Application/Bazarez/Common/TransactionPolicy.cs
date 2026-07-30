namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Política de transacciones (envío de totales) del módulo Bazares.
/// Una transacción = envío de total a un cliente (WhatsApp o manual "sin WhatsApp").
/// </summary>
public static class TransactionPolicy
{
    /// <summary>Pool vitalicio máximo de transacciones de cortesía por empresa.</summary>
    public const int CourtesyLimit = 50;

    /// <summary>Umbral para avisar "saldo bajo" en el banner global.</summary>
    public const int LowBalanceThreshold = 30;
}
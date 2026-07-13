namespace BusinessCloud.Domain.Common.Exceptions;

/// <summary>
/// Se lanza al intentar dar de alta o renombrar un recolector con un nombre que ya existe.
/// - "COLLECTOR_NAME_SAME_GROUP": ya existe en el mismo grupo → no se permite (definitivo).
/// - "COLLECTOR_NAME_OTHER_GROUP": existe en otro grupo → se permite solo bajo confirmación.
/// </summary>
public class CollectorNameConflictException : Exception
{
    public string Code { get; }
    public string? ExistingGroupDescription { get; }

    public CollectorNameConflictException(string message, string code, string? existingGroupDescription)
        : base(message)
    {
        Code = code;
        ExistingGroupDescription = existingGroupDescription;
    }
}

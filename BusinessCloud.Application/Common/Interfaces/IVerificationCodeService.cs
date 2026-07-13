namespace BusinessCloud.Application.Common.Interfaces;

/// <summary>
/// Genera y valida códigos de verificación (OTP) de un solo uso con expiración,
/// usados para autorizar operaciones sensibles (alta/edición/baja de usuarios).
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>
    /// Crea un nuevo desafío OTP para un propósito y sujeto (usuario) dados.
    /// Devuelve el identificador del desafío y el código generado.
    /// </summary>
    (string ChallengeId, string Code) Create(string purpose, string subjectId, TimeSpan ttl);

    /// <summary>
    /// Valida y consume un desafío OTP. Devuelve true solo si coincide el código,
    /// el propósito y el sujeto, y no ha expirado ni superado los intentos.
    /// </summary>
    bool Validate(string challengeId, string code, string purpose, string subjectId);
}

using Microsoft.AspNetCore.Identity;

namespace BusinessCloud.Api.Common;

/// <summary>
/// Traduce al español los mensajes de error de ASP.NET Core Identity
/// (validación de contraseña, usuarios, etc.) para que la API devuelva
/// descripciones claras y localizadas al frontend.
/// </summary>
public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
    {
        Code = nameof(DefaultError),
        Description = "Ocurrió un error desconocido."
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"La contraseña debe tener al menos {length} caracteres."
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = "La contraseña debe incluir al menos un carácter especial (por ejemplo: ! @ # $ %)."
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = "La contraseña debe incluir al menos un número."
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = "La contraseña debe incluir al menos una letra minúscula."
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = "La contraseña debe incluir al menos una letra mayúscula."
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = $"La contraseña debe contener al menos {uniqueChars} caracteres distintos."
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = "La contraseña actual es incorrecta."
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = $"El correo '{email}' ya está registrado."
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = $"El usuario '{userName}' ya está en uso."
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = $"El correo '{email}' no es válido."
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = $"El usuario '{userName}' no es válido."
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = nameof(UserAlreadyHasPassword),
        Description = "El usuario ya tiene una contraseña asignada."
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = "El bloqueo de cuenta no está habilitado para este usuario."
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = nameof(InvalidToken),
        Description = "El token proporcionado no es válido."
    };
}

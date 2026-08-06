using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Auth.Commands.ResetPasswordRecovery;

public sealed record ResetPasswordRecoveryCommand(string SessionId, string VerificationCode, string NewPassword)
    : IRequest<ResetPasswordRecoveryResult>;

public sealed record ResetPasswordRecoveryResult(bool Success, string Message);

public sealed class ResetPasswordRecoveryHandler(
    IIdentityDbContext identityContext,
    IPasswordRecoverySessionStore sessionStore,
    IVerificationCodeService verificationCodeService,
    IPasswordHasher<ApplicationUser> passwordHasher)
    : IRequestHandler<ResetPasswordRecoveryCommand, ResetPasswordRecoveryResult>
{
    private const string Purpose = "password.recovery";

    public async Task<ResetPasswordRecoveryResult> Handle(ResetPasswordRecoveryCommand request, CancellationToken cancellationToken)
    {
        if (!sessionStore.TryGet(request.SessionId, out var session))
            throw new KeyNotFoundException("La sesion de recuperacion no existe o expiro.");

        if (string.IsNullOrWhiteSpace(session.VerificationChallengeId))
            throw new InvalidOperationException("La sesion de recuperacion aun no ha sido confirmada.");

        if (!verificationCodeService.Validate(session.VerificationChallengeId, request.VerificationCode, Purpose, session.Email))
            throw new UnauthorizedAccessException("El codigo de verificacion es invalido o expiro.");

        var user = await identityContext.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == session.Email && u.IsActive, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("No fue posible localizar la cuenta para restablecer la contrasena.");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;

        await identityContext.SaveChangesAsync(cancellationToken);
        return new ResetPasswordRecoveryResult(true, "Contrasena actualizada correctamente. Ya puedes iniciar sesion.");
    }
}

public sealed class ResetPasswordRecoveryValidator : AbstractValidator<ResetPasswordRecoveryCommand>
{
    public ResetPasswordRecoveryValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.VerificationCode).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

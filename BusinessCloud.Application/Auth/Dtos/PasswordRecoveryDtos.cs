namespace BusinessCloud.Application.Auth.Dtos;

public class RequestPasswordRecoveryRequest
{
    public string Email { get; set; } = null!;

    public string Channel { get; set; } = "Email";
}

public class ConfirmPasswordRecoveryContactRequest
{
    public string SessionId { get; set; } = null!;

    public string ContactValue { get; set; } = null!;
}

public class ResetPasswordRecoveryRequest
{
    public string? SessionId { get; set; }

    public string? ChallengeId { get; set; }

    public string VerificationCode { get; set; } = null!;

    public string NewPassword { get; set; } = null!;
}

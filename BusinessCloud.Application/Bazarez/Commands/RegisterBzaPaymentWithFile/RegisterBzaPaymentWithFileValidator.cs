using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPaymentWithFile;

public class RegisterBzaPaymentWithFileValidator : AbstractValidator<RegisterBzaPaymentWithFileCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public RegisterBzaPaymentWithFileValidator()
    {
        RuleFor(x => x.BzaSaleId)
            .GreaterThan(0).WithMessage("El ID de venta es requerido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("El método de pago es requerido.")
            .MaximumLength(50).WithMessage("El método de pago no puede exceder 50 caracteres.");

        RuleFor(x => x.ProofFileContent)
            .Must(content => content == null || content.Length <= MaxFileSizeBytes)
            .WithMessage("El archivo no puede exceder 5 MB.");

        RuleFor(x => x.ProofFileName)
            .Must(fileName => fileName == null || AllowedExtensions.Contains(Path.GetExtension(fileName)?.ToLower()))
            .WithMessage("Solo se permiten archivos JPG, PNG o PDF.");
    }
}

using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UploadClosureProof;

public class UploadClosureProofValidator : AbstractValidator<UploadClosureProofCommand>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/jpg", "image/png", "image/webp", "application/pdf"];

    public UploadClosureProofValidator()
    {
        RuleFor(x => x.UploadToken)
            .NotEmpty().WithMessage("Token inválido.");

        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("Debes adjuntar al menos un archivo.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.Content)
                .NotNull().WithMessage("Archivo inválido.");

            file.RuleFor(f => f.ContentType)
                .Must(ct => AllowedContentTypes.Contains((ct ?? string.Empty).ToLowerInvariant()))
                .WithMessage("Formato no permitido. Sube una imagen (JPG, PNG, WEBP) o un PDF.");
        });
    }
}

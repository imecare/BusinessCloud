using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UploadPackedOrderPhotos;

public class UploadPackedOrderPhotosValidator : AbstractValidator<UploadPackedOrderPhotosCommand>
{
    private const long MaxFileSize = 15_000_000;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp"];

    public UploadPackedOrderPhotosValidator()
    {
        RuleFor(x => x.ClosureCustomerTotalId)
            .GreaterThan(0).WithMessage("El cliente del cierre es requerido.");

        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("Debes adjuntar al menos una foto.")
            .Must(files => files.Count <= 10).WithMessage("Puedes subir hasta 10 fotos a la vez.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.Content)
                .NotNull().WithMessage("La foto es inválida.")
                .Must(stream => stream.CanRead && (!stream.CanSeek || stream.Length <= MaxFileSize))
                .WithMessage("Cada foto debe pesar máximo 15 MB.");

            file.RuleFor(f => f.ContentType)
                .Must(type => AllowedContentTypes.Contains((type ?? string.Empty).ToLowerInvariant()))
                .WithMessage("Formato no permitido. Usa JPG, PNG o WEBP.");
        });
    }
}
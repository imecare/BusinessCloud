using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.ImportGlobalCollectorGroups;

public class ImportGlobalCollectorGroupsValidator : AbstractValidator<ImportGlobalCollectorGroupsCommand>
{
    public ImportGlobalCollectorGroupsValidator()
    {
        RuleFor(x => x).Custom((command, context) =>
        {
            var ids = command.GroupIds ?? [];
            if (!command.ImportAll && ids.Count == 0)
            {
                context.AddFailure(nameof(command.GroupIds), "Selecciona al menos un grupo para importar.");
            }

            if (command.ImportAll && ids.Count > 0)
            {
                context.AddFailure(nameof(command.GroupIds), "Para importar todos los grupos no envíes una selección individual.");
            }

            if (ids.Any(id => id <= 0))
            {
                context.AddFailure(nameof(command.GroupIds), "Los identificadores de grupo deben ser mayores a cero.");
            }

            if (ids.Count != ids.Distinct().Count())
            {
                context.AddFailure(nameof(command.GroupIds), "La selección contiene grupos repetidos.");
            }
        });
    }
}

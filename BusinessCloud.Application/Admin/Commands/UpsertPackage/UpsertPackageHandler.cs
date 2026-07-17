using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.UpsertPackage;

public class UpsertPackageHandler(
    IIdentityDbContext context,
    IValidator<UpsertPackageCommand> validator)
    : IRequestHandler<UpsertPackageCommand, int>
{
    private readonly IIdentityDbContext _context = context;
    private readonly IValidator<UpsertPackageCommand> _validator = validator;

    public async Task<int> Handle(UpsertPackageCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        Package package;

        if (request.Id.HasValue)
        {
            package = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"El paquete {request.Id} no existe.");
            package.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            package = new Package { CreatedAt = DateTime.UtcNow };
            _context.Packages.Add(package);
        }

        package.Name = request.Name.Trim();
        package.Module = request.Module.Trim();
        package.Price = request.Price;
        package.Currency = request.Currency.Trim().ToUpperInvariant();
        package.IncludedMessages = request.IncludedMessages;
        package.IsActive = request.IsActive;
        package.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return package.Id;
    }
}

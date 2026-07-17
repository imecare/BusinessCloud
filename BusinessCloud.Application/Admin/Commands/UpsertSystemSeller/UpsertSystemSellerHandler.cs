using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.UpsertSystemSeller;

public class UpsertSystemSellerHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IValidator<UpsertSystemSellerCommand> validator)
    : IRequestHandler<UpsertSystemSellerCommand, int>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IValidator<UpsertSystemSellerCommand> _validator = validator;

    public async Task<int> Handle(UpsertSystemSellerCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null
            : new string(request.Phone.Where(char.IsDigit).ToArray());

        SystemSeller seller;

        if (request.Id.HasValue)
        {
            seller = await _context.SystemSellers
                .FirstOrDefaultAsync(s => s.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"El comisionista {request.Id} no existe.");

            seller.UpdatedAt = DateTime.UtcNow;
            seller.UpdatedBy = _currentUser.UserId;
        }
        else
        {
            seller = new SystemSeller
            {
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
            };
            _context.SystemSellers.Add(seller);
        }

        seller.Name = request.Name.Trim();
        seller.Email = request.Email?.Trim();
        seller.Phone = phone;
        seller.IsActive = request.IsActive;
        seller.DefaultInitialAmount = request.DefaultInitialAmount;
        seller.DefaultMonthlyPercent = request.DefaultMonthlyPercent;

        await _context.SaveChangesAsync(cancellationToken);

        return seller.Id;
    }
}

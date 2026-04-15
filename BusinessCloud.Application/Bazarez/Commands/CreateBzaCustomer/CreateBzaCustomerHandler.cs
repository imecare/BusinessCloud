using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;

public class CreateBzaCustomerHandler : IRequestHandler<CreateBzaCustomerCommand, int>
{
    private readonly IBazaresDbContext _context;

    public CreateBzaCustomerHandler(IBazaresDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = new BzaCustomer
        {
            Name = request.Name,
            FacebookName = request.FacebookName,
            Phone = request.Phone,
            BzaCollectorId = request.BzaCollectorId,
            Status = 1
        };

        _context.Customers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
using MediatR;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;

public class UpdateBzaCustomerHandler : IRequestHandler<UpdateBzaCustomerCommand>
{
    private readonly IBazaresDbContext _context;

    public UpdateBzaCustomerHandler(IBazaresDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Customers
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Cliente de bazar con ID {request.Id} no encontrado.");
        }

        entity.Name = request.Name;
        entity.FacebookName = request.FacebookName;
        entity.Phone = request.Phone;
        entity.Status = request.Status;
        entity.BzaCollectorId = request.BzaCollectorId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
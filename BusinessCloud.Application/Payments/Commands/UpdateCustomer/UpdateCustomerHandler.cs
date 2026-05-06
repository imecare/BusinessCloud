using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdateCustomer;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdateCustomerHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer is null) return false;

        customer.Name = request.Name;
        customer.LastName = request.LastName;
        customer.RFC = request.RFC;
        customer.Phone = request.Phone;
        customer.SellerId = request.SellerId;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

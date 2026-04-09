using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateCustomer
{
    public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, int>
    {
        private readonly IPaymentsDbContext _db;

        public CreateCustomerHandler(IPaymentsDbContext db) => _db = db;

        public async Task<int> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = new Customer
            {
                Name = request.Name,
                LastName = request.LastName,
                RFC = request.RFC,
                Phone = request.Phone,
                SellerId = request.SellerId
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);

            return customer.Id;
        }
    }
}

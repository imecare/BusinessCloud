using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Api.Controllers.Payments;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly PaymentsDbContext _context;

    public CustomersController(PaymentsDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return Ok(customer);
    }
}
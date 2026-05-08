using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Auth.Dtos
{
    public record RegisterRequest(
     string Email,
     string Password,
     string FirstName,
     string LastName,
     string CompanyName,
     string[]? Modules = null // "Payments", "Bazares", "Commissions" — null = todos
 );
}

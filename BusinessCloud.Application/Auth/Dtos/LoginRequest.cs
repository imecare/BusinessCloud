using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Auth.Dtos
{
    public class LoginRequest
    {
        // Añadido Email porque el controlador usa request.Email
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Auth.Dtos
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        /// <summary>
        /// Módulo desde el cual se hace login: "Payments" | "Bazares".
        /// Cada SPA envía su propio identificador.
        /// </summary>
        public string? Module { get; set; }
    }
}

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

        /// <summary>
        /// Indica que el login proviene de la app instalada (PWA en modo standalone).
        /// En ese caso el servidor emite un token de larga duración (Jwt:AppExpireMinutes)
        /// para que la sesión no se cierre. En navegador se usa la duración corta
        /// (Jwt:ExpireMinutes).
        /// </summary>
        public bool PersistentSession { get; set; }
    }
}

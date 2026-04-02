using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Commissions.Dtos
{
    public class InfluenceCenterCreateRequest
    {
        public string Name { get; set; } = null!;
        public string RFC { get; set; } = null!;
        public string Email { get; set; } = null!;

        // opcional si quieres asignar credenciales desde el alta
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}

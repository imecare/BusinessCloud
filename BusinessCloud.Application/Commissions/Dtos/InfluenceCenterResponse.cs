using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Commissions.Dtos
{
    public class InfluenceCenterResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string RFC { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Username { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

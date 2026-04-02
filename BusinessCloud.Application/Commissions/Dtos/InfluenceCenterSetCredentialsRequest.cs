using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Commissions.Dtos
{
    public class InfluenceCenterSetCredentialsRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}

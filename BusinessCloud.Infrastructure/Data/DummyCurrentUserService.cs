using BusinessCloud.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Infrastructure.Data
{
    public class DummyCurrentUserService : ICurrentUserService
    {
        public string? UserId => "Admin_Test_ID";
        public string? Username => "admin@test.com";
        public string? Role => "Admin";

        // Forzamos el ID de empresa 1 para tus pruebas locales
        public int TenantId => 1;
    }
}

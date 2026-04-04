using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Payments.Dtos
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int SellerId { get; set; }
    }
}

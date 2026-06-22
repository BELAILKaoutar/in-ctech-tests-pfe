using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Invoices.DTOs
{
    public class UpdateBulkStatusDto
    {
        public List<string> References { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? TransferReference { get; set; }
    }
}

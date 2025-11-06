using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class SanctionDto
    {
        public int SanctionId { get; set; }
        public int UserId { get; set; }
        public String SanctionType { get; set; }
        public DateTime SanctionDateUtc { get; set; }
        public string ReportReason { get; set; }
        public bool AppliedBySystem { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class ActiveReportSearchCriteriaDto
    {
        public int ReporterUserId { get; set; }      
        public int ReportedUserId { get; set; }    
        public DateTime? LastSanctionDateUtc { get; set; } 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class AvatarProfileOptionDto
    {
        public string AvatarCode { get; set; }    
        public string DisplayName { get; set; }  
        public bool IsUnlocked { get; set; }      
        public bool IsCurrent { get; set; }      
    }
}

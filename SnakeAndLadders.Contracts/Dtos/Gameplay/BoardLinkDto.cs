using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class BoardLinkDto
    {
        [DataMember]
        public int StartIndex { get; set; }

        [DataMember]
        public int EndIndex { get; set; }

        [DataMember]
        public bool IsLadder { get; set; } 
    }
}

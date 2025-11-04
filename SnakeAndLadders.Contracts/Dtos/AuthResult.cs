using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class AuthResult
    {
        [DataMember] public bool Success { get; set; }

        [DataMember] public string Code { get; set; } 

        [DataMember] public Dictionary<string, string> Meta { get; set; }

        [DataMember] public int? UserId { get; set; }
        [DataMember] public string DisplayName { get; set; }

        [DataMember] public string TechnicalMessage { get; set; }

        [DataMember] public string Message { get; set; }

        [DataMember] public string ProfilePhotoId { get; set; }
    }
}

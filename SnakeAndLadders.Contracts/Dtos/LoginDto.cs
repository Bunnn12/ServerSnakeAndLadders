using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class LoginDto
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public string Password { get; set; }
    }
}

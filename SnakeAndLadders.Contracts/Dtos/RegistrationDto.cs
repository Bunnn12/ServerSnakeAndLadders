using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class RegistrationDto
    {
        [DataMember] public string UserName { get; set; }
        [DataMember] public string FirstName { get; set; }
        [DataMember] public string LastName { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string Password { get; set; } // plain from client; server hashes
    }
}

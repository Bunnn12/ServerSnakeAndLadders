using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    /// <summary>
    /// Credentials used to authenticate an existing account.
    /// </summary>
    [DataContract]
    public class LoginDto
    {
        [DataMember] public string Email { get; set; }
        [DataMember] public string Password { get; set; }
    }
}

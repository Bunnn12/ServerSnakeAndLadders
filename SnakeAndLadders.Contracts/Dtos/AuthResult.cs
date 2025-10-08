using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    /// <summary>
    /// Standard result for Register and Login operations.
    /// </summary>
    [DataContract]
    public class AuthResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public int UserId { get; set; }
        [DataMember] public string DisplayName { get; set; }
        [DataMember] public string Message { get; set; }
    }
}

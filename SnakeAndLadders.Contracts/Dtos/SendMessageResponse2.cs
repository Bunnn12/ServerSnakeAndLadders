using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Services
{
    [DataContract]
    public sealed class SendMessageResponse2
    {
        [DataMember(IsRequired = true)] public bool Ok { get; set; }
    }
}

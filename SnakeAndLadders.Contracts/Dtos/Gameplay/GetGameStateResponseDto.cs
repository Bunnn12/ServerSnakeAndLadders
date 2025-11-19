using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{

    [DataContract]
    public sealed class GetGameStateResponseDto
    {

        [DataMember]
        public int GameId { get; set; }

        [DataMember]
        public int CurrentTurnUserId { get; set; }

        [DataMember]
        public bool IsFinished { get; set; }

        [DataMember]
        public List<TokenStateDto> Tokens { get; set; } = new List<TokenStateDto>();
    }
}

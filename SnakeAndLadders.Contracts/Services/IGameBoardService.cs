using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IGameBoardService
    {
        [OperationContract]
        CreateBoardResponseDto CreateBoard(CreateBoardRequestDto request);

        [OperationContract]
        BoardDefinitionDto GetBoard(int gameId);
    }
}

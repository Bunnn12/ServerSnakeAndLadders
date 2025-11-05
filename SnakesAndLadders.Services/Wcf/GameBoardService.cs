
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using System;
using log4net;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using log4net.Repository.Hierarchy;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class GameBoardService : IGameBoardService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardService));

        // Hacemos el Builder de solo lectura e inicializado directamente.
        private readonly GameBoardBuilder gameBoardBuilder = new GameBoardBuilder();

        // Hacemos el diccionario de solo lectura e inicializado directamente.
        private readonly ConcurrentDictionary<int, BoardDefinitionDto> boardsByGameId =
            new ConcurrentDictionary<int, BoardDefinitionDto>();


        public GameBoardService()
        {
            
        }

        public CreateBoardResponseDto CreateBoard(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                const string message = "Request cannot be null.";
                Logger.Warn(message);
                throw new FaultException(message);
            }

            try
            {
                Logger.InfoFormat(
                    "Creating board. GameId={0}, BoardSize={1}, EnableBonusCells={2}, EnableTrapCells={3}, EnableTeleportCells={4}",
                    request.GameId,
                    request.BoardSize,
                    request.EnableBonusCells,
                    request.EnableTrapCells,
                    request.EnableTeleportCells);

                var board = gameBoardBuilder.BuildBoard(request);

                if (board == null)
                {
                    const string message = "GameBoardBuilder returned a null board.";
                    Logger.Error(message);
                    throw new FaultException(message);
                }

                boardsByGameId.AddOrUpdate(request.GameId, board, (_, __) => board);

                Logger.InfoFormat("Board created and stored for GameId={0}.", request.GameId);

                return new CreateBoardResponseDto
                {
                    Board = board
                };
            }
            catch (ArgumentException ex)
            {
                Logger.Warn("Validation error while creating board.", ex);
                throw new FaultException(ex.Message);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected critical error while creating board.", ex);
                throw new FaultException("An unexpected internal error occurred while creating the board.");
            }
        }



        // EN GameBoardService.cs

        public BoardDefinitionDto GetBoard(int gameId)
        {
            if (gameId <= 0)
            {
                // Esto puede seguir lanzando FaultException, lo cual es manejable.
                const string message = "GameId must be greater than zero.";
                throw new FaultException(message);
            }

            try
            {
                if (!boardsByGameId.TryGetValue(gameId, out BoardDefinitionDto board))
                {
                    // 🛑 CORRECCIÓN: Devolvemos null en lugar de lanzar FaultException.
                    Logger.Warn($"Board not found for GameId {gameId}. Returning null to client.");
                    return null;
                }

                // Logger.Info("Board retrieved...");
                return board;
            }
            catch (Exception ex)
            {
                // Para cualquier error inesperado, logueamos y devolvemos null o FaultException genérico.
                Logger.Error("Unexpected error while retrieving board.", ex);
                throw new FaultException("An unexpected error occurred while retrieving the board.");
            }
        }


    }
}

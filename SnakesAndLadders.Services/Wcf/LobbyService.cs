using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using System;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    /// <summary>Thin WCF adapter that logs and maps exceptions to faults.</summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class LobbyService : ILobbyService
    {
        private readonly ILobbyAppService app;
        private readonly IAppLogger log;

        public LobbyService(ILobbyAppService app, IAppLogger log)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            try
            {
                return app.CreateGame(request);
            }
            catch (ArgumentException ex)
            {
                log.Warn($"Validation failure in CreateGame: {ex.Message}");
                throw Faults.Create("InvalidRequest", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                log.Error("Business/State error in CreateGame.", ex);
                throw Faults.Create("Conflict", ex.Message);
            }
            catch (Exception ex)
            {
                log.Error("Unexpected error in LobbyService.CreateGame.", ex);
                throw Faults.Create("Unexpected", "An unexpected error occurred. Please try again.");
            }
        }
    }
}

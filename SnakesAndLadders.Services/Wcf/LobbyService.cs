using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class LobbyService : ILobbyService
    {
        private readonly ILobbyAppService lobbyAppService;

        private readonly ConcurrentDictionary<int, LobbyInfo> lobbies =
            new ConcurrentDictionary<int, LobbyInfo>();

        private readonly Random rng = new Random();
        private static bool IsGuestUser(int userId)
        {
            return userId < 0; 
        }

        private int NextId()
        {
            lock (rng)
            {
                return rng.Next(100_000, 999_999);
            }
        }

        private string GenerateCode()
        {
            lock (rng)
            {
                return rng.Next(0, 999_999).ToString("000000");
            }
        }

        public LobbyService(ILobbyAppService lobbyAppService)
        {
            this.lobbyAppService = lobbyAppService ?? throw new ArgumentNullException(nameof(lobbyAppService));
        }

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            if (request == null)
            {
                Throw("REQ_NULL", "Solicitud nula.");
            }

            if (request.MaxPlayers < 2 || request.MaxPlayers > 4)
            {
                Throw("MAX_PLAYERS", "MaxPlayers debe estar entre 2 y 4.");
            }

            bool isGuestHost = IsGuestUser(request.HostUserId);

            int partidaId;
            string codigo;
            DateTime expiresAtUtc;

            int effectiveTtl = Math.Max(5, request.TtlMinutes);

            if (isGuestHost)
            {
                // 👇 Lobby solo en memoria, nada en BD
                partidaId = NextId();
                codigo = GenerateCode();
                expiresAtUtc = DateTime.UtcNow.AddMinutes(effectiveTtl);
            }
            else
            {
                // 👇 Lobby persistido en BD
                CreateGameResponse created = lobbyAppService.CreateGame(request);

                partidaId = created.PartidaId;
                codigo = created.CodigoPartida;
                expiresAtUtc = created.ExpiresAtUtc;
            }

            var lobby = new LobbyInfo
            {
                PartidaId = partidaId,
                CodigoPartida = codigo,
                HostUserId = request.HostUserId,
                HostUserName = $"User{request.HostUserId}",
                MaxPlayers = request.MaxPlayers,
                Status = LobbyStatus.Waiting,
                ExpiresAtUtc = expiresAtUtc,
                BoardSide = request.BoardSide,
                Difficulty = request.Dificultad,
                PlayersRequested = request.PlayersRequested,
                SpecialTiles = request.SpecialTiles
            };

            lobby.Players.Add(
                new LobbyMember
                {
                    UserId = request.HostUserId,
                    UserName = lobby.HostUserName,
                    IsHost = true,
                    JoinedAtUtc = DateTime.UtcNow,
                    AvatarId = request.HostAvatarId,
                    CurrentSkinUnlockedId = request.CurrentSkinUnlockedId,
                    CurrentSkinId = request.CurrentSkinId
                });

            lobbies[partidaId] = lobby;

            return new CreateGameResponse
            {
                PartidaId = partidaId,
                CodigoPartida = codigo,
                ExpiresAtUtc = expiresAtUtc
            };
        }


        public JoinLobbyResponse JoinLobby(JoinLobbyRequest request)
        {
            if (request == null)
            {
                return Fail("Solicitud nula.");
            }

            LobbyInfo lobby = lobbies
                .Values
                .FirstOrDefault(info => info.CodigoPartida == request.CodigoPartida);

            if (lobby == null)
            {
                return Fail("Código inválido.");
            }

            if (lobby.Status != LobbyStatus.Waiting)
            {
                return Fail("La partida ya comenzó o está cerrada.");
            }

            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                return Fail("El lobby está lleno.");
            }

            if (lobby.Players.Any(player => player.UserId == request.UserId))
            {
                return new JoinLobbyResponse
                {
                    Success = true,
                    Lobby = lobby
                };
            }

            // 👇 Solo usuarios reales se registran en BD
            if (!IsGuestUser(request.UserId))
            {
                lobbyAppService.RegisterPlayerInGame(
                    lobby.PartidaId,
                    request.UserId,
                    isHost: false);
            }

            lobby.Players.Add(
                new LobbyMember
                {
                    UserId = request.UserId,
                    UserName = request.UserName,
                    IsHost = false,
                    JoinedAtUtc = DateTime.UtcNow,
                    AvatarId = request.AvatarId,
                    CurrentSkinUnlockedId = request.CurrentSkinUnlockedId,
                    CurrentSkinId = request.CurrentSkinId
                });

            return new JoinLobbyResponse
            {
                Success = true,
                Lobby = lobby
            };
        }



        public OperationResult LeaveLobby(LeaveLobbyRequest request)
        {
            if (!lobbies.TryGetValue(request.PartidaId, out LobbyInfo lobby))
            {
                return new OperationResult
                {
                    Success = true,
                    Message = "Lobby inexistente (ya cerrado)."
                };
            }

            LobbyMember member = lobby
                .Players
                .FirstOrDefault(player => player.UserId == request.UserId);

            if (member == null)
            {
                return new OperationResult
                {
                    Success = true,
                    Message = "No estaba en el lobby."
                };
            }

            lobby.Players.Remove(member);

            if (member.IsHost)
            {
                LobbyMember nextHost = lobby.Players.FirstOrDefault();

                if (nextHost != null)
                {
                    lobby.HostUserId = nextHost.UserId;
                    lobby.HostUserName = nextHost.UserName;

                    foreach (LobbyMember player in lobby.Players)
                    {
                        player.IsHost = player.UserId == nextHost.UserId;
                    }
                }
                else
                {
                    lobby.Status = LobbyStatus.Closed;
                    lobbies.TryRemove(request.PartidaId, out _);
                }
            }

            return new OperationResult
            {
                Success = true,
                Message = "Saliste del lobby."
            };
        }

        public OperationResult StartMatch(StartMatchRequest request)
        {
            if (!lobbies.TryGetValue(request.PartidaId, out LobbyInfo lobby))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Lobby no encontrado."
                };
            }

            if (lobby.HostUserId != request.HostUserId)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Solo el host puede iniciar."
                };
            }

            if (lobby.Players.Count < 2)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Se requieren al menos 2 jugadores."
                };
            }

            lobby.Status = LobbyStatus.InMatch;

            return new OperationResult
            {
                Success = true,
                Message = "La partida se está iniciando..."
            };
        }

        public LobbyInfo GetLobbyInfo(GetLobbyInfoRequest request)
        {
            lobbies.TryGetValue(request.PartidaId, out LobbyInfo lobby);
            return lobby;
        }

        private static void Throw(string code, string message)
        {
            var fault = new ServiceFault
            {
                Code = code,
                Message = message,
                CorrelationId = Guid.NewGuid().ToString("N")
            };

            throw new FaultException<ServiceFault>(fault);
        }

        private static JoinLobbyResponse Fail(string message)
        {
            return new JoinLobbyResponse
            {
                Success = false,
                FailureReason = message
            };
        }

        public void KickUserFromAllLobbies(int userId, string reason)
        {
            if (userId <= 0)
            {
                return;
            }

            LobbyInfo[] snapshot = lobbies
                .Values
                .ToArray();

            foreach (LobbyInfo lobby in snapshot)
            {
                LobbyMember member = lobby
                    .Players
                    .FirstOrDefault(player => player.UserId == userId);

                if (member == null)
                {
                    continue;
                }

                lobby.Players.Remove(member);

                if (member.IsHost)
                {
                    LobbyMember nextHost = lobby.Players.FirstOrDefault();

                    if (nextHost != null)
                    {
                        lobby.HostUserId = nextHost.UserId;
                        lobby.HostUserName = nextHost.UserName;

                        foreach (LobbyMember player in lobby.Players)
                        {
                            player.IsHost = player.UserId == nextHost.UserId;
                        }
                    }
                    else
                    {
                        lobby.Status = LobbyStatus.Closed;
                        lobbies.TryRemove(lobby.PartidaId, out _);
                    }
                }
            }
        }
    }
}

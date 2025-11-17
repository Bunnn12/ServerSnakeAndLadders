 
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class LobbyService : ILobbyService
    {
        private readonly ConcurrentDictionary<int, LobbyInfo> _lobbies = new ConcurrentDictionary<int, LobbyInfo>();
        private readonly Random _rng = new Random();

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            if (request == null) Throw("REQ_NULL", "Solicitud nula.");
            if (request.MaxPlayers < 2 || request.MaxPlayers > 4) Throw("MAX_PLAYERS", "MaxPlayers debe estar entre 2 y 4.");

            var partidaId = NextId();
            var codigo = GenerateCode();

            var effectiveTtl = Math.Max(5, request.TtlMinutes);

            var lobby = new LobbyInfo
            {
                PartidaId = partidaId,
                CodigoPartida = codigo,
                HostUserId = request.HostUserId,
                HostUserName = $"User{request.HostUserId}",
                MaxPlayers = request.MaxPlayers,
                Status = LobbyStatus.Waiting,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(effectiveTtl),


                BoardSide = request.BoardSide,
                Difficulty = request.Dificultad,
                PlayersRequested = request.PlayersRequested,
                SpecialTiles = request.SpecialTiles
            };

            lobby.Players.Add(new LobbyMember
            {
                UserId = request.HostUserId,
                UserName = lobby.HostUserName,
                IsHost = true,
                JoinedAtUtc = DateTime.UtcNow,
                AvatarId = request.HostAvatarId,

                CurrentSkinUnlockedId = request.CurrentSkinUnlockedId,
                CurrentSkinId = request.CurrentSkinId

            });

            _lobbies[partidaId] = lobby;

            return new CreateGameResponse
            {
                PartidaId = partidaId,
                CodigoPartida = codigo,
                ExpiresAtUtc = lobby.ExpiresAtUtc
            };
        }


        public JoinLobbyResponse JoinLobby(JoinLobbyRequest request)
        {
            if (request == null) return Fail("Solicitud nula.");
            var lobby = _lobbies.Values.FirstOrDefault(l => l.CodigoPartida == request.CodigoPartida);
            if (lobby == null) return Fail("Código inválido.");
            if (lobby.Status != LobbyStatus.Waiting) return Fail("La partida ya comenzó o está cerrada.");
            if (lobby.Players.Count >= lobby.MaxPlayers) return Fail("El lobby está lleno.");

            if (lobby.Players.Any(p => p.UserId == request.UserId))
                return new JoinLobbyResponse { Success = true, Lobby = lobby };

            lobby.Players.Add(new LobbyMember
            {
                UserId = request.UserId,
                UserName = request.UserName,
                IsHost = false,
                JoinedAtUtc = DateTime.UtcNow,
                AvatarId = request.AvatarId,
                CurrentSkinUnlockedId = request.CurrentSkinUnlockedId,
                CurrentSkinId = request.CurrentSkinId
            });

            return new JoinLobbyResponse { Success = true, Lobby = lobby };
        }

        public OperationResult LeaveLobby(LeaveLobbyRequest request)
        {
            if (!_lobbies.TryGetValue(request.PartidaId, out var lobby))
                return new OperationResult { Success = true, Message = "Lobby inexistente (ya cerrado)." };

            var member = lobby.Players.FirstOrDefault(p => p.UserId == request.UserId);
            if (member == null) return new OperationResult { Success = true, Message = "No estaba en el lobby." };

            lobby.Players.Remove(member);

            if (member.IsHost)
            {
                var next = lobby.Players.FirstOrDefault();
                if (next != null)
                {
                    lobby.HostUserId = next.UserId;
                    lobby.HostUserName = next.UserName;
                    foreach (var p in lobby.Players) p.IsHost = (p.UserId == next.UserId);
                }
                else
                {
                    lobby.Status = LobbyStatus.Closed;
                    _lobbies.TryRemove(request.PartidaId, out _);
                }
            }

            return new OperationResult { Success = true, Message = "Saliste del lobby." };
        }

        public OperationResult StartMatch(StartMatchRequest request)
        {
            if (!_lobbies.TryGetValue(request.PartidaId, out var lobby))
                return new OperationResult { Success = false, Message = "Lobby no encontrado." };

            if (lobby.HostUserId != request.HostUserId)
                return new OperationResult { Success = false, Message = "Solo el host puede iniciar." };

            if (lobby.Players.Count < 2)
                return new OperationResult { Success = false, Message = "Se requieren al menos 2 jugadores." };


            lobby.Status = LobbyStatus.InMatch;
            return new OperationResult { Success = true, Message = "La partida se está iniciando..." };
        }

        public LobbyInfo GetLobbyInfo(GetLobbyInfoRequest request)
        {
            _lobbies.TryGetValue(request.PartidaId, out var lobby);
            return lobby;
        }

        private static void Throw(string code, string message)
        {
            throw new FaultException<ServiceFault>(new ServiceFault { Code = code, Message = message, CorrelationId = Guid.NewGuid().ToString("N") });
        }

        private JoinLobbyResponse Fail(string msg) => new JoinLobbyResponse { Success = false, FailureReason = msg };

        private int NextId() => _rng.Next(100000, 999999);
        private string GenerateCode() => _rng.Next(0, 999999).ToString("000000");

        public void KickUserFromAllLobbies(int userId, string reason)
        {
                if (userId <= 0)
                {
                    return;
                }
                var snapshot = _lobbies.Values.ToArray();

                foreach (var lobby in snapshot)
                {
                    var member = lobby.Players.FirstOrDefault(p => p.UserId == userId);
                    if (member == null)
                    {
                        continue;
                    }

                    lobby.Players.Remove(member);

                    if (member.IsHost)
                    {
                        var next = lobby.Players.FirstOrDefault();
                        if (next != null)
                        {
                            lobby.HostUserId = next.UserId;
                            lobby.HostUserName = next.UserName;

                            foreach (var p in lobby.Players)
                            {
                                p.IsHost = p.UserId == next.UserId;
                            }
                        }
                        else
                        {
                            lobby.Status = LobbyStatus.Closed;
                            _lobbies.TryRemove(lobby.PartidaId, out _);
                        }
                    }
                }
        }
    }
}

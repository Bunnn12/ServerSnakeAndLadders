using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public sealed class LobbyService : ILobbyService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyService));

        private readonly ILobbyAppService lobbyAppService;

        private readonly ConcurrentDictionary<int, LobbyInfo> lobbies =
            new ConcurrentDictionary<int, LobbyInfo>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, ILobbyCallback>> lobbyCallbacks =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, ILobbyCallback>>();

        private readonly ConcurrentDictionary<int, ILobbyCallback> publicLobbySubscribers =
            new ConcurrentDictionary<int, ILobbyCallback>();

        private readonly Random rng = new Random();

        private readonly Timer cleanupTimer;

        private const string REASON_CLOSED = "CLOSED";
        private const string REASON_KICKED = "KICKED";

        private const int CLEANUP_INTERVAL_SECONDS = 30;

        public LobbyService(ILobbyAppService lobbyAppService)
        {
            this.lobbyAppService = lobbyAppService ?? throw new ArgumentNullException(nameof(lobbyAppService));

            cleanupTimer = new Timer(
                _ => CleanupExpiredLobbies(),
                null,
                TimeSpan.FromSeconds(CLEANUP_INTERVAL_SECONDS),
                TimeSpan.FromSeconds(CLEANUP_INTERVAL_SECONDS));
        }

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

        private static bool IsLobbyExpired(LobbyInfo lobby, DateTime nowUtc)
        {
            if (lobby == null)
            {
                return true;
            }

            return lobby.ExpiresAtUtc <= nowUtc;
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
                partidaId = NextId();
                codigo = GenerateCode();
                expiresAtUtc = DateTime.UtcNow.AddMinutes(effectiveTtl);
            }
            else
            {
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
                SpecialTiles = request.SpecialTiles,
                IsPrivate = request.IsPrivate
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

            RegisterLobbyCallback(partidaId, request.HostUserId);

            BroadcastPublicLobbies();

            Logger.InfoFormat(
                "CreateGame: LobbyId={0}, Code={1}, IsPrivate={2}, Status={3}, ExpiresAtUtc={4:u}",
                lobby.PartidaId,
                lobby.CodigoPartida,
                lobby.IsPrivate,
                lobby.Status,
                lobby.ExpiresAtUtc);

            return new CreateGameResponse
            {
                PartidaId = partidaId,
                CodigoPartida = codigo,
                ExpiresAtUtc = expiresAtUtc
            };
        }

        public JoinLobbyResponse JoinLobby(JoinLobbyRequest request)
        {
            CleanupExpiredLobbies();

            Logger.Info("JoinLobby: inicio.");

            if (request == null)
            {
                Logger.Warn("JoinLobby: request nulo.");
                return Fail("Solicitud nula.");
            }

            Logger.InfoFormat(
                "JoinLobby: buscando lobby con código {0}. UserId={1}",
                request.CodigoPartida,
                request.UserId);

            LobbyInfo lobby = lobbies
                .Values
                .FirstOrDefault(info => info.CodigoPartida == request.CodigoPartida);

            if (lobby == null)
            {
                Logger.WarnFormat(
                    "JoinLobby: lobby no encontrado para código {0}.",
                    request.CodigoPartida);
                return Fail("Código inválido.");
            }

            DateTime nowUtc = DateTime.UtcNow;

            if (IsLobbyExpired(lobby, nowUtc) || lobby.Status == LobbyStatus.Closed)
            {
                Logger.WarnFormat(
                    "JoinLobby: lobby expirado o cerrado. PartidaId={0}, Status={1}, ExpiresAtUtc={2:u}",
                    lobby.PartidaId,
                    lobby.Status,
                    lobby.ExpiresAtUtc);

                CloseLobby(lobby, REASON_CLOSED);
                BroadcastPublicLobbies();

                return Fail("La partida ha expirado o está cerrada.");
            }

            Logger.InfoFormat(
                "JoinLobby: lobby {0} encontrado. Status={1}, Players={2}/{3}",
                lobby.PartidaId,
                lobby.Status,
                lobby.Players.Count,
                lobby.MaxPlayers);

            if (lobby.Status != LobbyStatus.Waiting)
            {
                Logger.Warn("JoinLobby: lobby no está en estado Waiting.");
                return Fail("La partida ya comenzó o está cerrada.");
            }

            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                Logger.Warn("JoinLobby: lobby lleno.");
                return Fail("El lobby está lleno.");
            }

            LobbyMember existing = lobby.Players
                .FirstOrDefault(player => player.UserId == request.UserId);

            if (existing != null)
            {
                Logger.InfoFormat(
                    "JoinLobby: usuario {0} ya estaba en el lobby {1}. Solo se re-registra callback.",
                    request.UserId,
                    lobby.PartidaId);

                RegisterLobbyCallback(lobby.PartidaId, request.UserId);
                NotifyLobbyUpdated(lobby);

                return new JoinLobbyResponse
                {
                    Success = true,
                    Lobby = lobby
                };
            }

            Logger.InfoFormat(
                "JoinLobby: usuario {0} nuevo en lobby {1}. Registrando en BD (si no es guest).",
                request.UserId,
                lobby.PartidaId);

            if (!IsGuestUser(request.UserId))
            {
                lobbyAppService.RegisterPlayerInGame(
                    lobby.PartidaId,
                    request.UserId,
                    isHost: false);

                Logger.InfoFormat(
                    "JoinLobby: RegisterPlayerInGame completado para GameId={0}, UserId={1}.",
                    lobby.PartidaId,
                    request.UserId);
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

            Logger.InfoFormat(
                "JoinLobby: usuario {0} agregado al lobby {1}. Total players={2}.",
                request.UserId,
                lobby.PartidaId,
                lobby.Players.Count);

            RegisterLobbyCallback(lobby.PartidaId, request.UserId);
            NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbies();

            Logger.Info("JoinLobby: fin OK.");

            return new JoinLobbyResponse
            {
                Success = true,
                Lobby = lobby
            };
        }

        private void CloseLobby(LobbyInfo lobby, string reason)
        {
            if (lobby == null)
            {
                return;
            }

            lobby.Status = LobbyStatus.Closed;

            lobbies.TryRemove(lobby.PartidaId, out _);

            NotifyLobbyClosed(lobby.PartidaId, reason ?? REASON_CLOSED);
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
            RemoveLobbyCallback(lobby.PartidaId, request.UserId);

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
                    CloseLobby(lobby, REASON_CLOSED);
                    BroadcastPublicLobbies();

                    return new OperationResult
                    {
                        Success = true,
                        Message = "Lobby cerrado."
                    };
                }
            }

            NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbies();

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

            DateTime nowUtc = DateTime.UtcNow;

            if (IsLobbyExpired(lobby, nowUtc) || lobby.Status == LobbyStatus.Closed)
            {
                CloseLobby(lobby, REASON_CLOSED);
                BroadcastPublicLobbies();

                return new OperationResult
                {
                    Success = false,
                    Message = "El lobby ha expirado o está cerrado."
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
            NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbies();

            return new OperationResult
            {
                Success = true,
                Message = "La partida se está iniciando..."
            };
        }

        public LobbyInfo GetLobbyInfo(GetLobbyInfoRequest request)
        {
            CleanupExpiredLobbies();

            lobbies.TryGetValue(request.PartidaId, out LobbyInfo lobby);
            return lobby;
        }

        public void SubscribePublicLobbies(int userId)
        {
            CleanupExpiredLobbies();

            ILobbyCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();
            if (callback == null)
            {
                return;
            }

            publicLobbySubscribers[userId] = callback;

            IList<LobbySummary> snapshot = BuildPublicLobbySummaries();
            SafeInvoke(
                callback,
                cb => cb.OnPublicLobbiesChanged(snapshot));
        }

        public void UnsubscribePublicLobbies(int userId)
        {
            publicLobbySubscribers.TryRemove(userId, out _);
        }

        public void KickUserFromAllLobbies(int userId, string reason)
        {
            if (userId <= 0)
            {
                return;
            }

            LobbyInfo[] snapshot = lobbies.Values.ToArray();

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
                RemoveLobbyCallback(lobby.PartidaId, userId);

                NotifyKickedFromLobby(lobby.PartidaId, userId, reason ?? REASON_KICKED);

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

                        NotifyLobbyUpdated(lobby);
                    }
                    else
                    {
                        CloseLobby(lobby, REASON_CLOSED);
                    }
                }
                else
                {
                    NotifyLobbyUpdated(lobby);
                }
            }

            BroadcastPublicLobbies();
        }

        private void CleanupExpiredLobbies()
        {
            DateTime nowUtc = DateTime.UtcNow;

            LobbyInfo[] snapshot = lobbies.Values.ToArray();

            foreach (LobbyInfo lobby in snapshot)
            {
                if (IsLobbyExpired(lobby, nowUtc) && lobby.Status != LobbyStatus.Closed)
                {
                    Logger.InfoFormat(
                        "CleanupExpiredLobbies: cerrando lobby expirado. PartidaId={0}, ExpiresAtUtc={1:u}",
                        lobby.PartidaId,
                        lobby.ExpiresAtUtc);

                    CloseLobby(lobby, REASON_CLOSED);
                }
            }

            BroadcastPublicLobbies();
        }

        private void RegisterLobbyCallback(int partidaId, int userId)
        {
            ILobbyCallback callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();
            if (callback == null)
            {
                return;
            }

            ConcurrentDictionary<int, ILobbyCallback> perLobbyCallbacks =
                lobbyCallbacks.GetOrAdd(
                    partidaId,
                    _ => new ConcurrentDictionary<int, ILobbyCallback>());

            perLobbyCallbacks[userId] = callback;
        }

        private void RemoveLobbyCallback(int partidaId, int userId)
        {
            if (!lobbyCallbacks.TryGetValue(partidaId, out var perLobbyCallbacks))
            {
                return;
            }

            perLobbyCallbacks.TryRemove(userId, out _);

            if (perLobbyCallbacks.IsEmpty)
            {
                lobbyCallbacks.TryRemove(partidaId, out _);
            }
        }

        private void NotifyLobbyUpdated(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                return;
            }

            if (!lobbyCallbacks.TryGetValue(lobby.PartidaId, out var perLobbyCallbacks))
            {
                return;
            }

            LobbyInfo snapshot = CloneLobby(lobby);

            foreach (var entry in perLobbyCallbacks.ToArray())
            {
                SafeInvoke(
                    entry.Value,
                    cb => cb.OnLobbyUpdated(snapshot));
            }
        }

        private void NotifyLobbyClosed(int partidaId, string reason)
        {
            if (!lobbyCallbacks.TryGetValue(partidaId, out var perLobbyCallbacks))
            {
                return;
            }

            foreach (var entry in perLobbyCallbacks.ToArray())
            {
                SafeInvoke(
                    entry.Value,
                    cb => cb.OnLobbyClosed(partidaId, reason));
            }

            lobbyCallbacks.TryRemove(partidaId, out _);
        }

        private void NotifyKickedFromLobby(int partidaId, int userId, string reason)
        {
            if (!lobbyCallbacks.TryGetValue(partidaId, out var perLobbyCallbacks))
            {
                return;
            }

            if (!perLobbyCallbacks.TryGetValue(userId, out ILobbyCallback callback))
            {
                return;
            }

            SafeInvoke(
                callback,
                cb => cb.OnKickedFromLobby(partidaId, reason));
        }

        private void BroadcastPublicLobbies()
        {
            IList<LobbySummary> snapshot = BuildPublicLobbySummaries();

            foreach (var entry in publicLobbySubscribers.ToArray())
            {
                SafeInvoke(
                    entry.Value,
                    cb => cb.OnPublicLobbiesChanged(snapshot));
            }
        }

        private IList<LobbySummary> BuildPublicLobbySummaries()
        {
            DateTime nowUtc = DateTime.UtcNow;

            return lobbies
                .Values
                .Where(l =>
                    !l.IsPrivate &&
                    l.Status == LobbyStatus.Waiting &&
                    !IsLobbyExpired(l, nowUtc))
                .Select(l => new LobbySummary
                {
                    PartidaId = l.PartidaId,
                    CodigoPartida = l.CodigoPartida,
                    HostUserName = l.HostUserName,
                    MaxPlayers = l.MaxPlayers,
                    CurrentPlayers = (byte)l.Players.Count,
                    Difficulty = l.Difficulty,
                    IsPrivate = l.IsPrivate
                })
                .ToList();
        }

        private static LobbyInfo CloneLobby(LobbyInfo lobby)
        {
            return new LobbyInfo
            {
                PartidaId = lobby.PartidaId,
                CodigoPartida = lobby.CodigoPartida,
                HostUserId = lobby.HostUserId,
                HostUserName = lobby.HostUserName,
                MaxPlayers = lobby.MaxPlayers,
                Status = lobby.Status,
                ExpiresAtUtc = lobby.ExpiresAtUtc,
                BoardSide = lobby.BoardSide,
                Difficulty = lobby.Difficulty,
                PlayersRequested = lobby.PlayersRequested,
                SpecialTiles = lobby.SpecialTiles,
                IsPrivate = lobby.IsPrivate,
                Players = lobby.Players
                    .Select(p => new LobbyMember
                    {
                        UserId = p.UserId,
                        UserName = p.UserName,
                        IsHost = p.IsHost,
                        JoinedAtUtc = p.JoinedAtUtc,
                        AvatarId = p.AvatarId,
                        CurrentSkinUnlockedId = p.CurrentSkinUnlockedId,
                        CurrentSkinId = p.CurrentSkinId
                    })
                    .ToList()
            };
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

        private static void SafeInvoke(ILobbyCallback callback, Action<ILobbyCallback> invoker)
        {
            try
            {
                invoker(callback);
            }
            catch (Exception ex)
            {
                Logger.Warn("Error invoking lobby callback.", ex);
            }
        }
        public void KickPlayerFromLobby(KickPlayerFromLobbyRequest request)
        {
            if (request == null)
            {
                Throw("REQ_NULL", "Solicitud nula para expulsar jugador.");
            }

            if (request.LobbyId <= 0 ||
                request.HostUserId <= 0 ||
                request.TargetUserId == 0)   
            {
                Throw("REQ_INVALID", "Parámetros inválidos para expulsar jugador.");
            }

            if (request.HostUserId == request.TargetUserId)
            {
                Throw("KICK_SELF", "El host no puede expulsarse a sí mismo.");
            }

            if (!lobbies.TryGetValue(request.LobbyId, out LobbyInfo lobby))
            {
                return;
            }

            if (lobby.HostUserId != request.HostUserId)
            {
                Throw("KICK_NOT_HOST", "Solo el host puede expulsar jugadores del lobby.");
            }

            LobbyMember targetMember = lobby
                .Players
                .FirstOrDefault(player => player.UserId == request.TargetUserId);

            if (targetMember == null)
            {
                return;
            }

            lobby.Players.Remove(targetMember);

            const string KICK_REASON_CODE = "KICKED_BY_HOST";

            NotifyKickedUserAndRemoveCallback(
                lobby.PartidaId,
                targetMember.UserId,
                KICK_REASON_CODE);

            if (targetMember.IsHost)
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
                    CloseLobby(lobby, REASON_CLOSED);
                    BroadcastPublicLobbies();
                    return;
                }
            }

            NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbies();

            if (!IsGuestUser(targetMember.UserId))
            {
                lobbyAppService.KickPlayerFromLobby(
                    request.LobbyId,
                    request.HostUserId,
                    request.TargetUserId);
            }
        }

        private void NotifyKickedUserAndRemoveCallback(
            int partidaId,
            int targetUserId,
            string reason)
        {
            if (!lobbyCallbacks.TryGetValue(partidaId, out var perLobbyCallbacks))
            {
                return;
            }

            if (!perLobbyCallbacks.TryGetValue(targetUserId, out ILobbyCallback callback))
            {
                return;
            }

            perLobbyCallbacks.TryRemove(targetUserId, out _);

            if (perLobbyCallbacks.IsEmpty)
            {
                lobbyCallbacks.TryRemove(partidaId, out _);
            }

            SafeInvoke(
                callback,
                cb => cb.OnKickedFromLobby(partidaId, reason ?? REASON_KICKED));
        }
    }
}

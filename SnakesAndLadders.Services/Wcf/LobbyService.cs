using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Wcf.Lobby;
using System;
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
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(LobbyService));

        private readonly ILobbyAppService _lobbyAppService;
        private readonly ILobbyStore _lobbyStore;
        private readonly ILobbyNotification _notificationHub;
        private readonly ILobbyIdGenerator _idGenerator;

        private readonly Timer _cleanupTimer;

        public LobbyService(
            ILobbyAppService lobbyAppService,
            ILobbyStore lobbyStore,
            ILobbyNotification notificationHub,
            ILobbyIdGenerator idGenerator)
        {
            _lobbyAppService = lobbyAppService
                ?? throw new ArgumentNullException(nameof(lobbyAppService));
            _lobbyStore = lobbyStore
                ?? throw new ArgumentNullException(nameof(lobbyStore));
            _notificationHub = notificationHub
                ?? throw new ArgumentNullException(nameof(notificationHub));
            _idGenerator = idGenerator
                ?? throw new ArgumentNullException(nameof(idGenerator));

            _cleanupTimer = new Timer(
                _ => CleanupExpiredLobbies(),
                null,
                TimeSpan.FromSeconds(LobbyServiceConstants.CLEANUP_INTERVAL_SECONDS),
                TimeSpan.FromSeconds(LobbyServiceConstants.CLEANUP_INTERVAL_SECONDS));
        }

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            if (request == null)
            {
                Throw("REQ_NULL", LobbyServiceConstants.ERROR_REQ_NULL);
            }

            ValidateMaxPlayers(request.MaxPlayers);

            bool isGuestHost = IsGuestUser(request.HostUserId);

            int lobbyId;
            string code;
            DateTime expiresAtUtc;

            int effectiveTtlMinutes =
                Math.Max(LobbyServiceConstants.MIN_TTL_MINUTES, request.TtlMinutes);

            if (isGuestHost)
            {
                lobbyId = _idGenerator.GenerateLobbyId();
                code = _idGenerator.GenerateLobbyCode();
                expiresAtUtc = DateTime.UtcNow.AddMinutes(effectiveTtlMinutes);
            }
            else
            {
                CreateGameResponse created = _lobbyAppService.CreateGame(request);

                lobbyId = created.PartidaId;
                code = created.CodigoPartida;
                expiresAtUtc = created.ExpiresAtUtc;
            }

            LobbyInfo lobby = BuildLobbyFromRequest(
                request,
                lobbyId,
                code,
                expiresAtUtc);

            _lobbyStore.AddOrUpdateLobby(lobby);

            RegisterCurrentCallback(lobby.PartidaId, request.HostUserId);
            BroadcastPublicLobbiesSnapshot();

            _logger.InfoFormat(
                "CreateGame: LobbyId={0}, Code={1}, IsPrivate={2}, Status={3}, " +
                "ExpiresAtUtc={4:u}",
                lobby.PartidaId,
                lobby.CodigoPartida,
                lobby.IsPrivate,
                lobby.Status,
                lobby.ExpiresAtUtc);

            return new CreateGameResponse
            {
                PartidaId = lobbyId,
                CodigoPartida = code,
                ExpiresAtUtc = expiresAtUtc
            };
        }

        public JoinLobbyResponse JoinLobby(JoinLobbyRequest request)
        {
            CleanupExpiredLobbies();

            _logger.Info("JoinLobby: inicio.");

            if (request == null)
            {
                _logger.Warn("JoinLobby: request nulo.");
                return Fail(LobbyServiceConstants.ERROR_REQ_NULL);
            }

            _logger.InfoFormat(
                "JoinLobby: buscando lobby con código {0}. UserId={1}",
                request.CodigoPartida,
                request.UserId);

            LobbyInfo lobby;
            if (!_lobbyStore.TryFindByCode(request.CodigoPartida, out lobby))
            {
                _logger.WarnFormat(
                    "JoinLobby: lobby no encontrado para código {0}.",
                    request.CodigoPartida);

                return Fail(LobbyServiceConstants.ERROR_INVALID_CODE);
            }

            DateTime nowUtc = DateTime.UtcNow;

            if (IsLobbyExpired(lobby, nowUtc) ||
                lobby.Status == LobbyStatus.Closed)
            {
                _logger.WarnFormat(
                    "JoinLobby: lobby expirado o cerrado. PartidaId={0}, " +
                    "Status={1}, ExpiresAtUtc={2:u}",
                    lobby.PartidaId,
                    lobby.Status,
                    lobby.ExpiresAtUtc);

                CloseLobby(lobby, LobbyServiceConstants.REASON_CLOSED);
                BroadcastPublicLobbiesSnapshot();

                return Fail(LobbyServiceConstants.ERROR_EXPIRED_OR_CLOSED);
            }

            _logger.InfoFormat(
                "JoinLobby: lobby {0} encontrado. Status={1}, Players={2}/{3}",
                lobby.PartidaId,
                lobby.Status,
                lobby.Players.Count,
                lobby.MaxPlayers);

            if (lobby.Status != LobbyStatus.Waiting)
            {
                _logger.Warn("JoinLobby: lobby no está en estado Waiting.");
                return Fail(LobbyServiceConstants.ERROR_NOT_WAITING);
            }

            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                _logger.Warn("JoinLobby: lobby lleno.");
                return Fail(LobbyServiceConstants.ERROR_LOBBY_FULL);
            }

            LobbyMember existing = lobby
                .Players
                .FirstOrDefault(player => player.UserId == request.UserId);

            if (existing != null)
            {
                _logger.InfoFormat(
                    "JoinLobby: usuario {0} ya estaba en el lobby {1}. " +
                    "Solo se re-registra callback.",
                    request.UserId,
                    lobby.PartidaId);

                RegisterCurrentCallback(lobby.PartidaId, request.UserId);
                _notificationHub.NotifyLobbyUpdated(lobby);

                return new JoinLobbyResponse
                {
                    Success = true,
                    Lobby = lobby
                };
            }

            _logger.InfoFormat(
                "JoinLobby: usuario {0} nuevo en lobby {1}. " +
                "Registrando en BD (si no es guest).",
                request.UserId,
                lobby.PartidaId);

            if (!IsGuestUser(request.UserId))
            {
                _lobbyAppService.RegisterPlayerInGame(
                    lobby.PartidaId,
                    request.UserId);

                _logger.InfoFormat(
                    "JoinLobby: RegisterPlayerInGame completado " +
                    "para GameId={0}, UserId={1}.",
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

            _logger.InfoFormat(
                "JoinLobby: usuario {0} agregado al lobby {1}. Total players={2}.",
                request.UserId,
                lobby.PartidaId,
                lobby.Players.Count);

            _lobbyStore.AddOrUpdateLobby(lobby);
            RegisterCurrentCallback(lobby.PartidaId, request.UserId);

            _notificationHub.NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbiesSnapshot();

            _logger.Info("JoinLobby: fin OK.");

            return new JoinLobbyResponse
            {
                Success = true,
                Lobby = lobby
            };
        }

        public OperationResult LeaveLobby(LeaveLobbyRequest request)
        {
            LobbyInfo lobby;
            if (!_lobbyStore.TryGetLobby(request.PartidaId, out lobby))
            {
                return new OperationResult
                {
                    Success = true,
                    Message = LobbyServiceConstants
                        .INFO_LEFT_LOBBY_ALREADY_CLOSED
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
                    Message = LobbyServiceConstants.INFO_NOT_IN_LOBBY
                };
            }

            lobby.Players.Remove(member);
            _lobbyStore.AddOrUpdateLobby(lobby);

            _notificationHub.RemoveLobbyCallback(lobby.PartidaId, request.UserId);

            if (member.IsHost)
            {
                LobbyMember nextHost = lobby.Players.FirstOrDefault();

                if (nextHost != null)
                {
                    PromoteHost(lobby, nextHost);
                }
                else
                {
                    CloseLobby(lobby, LobbyServiceConstants.REASON_CLOSED);
                    BroadcastPublicLobbiesSnapshot();

                    return new OperationResult
                    {
                        Success = true,
                        Message = LobbyServiceConstants.INFO_LOBBY_CLOSED
                    };
                }
            }

            _notificationHub.NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbiesSnapshot();

            return new OperationResult
            {
                Success = true,
                Message = LobbyServiceConstants.INFO_LEFT_LOBBY
            };
        }

        public OperationResult StartMatch(StartMatchRequest request)
        {
            LobbyInfo lobby;
            if (!_lobbyStore.TryGetLobby(request.PartidaId, out lobby))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = LobbyServiceConstants.ERROR_LOBBY_NOT_FOUND
                };
            }

            DateTime nowUtc = DateTime.UtcNow;

            if (IsLobbyExpired(lobby, nowUtc) ||
                lobby.Status == LobbyStatus.Closed)
            {
                CloseLobby(lobby, LobbyServiceConstants.REASON_CLOSED);
                BroadcastPublicLobbiesSnapshot();

                return new OperationResult
                {
                    Success = false,
                    Message = LobbyServiceConstants.ERROR_EXPIRED_OR_CLOSED
                };
            }

            if (lobby.HostUserId != request.HostUserId)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = LobbyServiceConstants
                        .ERROR_ONLY_HOST_CAN_START
                };
            }

            if (lobby.Players.Count < LobbyServiceConstants.MIN_MAX_PLAYERS)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = LobbyServiceConstants.ERROR_NOT_ENOUGH_PLAYERS
                };
            }

            lobby.Status = LobbyStatus.InMatch;
            _lobbyStore.AddOrUpdateLobby(lobby);

            _notificationHub.NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbiesSnapshot();

            return new OperationResult
            {
                Success = true,
                Message = LobbyServiceConstants.INFO_MATCH_STARTING
            };
        }

        public LobbyInfo GetLobbyInfo(GetLobbyInfoRequest request)
        {
            CleanupExpiredLobbies();

            LobbyInfo lobby;
            if (_lobbyStore.TryGetLobby(request.PartidaId, out lobby))
            {
                return _lobbyStore.CloneLobby(lobby);
            }

            // No devolvemos null: devolvemos un lobby cerrado vacío.
            return new LobbyInfo
            {
                PartidaId = request.PartidaId,
                Status = LobbyStatus.Closed,
                Players = new List<LobbyMember>()
            };
        }

        public void SubscribePublicLobbies(int userId)
        {
            CleanupExpiredLobbies();

            ILobbyCallback callback =
                OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            if (callback == null)
            {
                return;
            }

            _notificationHub.SubscribePublicLobbies(userId, callback);

            IList<LobbySummary> snapshot = BuildPublicLobbySummaries();
            _notificationHub.BroadcastPublicLobbies(snapshot);
        }

        public void UnsubscribePublicLobbies(int userId)
        {
            _notificationHub.UnsubscribePublicLobbies(userId);
        }

        public void KickUserFromAllLobbies(int userId, string reason)
        {
            if (userId <= 0)
            {
                return;
            }

            IReadOnlyCollection<LobbyInfo> snapshot = _lobbyStore.GetAll();

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
                _lobbyStore.AddOrUpdateLobby(lobby);

                _notificationHub.RemoveLobbyCallback(lobby.PartidaId, userId);

                string kickReason =
                    reason ?? LobbyServiceConstants.REASON_KICKED;

                _notificationHub.NotifyKickedFromLobby(
                    lobby.PartidaId,
                    userId,
                    kickReason);

                if (member.IsHost)
                {
                    LobbyMember nextHost = lobby.Players.FirstOrDefault();

                    if (nextHost != null)
                    {
                        PromoteHost(lobby, nextHost);
                        _notificationHub.NotifyLobbyUpdated(lobby);
                    }
                    else
                    {
                        CloseLobby(lobby, LobbyServiceConstants.REASON_CLOSED);
                    }
                }
                else
                {
                    _notificationHub.NotifyLobbyUpdated(lobby);
                }
            }

            BroadcastPublicLobbiesSnapshot();
        }

        public void KickPlayerFromLobby(KickPlayerFromLobbyRequest request)
        {
            if (request == null)
            {
                Throw("REQ_NULL", LobbyServiceConstants.ERROR_KICK_REQ_NULL);
            }

            if (request.LobbyId <= 0 ||
                request.HostUserId <= 0 ||
                request.TargetUserId == 0)
            {
                Throw("REQ_INVALID", LobbyServiceConstants.ERROR_KICK_REQ_INVALID);
            }

            if (request.HostUserId == request.TargetUserId)
            {
                Throw("KICK_SELF", LobbyServiceConstants.ERROR_KICK_SELF);
            }

            LobbyInfo lobby;
            if (!_lobbyStore.TryGetLobby(request.LobbyId, out lobby))
            {
                return;
            }

            if (lobby.HostUserId != request.HostUserId)
            {
                Throw("KICK_NOT_HOST", LobbyServiceConstants.ERROR_KICK_NOT_HOST);
            }

            LobbyMember targetMember = lobby
                .Players
                .FirstOrDefault(player => player.UserId == request.TargetUserId);

            if (targetMember == null)
            {
                return;
            }

            lobby.Players.Remove(targetMember);
            _lobbyStore.AddOrUpdateLobby(lobby);

            NotifyKickedUserAndRemoveCallback(
                lobby.PartidaId,
                targetMember.UserId,
                LobbyServiceConstants.REASON_KICKED_BY_HOST);

            if (targetMember.IsHost)
            {
                LobbyMember nextHost = lobby.Players.FirstOrDefault();

                if (nextHost != null)
                {
                    PromoteHost(lobby, nextHost);
                }
                else
                {
                    CloseLobby(lobby, LobbyServiceConstants.REASON_CLOSED);
                    BroadcastPublicLobbiesSnapshot();
                    return;
                }
            }

            _notificationHub.NotifyLobbyUpdated(lobby);
            BroadcastPublicLobbiesSnapshot();

            if (!IsGuestUser(targetMember.UserId))
            {
                _lobbyAppService.KickPlayerFromLobby(
                    request.LobbyId,
                    request.HostUserId,
                    request.TargetUserId);
            }
        }

        private static bool IsGuestUser(int userId)
        {
            return userId < 0;
        }

        private static bool IsLobbyExpired(LobbyInfo lobby, DateTime nowUtc)
        {
            if (lobby == null)
            {
                return true;
            }

            return lobby.ExpiresAtUtc <= nowUtc;
        }

        private void CleanupExpiredLobbies()
        {
            DateTime nowUtc = DateTime.UtcNow;

            IReadOnlyCollection<LobbyInfo> snapshot = _lobbyStore.GetAll();

            foreach (LobbyInfo lobby in snapshot)
            {
                if (IsLobbyExpired(lobby, nowUtc) &&
                    lobby.Status != LobbyStatus.Closed)
                {
                    _logger.InfoFormat(
                        "CleanupExpiredLobbies: cerrando lobby expirado. " +
                        "PartidaId={0}, ExpiresAtUtc={1:u}",
                        lobby.PartidaId,
                        lobby.ExpiresAtUtc);

                    CloseLobby(lobby, LobbyServiceConstants.REASON_CLOSED);
                }
            }

            BroadcastPublicLobbiesSnapshot();
        }

        private void RegisterCurrentCallback(int lobbyId, int userId)
        {
            ILobbyCallback callback =
                OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            if (callback == null)
            {
                return;
            }

            _notificationHub.RegisterLobbyCallback(lobbyId, userId, callback);
        }

        private void CloseLobby(LobbyInfo lobby, string reason)
        {
            if (lobby == null)
            {
                return;
            }

            lobby.Status = LobbyStatus.Closed;
            _lobbyStore.RemoveLobby(lobby.PartidaId);

            _notificationHub.NotifyLobbyClosed(
                lobby.PartidaId,
                reason ?? LobbyServiceConstants.REASON_CLOSED);
        }

        private void PromoteHost(LobbyInfo lobby, LobbyMember newHost)
        {
            lobby.HostUserId = newHost.UserId;
            lobby.HostUserName = newHost.UserName;

            foreach (LobbyMember player in lobby.Players)
            {
                player.IsHost = player.UserId == newHost.UserId;
            }

            _lobbyStore.AddOrUpdateLobby(lobby);
        }

        private void NotifyKickedUserAndRemoveCallback(
            int lobbyId,
            int targetUserId,
            string reason)
        {
            _notificationHub.RemoveLobbyCallback(lobbyId, targetUserId);

            _notificationHub.NotifyKickedFromLobby(
                lobbyId,
                targetUserId,
                reason ?? LobbyServiceConstants.REASON_KICKED);
        }

        private void BroadcastPublicLobbiesSnapshot()
        {
            IList<LobbySummary> snapshot = BuildPublicLobbySummaries();
            _notificationHub.BroadcastPublicLobbies(snapshot);
        }

        private IList<LobbySummary> BuildPublicLobbySummaries()
        {
            DateTime nowUtc = DateTime.UtcNow;

            return _lobbyStore
                .GetAll()
                .Where(lobby =>
                    !lobby.IsPrivate &&
                    lobby.Status == LobbyStatus.Waiting &&
                    !IsLobbyExpired(lobby, nowUtc))
                .Select(lobby => new LobbySummary
                {
                    PartidaId = lobby.PartidaId,
                    CodigoPartida = lobby.CodigoPartida,
                    HostUserName = lobby.HostUserName,
                    MaxPlayers = lobby.MaxPlayers,
                    CurrentPlayers = (byte)lobby.Players.Count,
                    Difficulty = lobby.Difficulty,
                    IsPrivate = lobby.IsPrivate
                })
                .ToList();
        }

        private static void ValidateMaxPlayers(int maxPlayers)
        {
            if (maxPlayers < LobbyServiceConstants.MIN_MAX_PLAYERS ||
                maxPlayers > LobbyServiceConstants.MAX_MAX_PLAYERS)
            {
                Throw("MAX_PLAYERS", LobbyServiceConstants.ERROR_MAX_PLAYERS);
            }
        }

        private static LobbyInfo BuildLobbyFromRequest(
            CreateGameRequest request,
            int lobbyId,
            string code,
            DateTime expiresAtUtc)
        {
            string hostName = string.Format(
                LobbyServiceConstants.DEFAULT_HOST_NAME_FORMAT,
                request.HostUserId);

            var lobby = new LobbyInfo
            {
                PartidaId = lobbyId,
                CodigoPartida = code,
                HostUserId = request.HostUserId,
                HostUserName = hostName,
                MaxPlayers = request.MaxPlayers,
                Status = LobbyStatus.Waiting,
                ExpiresAtUtc = expiresAtUtc,
                BoardSide = request.BoardSide,
                Difficulty = request.Dificultad,
                PlayersRequested = request.PlayersRequested,
                SpecialTiles = request.SpecialTiles,
                IsPrivate = request.IsPrivate,
                Players = new List<LobbyMember>()
            };

            lobby.Players.Add(
                new LobbyMember
                {
                    UserId = request.HostUserId,
                    UserName = hostName,
                    IsHost = true,
                    JoinedAtUtc = DateTime.UtcNow,
                    AvatarId = request.HostAvatarId,
                    CurrentSkinUnlockedId = request.CurrentSkinUnlockedId,
                    CurrentSkinId = request.CurrentSkinId
                });

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
    }
}

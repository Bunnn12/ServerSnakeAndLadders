using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public sealed class InMemoryLobbyStore : ILobbyStore
    {
        private readonly ConcurrentDictionary<int, LobbyInfo> _lobbies =
            new ConcurrentDictionary<int, LobbyInfo>();

        public void AddOrUpdateLobby(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby));
            }

            _lobbies[lobby.PartidaId] = lobby;
        }

        public bool TryGetLobby(int lobbyId, out LobbyInfo lobby)
        {
            return _lobbies.TryGetValue(lobbyId, out lobby);
        }

        public bool TryFindByCode(string code, out LobbyInfo lobby)
        {
            lobby = null;

            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            LobbyInfo foundLobby = _lobbies
                .Values
                .FirstOrDefault(info => info.CodigoPartida == code);

            if (foundLobby == null)
            {
                return false;
            }

            lobby = foundLobby;
            return true;
        }

        public bool RemoveLobby(int lobbyId)
        {
            return _lobbies.TryRemove(lobbyId, out _);
        }

        public IReadOnlyCollection<LobbyInfo> GetAll()
        {
            return _lobbies.Values.ToArray();
        }

        public LobbyInfo CloneLobby(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby));
            }

            var clone = new LobbyInfo
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
                Players = new List<LobbyMember>()
            };

            foreach (LobbyMember player in lobby.Players)
            {
                clone.Players.Add(
                    new LobbyMember
                    {
                        UserId = player.UserId,
                        UserName = player.UserName,
                        IsHost = player.IsHost,
                        JoinedAtUtc = player.JoinedAtUtc,
                        AvatarId = player.AvatarId,
                        CurrentSkinUnlockedId = player.CurrentSkinUnlockedId,
                        CurrentSkinId = player.CurrentSkinId
                    });
            }

            return clone;
        }
    }
}

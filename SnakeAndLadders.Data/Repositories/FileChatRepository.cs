using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    /// <summary>
    /// File-based chat repository. Stores one JSON line per message by lobby.
    /// </summary>
    public sealed class FileChatRepository : IChatRepository
    {
        private readonly string baseDir;

        public FileChatRepository(string templatePathOrDir)
        {
            // If a file path is provided, use its directory; otherwise assume a directory.
            var expanded = Environment.ExpandEnvironmentVariables(templatePathOrDir);
            baseDir = Directory.Exists(expanded) ? expanded : Path.GetDirectoryName(expanded);

            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SnakesAndLadders", "Chat");
            }

            Directory.CreateDirectory(baseDir);
        }

        private string FileFor(int lobbyId) => Path.Combine(baseDir, $"{lobbyId}.jsonl");

        public void Append(int lobbyId, ChatMessageDto message)
        {
            var path = FileFor(lobbyId);
            var json = JsonSerializer.Serialize(message);
            File.AppendAllText(path, json + Environment.NewLine, Encoding.UTF8);
        }

        public IList<ChatMessageDto> ReadLast(int lobbyId, int take)
        {
            var path = FileFor(lobbyId);
            if (!File.Exists(path))
            {
                return new List<ChatMessageDto>();
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8);

            return lines
                .Reverse()
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(Math.Max(1, take))
                .Select(l => JsonSerializer.Deserialize<ChatMessageDto>(l))
                .Where(m => m != null)
                .Reverse()
                .ToList();
        }
    }
}

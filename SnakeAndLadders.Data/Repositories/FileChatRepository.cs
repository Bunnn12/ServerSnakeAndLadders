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
    public sealed class FileChatRepository : IChatRepository
    {
        private readonly string filePath;

        public FileChatRepository(string filePath)
        {
            this.filePath = Environment.ExpandEnvironmentVariables(filePath);
            var dir = Path.GetDirectoryName(this.filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(this.filePath)) File.WriteAllText(this.filePath, string.Empty, Encoding.UTF8);
        }

        public void Append(ChatMessageDto message)
        {
            var json = JsonSerializer.Serialize(message);
            File.AppendAllText(filePath, json + Environment.NewLine, Encoding.UTF8);
        }

        public IList<ChatMessageDto> ReadLast(int take)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            return lines.Reverse()
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Take(Math.Max(1, take))
                        .Select(l => JsonSerializer.Deserialize<ChatMessageDto>(l))
                        .Where(m => m != null)
                        .Reverse()
                        .ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class FileChatRepository : IChatRepository
    {
        private const string APP_FOLDER_NAME = "SnakesAndLadders";
        private const string CHAT_FOLDER_NAME = "Chat";

        private const int DEFAULT_TAKE_MESSAGES = 50;
        private const int FILE_BUFFER_SIZE = 4096;
        private const int MIN_VALID_LOBBY_ID = 1;

        private static readonly object SyncLock = new object();

        private static readonly string[] NewLineSeparators = { "\r\n", "\n" };

        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

        private readonly string chatStorageDirectory;
        private readonly JsonSerializerOptions jsonOptions;

        public FileChatRepository(string templatePathOrDir)
        {
            string baseAppFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string rootDirectory = Path.Combine(
                baseAppFolder,
                APP_FOLDER_NAME,
                CHAT_FOLDER_NAME);

            string subDirectoryName = GetSafeSubDirectoryName(templatePathOrDir);
            string baseDirectory = string.IsNullOrEmpty(subDirectoryName)
                ? rootDirectory
                : Path.Combine(rootDirectory, subDirectoryName);

            Directory.CreateDirectory(baseDirectory);

            chatStorageDirectory = Path.GetFullPath(baseDirectory);

            jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.Latin1Supplement),
                WriteIndented = false
            };
        }

        public void SaveMessage(int lobbyId, ChatMessageDto message)
        {
            ValidateLobbyId(lobbyId);
            ValidateMessage(message);

            string filePath = GetFilePathForLobby(lobbyId);
            string serializedLine = SerializeMessage(message);

            AppendLineToFile(filePath, serializedLine);
        }

        public IList<ChatMessageDto> ReadLast(int lobbyId, int take)
        {
            ValidateLobbyId(lobbyId);

            int effectiveTake = take <= 0
                ? DEFAULT_TAKE_MESSAGES
                : take;

            string filePath = GetFilePathForLobby(lobbyId);

            if (!File.Exists(filePath))
            {
                return new List<ChatMessageDto>(0);
            }

            string[] lines = ReadAllLines(filePath);
            IList<ChatMessageDto> lastMessages = ReadLastMessagesFromLines(lines, effectiveTake);

            return lastMessages;
        }

        private static void ValidateLobbyId(int lobbyId)
        {
            if (lobbyId < MIN_VALID_LOBBY_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lobbyId),
                    string.Format(
                        "LobbyId must be greater than or equal to {0}.",
                        MIN_VALID_LOBBY_ID));
            }
        }

        private static void ValidateMessage(ChatMessageDto message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            bool hasText = !string.IsNullOrWhiteSpace(message.Text);
            bool hasSticker = message.StickerId > 0
                              && !string.IsNullOrWhiteSpace(message.StickerCode);

            if (!hasText && !hasSticker)
            {
                throw new ArgumentException(
                    "Message must have text or a sticker.",
                    nameof(message));
            }
        }

        private static string GetSafeSubDirectoryName(string templatePathOrDir)
        {
            if (string.IsNullOrWhiteSpace(templatePathOrDir))
            {
                return string.Empty;
            }

            string trimmed = templatePathOrDir.Trim();
            return Path.GetFileName(trimmed) ?? string.Empty;
        }

        private string GetFilePathForLobby(int lobbyId)
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0:D10}.jsonl",
                lobbyId);

            string combinedPath = Path.Combine(chatStorageDirectory, fileName);
            string fullPath = Path.GetFullPath(combinedPath);

            string baseWithSeparator = chatStorageDirectory.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal)
                ? chatStorageDirectory
                : chatStorageDirectory + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Directory traversal attempt in FileChatRepository.");
            }

            return fullPath;
        }

        private string SerializeMessage(ChatMessageDto message)
        {
            return JsonSerializer.Serialize(message, jsonOptions);
        }

        private static void AppendLineToFile(string filePath, string line)
        {
            string lineWithNewLine = line + Environment.NewLine;

            lock (SyncLock)
            {
                using (var fileStream = new FileStream(
                           filePath,
                           FileMode.Append,
                           FileAccess.Write,
                           FileShare.Read,
                           FILE_BUFFER_SIZE))
                using (var streamWriter = new StreamWriter(fileStream, Utf8WithoutBom))
                {
                    streamWriter.Write(lineWithNewLine);
                }
            }
        }

        private static string[] ReadAllLines(string filePath)
        {
            using (var fileStream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Utf8WithBom, true))
            {
                string fileContent = streamReader.ReadToEnd();

                string[] lines = fileContent.Split(
                    NewLineSeparators,
                    StringSplitOptions.None);

                return lines;
            }
        }

        private IList<ChatMessageDto> ReadLastMessagesFromLines(string[] lines, int take)
        {
            var result = new List<ChatMessageDto>();

            if (lines == null || lines.Length == 0)
            {
                return result;
            }

            foreach (string lineText in lines
                         .Reverse()
                         .Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                if (TryDeserializeMessage(lineText, out ChatMessageDto message))
                {
                    result.Add(message);
                }

                if (result.Count >= take)
                {
                    break;
                }
            }

            result.Reverse();
            return result;
        }

        private bool TryDeserializeMessage(string jsonLine, out ChatMessageDto message)
        {
            message = new ChatMessageDto();

            try
            {
                ChatMessageDto deserialized = JsonSerializer.Deserialize<ChatMessageDto>(
                    jsonLine,
                    jsonOptions);

                if (deserialized == null)
                {
                    return false;
                }

                message = deserialized;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}

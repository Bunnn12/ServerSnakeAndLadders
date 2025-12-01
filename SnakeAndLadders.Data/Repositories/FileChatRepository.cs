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
        private static readonly object _syncLock = new object();

        private const string APP_FOLDER_NAME = "SnakesAndLadders";
        private const string CHAT_FOLDER_NAME = "Chat";

        private const int MAX_LOBBY_ID = 1_000_000_000;
        private const int DEFAULT_TAKE_MESSAGES = 50;
        private const int FILE_BUFFER_SIZE = 4096;

        private static readonly string[] NEW_LINE_SEPARATORS = { "\r\n", "\n" };

        private readonly string _chatStorageDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public FileChatRepository(string templatePathOrDir)
        {
            string safeRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                APP_FOLDER_NAME,
                CHAT_FOLDER_NAME);

            string subDirectoryName = string.IsNullOrWhiteSpace(templatePathOrDir)
                ? null
                : Path.GetFileName(templatePathOrDir.Trim());

            string baseDirectory = string.IsNullOrEmpty(subDirectoryName)
                ? safeRoot
                : Path.Combine(safeRoot, subDirectoryName);

            Directory.CreateDirectory(baseDirectory);

            _chatStorageDirectory = Path.GetFullPath(baseDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.Latin1Supplement),
                WriteIndented = false
            };
        }

        private static void ValidateLobbyId(int lobbyId)
        {
            if (lobbyId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lobbyId));
            }

            if (lobbyId > MAX_LOBBY_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(lobbyId));
            }
        }

        private string GetFilePathForLobby(int lobbyId)
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0:D10}.jsonl",
                lobbyId);

            string combinedPath = Path.Combine(_chatStorageDirectory, fileName);
            string fullPath = Path.GetFullPath(combinedPath);

            string baseWithSeparator = _chatStorageDirectory.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal)
                ? _chatStorageDirectory
                : _chatStorageDirectory + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Intento de escape de directorio en FileChatRepository.");
            }

            return fullPath;
        }
        public void Append(int lobbyId, ChatMessageDto message)
        {
            ValidateLobbyId(lobbyId);

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string filePath = GetFilePathForLobby(lobbyId);

            try
            {
                string line = JsonSerializer.Serialize(message, _jsonOptions) + Environment.NewLine;
                var utf8EncodingWithoutBom = new UTF8Encoding(false);

                lock (_syncLock)
                {
                    FileStream chatFileStream = null;
                    StreamWriter chatStreamWriter = null;

                    try
                    {
                        chatFileStream = new FileStream(
                            filePath,
                            FileMode.OpenOrCreate,
                            FileAccess.Write,
                            FileShare.Read,
                            FILE_BUFFER_SIZE,
                            FileOptions.WriteThrough);

                        chatFileStream.Seek(0, SeekOrigin.End);

                        chatStreamWriter = new StreamWriter(chatFileStream, utf8EncodingWithoutBom);
                        chatStreamWriter.Write(line);
                        chatStreamWriter.Flush();

                        chatFileStream.Flush(true);
                    }
                    finally
                    {
                        chatStreamWriter?.Dispose();
                        chatFileStream?.Dispose();
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    $"No se tienen permisos para escribir el archivo '{filePath}'.",
                    ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                try
                {
                    Directory.CreateDirectory(_chatStorageDirectory);
                }
                catch
                {
                    throw new IOException(
                        $"El directorio '{_chatStorageDirectory}' no existe y no se pudo crear.",
                        ex);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"Error de E/S al escribir el chat '{filePath}': {ex.Message}",
                    ex);
            }
            catch (SecurityException ex)
            {
                throw new UnauthorizedAccessException(
                    $"Error de seguridad al acceder a '{filePath}': {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error inesperado al escribir el chat '{filePath}': {ex.Message}",
                    ex);
            }
        }

        public IList<ChatMessageDto> ReadLast(int lobbyId, int take)
        {
            ValidateLobbyId(lobbyId);

            if (take <= 0)
            {
                take = DEFAULT_TAKE_MESSAGES;
            }

            string filePath = GetFilePathForLobby(lobbyId);

            if (!File.Exists(filePath))
            {
                return new List<ChatMessageDto>(0);
            }

            try
            {
                string[] lines;
                var utf8EncodingWithBom = new UTF8Encoding(true);

                using (var chatFileStream = new FileStream(
                           filePath,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.ReadWrite))
                using (var chatStreamReader = new StreamReader(
                           chatFileStream,
                           utf8EncodingWithBom,
                           true))
                {
                    string fileContent = chatStreamReader.ReadToEnd();
                    lines = fileContent.Split(
                        NEW_LINE_SEPARATORS,
                        StringSplitOptions.None);
                }

                var result = new List<ChatMessageDto>(Math.Min(take, lines.Length));

                foreach (string lineText in lines
                             .Reverse()
                             .Where(line => !string.IsNullOrWhiteSpace(line)))
                {
                    try
                    {
                        ChatMessageDto message = JsonSerializer.Deserialize<ChatMessageDto>(
                            lineText,
                            _jsonOptions);

                        if (message != null)
                        {
                            result.Add(message);
                        }

                        if (result.Count >= take)
                        {
                            break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        throw new JsonException(
                            "Hubo un problema con el archivo JSON: " + ex.Message,
                            ex);
                    }
                }

                result.Reverse();
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    $"No se tienen permisos para leer el archivo '{filePath}'.",
                    ex);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"Error de E/S al leer el chat '{filePath}': {ex.Message}",
                    ex);
            }
            catch (SecurityException ex)
            {
                throw new UnauthorizedAccessException(
                    $"Error de seguridad al acceder a '{filePath}': {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error inesperado al leer el chat '{filePath}': {ex.Message}",
                    ex);
            }
        }
    }
}

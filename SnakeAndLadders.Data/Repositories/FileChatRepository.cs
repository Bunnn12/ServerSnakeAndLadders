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
    public sealed class ChatRepositoryException : Exception
    {
        public ChatRepositoryException(string message, Exception inner) : base(message, inner) { }
    }

    public sealed class FileChatRepository : IChatRepository
    {
        private static readonly object _sync = new object();

        private readonly string _baseDirFull;
        private readonly JsonSerializerOptions _json;

        public FileChatRepository(string templatePathOrDir)
        {
            var safeRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SnakesAndLadders", "Chat");

            string sub = string.IsNullOrWhiteSpace(templatePathOrDir)
                ? null
                : Path.GetFileName(templatePathOrDir.Trim());

            var baseDir = string.IsNullOrEmpty(sub) ? safeRoot : Path.Combine(safeRoot, sub);

            Directory.CreateDirectory(baseDir);
            _baseDirFull = Path.GetFullPath(baseDir);

            _json = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement),
                WriteIndented = false
            };
        }

        private static void ValidateLobbyId(int lobbyId)
        {
            if (lobbyId <= 0) throw new ArgumentOutOfRangeException("lobbyId");
            if (lobbyId > 1_000_000_000) throw new ArgumentOutOfRangeException("lobbyId");
        }

        private string FileFor(int lobbyId)
        {
            var fileName = string.Format(CultureInfo.InvariantCulture, "{0:D10}.jsonl", lobbyId);
            var combined = Path.Combine(_baseDirFull, fileName);

            var full = Path.GetFullPath(combined);
            var baseWithSep = _baseDirFull.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? _baseDirFull
                : _baseDirFull + Path.DirectorySeparatorChar;

            if (!full.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Intento de escape de directorio en FileChatRepository.");
            }

            return full;
        }


        public void Append(int lobbyId, ChatMessageDto message)
        {
            ValidateLobbyId(lobbyId);
            if (message == null) throw new ArgumentNullException("message");

            var path = FileFor(lobbyId);

            try
            {
                var line = JsonSerializer.Serialize(message, _json) + Environment.NewLine;
                var utf8NoBom = new UTF8Encoding(false);

                lock (_sync)
                {
                    FileStream fs = null;
                    StreamWriter sw = null;
                    try
                    {
                        fs = new FileStream(
                            path,
                            FileMode.OpenOrCreate,
                            FileAccess.Write,
                            FileShare.Read,
                            4096,
                            FileOptions.WriteThrough);

                        fs.Seek(0, SeekOrigin.End);
                        sw = new StreamWriter(fs, utf8NoBom);
                        sw.Write(line);
                        sw.Flush();
                        fs.Flush(true);
                    }
                    finally
                    {
                        if (sw != null) sw.Dispose();
                        if (fs != null) fs.Dispose();
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ChatRepositoryException(
                    "UnauthorizedAccessException al escribir el chat '" + path + "': " + ex.Message, ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                try { Directory.CreateDirectory(_baseDirFull); } catch { }
                throw new ChatRepositoryException(
                    "DirectoryNotFoundException al escribir el chat '" + path + "': " + ex.Message, ex);
            }
            catch (IOException ex)
            {
                throw new ChatRepositoryException(
                    "IOException al escribir el chat '" + path + "': " + ex.Message, ex);
            }
            catch (SecurityException ex)
            {
                throw; 
            }
            catch (Exception ex)
            {
                throw new ChatRepositoryException(
                    ex.GetType().Name + " al escribir el chat '" + path + "': " + ex.Message, ex);
            }
        }

        public IList<ChatMessageDto> ReadLast(int lobbyId, int take)
        {
            ValidateLobbyId(lobbyId);
            if (take <= 0) take = 50;

            var path = FileFor(lobbyId);
            if (!File.Exists(path)) return new List<ChatMessageDto>(0);

            try
            {
                string[] lines;
                var utf8 = new UTF8Encoding(true);

                FileStream fs = null;
                StreamReader sr = null;
                try
                {
                    fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs, utf8, true);
                    var all = sr.ReadToEnd();
                    lines = all.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                }
                finally
                {
                    if (sr != null) sr.Dispose();
                    if (fs != null) fs.Dispose();
                }

                var result = new List<ChatMessageDto>(Math.Min(take, lines.Length));
                foreach (var l in lines.Reverse().Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    try
                    {
                        var m = JsonSerializer.Deserialize<ChatMessageDto>(l, _json);
                        if (m != null) result.Add(m);
                        if (result.Count >= take) break;
                    }
                    catch (JsonException ex)
                    {
                        throw new JsonException ("Hubo un problema con el archivo json"+ ex.Message);
                    }
                }

                result.Reverse();
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ChatRepositoryException(
                    "UnauthorizedAccessException al leer el chat '" + path + "': " + ex.Message, ex);
            }
            catch (IOException ex)
            {
                throw new ChatRepositoryException(
                    "IOException al leer el chat '" + path + "': " + ex.Message, ex);
            }
            catch (SecurityException ex)
            {
                throw new SecurityException(ex.Message); 
            }
            catch (Exception ex)
            {
                throw new ChatRepositoryException(
                    ex.GetType().Name + " al leer el chat '" + path + "': " + ex.Message, ex);
            }
        }
    }
}

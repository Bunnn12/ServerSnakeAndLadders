using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// Repositorio de chat basado en archivo. Un JSON por línea por lobby.
    /// Thread-safe (in-proc) y con validaciones básicas.
    /// </summary>
    public sealed class FileChatRepository : IChatRepository
    {
        private static readonly object _sync = new object();

        private readonly string _baseDir;
        private readonly JsonSerializerOptions _json;

        public FileChatRepository(string templatePathOrDir)
        {
            var safeRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SnakesAndLadders", "Chat");

            if (!string.IsNullOrWhiteSpace(templatePathOrDir))
            {
                var nameOnly = Path.GetFileName(templatePathOrDir.Trim());
                _baseDir = Path.Combine(safeRoot, nameOnly);
            }
            else
            {
                _baseDir = safeRoot;
            }

            Directory.CreateDirectory(_baseDir);

            _json = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement),
                WriteIndented = false
            };
        }

        private string FileFor(int lobbyId)
        {
            return Path.Combine(_baseDir, string.Format("{0}.jsonl", lobbyId));
        }

        public void Append(int lobbyId, ChatMessageDto message)
        {
            if (lobbyId <= 0) throw new ArgumentOutOfRangeException("lobbyId");
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
                        fs.Flush(true); // flushToDisk
                    }
                    finally
                    {
                        // C# 7.3: liberar explícitamente
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
                try { Directory.CreateDirectory(_baseDir); } catch { /* ignoramos; re-lanzamos abajo */ }
                throw new ChatRepositoryException(
                    "DirectoryNotFoundException al escribir el chat '" + path + "': " + ex.Message, ex);
            }
            catch (IOException ex)
            {
                throw new ChatRepositoryException(
                    "IOException al escribir el chat '" + path + "': " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new ChatRepositoryException(
                    ex.GetType().Name + " al escribir el chat '" + path + "': " + ex.Message, ex);
            }
        }

        public IList<ChatMessageDto> ReadLast(int lobbyId, int take)
        {
            if (lobbyId <= 0) throw new ArgumentOutOfRangeException("lobbyId");
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
                    catch (JsonException)
                    {
                        // línea corrupta: se omite; aquí podrías loggear el tipo exacto
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
            catch (Exception ex)
            {
                throw new ChatRepositoryException(
                    ex.GetType().Name + " al leer el chat '" + path + "': " + ex.Message, ex);
            }
        }
    }
}

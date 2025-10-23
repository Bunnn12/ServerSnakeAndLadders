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
            if (lobbyId <= 0) throw new ArgumentOutOfRangeException(nameof(lobbyId));
            if (lobbyId > 1_000_000_000) throw new ArgumentOutOfRangeException(nameof(lobbyId));
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
            if (message == null) throw new ArgumentNullException(nameof(message));

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
                        sw?.Dispose();
                        fs?.Dispose();
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    $"No se tienen permisos para escribir el archivo '{path}'.", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                try
                {
                    Directory.CreateDirectory(_baseDirFull);
                }
                catch
                {
                    throw new IOException(
                        $"El directorio '{_baseDirFull}' no existe y no se pudo crear.", ex);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"Error de E/S al escribir el chat '{path}': {ex.Message}", ex);
            }
            catch (SecurityException ex)
            {
                throw new UnauthorizedAccessException(
                    $"Error de seguridad al acceder a '{path}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error inesperado al escribir el chat '{path}': {ex.Message}", ex);
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

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, utf8, true))
                {
                    var all = sr.ReadToEnd();
                    lines = all.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
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
                        throw new JsonException("Hubo un problema con el archivo JSON: " + ex.Message, ex);
                    }
                }

                result.Reverse();
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    $"No se tienen permisos para leer el archivo '{path}'.", ex);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"Error de E/S al leer el chat '{path}': {ex.Message}", ex);
            }
            catch (SecurityException ex)
            {
                throw new UnauthorizedAccessException(
                    $"Error de seguridad al acceder a '{path}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error inesperado al leer el chat '{path}': {ex.Message}", ex);
            }
        }
    }
}

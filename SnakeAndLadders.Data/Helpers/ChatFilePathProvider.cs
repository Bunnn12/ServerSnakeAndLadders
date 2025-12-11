using SnakesAndLadders.Data.Constants;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Helpers
{
    internal sealed class ChatFilePathProvider
    {
        private readonly string _chatStorageDirectory;

        public ChatFilePathProvider(string templatePathOrDir)
        {
            string baseAppFolder = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            string rootDirectory = Path.Combine(
                baseAppFolder,
                ChatRepositoryConstants.APP_FOLDER_NAME,
                ChatRepositoryConstants.CHAT_FOLDER_NAME);

            string subDirectoryName = GetSafeSubDirectoryName(templatePathOrDir);

            string baseDirectory = string.IsNullOrEmpty(subDirectoryName)
                ? rootDirectory
                : Path.Combine(rootDirectory, subDirectoryName);

            Directory.CreateDirectory(baseDirectory);

            _chatStorageDirectory = Path.GetFullPath(baseDirectory);
        }

        public string GetLobbyFilePath(int lobbyId)
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
                throw new SecurityException(ChatRepositoryConstants.ERROR_DIRECTORY_TRAVERSAL);
            }

            return fullPath;
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
    }
}

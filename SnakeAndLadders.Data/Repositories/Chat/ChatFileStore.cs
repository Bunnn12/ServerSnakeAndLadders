using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Chat
{
    internal sealed class ChatFileStore : IChatFileStore
    {
        private static readonly object SyncLock = new object();

        private static readonly string[] NewLineSeparators = { "\r\n", "\n" };

        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

        public void AppendLine(string filePath, string line)
        {
            string lineWithNewLine = line + Environment.NewLine;

            lock (SyncLock)
            {
                using (FileStream fileStream = new FileStream(
                           filePath,
                           FileMode.Append,
                           FileAccess.Write,
                           FileShare.Read,
                           ChatRepositoryConstants.FILE_BUFFER_SIZE))
                using (StreamWriter streamWriter = new StreamWriter(fileStream, Utf8WithoutBom))
                {
                    streamWriter.Write(lineWithNewLine);
                }
            }
        }

        public string[] ReadAllLines(string filePath)
        {
            using (FileStream fileStream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(fileStream, Utf8WithBom, true))
            {
                string fileContent = streamReader.ReadToEnd();

                string[] lines = fileContent.Split(
                    NewLineSeparators,
                    StringSplitOptions.None);

                return lines;
            }
        }
    }
}

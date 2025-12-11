using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    internal interface IChatFileStore
    {
        void AppendLine(string filePath, string line);

        string[] ReadAllLines(string filePath);
    }
}

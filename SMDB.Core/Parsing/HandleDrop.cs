using System;
using System.Collections.Generic;
using SMDB.Core.Models;

namespace SMDB.Core.Parsing
{
    public partial class Parser
    {
        private void HandleDrop(int pos, string query)
        {
            (pos, string tableName) = ReadWord(pos, query);

            if (tableName == "")
            {
                Console.WriteLine("Name not specified.");
                return;
            }

            var table = new Table(tableName);
            table.Drop();
        }
    }
}
using System;
using System.Collections.Generic;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        private void HandleCheck(int pos, string query)
        {
            (pos, string tableName) = ReadWord(pos, query);
            if (tableName == "")
            {
                Console.WriteLine("Missing table name.");
                return;
            }

            var table = new Table(tableName);
            Console.WriteLine(table.CheckIntegrity());
        }

    }
}
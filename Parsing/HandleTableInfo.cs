using System;
using System.Collections.Generic;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        private void HandleTableInfo(int pos, string query)
        {
            (pos, string tableName) = ReadWord(pos, query);

            if (tableName == "")
            {
                Console.WriteLine("Table name is missing!");
                return;
            }

            var table = new Table(tableName);
            table.PrintInfo();
        }
    }
}
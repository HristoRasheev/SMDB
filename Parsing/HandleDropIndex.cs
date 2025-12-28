using System;
using System.Collections.Generic;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        private void HandleDropIndex(int pos, string query)
        {
            (pos, string indexName) = ReadWord(pos, query);
            if (indexName == "")
            {
                Console.WriteLine("Missing index name.");
                return;
            }
            Table.DropIndex(indexName);
        }
    }
}
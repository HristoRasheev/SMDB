using System;
using System.Collections.Generic;
using SMDB.Core.Models;  

namespace SMDB.Core.Parsing
{
    public partial class Parser
    {
        private void HandleCreateIndex(int pos, string query)
        {
            (pos, string indexName) = ReadWord(pos, query);
            if (indexName == "")
            {
                Console.WriteLine("Missing index name.");
                return;
            }

            (pos, string onWord) = ReadWord(pos, query);
            if (onWord != "ON")
            {
                Console.WriteLine("Expected ON.");
                return;
            }
            
            (pos, string tableName) = ReadWord(pos, query);
            if (tableName == "")
            {
                Console.WriteLine("Missing table name after ON.");
                return;
            }

            pos = SkipSpaces(pos, query);
            if (pos >= query.Length || query[pos] != '(')
            {
                Console.WriteLine("Expected '(' after table name.");
                return;
            }
            pos++; 

            (pos, string colName) = ReadWordInRange(pos, query.Length, query);
            if (colName == "")
            {
                Console.WriteLine("Missing column name inside (...). Example: CREATE INDEX PeopleId ON People (Id)");
                return;
            }

            pos = SkipSpaces(pos, query);

            if (pos >= query.Length || query[pos] != ')')
            {
                Console.WriteLine("Expected ')' after column name. Example: CREATE INDEX PeopleId ON People (Id)");
                return;
            }
            pos++;

            var table = new Table(tableName);
            table.CreateIndex(indexName, colName);
        }
    }
}
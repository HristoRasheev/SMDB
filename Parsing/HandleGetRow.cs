using System;
using System.Collections.Generic;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        private void HandleGetRow(int pos, string query)
        {
            (pos, string sw) = ReadWord(pos, query);
            string secondWord = sw;

            if (secondWord != "ROW")
            {
                Console.WriteLine("Expected keyword ROW after GET.");
                return;
            }

            List<int> rowNumbers = new List<int>();

            while (true)
            {
                (pos, string word) = ReadWord(pos, query);

                if (word == "")
                {
                    Console.WriteLine("Expected row number or FROM.");
                    return;
                }

                string upper = word;

                if (upper == "FROM")
                {
                    break;
                }

                if (!int.TryParse(word, out int rowNum) || rowNum <= 0)
                {
                    Console.WriteLine($"Invalid row number: '{word}'");
                    return;
                }

                rowNumbers.Add(rowNum);
                while (pos < query.Length && char.IsWhiteSpace(query[pos]))
                    pos++;

                if (pos < query.Length && query[pos] == ',')
                {
                    pos++;
                }
            }

            if (rowNumbers.Count == 0)
            {
                Console.WriteLine("No row numbers specified.");
                return;
            }

            (pos, string tableName) = ReadWord(pos, query);
            if (tableName == "")
            {
                Console.WriteLine("Missing table name after FROM.");
                return;
            }

            int[] rowsArray = new int[rowNumbers.Count];
            for (int i = 0; i < rowNumbers.Count; i++)
                rowsArray[i] = rowNumbers[i];

            var table = new Table(tableName);
            table.GetRows(rowsArray);
        }
    }
}
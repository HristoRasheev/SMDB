using System;
using System.Collections.Generic;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        private void HandleDelete(int pos, string query)
        {
            (pos, string sw) = ReadWord(pos, query);
            if (sw != "FROM")
            {
                Console.WriteLine("Expected FROM after DELETE.");
                return;
            }

            // table name
            (pos, string tableName) = ReadWord(pos, query);
            if (tableName == "")
            {
                Console.WriteLine("Missing table name after FROM.");
                return;
            }

            int save = pos;
            (pos, string nextWord) = ReadWord(pos, query);
            string upper = nextWord;

            // -------------------------
            // DELETE FROM T ROW 1,2,3
            // -------------------------
            if (upper == "ROW")
            {
                List<int> rowNumbers = new List<int>();

                while (true)
                {
                    (pos, string word) = ReadWord(pos, query);
                    if (word == "")
                        break;

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
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                if (rowNumbers.Count == 0)
                {
                    Console.WriteLine("No row numbers specified for DELETE.");
                    return;
                }

                int[] rowsArray = new int[rowNumbers.Count];
                for (int i = 0; i < rowNumbers.Count; i++)
                    rowsArray[i] = rowNumbers[i];

                var table = new Table(tableName);
                table.DeleteRows(rowsArray);
                return;
            }

            // -------------------------
            // DELETE FROM T WHERE ...
            // -------------------------
            if (upper == "WHERE")
            {
                SimpleCond[] conds = new SimpleCond[64];
                string[] links = new string[64];
                int condCount = 0;

                while (true)
                {
                    bool notFlag = false;
                    int saveNot = pos;
                    (pos, string maybeNot) = ReadWord(pos, query);
                    if (maybeNot == "NOT")
                        notFlag = true;
                    else
                        pos = saveNot;

                    // Column
                    (pos, string colName) = ReadWord(pos, query);
                    if (colName == "")
                    {
                        Console.WriteLine("Missing column in WHERE.");
                        return;
                    }

                    // Operator
                    pos = SkipSpaces(pos, query);
                    (pos, string op) = ReadOperator(pos, query);
                    if (op == "")
                    {
                        Console.WriteLine("Missing operator in WHERE.");
                        return;
                    }

                    // Value (като текст)
                    (pos, string valStr) = ReadValueInRange(pos, query.Length, query);
                    if (valStr == "")
                    {
                        Console.WriteLine("Missing value in WHERE.");
                        return;
                    }

                    // записваме текущото условие
                    conds[condCount].Col = colName;
                    conds[condCount].Op = op;
                    conds[condCount].ValStr = valStr;
                    conds[condCount].Not = notFlag;
                    condCount++; 

                    // next: AND / OR / end
                    int saveNext = pos;
                    (pos, string next) = ReadWord(pos, query);
                    string j = next;

                    if (j == "AND" || j == "OR")
                    {
                        links[condCount - 1] = j;
                        continue;
                    }

                    pos = saveNext; // няма повече условия
                    break;
                }

                if (condCount == 0)
                {
                    Console.WriteLine("WHERE exists but no condition is given.");
                    return;
                }

                var table = new Table(tableName);
                int[] rows = table.DeleteRowsWhere(conds, links, condCount);

                if (rows.Length == 0)
                {
                    Console.WriteLine("No rows matched WHERE. Nothing deleted.");
                    return;
                }

                table.DeleteRows(rows);
                return;
            }

            // ако не е ROW и не е WHERE
            pos = save;
            Console.WriteLine("Supported: DELETE FROM <Table> ROW ...  OR  DELETE FROM <Table> WHERE ...");
        }
    }
}

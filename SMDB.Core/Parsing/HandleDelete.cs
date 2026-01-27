using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SMDB.Core.Models;

namespace SMDB.Core.Parsing
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

            if (upper == "WHERE")
            {
                List<Cond> conds = new List<Cond>();
                List<string> links = new List<string>();
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

                    // Op
                    pos = SkipSpaces(pos, query);
                    (pos, string op) = ReadOperator(pos, query);
                    if (op == "")
                    {
                        Console.WriteLine("Missing operator in WHERE.");
                        return;
                    }

                    // Value
                    (pos, string valStr) = ReadValueInRange(pos, query.Length, query);
                    if (valStr == "")
                    {
                        Console.WriteLine("Missing value in WHERE.");
                        return;
                    }

                    conds.Add(new Cond
                    {
                        Col = colName,
                        Op = op,
                        ValStr = valStr,
                        Not = notFlag,
                    });

                    //AND / OR / end
                    int saveNext = pos;
                    (pos, string next) = ReadWord(pos, query);
                    string j = next;

                    if (j == "AND" || j == "OR")
                    {
                        links[condCount - 1] = j;
                        continue;
                    }

                    pos = saveNext; 
                    break;
                }

                if (conds.Count == 0)
                {
                    Console.WriteLine("WHERE exists but no condition is given.");
                    return;
                }

                var table = new Table(tableName);
                int[] rows = table.DeleteRowsWhere(conds.ToArray(), links.ToArray(), conds.Count);

                if (rows.Length == 0)
                {
                    Console.WriteLine("No rows matched WHERE. Nothing deleted.");
                    return;
                }

                table.DeleteRows(rows);
                return;
            }

            pos = save;
            Console.WriteLine("Supported: DELETE FROM <Table> ROW ...  OR  DELETE FROM <Table> WHERE ...");
        }
    }
}

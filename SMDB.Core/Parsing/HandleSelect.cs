using System;
using System.Collections.Generic;
using SMDB.Core.Models;

namespace SMDB.Core.Parsing
{
    public partial class Parser
    {
        private void HandleSelect(int pos, string query)
        {
            bool distinct = false;
            bool hasOrder = false;
            string orderCol = "";
            bool orderAsc = true;
            List<string> columns = new List<string>();

            int save0 = pos;
            (pos, string maybeDistinct) = ReadWord(pos, query);
            if (maybeDistinct == "DISTINCT")
            {
                distinct = true;
            }
            else
            {
                pos = save0;
            }

            while (true)
            {
                (pos, string word) = ReadWord(pos, query);

                if (word == "*")
                {
                    columns.Add("*");
                    // очакваме FROM
                    (pos, string next) = ReadWord(pos, query);
                    if (next.ToUpperInvariant() != "FROM")
                    {
                        Console.WriteLine("Expected FROM after *");
                        return;
                    }
                    break;
                }
                if (word == "")
                {
                    Console.WriteLine("Missing column list.");
                    return;
                }

                if (word.ToUpperInvariant() == "FROM")
                    break;

                columns.Add(word);

                while (pos < query.Length && char.IsWhiteSpace(query[pos]))
                    pos++;

                if (pos < query.Length && query[pos] == ',')
                {
                    pos++;
                    continue;
                }
            }

            if (columns.Count == 0)
            {
                Console.WriteLine("SELECT requires at least one column.");
                return;
            }

            (pos, string tableName) = ReadWord(pos, query);
            if (tableName == "")
            {
                Console.WriteLine("Missing table name.");
                return;
            }

            SimpleCond[] conds = new SimpleCond[64];
            string[] links = new string[64];
            int condCount = 0;

            int save = pos;
            (pos, string w) = ReadWord(pos, query);

            if (w.ToUpperInvariant() == "WHERE")
            {
                while (true)
                {
                    bool notFlag = false;
                    int saveNot = pos;
                    (pos, string maybeNot) = ReadWord(pos, query);
                    if (maybeNot.ToUpperInvariant() == "NOT") notFlag = true;
                    else pos = saveNot;

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

                    // Value
                    (pos, string valStr) = ReadValueInRange(pos, query.Length, query);

                    conds[condCount].Col = colName;
                    conds[condCount].Op = op;
                    conds[condCount].ValStr = valStr;
                    conds[condCount].Not = notFlag;
                    condCount++;

                    // AND / OR / ORDER / end
                    int saveNext = pos;
                    (pos, string next) = ReadWord(pos, query);
                    string up = next.ToUpperInvariant();

                    if (up == "AND" || up == "OR")
                    {
                        links[condCount - 1] = up; 
                        continue;
                    }

                    if (up == "ORDER")
                    {
                        pos = saveNext; 
                        break;
                    }

                    pos = saveNext;
                    break;
                }
            }
            else
            {
                pos = save;
            }

            int save2 = pos;
            (pos, string maybeOrder) = ReadWord(pos, query);
            if (maybeOrder.ToUpperInvariant() == "ORDER")
            {
                (pos, string byWord) = ReadWord(pos, query);
                if (byWord.ToUpperInvariant() != "BY")
                {
                    Console.WriteLine("Expected BY after ORDER.");
                    return;
                }

                (pos, orderCol) = ReadWord(pos, query);
                if (orderCol == "")
                {
                    Console.WriteLine("Missing column after ORDER BY.");
                    return;
                }

                // ASC/DESC
                int save3 = pos;
                (pos, string dir) = ReadWord(pos, query);
                if (dir.ToUpperInvariant() == "DESC") orderAsc = false;
                else if (dir.ToUpperInvariant() == "ASC") orderAsc = true;
                else pos = save3; // няма посока -> default ASC

                hasOrder = true;
            }
            else
            {
                pos = save2;
            }

            string[] cols = new string[columns.Count];
            for (int i = 0; i < columns.Count; i++)
                cols[i] = columns[i];

            var table = new Table(tableName);
            table.Select(cols, conds, links, condCount, distinct, hasOrder, orderCol, orderAsc);

        }
    }
}

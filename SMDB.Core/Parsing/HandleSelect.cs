using System;
using System.Collections.Generic;
using System.Numerics;
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
            (pos, string dist) = ReadWord(pos, query);
            if (dist == "DISTINCT")
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
                    (pos, string next) = ReadWord(pos, query);
                    if (ToUpperInvariant(next) != "FROM")
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

                if (ToUpperInvariant(word) == "FROM")
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

            List<Cond> conds = new List<Cond>();
            List<string> links = new List<string>();
            int save = pos;

            (pos, string w) = ReadWord(pos, query);

            if (ToUpperInvariant(w) == "WHERE")
            {
                while (true)
                {
                    bool notFlag = false;
                    int saveNot = pos;
                    (pos, string maybeNot) = ReadWord(pos, query);
                    if (ToUpperInvariant(maybeNot) == "NOT") notFlag = true;
                    else pos = saveNot;

                    // Col
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

                    // Val
                    (pos, string valStr) = ReadValueInRange(pos, query.Length, query);

                    conds.Add(new Cond
                    {
                        Col = colName,
                        Op = op,
                        ValStr = valStr,
                        Not = notFlag
                    });

                    // AND / OR / ORDER / end
                    int saveNext = pos;
                    (pos, string next) = ReadWord(pos, query);
                    string up = ToUpperInvariant(next);

                    if (up == "AND" || up == "OR")
                    {
                        links.Add(up);
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
            if (ToUpperInvariant(maybeOrder) == "ORDER")
            {
                (pos, string byWord) = ReadWord(pos, query);
                if (ToUpperInvariant(byWord) != "BY")
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
                if (ToUpperInvariant(dir) == "DESC") orderAsc = false;
                else if (ToUpperInvariant(dir) == "ASC") orderAsc = true;
                else pos = save3; // default ASC

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
            table.Select(cols, conds.ToArray(), links.ToArray(), conds.Count, distinct, hasOrder, orderCol, orderAsc);
        }
    }
}

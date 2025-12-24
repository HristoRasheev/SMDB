using System;
using System.Collections.Generic;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        private void HandleCreation(int pos, string query)
        {
            (pos, string sw) = ReadWord(pos, query);
            string secondWord = sw;

            if (secondWord != "TABLE")
            {
                Console.WriteLine($"Unknown/unfinished command.");
                return;
            }

            (pos, string tableName) = ReadWord(pos, query);

            if (tableName == "")
            {
                Console.WriteLine("Table name is not present!");
                return;
            }

            int opening = FindForward(pos, query, '(');
            int closing = FindBackward(query.Length - 1, query, ')');

            if (opening == -1 || closing == -1 || closing <= opening + 1)
            {
                Console.WriteLine("Invalid syntax! Expected (columns...)");
                return;
            }

            int startCols = opening + 1;
            int endCols = closing;

            List<Column> columnList = new List<Column>();
            int p = startCols;

            while (p < endCols)
            {
                // Име на колоната
                (p, string colName) = ReadWordInRange(p, endCols, query);
                if (colName == "")
                    break;

                // Тип на колоната
                (p, string rawType) = ReadWordInRange(p, endCols, query);
                if (rawType == "")
                {
                    Console.WriteLine("Invalid column definition (missing type).");
                    return;
                }

                string colType = rawType;
                int size = 0;

                int t = p;
                while (t < endCols && char.IsWhiteSpace(query[t])) t++;

                if (colType == "STRING")
                {
                    if (t >= endCols || query[t] != '(')
                    {
                        Console.WriteLine($"Column '{colName}' STRING needs Size (STRING(M)).");
                        return;
                    }

                    t++; // след '('
                    while (t < endCols && char.IsWhiteSpace(query[t])) t++;

                    int startNum = t;
                    while (t < endCols && char.IsDigit(query[t])) t++;

                    if (startNum == t)
                    {
                        Console.WriteLine($"Column '{colName}' STRING needs number in STRING(M).");
                        return;
                    }

                    int m = 0;
                    for (int i = startNum; i < t; i++)
                        m = m * 10 + (query[i] - '0');

                    while (t < endCols && char.IsWhiteSpace(query[t])) t++;

                    if (t >= endCols || query[t] != ')')
                    {
                        Console.WriteLine($"Column '{colName}' STRING(M) missing ')'.");
                        return;
                    }

                    t++; // след ')'
                    size = m;
                    p = t; // местим основната позиция след ')'
                }

                string? defaultValue = null;

                int pBefore = p;
                (p, string maybeDefault) = ReadWordInRange(p, endCols, query);

                if (maybeDefault != "")
                {
                    if (maybeDefault != "DEFAULT")
                    {
                        Console.WriteLine($"Unexpected token '{maybeDefault}' after column type. Expected DEFAULT or ','.");
                        return;
                    }

                    (p, string defVal) = ReadValueInRange(p, endCols, query);
                    if (defVal == "")
                    {
                        Console.WriteLine("DEFAULT specified but no value.");
                        return;
                    }

                    defaultValue = defVal;
                }
                else
                {
                    p = pBefore;
                }

                Column col = new Column();
                col.Name = colName;
                col.Type = colType;
                col.Size = size;
                col.DefaultValue = defaultValue ?? "NULL";

                columnList.Add(col);

                // Прескачаме интервали
                while (p < endCols && char.IsWhiteSpace(query[p]))
                    p++;

                if (p < endCols && query[p] == ',')
                {
                    p++;
                    while (p < endCols && char.IsWhiteSpace(query[p]))
                        p++;
                }
            }

            if (columnList.Count == 0)
            {
                Console.WriteLine("No columns specified.");
                return;
            }

            Column[] columns = new Column[columnList.Count];
            for (int i = 0; i < columnList.Count; i++)
                columns[i] = columnList[i];

            var table = new Table(tableName);
            table.Create(columns);
        }
    }
}

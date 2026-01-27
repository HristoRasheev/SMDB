using System;
using System.Collections.Generic;
using SMDB.Core.Models;

namespace SMDB.Core.Parsing
{
    public partial class Parser
    {
        private void HandleInsert(int pos, string query)
        {
            (pos, string sw) = ReadWord(pos, query);
            string secondWord = ToUpperInvariant(sw);
            if (secondWord != "INTO")
            {
                Console.WriteLine("Invalid command!");
                return;
            }

            (pos, string tableName) = ReadWord(pos, query);
            if (tableName == "")
            {
                Console.WriteLine("Missing table name!");
                return;
            }

            int openCols = FindForward(pos, query, '(');
            if (openCols == -1)
            {
                Console.WriteLine("Missing opening parenthesis");
                return;
            }
            int closeCols = FindForward(openCols + 1, query, ')');
            if (closeCols == -1)
            {
                Console.WriteLine("Missing closing parenthesis");
                return;
            }

            List<string> columnNames = new List<string>();
            int p = openCols + 1;

            while (p < closeCols)
            {
                (p, string colName) = ReadWordInRange(p, closeCols, query);
                if (colName == "")
                    break;

                columnNames.Add(colName);

                while (p < closeCols && char.IsWhiteSpace(query[p]))
                    p++;

                if (p < closeCols && query[p] == ',')
                {
                    p++;
                    while (p < closeCols && char.IsWhiteSpace(query[p]))
                        p++;
                }
                else
                {
                    break; 
                }
            }

            pos = closeCols + 1;
            (pos, string tw) = ReadWord(pos, query);
            string thirdWord = ToUpperInvariant(tw);
            if (thirdWord != "VALUES")
            {
                Console.WriteLine("Expected VALUES keyword.");
                return;
            }

            int openVals = FindForward(pos, query, '(');
            if (openVals == -1)
            {
                Console.WriteLine("Missing '('!");
                return;
            }
            int closeVals = FindBackward(query.Length - 1, query, ')');
            if (closeVals == -1 || closeVals <= openVals)
            {
                Console.WriteLine("Missing ')'!");
                return;
            }

            List<string> values = new List<string>();
            int q = openVals + 1;

            while (q < closeVals)
            {
                (q, string val) = ReadValueInRange(q, closeVals, query);
                if (val == "")
                    break;

                values.Add(val);

                while (q < closeVals && char.IsWhiteSpace(query[q]))
                    q++;

                if (q < closeVals && query[q] == ',')
                {
                    q++;
                    while (q < closeVals && char.IsWhiteSpace(query[q]))
                        q++;
                }
                else
                {
                    break;
                }
            }

            if (values.Count == 0)
            {
                Console.WriteLine("No values specified.");
                return;
            }

            if (values.Count != columnNames.Count)
            {
                Console.WriteLine("Number of values does not match number of columns.");
                return;
            }

            string[] colsArray = new string[columnNames.Count];
            for (int i = 0; i < columnNames.Count; i++)
                colsArray[i] = columnNames[i];

            string[] valsArray = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                valsArray[i] = values[i];

            var table = new Table(tableName);
            table.Insert(colsArray, valsArray);
        }
    }
}
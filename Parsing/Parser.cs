using System.Collections.Generic;
using System.Data;
using System.Reflection.Metadata;
using SMDB.Models;

namespace SMDB.Parsing
{
    public partial class Parser
    {
        public void ExecuteCommand(string query)
        {
            int pos = 0;
            (pos, string firstWord) = ReadWord(pos, query);

            int pos2 = pos;
            (pos2, string secondWord) = ReadWord(pos2, query);
            if (firstWord == "CREATE" && secondWord == "INDEX")
            {
                HandleCreateIndex(pos2, query);
                return;
            }

            if (firstWord == "DROP" && secondWord == "INDEX")
            {
                HandleDropIndex(pos2, query);
                return;
            }

            switch (firstWord)
            {
                case "CREATE":
                    HandleCreation(pos, query); break;

                case "DROP":
                    HandleDrop(pos, query); break;

                case "TABLEINFO":
                    HandleTableInfo(pos, query); break;

                case "INSERT":
                    HandleInsert(pos, query); break;

                case "GET":
                    HandleGetRow(pos, query); break;

                case "DELETE":
                    HandleDelete(pos, query); break;

                case "SELECT":
                    HandleSelect(pos, query); break;

                case "CHECK":
                    HandleCheck(pos, query); break;

                default:
                    Console.WriteLine($"Unknown command: '{firstWord}'");
                    break;
            }
        }

        private int SkipSpaces(int pos, string text)
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
            return pos;
        }

        private (int pos, string op) ReadOperator(int pos, string text)
        {
            pos = SkipSpaces(pos, text);
            if (pos >= text.Length) return (pos, "");

            char c = text[pos];

            // двусимволни
            if (pos + 1 < text.Length)
            {
                char n = text[pos + 1];
                if (c == '<' && n == '>') return (pos + 2, "<>");
                if (c == '<' && n == '=') return (pos + 2, "<=");
                if (c == '>' && n == '=') return (pos + 2, ">=");
            }

            // едносимволни
            if (c == '=' || c == '<' || c == '>') return (pos + 1, c.ToString());

            return (pos, "");
        }

        //Тези са вместо split или trim
        private (int pos, string word) ReadWord(int pos, string query)
        {
            return ReadWordInRange(pos, query.Length, query);
        }

        //Тези са вместо IndexOf
        private int FindForward(int pos, string text, char target)
        {
            while (pos < text.Length)
            {
                if (text[pos] == target)
                    return pos;
                pos++;
            }
            return -1;
        }

        private int FindBackward(int pos, string text, char target)
        {
            while (pos >= 0)
            {
                if (text[pos] == target)
                    return pos;
                pos--;
            }
            return -1;
        }

        //Toва е вместо Substring
        private (int pos, string word) ReadWordInRange(int pos, int end, string text)
        {
            // Прескачаме празни места
            while (pos < end && char.IsWhiteSpace(text[pos]))
                pos++;

            if (pos >= end)
                return (pos, ""); // няма дума

            string word = "";

            // Събираме символите до интервал, запетая или края
            while (pos < end &&
                    !char.IsWhiteSpace(text[pos]) &&
                    text[pos] != ',' &&
                    text[pos] != '(' &&
                    text[pos] != ')' &&
                    text[pos] != '=' &&
                    text[pos] != '<' &&
                    text[pos] != '>')
            {
                word += text[pos];
                pos++;
            }

            // НЕ пипаме запетаята тук – оставяме я да я види външният код
            return (pos, word);
        }

        private (int pos, string value) ReadValueInRange(int pos, int end, string text)
        {
            // Прескачаме празни места
            while (pos < end && char.IsWhiteSpace(text[pos]))
                pos++;

            if (pos >= end)
                return (pos, ""); // няма стойност

            char c = text[pos];

            if (c == '\'' || c == '\"')
            {
                char quote = c;
                pos++; // прескачаме отварящата кавичка
                string val = "";

                while (pos < end && text[pos] != quote)
                {
                    val += text[pos];
                    pos++;
                }

                if (pos < end && text[pos] == quote)
                    pos++;

                return (pos, val);
            }
            else
            {
                string val = "";
                while (pos < end && !char.IsWhiteSpace(text[pos]) && text[pos] != ',')
                {
                    val += text[pos];
                    pos++;
                }
                return (pos, val);
            }
        }

    }
}
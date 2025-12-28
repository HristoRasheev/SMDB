using System;
using System.IO;
using SMDB.Core.Models;
using SMDB.Core.Parsing;

namespace SMDB.Core;

public class DatabaseEngine
{
    public string Execute(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Empty query.";

        try
        {
            Parser parser = new Parser();

            using StringWriter sw = new StringWriter();
            TextWriter oldOut = Console.Out;
            Console.SetOut(sw);

            parser.ExecuteCommand(query);

            Console.SetOut(oldOut);
            return sw.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }
}
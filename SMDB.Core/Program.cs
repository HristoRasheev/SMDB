using SMDB.Core.Models;
using SMDB.Core.Parsing;

namespace SMDB
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("             =========SMDB=========");
            Console.WriteLine();
            var parser = new Parser();

            while (true)
            {
                Console.Write("Enter query:  ");
                string query = Console.ReadLine()!;

                if (query.Equals("exit") || query.Equals("EXIT"))
                {
                    Console.WriteLine("Exiting..");
                    break;
                }

                parser.ExecuteCommand(query);
                Console.WriteLine();
            }
        }
    }
}

using diagexp.lib;
using Microsoft.Diagnostics.Runtime;
using System;
using System.IO;
using System.Linq;

#nullable enable

namespace diagexp
{

    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("ERROR: missing dump file");
                return;
            }

            using var dataTarget = DataTarget.LoadDump(args[0]);
            using var runtime = dataTarget.ClrVersions.Single().CreateRuntime();

            var ts = new TypeCache(runtime);

            while (true)
            {
                Console.Write("Type to search: ");
                var query = Console.ReadLine();
                foreach (var t in ts.FindTypes(query))
                {
                    Console.WriteLine($"{t.ModuleName}!{t.Name} (MT: 0x{t.MethodTable:X})");
                }
            }
        }
    }
}

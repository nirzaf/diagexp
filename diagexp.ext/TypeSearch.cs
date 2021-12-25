using System;
using System.Runtime.InteropServices;
using diagexp.lib;
using RGiesecke.DllExport;

namespace diagexp.ext
{
    public partial class DebuggerExtensions
    {
        private static TypeCache ts;

        [DllExport("typesearch")]
        public static void TypeSearch(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            if (!InitApi(client))
                return;

            if (ts == null)
            {
                ts = new TypeCache(Runtime);
            }

            foreach (var t in ts.FindTypes(args))
            {
                Console.WriteLine($"{t.ModuleName}!{t.Name} (MT: 0x{t.MethodTable:X})");
            }
        }
    }
}

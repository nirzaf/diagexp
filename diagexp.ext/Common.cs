using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using RGiesecke.DllExport;

namespace diagexp.ext
{
    public partial class DebuggerExtensions
    {
        public static IDebugClient DebugClient { get; private set; }
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        private readonly static string codebase;

        static DebuggerExtensions()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            var p = Assembly.GetExecutingAssembly().CodeBase;
            if (p.StartsWith("file://"))
            {
                p = p.Substring(8).Replace('/', '\\');
            }
            codebase = Path.GetDirectoryName(p);
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var simpleName = args.Name;
            var commaIdx = simpleName.IndexOf(',');
            if (0 <= commaIdx)
            {
                simpleName = simpleName.Substring(0, commaIdx);
            }

            string fileName = Path.Combine(codebase, simpleName + ".dll");
            if (File.Exists(fileName))
            {
                return Assembly.LoadFrom(fileName);
            }

            return null;
        }

        private static bool InitApi(IntPtr ptrClient)
        {
            // On our first call to the API:
            //   1. Store a copy of IDebugClient in DebugClient.
            //   2. Replace Console's output stream to be the debugger window.
            //   3. Create an instance of DataTarget using the IDebugClient.
            if (DebugClient is null)
            {
                DebugClient = (IDebugClient)Marshal.GetUniqueObjectForIUnknown(ptrClient);
                Console.SetOut(new StreamWriter(new DbgEngStream(DebugClient)) { AutoFlush = true });
                DataTarget = DataTarget.CreateFromDbgEng(ptrClient);
            }

            if (Runtime is null)
            {
                // works only when one runtime version is found in the process
                Runtime = DataTarget.ClrVersions.Single().CreateRuntime();

                if (Runtime is null)
                {
                    Console.WriteLine("CLR runtime initialization failed");
                }
            }
            else
            {
                // If we already had a runtime, flush it for this use.  This is ONLY required
                // for a live process or iDNA trace.  If you use the IDebug* apis to detect
                // that we are debugging a crash dump you may skip this call for better perf.
                Runtime.FlushCachedData();
            }

            return Runtime != null;
        }

        [DllExport("DebugExtensionInitialize")]
        public static int DebugExtensionInitialize(ref uint version, ref uint flags)
        {
            // Set the extension version to 1, which expects exports with this signature:
            //      void _stdcall function(IDebugClient *client, const char *args)
            version = DEBUG_EXTENSION_VERSION(1, 0);
            flags = 0;
            return 0;
        }

        static uint DEBUG_EXTENSION_VERSION(uint Major, uint Minor)
        {
            return (((Major) & 0xffff) << 16) | ((Minor) & 0xffff);
        }
    }

    class DbgEngStream : Stream
    {
        public void Clear()
        {
            while (Marshal.ReleaseComObject(m_client) > 0) { }
            while (Marshal.ReleaseComObject(m_control) > 0) { }
        }

        IDebugClient m_client;
        private IDebugControl m_control;
        public DbgEngStream(IDebugClient client)
        {
            m_client = client;
            m_control = (IDebugControl)client;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
        }

        public override long Length => -1;

        public override long Position
        {
            get => 0;

            set
            {
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            UTF8Encoding enc = new UTF8Encoding();
            string str = enc.GetString(buffer, offset, count);
            m_control.ControlledOutput(DEBUG_OUTCTL.ALL_CLIENTS, DEBUG_OUTPUT.NORMAL, str);
        }
    }
}

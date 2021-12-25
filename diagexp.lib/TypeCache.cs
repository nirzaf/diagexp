using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace diagexp.lib
{
    public class ObjectType
    {
        private readonly ulong methodTable;
        private readonly string name;
        private readonly string moduleName;

        public ObjectType(ulong mt, string name, string moduleName)
        {
            this.methodTable = mt;
            this.name = name;
            this.moduleName = moduleName;
        }

        public ulong MethodTable => methodTable;

        public string Name => name;

        public string ModuleName => moduleName;
    }

    public sealed class TypeCache
    {
        private readonly ObjectType[] types;

        public TypeCache(ClrRuntime runtime)
        {
            types = runtime.EnumerateModules().SelectMany(m => m.EnumerateTypeDefToMethodTableMap())
                           .Select(m => runtime.GetTypeByMethodTable(m.MethodTable))
                           .Where(t => t != null)
                           .Select(t => new ObjectType(t.MethodTable, t.Name, Path.GetFileName(t.Module.Name)))
                           .ToArray();
        }

        public IEnumerable<ObjectType> FindTypes(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                yield break;
            }

            var queryFunc = GetQueryFunc(query);

            foreach (var t in types)
            {
                if (queryFunc(t.Name))
                {
                    yield return t;
                }
            }
        }

        public Func<string, bool> GetQueryFunc (string query)
        {
            if (query[0] == '^' && query[query.Length - 1] == '$') {
                return (string s) => s.Equals(query.Substring(1, query.Length - 2), StringComparison.OrdinalIgnoreCase);
            }
            if (query[0] == '^')
            {
                return (string s) => s.StartsWith(query.Substring(1), StringComparison.OrdinalIgnoreCase);
            }
            if (query[query.Length - 1] == '$')
            {
                return (string s) => s.EndsWith(query.Substring(0, query.Length - 1), StringComparison.OrdinalIgnoreCase);
            }
            return (string s) => s.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

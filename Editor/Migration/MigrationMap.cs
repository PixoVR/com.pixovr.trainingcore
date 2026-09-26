using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PixoVR.TrainingCore.Editor.Migration
{
    /// <summary>One Luminous→Pixo type mapping entry.</summary>
    public class TypeMapping
    {
        /// <summary>Luminous namespace (empty for global).</summary>
        public string LuminousNs;

        /// <summary>Luminous class name.</summary>
        public string LuminousClass;

        /// <summary>Luminous DLL-type fileID (computed for DLL types; -1 for loose scripts).</summary>
        public int FileID = -1;

        /// <summary>Loose-script meta guid (middlemen); null for DLL types.</summary>
        public string ScriptGuid;

        /// <summary>Pixo namespace (null when unmapped).</summary>
        public string PixoNs;

        /// <summary>Pixo class name.</summary>
        public string PixoClass;

        /// <summary>Pixo assembly name (null → map default).</summary>
        public string PixoAsm;

        /// <summary>Serialized field renames old→new.</summary>
        public Dictionary<string, string> Fields = new Dictionary<string, string>();

        /// <summary>Keep the reference as-is (third-party asset relocated into the project).</summary>
        public bool Keep;

        /// <summary>True when a Pixo target exists.</summary>
        public bool IsMapped => PixoClass != null;

        /// <summary>Display key.</summary>
        public string Key => string.IsNullOrEmpty(LuminousNs) ? LuminousClass : LuminousNs + "." + LuminousClass;

        /// <summary>Pixo qualified name.</summary>
        public string PixoQualifiedName =>
            PixoClass == null ? null : (string.IsNullOrEmpty(PixoNs) ? PixoClass : PixoNs + "." + PixoClass);
    }

    /// <summary>The loaded luminous-map.json table.</summary>
    public class MigrationMap
    {
        /// <summary>Luminous DLL guids.</summary>
        public List<string> DllGuids = new List<string>();

        /// <summary>Luminous runtime assembly name inside managed refs.</summary>
        public string LuminousAsm = "CoreSystemRuntime";

        /// <summary>Pixo assembly name.</summary>
        public string PixoAsm = "PixoVR.TrainingCore";

        /// <summary>All type mappings.</summary>
        public List<TypeMapping> Types = new List<TypeMapping>();

        /// <summary>fileID+guid → mapping for DLL-type script refs.</summary>
        public Dictionary<string, TypeMapping> ScriptRefs = new Dictionary<string, TypeMapping>();

        /// <summary>guid → mapping for loose middlemen scripts.</summary>
        public Dictionary<string, TypeMapping> LooseScripts = new Dictionary<string, TypeMapping>();

        /// <summary>"ns.Class" managed-ref key → mapping.</summary>
        public Dictionary<string, TypeMapping> ManagedRefTypes = new Dictionary<string, TypeMapping>();

        /// <summary>"ns.Class" (or "Class" when ns empty) → mapping, for all types and scripts.</summary>
        public Dictionary<string, TypeMapping> QualifiedNames = new Dictionary<string, TypeMapping>();

        /// <summary>Luminous asset guid → Pixo asset guid.</summary>
        public Dictionary<string, string> AssetGuids = new Dictionary<string, string>();

        /// <summary>Load and index the map.</summary>
        public static MigrationMap Load(string json)
        {
            var map = new MigrationMap();
            map.Merge(json);
            return map;
        }

        /// <summary>Parse the same schema and add/override entries (later wins; re-index).</summary>
        public void Merge(string json)
        {
            var root = JObject.Parse(json);
            if (root["dlls"] != null)
                DllGuids = root["dlls"].ToObject<List<string>>();
            if (root["luminousAsm"] != null)
                LuminousAsm = root["luminousAsm"].ToString();
            if (root["pixoAsm"] != null)
                PixoAsm = root["pixoAsm"].ToString();

            foreach (var t in (root["types"] as JArray) ?? new JArray())
                AddType(ParseMapping(t, null), true);
            foreach (var s in (root["scripts"] as JArray) ?? new JArray())
                AddType(ParseMapping(s, s["guid"]?.ToString()), false);
            foreach (var a in (root["assets"] as JArray) ?? new JArray())
            {
                var from = a["luminous"]?.ToString();
                var to = a["pixo"]?.ToString();
                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                    AssetGuids[from] = to;
            }
            Reindex();
        }

        private static TypeMapping ParseMapping(JToken t, string scriptGuid)
        {
            var lum = t["luminous"];
            var m = new TypeMapping
            {
                ScriptGuid = scriptGuid,
                LuminousNs = lum?["ns"]?.ToString() ?? "",
                LuminousClass = lum?["class"]?.ToString() ?? "",
            };
            var pixo = t["pixo"];
            if (pixo != null && pixo.Type != JTokenType.Null)
            {
                m.PixoNs = pixo["ns"]?.ToString();
                m.PixoClass = pixo["class"]?.ToString();
                m.PixoAsm = pixo["asm"]?.ToString();
            }
            var fields = t["fields"] as JObject;
            if (fields != null)
                foreach (var p in fields.Properties())
                    m.Fields[p.Name] = p.Value.ToString();
            m.Keep = t["keep"]?.ToObject<bool>() ?? false;
            m.FileID = FileIDUtil.ComputeFileID(m.LuminousNs, m.LuminousClass);
            return m;
        }

        private void AddType(TypeMapping m, bool isTypeEntry)
        {
            Types.RemoveAll(x => x.LuminousClass == m.LuminousClass && x.LuminousNs == m.LuminousNs
                                 && x.ScriptGuid == m.ScriptGuid);
            Types.Add(m);
        }

        private void Reindex()
        {
            ScriptRefs.Clear();
            LooseScripts.Clear();
            ManagedRefTypes.Clear();
            QualifiedNames.Clear();
            foreach (var m in Types)
            {
                QualifiedNames[m.Key] = m;
                if (m.ScriptGuid != null)
                    LooseScripts[m.ScriptGuid] = m;
                else
                    foreach (var g in DllGuids)
                        ScriptRefs[$"{m.FileID}:{g}"] = m;
                ManagedRefTypes[m.Key] = m;
            }
        }

        /// <summary>Load from a file path.</summary>
        public static MigrationMap LoadFile(string path) => Load(File.ReadAllText(path));

        /// <summary>Mappings with no Pixo target.</summary>
        public IEnumerable<TypeMapping> Unmapped => Types.Where(t => !t.IsMapped);
    }
}

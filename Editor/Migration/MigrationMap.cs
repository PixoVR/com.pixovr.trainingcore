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

        /// <summary>Load and index the map.</summary>
        public static MigrationMap Load(string json)
        {
            var root = JObject.Parse(json);
            var map = new MigrationMap
            {
                DllGuids = root["dlls"]?.ToObject<List<string>>() ?? new List<string>(),
                LuminousAsm = root["luminousAsm"]?.ToString() ?? "CoreSystemRuntime",
                PixoAsm = root["pixoAsm"]?.ToString() ?? "PixoVR.TrainingCore",
            };

            foreach (var t in (root["types"] as JArray) ?? new JArray())
            {
                var lum = t["luminous"];
                var m = new TypeMapping
                {
                    LuminousNs = lum?["ns"]?.ToString() ?? "",
                    LuminousClass = lum?["class"]?.ToString() ?? "",
                };
                var pixo = t["pixo"];
                if (pixo != null && pixo.Type != JTokenType.Null)
                {
                    m.PixoNs = pixo["ns"]?.ToString();
                    m.PixoClass = pixo["class"]?.ToString();
                }
                var fields = t["fields"] as JObject;
                if (fields != null)
                    foreach (var p in fields.Properties())
                        m.Fields[p.Name] = p.Value.ToString();
                m.Keep = t["keep"]?.ToObject<bool>() ?? false;
                m.FileID = FileIDUtil.ComputeFileID(m.LuminousNs, m.LuminousClass);
                map.Types.Add(m);
                map.ManagedRefTypes[m.Key] = m;
            }

            foreach (var s in (root["scripts"] as JArray) ?? new JArray())
            {
                var lum = s["luminous"];
                var m = new TypeMapping
                {
                    ScriptGuid = s["guid"]?.ToString(),
                    LuminousNs = lum?["ns"]?.ToString() ?? "",
                    LuminousClass = lum?["class"]?.ToString() ?? "",
                };
                var pixo = s["pixo"];
                if (pixo != null && pixo.Type != JTokenType.Null)
                {
                    m.PixoNs = pixo["ns"]?.ToString();
                    m.PixoClass = pixo["class"]?.ToString();
                }
                var fields = s["fields"] as JObject;
                if (fields != null)
                    foreach (var p in fields.Properties())
                        m.Fields[p.Name] = p.Value.ToString();
                m.Keep = s["keep"]?.ToObject<bool>() ?? false;
                map.Types.Add(m);
                if (m.ScriptGuid != null)
                    map.LooseScripts[m.ScriptGuid] = m;
            }

            foreach (var m in map.Types.Where(t => t.ScriptGuid == null))
                foreach (var g in map.DllGuids)
                    map.ScriptRefs[$"{m.FileID}:{g}"] = m;

            return map;
        }

        /// <summary>Load from a file path.</summary>
        public static MigrationMap LoadFile(string path) => Load(File.ReadAllText(path));

        /// <summary>Mappings with no Pixo target.</summary>
        public IEnumerable<TypeMapping> Unmapped => Types.Where(t => !t.IsMapped);
    }
}

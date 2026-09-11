using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PixoVR.TrainingCore.Editor.Migration
{
    /// <summary>One row of the dry-run/migration report.</summary>
    public class MigrationReportEntry
    {
        /// <summary>Asset file.</summary>
        public string File;
        /// <summary>Line number.</summary>
        public int Line;
        /// <summary>What was found.</summary>
        public string From;
        /// <summary>What it became.</summary>
        public string To;
        /// <summary>mapped / unmapped / unmapped-dll-guid.</summary>
        public string Status;

        /// <summary>CSV row.</summary>
        public string ToCsv() =>
            $"\"{File}\",{Line},\"{From?.Replace("\"", "\"\"")}\",\"{To?.Replace("\"", "\"\"")}\",{Status}";
    }

    /// <summary>Resolved pixo script guid for a mapped class.</summary>
    /// <param name="pixoNs">Pixo namespace.</param>
    /// <param name="pixoClass">Pixo class name.</param>
    public delegate string PixoGuidResolver(string pixoNs, string pixoClass);

    /// <summary>
    /// Byte-preserving line rewriter for Unity YAML files. Rewrites:
    /// m_Script refs to Luminous DLLs or middlemen scripts, managed-reference type: blocks,
    /// and per-component field renames. Everything else passes through untouched.
    /// </summary>
    public static class YamlRewriter
    {
        private static readonly Regex ScriptRef =
            new Regex(@"m_Script:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32}),\s*type:\s*3\}",
                RegexOptions.Compiled);

        private static readonly Regex ManagedType =
            new Regex(@"type:\s*\{class:\s*([^,}]+),\s*ns:\s*([^,}]*),\s*asm:\s*([^,}]*)\}",
                RegexOptions.Compiled);

        private static readonly Regex DocHeader =
            new Regex(@"^--- !u!\d+ &\d+", RegexOptions.Compiled);

        private static readonly Regex FieldLine =
            new Regex(@"^(\s+)(\w+):", RegexOptions.Compiled);

        /// <summary>
        /// Rewrite a YAML document. <paramref name="resolve"/> returns the pixo script .meta guid
        /// for a mapped class (or null when the target script can't be found).
        /// </summary>
        public static string Rewrite(string yaml, MigrationMap map, PixoGuidResolver resolve,
            List<MigrationReportEntry> report, string fileName)
        {
            if (yaml == null || map == null)
                return yaml;

            var lines = yaml.Split('\n');
            var output = new StringBuilder(yaml.Length);
            TypeMapping currentComponent = null;
            int componentIndent = 0;
            bool changed = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var rewritten = line;

                // track document/component boundaries for scoped field renames
                if (DocHeader.IsMatch(line))
                {
                    currentComponent = null;
                }
                else if (line.TrimEnd('\r').EndsWith(":"))
                {
                    // nested object block start below component fields — component stays current
                }

                var scriptMatch = ScriptRef.Match(line);
                if (scriptMatch.Success)
                {
                    var fileId = int.Parse(scriptMatch.Groups[1].Value);
                    var guid = scriptMatch.Groups[2].Value;
                    TypeMapping mapping = null;
                    if (map.ScriptRefs.TryGetValue($"{fileId}:{guid}", out var byDll))
                        mapping = byDll;
                    else if (map.LooseScripts.TryGetValue(guid, out var byGuid))
                        mapping = byGuid;
                    else if (map.DllGuids.Contains(guid))
                        report?.Add(new MigrationReportEntry
                        {
                            File = fileName, Line = i + 1, From = $"fileID {fileId} guid {guid}",
                            To = "", Status = "unmapped-dll-guid"
                        });

                    if (mapping != null)
                    {
                        if (mapping.Keep)
                        {
                            // Third-party asset (HighlightPlus, Ultimate Replay) relocated into the
                            // project — the guid still resolves, so leave the line untouched.
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = mapping.Key,
                                To = mapping.Key, Status = "kept (relocated)"
                            });
                        }
                        else if (mapping.IsMapped)
                        {
                            var pixoGuid = resolve?.Invoke(mapping.PixoNs, mapping.PixoClass);
                            if (pixoGuid != null)
                            {
                                rewritten = line.Replace(scriptMatch.Value,
                                    $"m_Script: {{fileID: 11500000, guid: {pixoGuid}, type: 3}}");
                                changed = true;
                                currentComponent = mapping;
                                report?.Add(new MigrationReportEntry
                                {
                                    File = fileName, Line = i + 1, From = mapping.Key,
                                    To = mapping.PixoQualifiedName, Status = "mapped"
                                });
                            }
                            else
                            {
                                report?.Add(new MigrationReportEntry
                                {
                                    File = fileName, Line = i + 1, From = mapping.Key,
                                    To = mapping.PixoQualifiedName, Status = "unresolved-pixo-script"
                                });
                            }
                        }
                        else
                        {
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = mapping.Key,
                                To = "", Status = "unmapped"
                            });
                        }
                    }
                }

                var typeMatch = ManagedType.Match(line);
                if (typeMatch.Success)
                {
                    var cls = typeMatch.Groups[1].Value.Trim();
                    var ns = typeMatch.Groups[2].Value.Trim();
                    var asm = typeMatch.Groups[3].Value.Trim();
                    var key = string.IsNullOrEmpty(ns) ? cls : ns + "." + cls;
                    if (map.ManagedRefTypes.TryGetValue(key, out var mapping))
                    {
                        if (mapping.IsMapped)
                        {
                            rewritten = line.Replace(typeMatch.Value,
                                $"type: {{class: {mapping.PixoClass}, ns: {mapping.PixoNs}, asm: {map.PixoAsm}}}");
                            changed = true;
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = key,
                                To = mapping.PixoQualifiedName, Status = "mapped"
                            });
                        }
                        else
                        {
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = key,
                                To = "", Status = "unmapped"
                            });
                        }
                    }
                }

                // scoped field renames inside the current component block
                if (currentComponent != null && currentComponent.Fields.Count > 0)
                {
                    var fm = FieldLine.Match(line);
                    if (fm.Success && currentComponent.Fields.TryGetValue(fm.Groups[2].Value, out var newName))
                    {
                        rewritten = line.Substring(0, fm.Groups[2].Index) + newName +
                                    line.Substring(fm.Groups[2].Index + fm.Groups[2].Length);
                        changed = true;
                        report?.Add(new MigrationReportEntry
                        {
                            File = fileName, Line = i + 1, From = fm.Groups[2].Value,
                            To = newName, Status = "field-renamed"
                        });
                    }
                }

                output.Append(rewritten);
                if (i < lines.Length - 1)
                    output.Append('\n');
            }

            return changed ? output.ToString() : yaml;
        }
    }
}

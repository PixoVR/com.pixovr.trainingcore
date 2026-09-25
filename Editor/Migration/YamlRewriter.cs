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
        /// <summary>mapped / unmapped / unmapped-dll-guid / extended-rule statuses.</summary>
        public string Status;

        /// <summary>CSV row.</summary>
        public string ToCsv() =>
            $"\"{File}\",{Line},\"{From?.Replace("\"", "\"\"")}\",\"{To?.Replace("\"", "\"\"")}\",{Status}";
    }

    /// <summary>Resolved pixo script guid for a mapped class.</summary>
    /// <param name="pixoNs">Pixo namespace.</param>
    /// <param name="pixoClass">Pixo class name.</param>
    /// <param name="wantsComponent">True when the reference lives on a component (MonoBehaviour doc).</param>
    public delegate string PixoGuidResolver(string pixoNs, string pixoClass, bool wantsComponent);

    /// <summary>
    /// Byte-preserving line rewriter for Unity YAML files. Rewrites:
    /// m_Script refs to Luminous DLLs or middlemen scripts, managed-reference type: blocks,
    /// per-component field renames, qualified type-name strings, UnityEvent target type names,
    /// and known asset guid remaps. Everything else passes through untouched.
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
            new Regex(@"^--- !u!(\d+) &\d+", RegexOptions.Compiled);

        private static readonly Regex FieldLine =
            new Regex(@"^(\s+)(\w+):", RegexOptions.Compiled);

        private static readonly Regex GameObjectRef =
            new Regex(@"m_GameObject:\s*\{fileID:\s*(-?\d+)", RegexOptions.Compiled);

        private static readonly Regex QualifiedTypeName =
            new Regex(@"(\s*assemblyQualifiedName:\s*)([\w.]+),\s*([^,]+)(,.*)?$", RegexOptions.Compiled);

        private static readonly Regex EventTargetName =
            new Regex(@"(\s*m_TargetAssemblyTypeName:\s*)([\w.]+),\s*([^,]+)(.*)$", RegexOptions.Compiled);

        private static readonly Regex RemovedComponents =
            new Regex(@"m_RemovedComponents:\s*$", RegexOptions.Compiled);

        // PrefabInstance override form: 'propertyPath: ...m_TargetAssemblyTypeName' followed by 'value: <type>, <asm>'
        private static readonly Regex EventTargetPropertyPath =
            new Regex(@"propertyPath:.*m_TargetAssemblyTypeName", RegexOptions.Compiled);

        private static readonly Regex ValueTypeName =
            new Regex(@"(\s*value:\s*)([\w.]+),\s*([^,\n]+)(.*)$", RegexOptions.Compiled);

        /// <summary>
        /// Rewrite a YAML document. <paramref name="resolve"/> returns the pixo script .meta guid
        /// for a mapped class (or null when the target script can't be found or is the wrong kind).
        /// </summary>
        public static string Rewrite(string yaml, MigrationMap map, PixoGuidResolver resolve,
            List<MigrationReportEntry> report, string fileName, bool extendedRules = true)
        {
            if (yaml == null || map == null)
                return yaml;

            var lines = yaml.Split('\n');
            var output = new StringBuilder(yaml.Length);
            TypeMapping currentComponent = null;
            long currentGameObjectFileId = 0;
            bool inPrefabInstance = false;
            var unmappedExtendedLines = new HashSet<int>();
            bool pendingEventTargetValue = false;
            bool changed = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var rewritten = line;

                // track document/component boundaries for scoped field renames
                var docMatch = DocHeader.Match(line);
                if (docMatch.Success)
                {
                    currentComponent = null;
                    currentGameObjectFileId = 0;
                    inPrefabInstance = docMatch.Groups[1].Value == "1001";
                }

                var goMatch = GameObjectRef.Match(line);
                if (goMatch.Success)
                    currentGameObjectFileId = long.Parse(goMatch.Groups[1].Value);

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
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = mapping.Key,
                                To = mapping.Key, Status = "kept"
                            });
                            if (mapping.Fields.Count > 0)
                                currentComponent = mapping;
                        }
                        else if (mapping.IsMapped)
                        {
                            var pixoGuid = resolve?.Invoke(mapping.PixoNs, mapping.PixoClass,
                                currentGameObjectFileId != 0);
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
                                    To = mapping.PixoQualifiedName,
                                    Status = currentGameObjectFileId != 0
                                        ? "target-kind-mismatch" : "unresolved-pixo-script"
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
                    var key = string.IsNullOrEmpty(ns) ? cls : ns + "." + cls;
                    if (map.ManagedRefTypes.TryGetValue(key, out var mapping))
                    {
                        if (mapping.IsMapped)
                        {
                            rewritten = line.Replace(typeMatch.Value,
                                $"type: {{class: {mapping.PixoClass}, ns: {mapping.PixoNs}, asm: {mapping.PixoAsm ?? map.PixoAsm}}}");
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

                if (extendedRules)
                {
                    var qMatch = QualifiedTypeName.Match(line);
                    if (qMatch.Success)
                    {
                        var key = qMatch.Groups[2].Value;
                        if (map.QualifiedNames.TryGetValue(key, out var mapping) && mapping.IsMapped)
                        {
                            rewritten = qMatch.Groups[1].Value + mapping.PixoQualifiedName + ", " +
                                (mapping.PixoAsm ?? map.PixoAsm) +
                                ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
                            changed = true;
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = key,
                                To = mapping.PixoQualifiedName, Status = "mapped-qualified-name"
                            });
                        }
                        else if (key.StartsWith("Luminous.", StringComparison.Ordinal))
                        {
                            unmappedExtendedLines.Add(i);
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = key,
                                To = "", Status = "unmapped-qualified-name"
                            });
                        }
                    }

                    if (EventTargetPropertyPath.IsMatch(line))
                    {
                        pendingEventTargetValue = true;
                    }
                    else if (pendingEventTargetValue)
                    {
                        var vMatch = ValueTypeName.Match(line);
                        pendingEventTargetValue = false;
                        if (vMatch.Success)
                        {
                            var key = vMatch.Groups[2].Value;
                            if (map.QualifiedNames.TryGetValue(key, out var mapping) && mapping.IsMapped)
                            {
                                rewritten = vMatch.Groups[1].Value + mapping.PixoQualifiedName + ", " +
                                    (mapping.PixoAsm ?? map.PixoAsm) + vMatch.Groups[4].Value;
                                changed = true;
                                report?.Add(new MigrationReportEntry
                                {
                                    File = fileName, Line = i + 1, From = key,
                                    To = mapping.PixoQualifiedName, Status = "mapped-event-target"
                                });
                            }
                            else if (key.StartsWith("Luminous.", StringComparison.Ordinal))
                            {
                                unmappedExtendedLines.Add(i);
                                report?.Add(new MigrationReportEntry
                                {
                                    File = fileName, Line = i + 1, From = key,
                                    To = "", Status = "unmapped-event-target"
                                });
                            }
                        }
                    }

                    var eMatch = EventTargetName.Match(line);
                    if (eMatch.Success)
                    {
                        var key = eMatch.Groups[2].Value;
                        if (map.QualifiedNames.TryGetValue(key, out var mapping) && mapping.IsMapped)
                        {
                            rewritten = eMatch.Groups[1].Value + mapping.PixoQualifiedName + ", " +
                                (mapping.PixoAsm ?? map.PixoAsm) + eMatch.Groups[4].Value;
                            changed = true;
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = key,
                                To = mapping.PixoQualifiedName, Status = "mapped-event-target"
                            });
                        }
                        else if (key.StartsWith("Luminous.", StringComparison.Ordinal))
                        {
                            unmappedExtendedLines.Add(i);
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = key,
                                To = "", Status = "unmapped-event-target"
                            });
                        }
                    }

                    if (!scriptMatch.Success && map.AssetGuids.Count > 0)
                    {
                        foreach (var kv in map.AssetGuids)
                            if (rewritten.Contains(kv.Key))
                            {
                                rewritten = rewritten.Replace(kv.Key, kv.Value);
                                changed = true;
                                report?.Add(new MigrationReportEntry
                                {
                                    File = fileName, Line = i + 1, From = kv.Key,
                                    To = kv.Value, Status = "mapped-asset-guid"
                                });
                            }
                    }

                    if (inPrefabInstance && RemovedComponents.IsMatch(line))
                    {
                        int n = 0;
                        for (int j = i + 1; j < lines.Length && lines[j].TrimStart().StartsWith("- {fileID:"); j++)
                            n++;
                        if (n > 0)
                            report?.Add(new MigrationReportEntry
                            {
                                File = fileName, Line = i + 1, From = $"m_RemovedComponents ({n})",
                                To = "", Status = "info-removed-components"
                            });
                    }
                }

                output.Append(rewritten);
                if (i < lines.Length - 1)
                    output.Append('\n');
            }

            if (extendedRules && report != null)
            {
                var final = changed ? output.ToString() : yaml;
                var finalLines = final.Split('\n');
                for (int i = 0; i < finalLines.Length; i++)
                {
                    var trimmed = finalLines[i].Trim();
                    if (trimmed.Contains("Luminous") && !unmappedExtendedLines.Contains(i))
                        report.Add(new MigrationReportEntry
                        {
                            File = fileName, Line = i + 1,
                            From = trimmed.Length > 120 ? trimmed.Substring(0, 120) : trimmed,
                            To = "", Status = "residual-luminous"
                        });
                }
            }

            return changed ? output.ToString() : yaml;
        }
    }
}

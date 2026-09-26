using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PixoVR.TrainingCore.Editor.Migration;

namespace PixoVR.TrainingCore.Tests.Editor
{
    /// <summary>Tests for the Luminous migration machinery.</summary>
    public class MigrationTests
    {
        [Test]
        public void FileID_KnownValues()
        {
            // verified against real serialized references in the sample project
            Assert.AreEqual(-1137388002, FileIDUtil.ComputeFileID("Luminous.GraphSystem", "DefaultGraph"));
            Assert.AreEqual(1001651715, FileIDUtil.ComputeFileID("Luminous.GraphSystem", "GuidComponent"));
            Assert.AreEqual(-1449447852, FileIDUtil.ComputeFileID("Luminous.Observer", "ObservableSubject"));
        }

        private const string LuminousGuid = "9049b7504ee0d084494b1b4e3dbe49e5";

        private static MigrationMap BuildMap()
        {
            var json = @"{
                ""dlls"": [""" + LuminousGuid + @"""],
                ""luminousAsm"": ""CoreSystemRuntime"",
                ""pixoAsm"": ""PixoVR.TrainingCore"",
                ""types"": [
                    { ""luminous"": { ""ns"": ""Luminous.GraphSystem"", ""class"": ""DefaultGraph"" },
                      ""pixo"": { ""ns"": ""PixoVR.TrainingCore.Graph"", ""class"": ""TrainingGraph"" } },
                    { ""luminous"": { ""ns"": ""Luminous.Core"", ""class"": ""Renamer"" },
                      ""pixo"": { ""ns"": ""PixoVR.TrainingCore.Core"", ""class"": ""Renamer"" },
                      ""fields"": { ""oldName"": ""newName"" } },
                    { ""luminous"": { ""ns"": ""Luminous.Mystery"", ""class"": ""Unknown"" }, ""pixo"": null },
                    { ""luminous"": { ""ns"": ""Luminous.GraphSystem"", ""class"": ""InputActionStepNode"" },
                      ""pixo"": { ""ns"": ""PixoVR.TrainingCore.Graph"", ""class"": ""InputActionStepNode"" } }
                ],
                ""scripts"": []
            }";
            return MigrationMap.Load(json);
        }

        private static ScriptResolution PixoGuidResolver(string ns, string cls, bool wantsComponent)
        {
            var guid = cls == "TrainingGraph" ? "aaaa1111" : cls == "Renamer" ? "bbbb2222" : null;
            return guid == null ? null : new ScriptResolution { Guid = guid };
        }

        [Test]
        public void Rewrite_ManagedRef_TypeBlock()
        {
            var yaml =
                "--- !u!114 &1\n" +
                "  type: {class: DefaultGraph, ns: Luminous.GraphSystem, asm: CoreSystemRuntime}\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), PixoGuidResolver, report, "f.asset");
            StringAssert.Contains("type: {class: TrainingGraph, ns: PixoVR.TrainingCore.Graph, asm: PixoVR.TrainingCore}", output);
            Assert.AreEqual("mapped", report[0].Status);
        }

        [Test]
        public void Rewrite_ManagedRef_InputActionStepNode()
        {
            var yaml =
                "--- !u!114 &1\n" +
                "  type: {class: InputActionStepNode, ns: Luminous.GraphSystem, asm: CoreSystemRuntime}\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), PixoGuidResolver, report, "f.asset");
            StringAssert.Contains("type: {class: InputActionStepNode, ns: PixoVR.TrainingCore.Graph, asm: PixoVR.TrainingCore}", output);
            Assert.AreEqual("mapped", report[0].Status);
        }

        [Test]
        public void Rewrite_ScriptRef()
        {
            var yaml =
                "  m_Script: {fileID: -1137388002, guid: " + LuminousGuid + ", type: 3}\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), PixoGuidResolver, report, "f.unity");
            StringAssert.Contains("{fileID: 11500000, guid: aaaa1111, type: 3}", output);
        }

        [Test]
        public void Rewrite_UnmappedAndUnrelatedPreserved()
        {
            var unknownId = FileIDUtil.ComputeFileID("Luminous.Mystery", "Unknown");
            var yaml =
                "  m_Script: {fileID: " + unknownId + ", guid: " + LuminousGuid + ", type: 3}\n" +
                "  m_Script: {fileID: 999, guid: deadbeefdeadbeefdeadbeefdeadbeef, type: 3}\n" +
                "  someOther: value\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), PixoGuidResolver, report, "f.unity");
            StringAssert.Contains("fileID: " + unknownId, output); // unmapped: preserved
            StringAssert.Contains("deadbeef", output);    // foreign guid: untouched
            StringAssert.Contains("someOther: value", output);
            Assert.AreEqual("unmapped", report[0].Status);
            Assert.AreEqual(1, report.Count);
        }

        [Test]
        public void Rewrite_FieldRenameScopedToBlock()
        {
            var yaml =
                "--- !u!114 &1\n" +
                "MonoBehaviour:\n" +
                "  m_Script: {fileID: " + FileIDUtil.ComputeFileID("Luminous.Core", "Renamer") + ", guid: " + LuminousGuid + ", type: 3}\n" +
                "  oldName: 5\n" +
                "--- !u!114 &2\n" +
                "MonoBehaviour:\n" +
                "  m_Script: {fileID: 999, guid: deadbeefdeadbeefdeadbeefdeadbeef, type: 3}\n" +
                "  oldName: 7\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), PixoGuidResolver, report, "f.prefab");
            // rename applied inside the Renamer's document only
            Assert.IsTrue(output.Contains("newName: 5"));
            Assert.IsTrue(output.Contains("oldName: 7"));
        }

        private static MigrationMap BuildExtendedMap()
        {
            var json = @"{
                ""dlls"": [""" + LuminousGuid + @"""],
                ""luminousAsm"": ""CoreSystemRuntime"",
                ""pixoAsm"": ""PixoVR.TrainingCore"",
                ""types"": [
                    { ""luminous"": { ""ns"": ""Luminous.Unity"", ""class"": ""DisplayInteractionMiddleman"" },
                      ""pixo"": { ""ns"": ""PixoVR.TrainingCore.Utility.Display"", ""class"": ""DisplayInteraction"" } },
                    { ""luminous"": { ""ns"": ""Luminous.GraphSystem"", ""class"": ""DefaultGraph"" },
                      ""pixo"": { ""ns"": ""PixoVR.TrainingCore.Graph"", ""class"": ""TrainingGraph"",
                        ""asm"": ""PixoVR.TrainingCore.XRI"" } }
                ],
                ""scripts"": [
                    { ""guid"": ""11111111111111111111111111111111"",
                      ""luminous"": { ""ns"": ""Luminous.Interactables"", ""class"": ""GrabbableOpenXR"" },
                      ""pixo"": { ""ns"": ""PixoVR.TrainingCore.XRI"", ""class"": ""XRIGrabBehaviour"",
                        ""asm"": ""PixoVR.TrainingCore.XRI"" } },
                    { ""guid"": ""22222222222222222222222222222222"",
                      ""luminous"": { ""ns"": ""Luminous.Middleman"", ""class"": ""GenericActionFunctionality"" },
                      ""pixo"": null, ""keep"": true,
                      ""fields"": { ""LocomotionSystem"": ""LocomotionMediator"" } }
                ],
                ""assets"": [
                    { ""luminous"": ""c348712bda248c246b8c49b3db54643f"",
                      ""pixo"": ""3d1634cb0140478092fc030578072e4d"", ""note"": ""input actions"" }
                ]
            }";
            return MigrationMap.Load(json);
        }

        // real snippets from sa-collect-gas-sample commit a17adaf8
        private const string QualifiedNameLine =
            "        assemblyQualifiedName: Luminous.Interactables.GrabbableOpenXR, MiddlemanRuntime,\n";
        private const string EventTargetSnippet =
            "      propertyPath: m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_TargetAssemblyTypeName\n" +
            "      value: Luminous.Unity.DisplayInteractionMiddleman, CoreSystemRuntime\n";
        private const string AssetRefLine =
            "    m_Reference: {fileID: 6539153397825551058, guid: c348712bda248c246b8c49b3db54643f, type: 3}\n";

        [Test]
        public void QualifiedName_GrabbableOpenXR_Rewritten()
        {
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(QualifiedNameLine, BuildExtendedMap(), PixoGuidResolver, report, "f.asset");
            StringAssert.Contains("assemblyQualifiedName: PixoVR.TrainingCore.XRI.XRIGrabBehaviour, " +
                "PixoVR.TrainingCore.XRI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", output);
            Assert.AreEqual("mapped-qualified-name", report[0].Status);
        }

        [Test]
        public void QualifiedName_UnmappedLuminous_ReportedNotRewritten()
        {
            var yaml = "        assemblyQualifiedName: Luminous.Mystery.Unknown, Somewhere,\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.asset");
            StringAssert.Contains("Luminous.Mystery.Unknown", output);
            Assert.AreEqual("unmapped-qualified-name", report[0].Status);
        }

        [Test]
        public void EventTarget_DisplayInteractionMiddleman_Rewritten()
        {
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(EventTargetSnippet, BuildExtendedMap(), PixoGuidResolver, report, "f.prefab");
            StringAssert.Contains("value: PixoVR.TrainingCore.Utility.Display.DisplayInteraction, PixoVR.TrainingCore", output);
            Assert.AreEqual("mapped-event-target", report[0].Status);
        }

        [Test]
        public void AssetGuid_PlainAndJsonEscaped_Rewritten()
        {
            var yaml = AssetRefLine +
                "    \"m_SerializedSubGraph\": \"{\\n    \\\"subGraph\\\": {\\\"guid\\\": \\\"c348712bda248c246b8c49b3db54643f\\\"}}\",\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.prefab");
            StringAssert.Contains("3d1634cb0140478092fc030578072e4d", output);
            StringAssert.DoesNotContain("c348712bda248c246b8c49b3db54643f", output);
            Assert.AreEqual(2, report.Count(r => r.Status == "mapped-asset-guid"));
        }

        [Test]
        public void KeepMapping_FieldRenamed()
        {
            var yaml =
                "--- !u!114 &1\n" +
                "MonoBehaviour:\n" +
                "  m_GameObject: {fileID: 5}\n" +
                "  m_Script: {fileID: 11500000, guid: 22222222222222222222222222222222, type: 3}\n" +
                "  LocomotionSystem: {fileID: 0}\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.prefab");
            StringAssert.Contains("LocomotionMediator: {fileID: 0}", output);
            Assert.IsTrue(report.Exists(r => r.Status == "kept"));
            Assert.IsTrue(report.Exists(r => r.Status == "field-renamed"));
        }

        [Test]
        public void RemovedComponents_Reported()
        {
            var yaml =
                "--- !u!1001 &9\n" +
                "PrefabInstance:\n" +
                "    m_RemovedComponents:\n" +
                "    - {fileID: 1}\n" +
                "    - {fileID: 2}\n";
            var report = new List<MigrationReportEntry>();
            YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.unity");
            Assert.IsTrue(report.Exists(r => r.Status == "info-removed-components" && r.From == "m_RemovedComponents (2)"));
        }

        [Test]
        public void Residual_Reported()
        {
            var yaml = "  someField: Luminous.Something\n";
            var report = new List<MigrationReportEntry>();
            YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.asset");
            Assert.IsTrue(report.Exists(r => r.Status == "residual-luminous"));
        }

        [Test]
        public void ExtendedRulesDisabled_LeavesQualifiedNameAndEventTarget()
        {
            var yaml = QualifiedNameLine + EventTargetSnippet;
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.asset",
                extendedRules: false);
            StringAssert.Contains("Luminous.Interactables.GrabbableOpenXR", output);
            StringAssert.Contains("Luminous.Unity.DisplayInteractionMiddleman", output);
            Assert.IsFalse(report.Exists(r => r.Status.StartsWith("mapped-") || r.Status == "unmapped-event-target"));
        }

        [Test]
        public void ManagedRef_UsesPerTypeAsm()
        {
            var yaml = "  type: {class: DefaultGraph, ns: Luminous.GraphSystem, asm: CoreSystemRuntime}\n";
            var report = new List<MigrationReportEntry>();
            var output = YamlRewriter.Rewrite(yaml, BuildExtendedMap(), PixoGuidResolver, report, "f.asset");
            StringAssert.Contains("asm: PixoVR.TrainingCore.XRI", output);
        }

        [Test]
        public void Merge_OverlayOverridesAndAddsAssets()
        {
            var map = BuildExtendedMap();
            map.Merge(@"{ ""types"": [
                    { ""luminous"": { ""ns"": ""Luminous.Unity"", ""class"": ""DisplayInteractionMiddleman"" },
                      ""pixo"": { ""ns"": ""Over.Ride"", ""class"": ""Other"" } } ],
                ""assets"": [ { ""luminous"": ""aa"", ""pixo"": ""bb"" } ] }");
            Assert.AreEqual("Other", map.QualifiedNames["Luminous.Unity.DisplayInteractionMiddleman"].PixoClass);
            Assert.AreEqual("bb", map.AssetGuids["aa"]);
            Assert.AreEqual("3d1634cb0140478092fc030578072e4d",
                map.AssetGuids["c348712bda248c246b8c49b3db54643f"]);
        }

        [Test]
        public void TargetKind_Mismatch_NotRewritten()
        {
            var yaml =
                "MonoBehaviour:\n" +
                "  m_GameObject: {fileID: 5}\n" +
                "  m_Script: {fileID: " + FileIDUtil.ComputeFileID("Luminous.GraphSystem", "DefaultGraph") +
                ", guid: " + LuminousGuid + ", type: 3}\n";
            var report = new List<MigrationReportEntry>();
            ScriptResolution MismatchForComponents(string ns, string cls, bool wantsComponent) =>
                wantsComponent
                    ? new ScriptResolution { KindMismatch = true }
                    : new ScriptResolution { Guid = "aaaa1111" };
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), MismatchForComponents, report, "f.unity");
            StringAssert.Contains(LuminousGuid, output);
            Assert.AreEqual("target-kind-mismatch", report[0].Status);
        }

        [Test]
        public void TargetKind_NotFound_UnresolvedPixoScript()
        {
            var yaml =
                "MonoBehaviour:\n" +
                "  m_GameObject: {fileID: 5}\n" +
                "  m_Script: {fileID: " + FileIDUtil.ComputeFileID("Luminous.GraphSystem", "DefaultGraph") +
                ", guid: " + LuminousGuid + ", type: 3}\n";
            var report = new List<MigrationReportEntry>();
            ScriptResolution NotFound(string ns, string cls, bool wantsComponent) => null;
            var output = YamlRewriter.Rewrite(yaml, BuildMap(), NotFound, report, "f.unity");
            StringAssert.Contains(LuminousGuid, output);
            Assert.AreEqual("unresolved-pixo-script", report[0].Status);
        }

        [Test]
        public void ManifestRewrite_OnSampleManifest()
        {
            // Luminous-era manifest fixture (the real sample manifest is already migrated).
            var fixture = @"{
  ""dependencies"": {
    ""com.luminous.core"": ""1.0.0"",
    ""com.luminous.core.middlemen"": ""1.0.0"",
    ""com.luminous.core.middlemen.interactiontoolkit"": ""1.0.0"",
    ""com.unity.xr.interaction.toolkit"": ""2.0.0""
  },
  ""scopedRegistries"": [
    { ""name"": ""Luminous"", ""url"": ""https://example.invalid"", ""scopes"": [""com.luminous""] }
  ]
}";
            var options = new MigrationOptions { TrainingCoreDependency = "file:../com.pixovr.trainingcore" };
            var notes = new List<string>();
            var rewritten = ManifestRewriter.Rewrite(fixture, options, notes);

            StringAssert.DoesNotContain("luminous", rewritten.ToLowerInvariant());
            StringAssert.Contains("com.pixovr.trainingcore", rewritten);
            StringAssert.Contains("com.unity.addressables", rewritten);
            StringAssert.Contains("com.unity.inputsystem", rewritten);
            StringAssert.Contains("com.unity.nuget.newtonsoft-json", rewritten);
            StringAssert.Contains("com.unity.timeline", rewritten);
            StringAssert.Contains("NodeGraphProcessor", rewritten);
            // other deps preserved
            StringAssert.Contains("com.unity.xr.interaction.toolkit", rewritten);
            Assert.IsTrue(notes.Count > 0);
        }
    }
}

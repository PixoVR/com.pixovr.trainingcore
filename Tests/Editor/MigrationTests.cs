using System.Collections.Generic;
using System.IO;
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
                    { ""luminous"": { ""ns"": ""Luminous.Mystery"", ""class"": ""Unknown"" }, ""pixo"": null }
                ],
                ""scripts"": []
            }";
            return MigrationMap.Load(json);
        }

        private static string PixoGuidResolver(string ns, string cls) =>
            cls == "TrainingGraph" ? "aaaa1111" : cls == "Renamer" ? "bbbb2222" : null;

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

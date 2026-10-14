#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    public static class LavaFlowValidation
    {
        private const string Folder = "Assets/_Project/Graphics/Enviorment/Battle/LavaArenaVFX/";
        private const string Request = "Library/LavaFlowValidation.request";
        private const string ReportPath = "Library/LavaFlowValidation.json";

        [Serializable]
        private class Report
        {
            public string checkedAt;
            public string shader;
            public string shaderGuid;
            public long shaderLocalId;
            public bool success;
            public int passes;
            public string[] messages;
        }

        [InitializeOnLoadMethod]
        private static void RunRequestedCheck()
        {
            // An explicit local request is used by tooling; normal reloads do nothing.
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(Request))
                {
                    File.Delete(Request);
                    Validate();
                }
            };
        }

        [MenuItem("Tools/M3P/Validate Lava Flow Shader")]
        public static void Validate()
        {
            var report = new Report { checkedAt = DateTime.UtcNow.ToString("O") };
            Material probe = null;
            try
            {
                AssetDatabase.ImportAsset(Folder + "LavaFlowMap.png", ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(Folder + "LavaFlow.hlsl", ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(Folder + "LawaWaterFall.shadergraph", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(Folder + "LawaWaterfall.mat", ImportAssetOptions.ForceSynchronousImport);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "LawaWaterFall.shadergraph");
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "LawaWaterfall.mat");
                if (!shader) throw new InvalidOperationException("The lava graph did not import a Shader asset.");
                if (!material) throw new InvalidOperationException("The lava Material asset did not import.");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shader, out report.shaderGuid, out report.shaderLocalId);
                if (material.shader != shader)
                    throw new InvalidOperationException("Material does not reference " + report.shaderGuid + ":" + report.shaderLocalId);
                foreach (var property in new[] { "_FlowMap", "_FlowSpeed", "_FlowCycle", "_Noise", "_Mask" })
                    if (!material.HasProperty(property))
                        throw new InvalidOperationException("Missing shader property: " + property);
                if (material.GetTexture("_FlowMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "LavaFlowMap.png"))
                    throw new InvalidOperationException("The flow map is not assigned.");
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "LavaFlowMap.png");
                if (importer.sRGBTexture || importer.wrapModeU != TextureWrapMode.Clamp || importer.wrapModeV != TextureWrapMode.Clamp)
                    throw new InvalidOperationException("Flow map must be linear and clamped.");
                probe = new Material(material);
                report.shader = shader.name;
                report.passes = probe.passCount;
                for (int pass = 0; pass < probe.passCount; pass++)
                    ShaderUtil.CompilePass(probe, pass, true);
                report.messages = ShaderUtil.GetShaderMessages(shader)
                    .Select(m => m.severity + ": " + m.message + " (" + m.file + ":" + m.line + ")").ToArray();
                report.success = probe.passCount > 0 && !ShaderUtil.ShaderHasError(shader);
            }
            catch (Exception exception)
            {
                report.success = false;
                report.messages = new[] { exception.ToString() };
            }
            finally
            {
                if (probe) UnityEngine.Object.DestroyImmediate(probe);
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            }
            if (report.success) Debug.Log("Lava flow shader validated: " + report.passes + " passes.");
            else Debug.LogError("Lava flow validation failed. See " + ReportPath);
        }
    }
}
#endif

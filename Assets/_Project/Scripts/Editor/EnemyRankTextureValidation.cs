#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    public static class EnemyRankTextureValidation
    {
        [MenuItem("Tools/M3P/Validate Frenzi Rank Textures")]
        public static void Validate()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Configs/Enemies/Frenzi.asset");
            Require(definition != null && definition.RankThresholds.Count == 5, "Expected five Frenzi ranks.");
            int checkedSlots = 0;
            foreach (EnemyRankThreshold rank in definition.RankThresholds)
            {
                EnemyRuntimeSpec spec = EnemyProgressionResolver.Resolve(definition, rank.MinFloor, MapNodeType.Battle);
                Require(spec.DisplayName.EndsWith(rank.RankName), "Rank selection mismatch.");
                Require(spec.MaterialTextures.Count == 2, "Expected head and body textures.");
                var instance = UnityEngine.Object.Instantiate(definition.EnemyModelPrefab);
                try
                {
                    var character = instance.GetComponentInChildren<WorldCharacter>(true);
                    Require(character != null, "Missing WorldCharacter.");
                    var block = new MaterialPropertyBlock();
                    var targets = character.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer target in targets)
                    {
                        block.Clear();
                        block.SetFloat("_RankValidationSentinel", 0.375f);
                        target.SetPropertyBlock(block);
                    }
                    character.ApplyRankTextures(spec.MainTexture, spec.MaterialTextures);
                    foreach (EnemyMaterialTexture entry in spec.MaterialTextures)
                    {
                        Require(entry.Material != null && entry.Texture != null, "Broken rank reference.");
                        int matches = 0;
                        foreach (Renderer target in targets)
                        {
                            for (int slot = 0; slot < target.sharedMaterials.Length; slot++)
                            {
                                if (target.sharedMaterials[slot] != entry.Material) continue;
                                block.Clear();
                                target.GetPropertyBlock(block, slot);
                                Require(block.GetTexture("_MainTexture") == entry.Texture, "Texture not applied to " + target.name);
                                Require(Mathf.Approximately(block.GetFloat("_RankValidationSentinel"), 0.375f), "Renderer property was lost.");
                                matches++;
                                checkedSlots++;
                            }
                        }
                        Require(matches > 0, "Override material absent from prefab: " + entry.Material.name);
                    }
                    // Empty or invalid overrides must preserve the previously applied appearance.
                    character.ApplyRankTextures(null, new[] { default(EnemyMaterialTexture) });
                    foreach (Renderer target in targets)
                    {
                        for (int slot = 0; slot < target.sharedMaterials.Length; slot++)
                        {
                            foreach (EnemyMaterialTexture entry in spec.MaterialTextures)
                            {
                                if (target.sharedMaterials[slot] != entry.Material) continue;
                                block.Clear();
                                target.GetPropertyBlock(block, slot);
                                Require(block.GetTexture("_MainTexture") == entry.Texture, "Empty override changed a texture.");
                            }
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            var boss = EnemyProgressionResolver.Resolve(definition, 1, MapNodeType.Boss);
            Require(boss.DisplayName.EndsWith("Herszt") && boss.MaterialTextures.Count == 2, "Boss must select the final texture pair.");
            var elite = EnemyProgressionResolver.Resolve(definition, 1, MapNodeType.Elite);
            Require(elite.DisplayName.EndsWith("Wartownik"), "Elite rank selection mismatch.");
            Require(EnemyProgressionResolver.ResolveRankMaterialTextures(null, 1) == null, "Legacy fallback changed.");
            string report = "PASS: five Frenzi ranks, " + checkedSlots + " material slots, property preservation, null overrides, elite and boss selection.";
            Directory.CreateDirectory("outputs/frenzi_variants");
            File.WriteAllText("outputs/frenzi_variants/UnityValidation.txt", report);
            Debug.Log(report);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;

public class BattleEnviormentSettings : MonoBehaviour
{
    [SerializeField] M3P.MapBiome _biome;
    [SerializeField] Color LowerColor;
    [SerializeField] Color MidColor;
    [SerializeField] Color HighColor;

    /// <summary>
    /// Biome this arena shows. An unset value still follows the object name, so the
    /// lava and frost levels keep matching their pedestals.
    /// </summary>
    public M3P.MapBiome Biome
    {
        get
        {
            if (_biome != M3P.MapBiome.Default)
                return _biome;

            if (name.IndexOf("Lava", StringComparison.OrdinalIgnoreCase) >= 0)
                return M3P.MapBiome.Lava;

            if (name.IndexOf("Frost", StringComparison.OrdinalIgnoreCase) >= 0)
                return M3P.MapBiome.Frost;

            return M3P.MapBiome.Default;
        }
    }

    public void ApplyEnvironmentLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = HighColor;
        RenderSettings.ambientEquatorColor = MidColor;
        RenderSettings.ambientGroundColor = LowerColor;
        DynamicGI.UpdateEnvironment();
    }
}

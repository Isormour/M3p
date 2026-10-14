using UnityEngine;
using UnityEngine.Rendering;

public class BattleEnviormentSettings : MonoBehaviour
{
    [SerializeField] Color LowerColor;
    [SerializeField] Color MidColor;
    [SerializeField] Color HighColor;

    public void ApplyEnvironmentLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = HighColor;
        RenderSettings.ambientEquatorColor = MidColor;
        RenderSettings.ambientGroundColor = LowerColor;
        DynamicGI.UpdateEnvironment();
    }
}

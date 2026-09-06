using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Status outline palettes and other authored combat VFX tuning.</summary>
    [CreateAssetMenu(fileName = "VFXConfig", menuName = "M3P/VFX Config", order = 4)]
    public class VFXConfig : ScriptableObject
    {
        [SerializeField] StatusPalette[] _statusPalettes;

        Dictionary<EStatusType, StatusVFXParams> _paramsByType;

        public StatusPalette[] StatusPalettes => _statusPalettes ?? System.Array.Empty<StatusPalette>();

        public bool TryGetParams(EStatusType statusType, out StatusVFXParams parameters)
        {
            EnsureLookup();
            return _paramsByType.TryGetValue(statusType, out parameters);
        }

        /// <summary>
        /// Palette for <paramref name="statusType"/>, or the authored None look when that type has no entry.
        /// </summary>
        public StatusVFXParams GetParams(EStatusType statusType)
        {
            if (TryGetParams(statusType, out StatusVFXParams parameters))
                return parameters;

            if (statusType != EStatusType.None && TryGetParams(EStatusType.None, out parameters))
                return parameters;

            return StatusVFXParams.Off;
        }

        void EnsureLookup()
        {
            if (_paramsByType != null)
                return;

            StatusPalette[] palettes = StatusPalettes;
            _paramsByType = new Dictionary<EStatusType, StatusVFXParams>(palettes.Length);
            for (int i = 0; i < palettes.Length; i++)
                _paramsByType[palettes[i].StatusType] = palettes[i].Params;
        }

        void OnEnable()
        {
            _paramsByType = null;
        }

        void OnValidate()
        {
            _paramsByType = null;
        }
    }
}

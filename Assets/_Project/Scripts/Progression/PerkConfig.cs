using System;
using UnityEngine;

namespace M3P
{
    /// <summary>Catalog of every automatic hard-stat perk available to progression configs.</summary>
    [CreateAssetMenu(fileName = "PerkConfig", menuName = "M3P/Perk Config", order = 7)]
    public class PerkConfig : ScriptableObject
    {
        [SerializeField] PerkDefinition[] _perks = Array.Empty<PerkDefinition>();

        public PerkDefinition[] Perks => _perks ?? Array.Empty<PerkDefinition>();

        public bool Contains(PerkDefinition perk)
        {
            if (perk == null)
                return false;

            PerkDefinition[] perks = Perks;
            for (int i = 0; i < perks.Length; i++)
            {
                if (perks[i] == perk)
                    return true;
            }

            return false;
        }

        void OnValidate()
        {
            PerkDefinition[] perks = Perks;
            for (int i = 0; i < perks.Length; i++)
            {
                PerkDefinition perk = perks[i];
                if (perk == null)
                    continue;

                for (int j = i + 1; j < perks.Length; j++)
                {
                    if (perks[j] == perk)
                        Debug.LogWarning($"{nameof(PerkConfig)} '{name}' contains '{perk.name}' more than once.", this);
                }
            }
        }
    }
}

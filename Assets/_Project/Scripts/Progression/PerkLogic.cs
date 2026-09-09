using System;
using UnityEngine;

namespace M3P
{
    public struct SoftStatValues
    {
        public int BasicAttackDamage;
        public int MaxHP;
        public int MaxActionPoints;
        public int MaxHandSize;
    }

    [Serializable]
    public abstract class PerkLogic
    {
        public abstract void Apply(ref SoftStatValues stats);
    }

    [Serializable]
    public sealed class AddBasicDamage : PerkLogic
    {
        [Min(0), SerializeField] int _amount = 1;

        public AddBasicDamage() { }
        public AddBasicDamage(int amount) => _amount = Mathf.Max(0, amount);

        public override void Apply(ref SoftStatValues stats)
        {
            stats.BasicAttackDamage += Mathf.Max(0, _amount);
        }
    }

    [Serializable]
    public sealed class AddActionPoints : PerkLogic
    {
        [Min(0), SerializeField] int _amount = 1;

        public AddActionPoints() { }
        public AddActionPoints(int amount) => _amount = Mathf.Max(0, amount);

        public override void Apply(ref SoftStatValues stats)
        {
            stats.MaxActionPoints += Mathf.Max(0, _amount);
        }
    }

    [Serializable]
    public sealed class AddMaxHP : PerkLogic
    {
        [Min(0), SerializeField] int _amount = 5;

        public AddMaxHP() { }
        public AddMaxHP(int amount) => _amount = Mathf.Max(0, amount);

        public override void Apply(ref SoftStatValues stats)
        {
            stats.MaxHP += Mathf.Max(0, _amount);
        }
    }

    [Serializable]
    public sealed class AddHandCardCapacity : PerkLogic
    {
        [Min(0), SerializeField] int _amount = 1;

        public AddHandCardCapacity() { }
        public AddHandCardCapacity(int amount) => _amount = Mathf.Max(0, amount);

        public override void Apply(ref SoftStatValues stats)
        {
            stats.MaxHandSize += Mathf.Max(0, _amount);
        }
    }
}

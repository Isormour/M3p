using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public struct SoftStatValues
    {
        public int BasicAttackDamage;
        public int MaxHP;
        public int MaxActionPoints;
        public int MaxHandSize;
        public int Power;
        public int Arcane;
        public int Guard;
        public int Recovery;
        public int Precision;
        public int Tempo;
        public int StartingSouls;
        public int KindleBurn;
        public int EmberLife;
        public int RenewingFlame;
        public int Fury;
        public int Tenacity;
        public int Regroup;
        public int Reserve;
        public int Faith;
        public int Sanctuary;
        public int StrikeSeries;
        public int Breakthrough;
        public int Focus;
        public int Inspiration;
        public int Fortitude;
        public int SecondWind;
        public int Accuracy;
        public int Sprint;

        public void AddRule(PerkRule rule)
        {
            switch (rule)
            {
                case PerkRule.Kindle:
                    KindleBurn++;
                    break;
                case PerkRule.EmberLife:
                    EmberLife++;
                    break;
                case PerkRule.RenewingFlame:
                    RenewingFlame++;
                    break;
                case PerkRule.Fury:
                    Fury++;
                    break;
                case PerkRule.Tenacity:
                    Tenacity++;
                    break;
                case PerkRule.Regroup:
                    Regroup++;
                    break;
                case PerkRule.Reserve:
                    Reserve++;
                    break;
                case PerkRule.Faith:
                    Faith++;
                    break;
                case PerkRule.Sanctuary:
                    Sanctuary++;
                    break;
                case PerkRule.StrikeSeries:
                    StrikeSeries++;
                    break;
                case PerkRule.Breakthrough:
                    Breakthrough++;
                    break;
                case PerkRule.Focus:
                    Focus++;
                    break;
                case PerkRule.Inspiration:
                    Inspiration++;
                    break;
                case PerkRule.Fortitude:
                    Fortitude++;
                    break;
                case PerkRule.SecondWind:
                    SecondWind++;
                    break;
                case PerkRule.Accuracy:
                    Accuracy++;
                    break;
                case PerkRule.Sprint:
                    Sprint++;
                    break;
            }
        }

        public void Add(PlayerProperty property, int amount)
        {
            if (amount == 0)
                return;

            switch (property)
            {
                case PlayerProperty.Health:
                    MaxHP += amount;
                    break;
                case PlayerProperty.ActionPoints:
                    MaxActionPoints += amount;
                    break;
                case PlayerProperty.HandSize:
                    MaxHandSize += amount;
                    break;
                case PlayerProperty.Power:
                    Power += amount;
                    break;
                case PlayerProperty.Arcane:
                    Arcane += amount;
                    break;
                case PlayerProperty.Guard:
                    Guard += amount;
                    break;
                case PlayerProperty.Recovery:
                    Recovery += amount;
                    break;
                case PlayerProperty.Precision:
                    Precision += amount;
                    break;
                case PlayerProperty.Tempo:
                    Tempo += amount;
                    break;
            }
        }

        public PlayerProperties ToProperties()
        {
            return new PlayerProperties
            {
                Health = MaxHP,
                ActionPoints = MaxActionPoints,
                HandSize = MaxHandSize,
                Power = Power,
                Arcane = Arcane,
                Guard = Guard,
                Recovery = Recovery,
                Precision = Precision,
                Tempo = Tempo,
            };
        }
    }

    [Serializable]
    public abstract class PerkLogic
    {
        public abstract void Apply(ref SoftStatValues stats);

        public virtual void AppendTooltipLines(List<TooltipLine> lines) { }

        /// <summary>Short effect line for the unlock offer, using the amount authored on this perk.</summary>
        public virtual string FormatOffer() => string.Empty;
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

        public override void AppendTooltipLines(List<TooltipLine> lines)
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Damage, $"+{amount} obrażeń podstawowych"));
        }

        public override string FormatOffer()
        {
            int amount = Mathf.Max(0, _amount);
            return amount > 0 ? $"+{amount} obrażeń" : string.Empty;
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

        public override void AppendTooltipLines(List<TooltipLine> lines)
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Other, $"+{amount} AP"));
        }

        public override string FormatOffer()
        {
            int amount = Mathf.Max(0, _amount);
            return amount > 0 ? $"+{amount} AP" : string.Empty;
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

        public override void AppendTooltipLines(List<TooltipLine> lines)
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Heal, $"+{amount} maks. HP"));
        }

        public override string FormatOffer()
        {
            int amount = Mathf.Max(0, _amount);
            return amount > 0 ? $"+{amount} maks. HP" : string.Empty;
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

        public override void AppendTooltipLines(List<TooltipLine> lines)
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Draw, $"+{amount} kart w ręce"));
        }

        public override string FormatOffer()
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return string.Empty;

            return $"+{amount} {CardWord(amount)}";
        }

        static string CardWord(int amount)
        {
            if (amount == 1)
                return "karta";

            int mod10 = amount % 10;
            int mod100 = amount % 100;
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14))
                return "karty";

            return "kart";
        }
    }

    /// <summary>
    /// Adds one <see cref="PlayerProperty"/>. Any perk tree can grant any property; skills read the sum.
    /// </summary>
    [Serializable]
    public sealed class AddPlayerProperty : PerkLogic
    {
        [SerializeField] PlayerProperty _property = PlayerProperty.Power;
        [Min(0), SerializeField] int _amount = 1;

        public AddPlayerProperty() { }

        public AddPlayerProperty(PlayerProperty property, int amount)
        {
            _property = property;
            _amount = Mathf.Max(0, amount);
        }

        public override void Apply(ref SoftStatValues stats)
        {
            stats.Add(_property, Mathf.Max(0, _amount));
        }

        public override void AppendTooltipLines(List<TooltipLine> lines)
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Other, $"+{amount} {PlayerProperties.DisplayName(_property)}"));
        }
    }

    /// <summary>Souls granted once, when the battle starts.</summary>
    [Serializable]
    public sealed class AddStartingSouls : PerkLogic
    {
        [Min(0), SerializeField] int _amount = 1;

        public override void Apply(ref SoftStatValues stats)
        {
            stats.StartingSouls += Mathf.Max(0, _amount);
        }

        public override void AppendTooltipLines(List<TooltipLine> lines)
        {
            int amount = Mathf.Max(0, _amount);
            if (amount <= 0)
                return;

            lines.Add(new TooltipLine(TooltipLineKind.Other, $"+{amount} Dusz na początku walki"));
        }

        public override string FormatOffer()
        {
            int amount = Mathf.Max(0, _amount);
            return amount > 0 ? $"+{amount} Dusz" : string.Empty;
        }
    }

    /// <summary>A conditional perk. Several copies of the same rule add up.</summary>
    public enum PerkRule
    {
        Kindle = 0,
        EmberLife = 1,
        RenewingFlame = 2,
        Fury = 3,
        Tenacity = 4,
        Regroup = 5,
        Reserve = 6,
        Faith = 7,
        Sanctuary = 8,
        StrikeSeries = 9,
        Breakthrough = 10,
        Focus = 11,
        Inspiration = 12,
        Fortitude = 13,
        SecondWind = 14,
        Accuracy = 15,
        Sprint = 16,
    }

    [Serializable]
    public sealed class GrantPerkRule : PerkLogic
    {
        [SerializeField] PerkRule _rule;

        public override void Apply(ref SoftStatValues stats)
        {
            stats.AddRule(_rule);
        }
    }
}

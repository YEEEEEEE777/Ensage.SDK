﻿// <copyright file="ancient_apparition_cold_feet.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace Ensage.SDK.Abilities.npc_dota_hero_ancient_apparition
{
    using System.Linq;

    using Ensage.SDK.Abilities.Components;
    using Ensage.SDK.Extensions;
    using Ensage.SDK.Helpers;

    public class ancient_apparition_cold_feet : RangedAbility, IHasDot
    {
        public ancient_apparition_cold_feet(Ability ability)
            : base(ability)
        {
        }

        public float DamageDuration
        {
            get
            {
                return this.Duration;
            }
        }

        public bool HasInitialDamage { get; } = true;

        public float RawTickDamage
        {
            get
            {
                return this.Ability.GetAbilitySpecialData("damage");
            }
        }

        public string TargetModifierName { get; } = "modifier_cold_feet";

        public float TickRate { get; } = 1f;

        protected override float RawDamage
        {
            get
            {
                return this.RawTickDamage;
            }
        }

        public float GetTickDamage(params Unit[] targets)
        {
            var damage = this.RawTickDamage;
            var amplify = this.Ability.SpellAmplification();
            var reduction = 0.0f;
            if (targets.Any())
            {
                reduction = this.Ability.GetDamageReduction(targets.First(), this.DamageType);
            }

            return DamageHelpers.GetSpellDamage(damage, amplify, reduction);
        }

        public float GetTotalDamage(params Unit[] targets)
        {
            return this.GetTickDamage(targets) * (this.DamageDuration / this.TickRate);
        }
    }
}
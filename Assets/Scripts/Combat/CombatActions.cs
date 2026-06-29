using UnityEngine;
using VisionsOfGenesis.Data;
using VisionsOfGenesis.Home;

namespace VisionsOfGenesis.Combat
{
    public static class CombatActions
    {

        public static int Attack(UnitComponent source, UnitComponent target)
            => Attack(source, target, out _);

        public static int Attack(UnitComponent source, UnitComponent target, out float affinity)
        {
            affinity = 1f;
            if (source == null || target == null) return 0;

            affinity = Elements.Affinity(source.ElementType, target.ElementType);

            int baseDamage = Mathf.Max(1, source.Attack - target.Defense / 2);
            int variance = Mathf.RoundToInt(baseDamage * Random.Range(-0.1f, 0.1f));
            int damage = Mathf.Max(1, Mathf.RoundToInt((baseDamage + variance) * affinity));

            target.TakeDamage(damage);
            return damage;
        }

        public static void Defend(UnitComponent source)
        {
            if (source == null) return;
            source.SetDefending(true);
        }


        public static int UseSkill(UnitComponent source, SkillData skill, UnitComponent enemyTarget)
            => UseSkill(source, skill, enemyTarget, out _);

        public static int UseSkill(UnitComponent source, SkillData skill, UnitComponent enemyTarget, out float affinity)
        {
            affinity = 1f;
            if (source == null || skill == null) return 0;
            if (!source.SpendMP(skill.mpCost))
            {
                Debug.Log($"{source.data.unitName} doesn't have enough MP for {skill.skillName}.");
                return 0;
            }

            switch (skill.type)
            {
                case SkillType.Damage:
                {
                    if (enemyTarget == null) return 0;
                    affinity = Elements.Affinity(skill.element, enemyTarget.ElementType);
                    int preMitigation = Mathf.Max(1, Mathf.RoundToInt(source.Attack * skill.power) - enemyTarget.Defense / 2);
                    int damage = Mathf.Max(1, Mathf.RoundToInt(preMitigation * affinity));
                    enemyTarget.TakeDamage(damage);
                    return damage;
                }
                case SkillType.Heal:
                {
                    int amount = Mathf.RoundToInt(skill.power);
                    source.Heal(amount);
                    return amount;
                }
            }
            return 0;
        }


        public static int UseItem(UnitComponent source, ItemData item, UnitComponent target)
        {
            if (source == null || item == null || target == null) return 0;
            if (!Inventory.SpendItem(item))
            {
                Debug.Log($"No {item.itemName} left in the inventory.");
                return 0;
            }

            switch (item.effect)
            {
                case ItemEffect.HealHP:
                    target.Heal(item.amount);
                    return item.amount;
                case ItemEffect.HealMP:
                    target.RestoreMP(item.amount);
                    return item.amount;
            }
            return 0;
        }
    }
}

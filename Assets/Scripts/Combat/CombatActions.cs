using UnityEngine;
using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Combat
{
    public static class CombatActions
    {

        public static int Attack(UnitComponent source, UnitComponent target)
        {
            if (source == null || target == null) return 0;

            int baseDamage = Mathf.Max(1, source.Attack - target.Defense / 2);
            int variance = Mathf.RoundToInt(baseDamage * Random.Range(-0.1f, 0.1f));
            int damage = Mathf.Max(1, baseDamage + variance);

            target.TakeDamage(damage);
            return damage;
        }

        public static void Defend(UnitComponent source)
        {
            if (source == null) return;
            source.SetDefending(true);
        }


        public static int UseSkill(UnitComponent source, SkillData skill, UnitComponent enemyTarget)
        {
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
                    int baseDamage = Mathf.RoundToInt(source.Attack * skill.power);
                    int damage = Mathf.Max(1, baseDamage - enemyTarget.Defense / 2);
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


        public static int UseItem(UnitComponent source, ItemData item)
        {
            if (source == null || item == null) return 0;

            switch (item.effect)
            {
                case ItemEffect.HealHP:
                    source.Heal(item.amount);
                    return item.amount;
                case ItemEffect.HealMP:
                    return 0;
            }
            return 0;
        }
    }
}

using UnityEngine;

namespace VisionsOfGenesis.Data
{
    public enum SkillType
    {
        Damage,
        Heal
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "Visions of Genesis/Skill Data", order = 1)]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillName = "New Skill";
        [TextArea] public string description;

        [Header("Mechanics")]
        public SkillType type = SkillType.Damage;
        public int mpCost = 5;

        [Tooltip("For Damage: multiplier applied to caster.ATK. For Heal: raw HP restored.")]
        public float power = 1.5f;

        public Element element = Element.Neutral;
    }
}

using System;
using UnityEngine;
using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Combat
{
    public class UnitComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The ScriptableObject that defines this unit's base stats and loadout.")]
        public UnitData data;

        [Tooltip("True for party members, false for enemies. Used by the BattleManager and CombatActions.")]
        public bool isPlayer = true;

        public int currentHP { get; private set; }
        public int currentMP { get; private set; }
        public bool isDefending { get; private set; }
        public bool isDead => currentHP <= 0;

        // Effective stats: base stats from UnitData scaled by Level.
        public int Level { get; private set; } = 1;
        public int MaxHP { get; private set; }
        public int MaxMP { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }

        public event Action OnStateChanged;

        public event Action<int> OnDamaged;

        public event Action<int> OnHealed;

        public event Action OnDied;

        private bool _initialized;

        private void Awake()
        {
            if (_initialized) return;
            if (data == null) return;
            ApplyLevelScaling();
            currentHP = MaxHP;
            currentMP = MaxMP;
            isDefending = false;
            _initialized = true;
        }

        public void Initialize(UnitData newData, bool asPlayer, int level = 1)
        {
            data = newData;
            isPlayer = asPlayer;
            Level = Mathf.Max(1, level);
            if (data != null)
            {
                ApplyLevelScaling();
                currentHP = MaxHP;
                currentMP = MaxMP;
            }
            isDefending = false;
            _initialized = true;
            OnStateChanged?.Invoke();
        }

        private void ApplyLevelScaling()
        {
            float hpMul  = 1f + 0.10f * (Level - 1);
            float mpMul  = 1f + 0.05f * (Level - 1);
            float atkMul = 1f + 0.08f * (Level - 1);

            MaxHP   = Mathf.Max(1, Mathf.RoundToInt(data.maxHP * hpMul));
            MaxMP   = Mathf.Max(0, Mathf.RoundToInt(data.maxMP * mpMul));
            Attack  = Mathf.Max(1, Mathf.RoundToInt(data.attack * atkMul));
            Defense = Mathf.Max(0, Mathf.RoundToInt(data.defense * atkMul));
        }


        public void TakeDamage(int rawAmount)
        {
            int finalAmount = isDefending ? Mathf.Max(1, rawAmount / 2) : rawAmount;
            currentHP = Mathf.Max(0, currentHP - finalAmount);

            OnDamaged?.Invoke(finalAmount);
            OnStateChanged?.Invoke();

            if (currentHP == 0)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            int before = currentHP;
            currentHP = Mathf.Min(MaxHP, currentHP + amount);
            int healed = currentHP - before;

            if (healed > 0)
            {
                OnHealed?.Invoke(healed);
                OnStateChanged?.Invoke();
            }
        }

        public bool SpendMP(int cost)
        {
            if (currentMP < cost) return false;
            currentMP -= cost;
            OnStateChanged?.Invoke();
            return true;
        }

        public void SetDefending(bool value)
        {
            if (isDefending == value) return;
            isDefending = value;
            OnStateChanged?.Invoke();
        }

        public void ClearTurnFlags()
        {
            SetDefending(false);
        }
    }
}

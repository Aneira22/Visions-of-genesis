using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VisionsOfGenesis.Data;
using VisionsOfGenesis.Home;

namespace VisionsOfGenesis.Combat
{
    public enum BattleState
    {
        Intro,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }

    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Participants")]
        public UnitComponent[] playerUnits;
        public UnitComponent[] enemyUnits;

        [Header("Pacing")]
        [Tooltip("Seconds between automated steps (enemy actions, victory check, etc.).")]
        public float stepDelay = 0.6f;

        [Header("UI Hooks")]
        public UI.UIManager uiManager;

        public BattleState State { get; private set; } = BattleState.Intro;

        public int RoundNumber { get; private set; }

        public UnitComponent CurrentTarget { get; private set; }

        // Fired once when the battle ends; true = victory, false = defeat.
        public System.Action<bool> OnBattleEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _lastActions = new PlayerAction[playerUnits.Length];
            _hasLastAction = new bool[playerUnits.Length];
        }


        public void BeginBattle()
        {
            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (true)
            {
                RoundNumber++;
                State = BattleState.PlayerTurn;
                EnsureValidTarget();
                BeginPlayerRound();
                uiManager?.SetRoundNumber(RoundNumber);

                while (!AllLivingPlayersActed())
                {
                    if (CheckEndConditions()) yield break;
                    yield return null;
                }

                uiManager?.HighlightActivePortrait(-1);
                if (CheckEndConditions()) yield break;
                yield return new WaitForSeconds(stepDelay);

                State = BattleState.EnemyTurn;
                foreach (var enemy in enemyUnits)
                {
                    if (enemy == null || enemy.isDead) continue;
                    enemy.ClearTurnFlags();

                    UnitComponent target = PickRandomLivingPlayer();
                    if (target == null) break;

                    int dmg = CombatActions.Attack(enemy, target, out float enemyAff);
                    uiManager?.ShowFloatingNumber(target, dmg, false);
                    ShowAffinity(target, enemyAff);

                    if (CheckEndConditions()) yield break;
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }

        private void BeginPlayerRound()
        {
            _hasActed = new bool[playerUnits.Length];
            for (int i = 0; i < playerUnits.Length; i++)
            {
                UnitComponent p = playerUnits[i];
                if (p == null || p.isDead)
                {
                    _hasActed[i] = true;
                    uiManager?.SetPortraitHighlight(i, false);
                    continue;
                }
                p.ClearTurnFlags();
                uiManager?.SetPortraitHighlight(i, true);
            }
        }

        private bool AllLivingPlayersActed()
        {
            if (_hasActed == null) return false;
            for (int i = 0; i < playerUnits.Length; i++)
            {
                UnitComponent p = playerUnits[i];
                if (p == null || p.isDead) continue;
                if (!_hasActed[i]) return false;
            }
            return true;
        }


        public bool IsAwaitingActionFrom(int playerIndex)
        {
            if (State != BattleState.PlayerTurn) return false;
            if (_hasActed == null) return false;
            if (playerIndex < 0 || playerIndex >= playerUnits.Length) return false;
            UnitComponent p = playerUnits[playerIndex];
            if (p == null || p.isDead) return false;
            return !_hasActed[playerIndex];
        }

        public void QueuePlayerAction(int playerIndex, PlayerAction action)
        {
            if (!IsAwaitingActionFrom(playerIndex)) return;

            UnitComponent source = playerUnits[playerIndex];
            EnsureValidTarget();
            UnitComponent enemy = CurrentTarget;

            if (action.kind == PlayerActionKind.Skill &&
                (action.skill == null || source.currentMP < action.skill.mpCost))
            {
                uiManager?.ShowFloatingText(source, "Sin MP");
                return;
            }

            if (action.kind == PlayerActionKind.Item &&
                (action.item == null || Inventory.GetItemCount(action.item) <= 0))
            {
                uiManager?.ShowFloatingText(source, "Sin items");
                return;
            }

            switch (action.kind)
            {
                case PlayerActionKind.Attack:
                {
                    int dmg = CombatActions.Attack(source, enemy, out float aff);
                    uiManager?.ShowFloatingNumber(enemy, dmg, false);
                    ShowAffinity(enemy, aff);
                    break;
                }
                case PlayerActionKind.Defend:
                {
                    CombatActions.Defend(source);
                    uiManager?.ShowFloatingText(source, "DEFEND");
                    break;
                }
                case PlayerActionKind.Skill:
                {
                    int amount = CombatActions.UseSkill(source, action.skill, enemy, out float aff);
                    if (amount > 0)
                    {
                        bool isHeal = action.skill.type == SkillType.Heal;
                        UnitComponent target = isHeal ? source : enemy;
                        uiManager?.ShowFloatingNumber(target, amount, isHeal);
                        if (!isHeal) ShowAffinity(enemy, aff);
                    }
                    break;
                }
                case PlayerActionKind.Item:
                {
                    UnitComponent itemTarget = action.target ?? source;
                    int amount = CombatActions.UseItem(source, action.item, itemTarget);
                    if (amount > 0)
                    {
                        if (action.item.effect == ItemEffect.HealMP)
                            uiManager?.ShowFloatingText(itemTarget, "+" + amount + " MP");
                        else
                            uiManager?.ShowFloatingNumber(itemTarget, amount, true);
                    }
                    break;
                }
            }

            _hasActed[playerIndex] = true;

            if (_lastActions != null)
            {
                _lastActions[playerIndex] = action;
                _hasLastAction[playerIndex] = true;
            }

            uiManager?.SetPortraitHighlight(playerIndex, false);

            EnsureValidTarget();
            CheckEndConditions();
        }

        public void RepeatLastRound()
        {
            if (State != BattleState.PlayerTurn) return;
            if (_lastActions == null || _hasLastAction == null) return;

            for (int i = 0; i < playerUnits.Length; i++)
            {
                if (!IsAwaitingActionFrom(i)) continue;
                if (!_hasLastAction[i]) continue;
                QueuePlayerAction(i, _lastActions[i]);
            }
        }


        private bool[] _hasActed;

        private PlayerAction[] _lastActions;
        private bool[] _hasLastAction;

        private UnitComponent PickRandomLivingPlayer()
        {
            var alive = new List<UnitComponent>();
            foreach (var p in playerUnits) if (p != null && !p.isDead) alive.Add(p);
            if (alive.Count == 0) return null;
            return alive[Random.Range(0, alive.Count)];
        }

        private UnitComponent PickFirstLivingEnemy()
        {
            foreach (var e in enemyUnits) if (e != null && !e.isDead) return e;
            return null;
        }

        public void SetTarget(UnitComponent enemy)
        {
            if (enemy == null || enemy.isDead) return;

            bool isEnemy = false;
            foreach (var e in enemyUnits) if (e == enemy) { isEnemy = true; break; }
            if (!isEnemy) return;

            CurrentTarget = enemy;
            uiManager?.SetTargetEnemy(enemy);
        }

        private void EnsureValidTarget()
        {
            if (CurrentTarget != null && !CurrentTarget.isDead) return;
            CurrentTarget = PickFirstLivingEnemy();
            uiManager?.SetTargetEnemy(CurrentTarget);
        }

        private bool CheckEndConditions()
        {
            if (State == BattleState.Victory || State == BattleState.Defeat) return true;

            bool anyEnemyAlive = false;
            foreach (var e in enemyUnits) if (e != null && !e.isDead) { anyEnemyAlive = true; break; }
            if (!anyEnemyAlive) { EnterVictory(); return true; }

            bool anyPlayerAlive = false;
            foreach (var p in playerUnits) if (p != null && !p.isDead) { anyPlayerAlive = true; break; }
            if (!anyPlayerAlive) { EnterDefeat(); return true; }

            return false;
        }

        private void EnterVictory()
        {
            State = BattleState.Victory;
            uiManager?.HighlightActivePortrait(-1);
            uiManager?.ShowEndScreen("VICTORIA");
            OnBattleEnded?.Invoke(true);
        }

        private void EnterDefeat()
        {
            State = BattleState.Defeat;
            uiManager?.HighlightActivePortrait(-1);
            uiManager?.ShowEndScreen("DERROTA");
            OnBattleEnded?.Invoke(false);
        }

        // Floating "¡Débil!" / "Resiste" feedback, only when the hit was non-neutral.
        private void ShowAffinity(UnitComponent target, float affinity)
        {
            if (uiManager == null || target == null) return;
            string label = Elements.Label(affinity);
            if (!string.IsNullOrEmpty(label))
                uiManager.ShowFloatingText(target, label);
        }
    }


    public enum PlayerActionKind { Attack, Defend, Skill, Item }

    public struct PlayerAction
    {
        public PlayerActionKind kind;
        public SkillData skill;
        public ItemData item;
        public UnitComponent target;

        public static PlayerAction MakeAttack()                              => new PlayerAction { kind = PlayerActionKind.Attack };
        public static PlayerAction MakeDefend()                              => new PlayerAction { kind = PlayerActionKind.Defend };
        public static PlayerAction MakeSkill(SkillData s)                    => new PlayerAction { kind = PlayerActionKind.Skill, skill = s };
        public static PlayerAction MakeItem(ItemData i, UnitComponent t)     => new PlayerAction { kind = PlayerActionKind.Item, item = i, target = t };
    }
}

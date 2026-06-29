using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VisionsOfGenesis.Combat;
using VisionsOfGenesis.Data;
using VisionsOfGenesis.Home;
using VisionsOfGenesis.InputSystem;

namespace VisionsOfGenesis.UI
{
    public class PortraitPanel : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Which slot this panel represents in BattleManager.playerUnits.")]
        public int playerIndex;
        public UnitComponent boundUnit;

        [Header("Main View References")]
        public GameObject mainView;
        public Image portraitImage;
        public Image hpBarFill;
        public Image mpBarFill;
        public Text  nameLabel;
        public Text  hpLabel;
        public Text  mpLabel;
        [Tooltip("Optional. Shows the currently armed action, e.g. 'Attack' / 'Genesis Slash'. Tap to confirm.")]
        public Text  selectedActionLabel;
        public GameObject activeHighlight;

        [Header("Sub-Menu View References")]
        public GameObject subMenuView;
        public Transform  subMenuButtonsRoot;
        public Button     subMenuButtonPrefab;

        [Header("Input")]
        public SwipeInputHandler swipeHandler;

        private enum SubMenu { None, Skills, Items, ItemTarget }
        private SubMenu _openMenu = SubMenu.None;
        private ItemData _pendingItem;

        private PlayerAction _armedAction;
        private string _armedLabel = "Attack";

        private static readonly List<PortraitPanel> AllPanels = new List<PortraitPanel>();

        private void Awake()
        {
            if (!AllPanels.Contains(this)) AllPanels.Add(this);

            if (swipeHandler != null)
                swipeHandler.OnSwipe += HandleSwipe;

            if (boundUnit != null)
                boundUnit.OnStateChanged += Refresh;

            _armedAction = PlayerAction.MakeAttack();
        }

        private void Start()
        {
            ShowMainView();
            Refresh();
            UpdateSelectedActionLabel();
        }

        private void OnDestroy()
        {
            AllPanels.Remove(this);
            if (swipeHandler != null)
                swipeHandler.OnSwipe -= HandleSwipe;
            if (boundUnit != null)
                boundUnit.OnStateChanged -= Refresh;
        }


        private void HandleSwipe(SwipeDirection dir)
        {
            var bm = BattleManager.Instance;
            if (bm == null || !bm.IsAwaitingActionFrom(playerIndex)) return;

            switch (dir)
            {
                case SwipeDirection.Tap:
                    if (_openMenu != SubMenu.None) return;
                    ExecuteArmedAction();
                    break;

                case SwipeDirection.Up:
                    ShowMainView();
                    Arm(PlayerAction.MakeAttack(), "Attack");
                    break;

                case SwipeDirection.Down:
                    ShowMainView();
                    Arm(PlayerAction.MakeDefend(), "Defend");
                    break;

                case SwipeDirection.Right:
                    OpenSkillsMenu();
                    break;

                case SwipeDirection.Left:
                    OpenItemsMenu();
                    break;
            }
        }

        private void Arm(PlayerAction action, string label)
        {
            _armedAction = action;
            _armedLabel = label;
            UpdateSelectedActionLabel();
        }

        private void ExecuteArmedAction()
        {
            ShowMainView();
            BattleManager.Instance?.QueuePlayerAction(playerIndex, _armedAction);
        }


        private void OpenSkillsMenu()
        {
            _openMenu = SubMenu.Skills;
            mainView.SetActive(false);
            subMenuView.SetActive(true);
            SetOthersMainViewVisible(false);

            ClearSubMenuButtons();
            if (boundUnit?.data?.skills == null) return;

            foreach (var skill in boundUnit.data.skills)
            {
                if (skill == null) continue;
                SkillData captured = skill;
                SpawnSubMenuButton($"{skill.skillName}\n<size=14>MP {skill.mpCost}</size>", () =>
                {
                    Arm(PlayerAction.MakeSkill(captured), captured.skillName);
                    ShowMainView();
                });
            }
        }

        private void OpenItemsMenu()
        {
            _openMenu = SubMenu.Items;
            mainView.SetActive(false);
            subMenuView.SetActive(true);
            SetOthersMainViewVisible(false);
            ClearSubMenuButtons();

            var usable = BattleBag.GetUsable();

            if (usable.Count == 0)
            {
                if (subMenuButtonsRoot != null)
                {
                    var empty = UIFactory.CreateText("NoItems", subMenuButtonsRoot, UIFactory.DefaultFont(),
                        "Sin items\nConfigura tu mochila en Equipo", 18,
                        new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter);
                    UIFactory.Stretch(empty.rectTransform);
                }
                return;
            }

            foreach (var item in usable)
            {
                int count = Inventory.GetItemCount(item);
                ItemData captured = item;
                SpawnSubMenuButton(captured.itemName + "\n<size=14>x" + count + "</size>",
                    () => OpenItemTargetMenu(captured));
            }
        }

        private void OpenItemTargetMenu(ItemData item)
        {
            _openMenu = SubMenu.ItemTarget;
            _pendingItem = item;
            ClearSubMenuButtons();

            var bm = BattleManager.Instance;
            if (bm == null) { ShowMainView(); return; }

            foreach (var unit in bm.playerUnits)
            {
                if (unit == null || unit.isDead) continue;
                UnitComponent captured = unit;
                string label = (unit.data?.unitName ?? "Aliado") +
                               "\n<size=14>HP " + unit.currentHP + "/" + unit.MaxHP + "</size>";
                SpawnSubMenuButton(label, () =>
                {
                    Arm(PlayerAction.MakeItem(item, captured), item.itemName);
                    ShowMainView();
                });
            }

            foreach (var unit in bm.enemyUnits)
            {
                if (unit == null || unit.isDead) continue;
                UnitComponent captured = unit;
                string label = (unit.data?.unitName ?? "Enemigo") +
                               "\n<size=14>HP " + unit.currentHP + "/" + unit.MaxHP + "</size>";
                SpawnSubMenuButton(label, () =>
                {
                    Arm(PlayerAction.MakeItem(item, captured), item.itemName);
                    ShowMainView();
                });
            }

            SpawnSubMenuButton("< Volver", () => OpenItemsMenu());
        }

        private void ShowMainView()
        {
            _openMenu = SubMenu.None;
            mainView.SetActive(true);
            subMenuView.SetActive(false);
            ClearSubMenuButtons();
            SetOthersMainViewVisible(true);
        }

        private void SetOthersMainViewVisible(bool visible)
        {
            foreach (var panel in AllPanels)
            {
                if (panel == null || panel == this) continue;
                if (panel.mainView != null) panel.mainView.SetActive(visible);
            }
        }

        private void ClearSubMenuButtons()
        {
            if (subMenuButtonsRoot == null) return;
            for (int i = subMenuButtonsRoot.childCount - 1; i >= 0; i--)
                Destroy(subMenuButtonsRoot.GetChild(i).gameObject);
        }

        private void SpawnSubMenuButton(string label, System.Action onClick)
        {
            if (subMenuButtonPrefab == null || subMenuButtonsRoot == null) return;
            Button b = Instantiate(subMenuButtonPrefab, subMenuButtonsRoot);
            var text = b.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            b.onClick.AddListener(() => onClick?.Invoke());
        }


        public void SetActiveHighlight(bool on)
        {
            if (activeHighlight != null) activeHighlight.SetActive(on);

            if (on)
            {
                ShowMainView();
                Arm(PlayerAction.MakeAttack(), "Attack");
            }
        }

        private void UpdateSelectedActionLabel()
        {
            if (selectedActionLabel != null)
                selectedActionLabel.text = $"â–º {_armedLabel}";
        }

        private void Refresh()
        {
            if (boundUnit == null || boundUnit.data == null) return;

            if (portraitImage != null && boundUnit.data.portrait != null)
                portraitImage.sprite = boundUnit.data.portrait;
            if (nameLabel != null)
                nameLabel.text = $"{boundUnit.data.unitName}  Nv {boundUnit.Level}";

            if (hpBarFill != null)
                hpBarFill.fillAmount = (float)boundUnit.currentHP / Mathf.Max(1, boundUnit.MaxHP);
            if (hpLabel != null)
                hpLabel.text = $"HP {boundUnit.currentHP}/{boundUnit.MaxHP}";

            if (mpBarFill != null)
                mpBarFill.fillAmount = (float)boundUnit.currentMP / Mathf.Max(1, boundUnit.MaxMP);
            if (mpLabel != null)
                mpLabel.text = $"MP {boundUnit.currentMP}/{boundUnit.MaxMP}";
        }
    }
}

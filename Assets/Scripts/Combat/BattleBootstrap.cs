using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VisionsOfGenesis.Data;
using VisionsOfGenesis.UI;
using VisionsOfGenesis.Home;

namespace VisionsOfGenesis.Combat
{
    [DefaultExecutionOrder(-500)]
    public class BattleBootstrap : MonoBehaviour
    {
        private readonly List<HeroInfo> _participants = new List<HeroInfo>();
        private bool _rewardsGranted;
        private bool _exitBuilt;

        private void Awake()
        {
            bool hasContext = TeamState.Initialized && StorySelection.SelectedEntry != null;
            if (!hasContext) return;

            var bm = FindObjectOfType<BattleManager>();
            var ui = FindObjectOfType<UIManager>();
            if (bm == null || ui == null) return;
            if (bm.playerUnits == null || bm.playerUnits.Length == 0) return;
            if (bm.enemyUnits == null || bm.enemyUnits.Length == 0) return;
            if (ui.portraitPanels == null || ui.portraitPanels.Length == 0) return;

            var registry = new Dictionary<string, UnitData>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var u in bm.playerUnits) if (u != null && u.data != null) registry[u.data.unitName] = u.data;
            foreach (var e in bm.enemyUnits) if (e != null && e.data != null) registry[e.data.unitName] = e.data;

            var heroDatas = ResolveHeroes(registry);
            var enemyDatas = ResolveEnemies(registry);
            if (heroDatas.Count == 0 || enemyDatas.Count == 0) return;

            var panelTemplate = ui.portraitPanels[0].gameObject;
            var panelParent = ui.portraitPanels[0].transform.parent;

            var enemyViewGo = GameObject.Find("EnemyView");
            Image enemyViewImg = enemyViewGo != null ? enemyViewGo.GetComponent<Image>() : null;
            Transform field = ui.worldCanvas != null ? ui.worldCanvas.transform : panelParent;

            foreach (var p in ui.portraitPanels) if (p != null) p.gameObject.SetActive(false);
            foreach (var u in bm.playerUnits) if (u != null) u.gameObject.SetActive(false);
            foreach (var e in bm.enemyUnits) if (e != null) e.gameObject.SetActive(false);

            var heroUnits = new List<UnitComponent>();
            var panels = new List<PortraitPanel>();
            for (int k = 0; k < heroDatas.Count; k++)
            {
                var unitGo = new GameObject("Hero_" + heroDatas[k].unitName);
                var unit = unitGo.AddComponent<UnitComponent>();
                unit.Initialize(heroDatas[k], true, _participants[k].level);

                var panel = Instantiate(panelTemplate, panelParent).GetComponent<PortraitPanel>();
                panel.playerIndex = k;
                panel.boundUnit = unit;
                PositionPanel((RectTransform)panel.transform, k, heroDatas.Count);
                panel.gameObject.SetActive(true);

                SpawnHeroVisual(field, heroDatas[k], k, heroDatas.Count);

                heroUnits.Add(unit);
                panels.Add(panel);
            }

            var enemyUnits = new List<UnitComponent>();
            for (int k = 0; k < enemyDatas.Count; k++)
            {
                var enemyGo = new GameObject("Enemy_" + enemyDatas[k].unitName);
                var enemy = enemyGo.AddComponent<UnitComponent>();
                enemy.Initialize(enemyDatas[k], false);
                enemyUnits.Add(enemy);

                var visual = SpawnEnemyVisual(enemyViewImg, field, enemyDatas[k], k, enemyDatas.Count);
                if (visual != null)
                {
                    var tgt = visual.GetComponent<EnemyTarget>();
                    if (tgt == null) tgt = visual.gameObject.AddComponent<EnemyTarget>();
                    tgt.unit = enemy;
                    ui.RegisterEnemyVisual(enemy, visual.rectTransform);
                }
            }

            bm.playerUnits = heroUnits.ToArray();
            bm.enemyUnits = enemyUnits.ToArray();
            ui.Configure(panels.ToArray(), enemyUnits);

            bm.OnBattleEnded += victory => HandleBattleEnded(ui, victory);
        }

        private void HandleBattleEnded(UIManager ui, bool victory)
        {
            BuildExitButton(ui);

            if (!victory || _rewardsGranted) return;
            _rewardsGranted = true;

            var entry = StorySelection.SelectedEntry;
            if (entry == null) return;

            var lines = new List<string>();
            bool firstClear = PlayerProgress.MarkCompleted(entry.id);

            if (entry.heroExp > 0)
            {
                foreach (var hero in _participants)
                {
                    if (hero == null) continue;
                    int gained = HeroLeveling.AddExp(hero, entry.heroExp);
                    string line = hero.name + "  +" + entry.heroExp + " EXP";
                    if (gained > 0) line += "   Nivel " + hero.level + "!";
                    lines.Add(line);
                }
            }

            if (firstClear && entry.playerExp > 0)
            {
                int ranks = PlayerProgress.AddExp(entry.playerExp);
                string line = "Sincronizador  +" + entry.playerExp + " EXP";
                if (ranks > 0) line += "   Rango " + PlayerProgress.Rank + "!";
                lines.Add(line);
            }

            if (entry.goldReward > 0)
            {
                Wallet.AddGold(entry.goldReward);
                lines.Add("Oro  +" + entry.goldReward);
            }

            if (firstClear && entry.crystalReward > 0)
            {
                Wallet.AddCrystals(entry.crystalReward);
                lines.Add("Cristales  +" + entry.crystalReward);
            }

            if (lines.Count > 0 && ui != null)
                ui.AppendEndScreenDetail(string.Join("\n", lines));
        }

        private void BuildExitButton(UIManager ui)
        {
            if (_exitBuilt || ui == null) return;
            _exitBuilt = true;

            Transform parent = null;
            if (ui.endScreenRoot != null) parent = ui.endScreenRoot.transform;
            else if (ui.worldCanvas != null) parent = ui.worldCanvas.transform;
            if (parent == null) return;

            var btn = UIFactory.CreateButton("ContinueButton", parent, UIFactory.DefaultFont(),
                "CONTINUAR", 34,
                new Color(0.49581182f, 0f, 1f, 1f), new Color(0.92941177f, 0.9137255f, 1f, 1f));
            UIFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 90f), new Vector2(420f, 110f));
            btn.transform.SetAsLastSibling();
            btn.onClick.AddListener(() => SceneManager.LoadScene("Home"));
        }

        private List<UnitData> ResolveHeroes(Dictionary<string, UnitData> registry)
        {
            var list = new List<UnitData>();
            _participants.Clear();
            if (TeamState.Party == null) return list;
            foreach (var h in TeamState.Party)
            {
                if (h == null) continue;
                if (registry.TryGetValue(h.name, out var ud) && ud != null)
                {
                    list.Add(ud);
                    _participants.Add(h);
                }
            }
            return list;
        }

        private List<UnitData> ResolveEnemies(Dictionary<string, UnitData> registry)
        {
            var list = new List<UnitData>();
            var entry = StorySelection.SelectedEntry;
            if (entry == null || entry.enemies == null) return list;
            foreach (var spawn in entry.enemies)
            {
                if (spawn == null) continue;
                if (!registry.TryGetValue(spawn.unitName, out var ud) || ud == null) continue;
                for (int c = 0; c < Mathf.Max(1, spawn.count); c++) list.Add(ud);
            }
            return list;
        }

        private Image SpawnEnemyVisual(Image template, Transform field, UnitData data, int index, int count)
        {
            if (template == null) return null;

            Image img;
            if (index == 0)
            {
                img = template;
            }
            else
            {
                var clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = "EnemyView_" + index;
                img = clone.GetComponent<Image>();
            }
            if (img == null) return null;

            if (data.battleSprite != null) img.sprite = data.battleSprite;
            img.preserveAspect = true;

            if (field != null) img.transform.SetParent(field, false);

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 220f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(-240f - index * 36f, 280f - index * 110f);
            rt.SetAsFirstSibling();

            return img;
        }

        private void SpawnHeroVisual(Transform field, UnitData data, int index, int count)
        {
            if (field == null || data == null) return;

            var img = UIFactory.CreateImage("HeroSprite_" + data.unitName, field,
                new Color(0.2f, 0.16f, 0.34f, 0.55f));
            img.raycastTarget = false;

            UIFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(240f + index * 36f, 280f - index * 110f), new Vector2(200f, 260f));
            img.rectTransform.SetAsFirstSibling();

            if (data.battleSprite != null)
            {
                img.sprite = data.battleSprite;
                img.color = Color.white;
                img.preserveAspect = true;
                return;
            }

            var label = UIFactory.CreateText("Name", img.transform, UIFactory.DefaultFont(),
                data.unitName + "\n<size=18>(sprite)</size>", 26,
                new Color(0.92941177f, 0.9137255f, 1f, 1f), TextAnchor.MiddleCenter);
            UIFactory.Stretch(label.rectTransform);
        }

        private void PositionPanel(RectTransform rt, int index, int count)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            float slot = Mathf.Min(360f, 1040f / Mathf.Max(1, count));
            float x = (index - (count - 1) / 2f) * slot;
            rt.anchoredPosition = new Vector2(x, 24f);

            float scale = count <= 2 ? 1f : Mathf.Clamp(2.2f / count + 0.25f, 0.55f, 1f);
            rt.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VisionsOfGenesis.Combat;

namespace VisionsOfGenesis.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Portraits")]
        public PortraitPanel[] portraitPanels;

        [Header("Enemy")]
        public UnitComponent enemyUnit;
        public Image enemyHpFill;
        public Text  enemyHpLabel;
        public Text  enemyNameLabel;

        [Header("Floating numbers")]
        public Canvas worldCanvas;
        public Text   floatingNumberPrefab;
        public float  floatingLifetime = 1.0f;
        public float  floatingRise = 60f;

        [Header("End Screen")]
        public GameObject endScreenRoot;
        public Text       endScreenLabel;

        private bool _enemyBound;

        private void Awake()
        {
            if (!_enemyBound && enemyUnit != null)
            {
                enemyUnit.OnStateChanged += RefreshEnemy;
                _enemyBound = true;
            }
        }

        private void Start()
        {
            if (endScreenRoot != null) endScreenRoot.SetActive(false);
            RefreshEnemy();
            CreateRepeatButton();
        }

        private void OnDestroy()
        {
            if (enemyUnit != null)
                enemyUnit.OnStateChanged -= RefreshEnemy;
        }


        public void HighlightActivePortrait(int index)
        {
            if (portraitPanels == null) return;
            for (int i = 0; i < portraitPanels.Length; i++)
            {
                if (portraitPanels[i] == null) continue;
                portraitPanels[i].SetActiveHighlight(i == index);
            }
        }

        public void SetPortraitHighlight(int index, bool on)
        {
            if (portraitPanels == null) return;
            if (index < 0 || index >= portraitPanels.Length) return;
            if (portraitPanels[index] == null) return;
            portraitPanels[index].SetActiveHighlight(on);
        }

        public void ShowFloatingNumber(UnitComponent target, int amount, bool isHeal)
        {
            string text = isHeal ? $"+{amount}" : amount.ToString();
            Color color = isHeal ? new Color(0.5f, 1f, 0.5f) : Color.white;
            ShowFloatingTextInternal(target, text, color);
        }

        public void ShowFloatingText(UnitComponent target, string text)
        {
            ShowFloatingTextInternal(target, text, Color.yellow);
        }

        private void ShowFloatingTextInternal(UnitComponent target, string text, Color color)
        {
            if (floatingNumberPrefab == null || worldCanvas == null || target == null) return;

            Text instance = Instantiate(floatingNumberPrefab, worldCanvas.transform);
            instance.text = text;
            instance.color = color;
            instance.transform.SetAsLastSibling();

            RectTransform anchor = GetUiAnchorFor(target);
            if (anchor != null)
                instance.rectTransform.position = anchor.position;
            else
                instance.rectTransform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            StartCoroutine(AnimateFloating(instance));
        }

        private RectTransform GetUiAnchorFor(UnitComponent target)
        {
            if (target != null && _enemyAnchors.TryGetValue(target, out var enemyAnchor) && enemyAnchor != null)
                return enemyAnchor;

            if (target == enemyUnit && enemyHpFill != null)
                return enemyHpFill.rectTransform;

            if (portraitPanels != null)
            {
                foreach (var panel in portraitPanels)
                {
                    if (panel != null && panel.boundUnit == target)
                        return panel.transform as RectTransform;
                }
            }
            return null;
        }

        private IEnumerator AnimateFloating(Text t)
        {
            float elapsed = 0f;
            Vector3 startPos = t.rectTransform.position;
            Color startColor = t.color;

            const float fadeStart = 0.6f;

            while (elapsed < floatingLifetime)
            {
                elapsed += Time.deltaTime;
                float n = elapsed / floatingLifetime;

                t.rectTransform.position = startPos + Vector3.up * (floatingRise * n);

                float alpha = n < fadeStart ? 1f : 1f - (n - fadeStart) / (1f - fadeStart);
                Color c = startColor; c.a = alpha; t.color = c;

                yield return null;
            }
            Destroy(t.gameObject);
        }

        private void RefreshEnemy()
        {
            if (enemyUnit == null || enemyUnit.data == null) return;
            if (enemyNameLabel != null) enemyNameLabel.text = enemyUnit.data.unitName;
            if (enemyHpFill != null)
                enemyHpFill.fillAmount = (float)enemyUnit.currentHP / Mathf.Max(1, enemyUnit.MaxHP);
            if (enemyHpLabel != null)
                enemyHpLabel.text = $"HP {enemyUnit.currentHP}/{enemyUnit.MaxHP}";
        }

        private readonly Dictionary<UnitComponent, RectTransform> _enemyAnchors =
            new Dictionary<UnitComponent, RectTransform>();
        private Text _targetMarker;

        public void Configure(PortraitPanel[] panels, List<UnitComponent> enemies)
        {
            if (panels != null) portraitPanels = panels;
            if (enemies == null || enemies.Count == 0) return;

            UnitComponent first = null;
            foreach (var e in enemies)
                if (e != null && !e.isDead) { first = e; break; }

            SetTargetEnemy(first != null ? first : enemies[0]);
        }

        public void RegisterEnemyVisual(UnitComponent enemy, RectTransform visual)
        {
            if (enemy == null || visual == null) return;
            _enemyAnchors[enemy] = visual;
            enemy.OnStateChanged += () =>
            {
                if (enemy.isDead && visual != null) visual.gameObject.SetActive(false);
            };
        }

        public void SetTargetEnemy(UnitComponent enemy)
        {
            if (_enemyBound && enemyUnit != null)
                enemyUnit.OnStateChanged -= RefreshEnemy;

            enemyUnit = enemy;

            if (enemyUnit != null)
            {
                enemyUnit.OnStateChanged += RefreshEnemy;
                _enemyBound = true;
            }

            RefreshEnemy();
            MoveTargetMarker();
        }

        private void MoveTargetMarker()
        {
            if (worldCanvas == null) return;

            if (_targetMarker == null)
            {
                Font font = null;
                if (enemyHpLabel != null) font = enemyHpLabel.font;
                else if (floatingNumberPrefab != null) font = floatingNumberPrefab.font;

                var go = new GameObject("TargetMarker",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                _targetMarker = go.GetComponent<Text>();
                if (font != null) _targetMarker.font = font;
                _targetMarker.text = "▼";
                _targetMarker.fontSize = 48;
                _targetMarker.fontStyle = FontStyle.Bold;
                _targetMarker.alignment = TextAnchor.MiddleCenter;
                _targetMarker.horizontalOverflow = HorizontalWrapMode.Overflow;
                _targetMarker.verticalOverflow = VerticalWrapMode.Overflow;
                _targetMarker.color = new Color(1f, 0.9878479f, 0f, 1f);
                _targetMarker.raycastTarget = false;
            }

            if (enemyUnit != null && _enemyAnchors.TryGetValue(enemyUnit, out var visual) && visual != null)
            {
                _targetMarker.gameObject.SetActive(true);
                var rt = _targetMarker.rectTransform;
                rt.SetParent(visual, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 12f);
                rt.sizeDelta = new Vector2(80f, 60f);
                rt.SetAsLastSibling();
            }
            else
            {
                _targetMarker.gameObject.SetActive(false);
            }
        }

        public void ShowEndScreen(string label)
        {
            if (endScreenRoot != null) endScreenRoot.SetActive(true);
            if (endScreenLabel != null) endScreenLabel.text = label;
            if (_repeatButton != null) _repeatButton.gameObject.SetActive(false);
        }

        private Text _endDetail;

        public void AppendEndScreenDetail(string detail)
        {
            if (endScreenRoot == null || string.IsNullOrEmpty(detail)) return;

            if (_endDetail == null)
            {
                Font font = null;
                if (enemyHpLabel != null) font = enemyHpLabel.font;
                else if (floatingNumberPrefab != null) font = floatingNumberPrefab.font;

                var go = new GameObject("EndDetail",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(endScreenRoot.transform, false);

                _endDetail = go.GetComponent<Text>();
                if (font != null) _endDetail.font = font;
                _endDetail.fontSize = 30;
                _endDetail.alignment = TextAnchor.UpperCenter;
                _endDetail.horizontalOverflow = HorizontalWrapMode.Overflow;
                _endDetail.verticalOverflow = VerticalWrapMode.Overflow;
                _endDetail.color = new Color(0.92941177f, 0.9137255f, 1f, 1f);
                _endDetail.raycastTarget = false;
                _endDetail.text = "";

                var rt = _endDetail.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -120f);
                rt.sizeDelta = new Vector2(900f, 500f);
                go.transform.SetAsLastSibling();
            }

            _endDetail.text += (_endDetail.text.Length > 0 ? "\n" : "") + detail;
        }

        private Text _roundLabel;

        public void SetRoundNumber(int round)
        {
            if (_roundLabel == null) CreateRoundLabel();
            if (_roundLabel != null) _roundLabel.text = $"Ronda {round}";
        }

        private void CreateRoundLabel()
        {
            if (worldCanvas == null) return;

            Font font = null;
            if (enemyHpLabel != null) font = enemyHpLabel.font;
            else if (floatingNumberPrefab != null) font = floatingNumberPrefab.font;

            var go = new GameObject("RoundLabel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(worldCanvas.transform, false);

            _roundLabel = go.GetComponent<Text>();
            if (font != null) _roundLabel.font = font;
            _roundLabel.fontSize = 34;
            _roundLabel.fontStyle = FontStyle.Bold;
            _roundLabel.alignment = TextAnchor.UpperLeft;
            _roundLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _roundLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _roundLabel.color = new Color(0.92941177f, 0.9137255f, 1f, 1f);

            var rt = _roundLabel.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -20f);
            rt.sizeDelta = new Vector2(400f, 60f);
            go.transform.SetAsLastSibling();
        }

        private Button _repeatButton;

        private void CreateRepeatButton()
        {
            if (worldCanvas == null) return;

            Font font = null;
            if (enemyHpLabel != null) font = enemyHpLabel.font;
            else if (floatingNumberPrefab != null) font = floatingNumberPrefab.font;

            var go = new GameObject("RepeatRoundButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(worldCanvas.transform, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.16f, 0.34f, 0.95f);
            _repeatButton = go.GetComponent<Button>();
            _repeatButton.targetGraphic = img;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(340f, 80f);
            rt.anchoredPosition = new Vector2(0f, 20f);

            var txtGo = new GameObject("Text",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.GetComponent<Text>();
            if (font != null) txt.font = font;
            txt.text = "Repetir ronda";
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.color = new Color(0.92941177f, 0.9137255f, 1f, 1f);

            var trt = (RectTransform)txtGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            trt.anchoredPosition = Vector2.zero;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            _repeatButton.onClick.AddListener(() =>
            {
                if (Combat.BattleManager.Instance != null)
                    Combat.BattleManager.Instance.RepeatLastRound();
            });

            go.transform.SetAsLastSibling();
        }
    }
}

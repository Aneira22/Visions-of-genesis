using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VisionsOfGenesis.Data;
using VisionsOfGenesis.InputSystem;

namespace VisionsOfGenesis.Home
{
    public class AlphaHomeController : MonoBehaviour
    {
        [Header("Optional art (drop your PNGs here later)")]
        public Sprite forestMapSprite;
        public Sprite[] wheelIcons;
        public Sprite[] heroPortraits;

        static readonly Color BgDark      = new Color(0.07058824f, 0.0627451f, 0.16470589f, 1f);
        static readonly Color TextLight   = new Color(0.92941177f, 0.9137255f, 1f, 1f);
        static readonly Color Purple      = new Color(0.2f, 0.16f, 0.34f, 1f);
        static readonly Color PurpleVivid = new Color(0.49581182f, 0f, 1f, 1f);
        static readonly Color Cyan        = new Color(0.21176471f, 0.7882353f, 0.9411765f, 1f);
        static readonly Color Green       = new Color(0.24705882f, 0.8235294f, 0.47843137f, 1f);
        static readonly Color Crimson     = new Color(0.9098039f, 0.2901961f, 0.3529412f, 1f);
        static readonly Color GoldColor   = new Color(0.95f, 0.78f, 0.34f, 1f);

        const float WheelSpacing = 380f;

        Canvas _canvas;
        Font _font;
        StoryMap _map;
        StoryZone _currentZone;

        readonly string[] _sections = { "Historia", "Equipo", "Tienda", "Gacha" };
        Color[] _sectionColors;
        int _wheelIndex;

        readonly List<RectTransform> _wheelItems = new List<RectTransform>();
        readonly List<CanvasGroup> _wheelGroups = new List<CanvasGroup>();

        GameObject _wheelScreen;
        GameObject _mapScreen;
        GameObject _battleListScreen;
        GameObject _missionScreen;
        GameObject _comingSoonScreen;
        GameObject _teamScreen;
        GameObject _rosterScreen;
        GameObject _itemsScreen;
        GameObject _bagPickerScreen;
        GameObject _gachaScreen;
        GameObject _mejoraScreen;

        Transform _battleListContent;
        Transform _missionContent;
        Transform _teamContent;
        Transform _rosterContent;
        Transform _itemsContent;
        Transform _bagPickerContent;
        Transform _mejoraContent;
        Text _bagPickerTitle;
        int _bagPickerSlot;
        Text _gachaBalance;
        Text _gachaResult;
        Text _wheelCrystals;
        Text _wheelGold;
        Text _comingSoonTitle;
        Text _comingSoonBalance;
        Text _energyLabel;
        Text _regenLabel;
        int _lastEnergyShown = -1;
        int _lastMaxEnergyShown = -1;
        int _lastRegenShown = -2;
        Text _teamStatus;
        Text _rosterTitle;
        Text _partyLabel;
        int _selectedSlot = -1;
        List<HeroInfo> _owned;

        void Start()
        {
            SaveSystem.LoadIfNeeded();

            _font = UIFactory.DefaultFont();
            _map = StoryDatabase.BuildForestMap();
            _sectionColors = new[] { Cyan, Green, PurpleVivid, Crimson };

            BuildCanvas();
            BuildWheelScreen();
            BuildMapScreen();
            BuildBattleListScreen();
            BuildMissionScreen();
            BuildComingSoonScreen();

            TeamState.EnsureInit();
            Inventory.EnsureInit();
            _owned = TeamState.Owned;
            BuildTeamScreen();
            BuildRosterScreen();
            BuildItemsScreen();
            BuildBagPickerScreen();
            BuildGachaScreen();
            BuildMejoraScreen();

            ShowWheel();
        }

        void Update()
        {
            PlayerProgress.TickRegen();
            RefreshEnergyHud();

            if (_wheelScreen == null || !_wheelScreen.activeSelf) return;

            for (int i = 0; i < _wheelItems.Count; i++)
            {
                float offset = i - _wheelIndex;
                Vector2 targetPos = new Vector2(offset * WheelSpacing, 0f);
                float targetScale = offset == 0f ? 1f : 0.62f;
                float targetAlpha = offset == 0f ? 1f : (Mathf.Abs(offset) <= 1f ? 0.5f : 0f);

                var rt = _wheelItems[i];
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, Time.deltaTime * 12f);
                float s = Mathf.Lerp(rt.localScale.x, targetScale, Time.deltaTime * 12f);
                rt.localScale = new Vector3(s, s, 1f);

                var cg = _wheelGroups[i];
                if (cg != null) cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.deltaTime * 12f);
            }
        }

        void RefreshEnergyHud()
        {
            if (_energyLabel != null &&
                (PlayerProgress.Energy != _lastEnergyShown || PlayerProgress.MaxEnergy != _lastMaxEnergyShown))
            {
                _lastEnergyShown = PlayerProgress.Energy;
                _lastMaxEnergyShown = PlayerProgress.MaxEnergy;
                _energyLabel.text = "Energia: " + _lastEnergyShown + "/" + _lastMaxEnergyShown;
            }

            if (_regenLabel != null)
            {
                int s = PlayerProgress.SecondsToNextRegen();
                if (s != _lastRegenShown)
                {
                    _lastRegenShown = s;
                    _regenLabel.text = s >= 0 ? "+1 energia en " + (s / 60) + ":" + (s % 60).ToString("00") : "";
                }
            }
        }

        void BuildCanvas()
        {
            var go = UIFactory.NewUI("AlphaCanvas", null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
        }

        GameObject MakeScreen(string name, Color background)
        {
            var go = UIFactory.NewUI(name, _canvas.transform);
            UIFactory.Stretch((RectTransform)go.transform);

            var bg = UIFactory.CreateImage("BG", go.transform, background);
            UIFactory.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            return go;
        }

        void AddHeader(Transform parent, string title)
        {
            var t = UIFactory.CreateText("Header", parent, _font, title, 46, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 90f));
            t.fontStyle = FontStyle.Bold;
        }

        void AddBackButton(Transform parent, System.Action onBack)
        {
            var b = UIFactory.CreateButton("Back", parent, _font, "< Volver", 30, Purple, TextLight);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -50f), new Vector2(220f, 90f));
            b.onClick.AddListener(() => onBack());
        }

        void BuildWheelScreen()
        {
            _wheelScreen = MakeScreen("WheelScreen", BgDark);
            var root = _wheelScreen.transform;

            _wheelCrystals = UIFactory.CreateText("Crystals", root, _font, "Cristales: " + Wallet.Crystals, 28, Cyan, TextAnchor.UpperLeft);
            UIFactory.Place(_wheelCrystals.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(400f, 50f));

            _energyLabel = UIFactory.CreateText("Energy", root, _font,
                "Energia: " + PlayerProgress.Energy + "/" + PlayerProgress.MaxEnergy, 28, Green, TextAnchor.UpperRight);
            UIFactory.Place(_energyLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 50f));

            _wheelGold = UIFactory.CreateText("Gold", root, _font, "Oro: " + Wallet.Gold, 28, GoldColor, TextAnchor.UpperRight);
            UIFactory.Place(_wheelGold.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -78f), new Vector2(400f, 50f));

            _regenLabel = UIFactory.CreateText("RegenHint", root, _font, "", 20,
                new Color(0.24705882f, 0.8235294f, 0.47843137f, 0.8f), TextAnchor.UpperRight);
            UIFactory.Place(_regenLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -126f), new Vector2(400f, 36f));

            var rankT = UIFactory.CreateText("Rank", root, _font, "Rango " + PlayerProgress.Rank, 26, TextLight, TextAnchor.UpperLeft);
            UIFactory.Place(rankT.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -82f), new Vector2(400f, 40f));
            rankT.fontStyle = FontStyle.Bold;

            var expBg = UIFactory.CreateImage("RankExpBg", root, new Color(1f, 1f, 1f, 0.12f));
            expBg.raycastTarget = false;
            UIFactory.Place(expBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -126f), new Vector2(320f, 14f));

            bool rankMax = PlayerProgress.Rank >= PlayerProgress.MaxRank;
            int toNext = PlayerProgress.ExpToNext(PlayerProgress.Rank);
            float pct = rankMax ? 1f : Mathf.Clamp01((float)PlayerProgress.Exp / Mathf.Max(1, toNext));

            var expFill = UIFactory.CreateImage("RankExpFill", expBg.transform, Cyan);
            expFill.raycastTarget = false;
            var fillRt = expFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(320f * pct, 0f);

            var expT = UIFactory.CreateText("RankExp", root, _font,
                rankMax ? "EXP MAX" : "EXP " + PlayerProgress.Exp + "/" + toNext,
                20, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft);
            UIFactory.Place(expT.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -146f), new Vector2(400f, 36f));

            var title = UIFactory.CreateText("Title", root, _font, "Visions of Genesis", 58, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1000f, 130f));
            title.fontStyle = FontStyle.Bold;

            var container = UIFactory.NewUI("WheelContainer", root);
            UIFactory.Place((RectTransform)container.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(0f, 0f));

            var surface = UIFactory.CreateImage("WheelInput", root, new Color(1f, 1f, 1f, 0f));
            UIFactory.Place(surface.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(780f, 380f));
            surface.raycastTarget = true;
            var swipe = surface.gameObject.AddComponent<SwipeInputHandler>();
            swipe.swipeThreshold = 40f;
            swipe.OnSwipe += OnWheelSwipe;

            for (int i = 0; i < _sections.Length; i++)
            {
                BuildWheelItem(container.transform, i);
            }

            var leftArrow = UIFactory.CreateButton("Left", root, _font, "<", 54, Purple, TextLight);
            UIFactory.Place((RectTransform)leftArrow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-470f, 120f), new Vector2(90f, 150f));
            leftArrow.onClick.AddListener(() => SetWheel(_wheelIndex - 1));

            var rightArrow = UIFactory.CreateButton("Right", root, _font, ">", 54, Purple, TextLight);
            UIFactory.Place((RectTransform)rightArrow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470f, 120f), new Vector2(90f, 150f));
            rightArrow.onClick.AddListener(() => SetWheel(_wheelIndex + 1));

            var hint = UIFactory.CreateText("Hint", root, _font, "Desliza para cambiar  -  Toca o ENTRAR para abrir", 28, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(1000f, 60f));

            var enter = UIFactory.CreateButton("Enter", root, _font, "ENTRAR", 40, PurpleVivid, TextLight);
            UIFactory.Place((RectTransform)enter.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(440f, 120f));
            enter.onClick.AddListener(EnterCurrentSection);
        }

        void BuildWheelItem(Transform container, int i)
        {
            var item = UIFactory.NewUI("Item_" + _sections[i], container, typeof(CanvasGroup));
            UIFactory.Place((RectTransform)item.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(i * WheelSpacing, 0f), new Vector2(280f, 240f));

            var cg = item.GetComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            var icon = UIFactory.CreateImage("Icon", item.transform, _sectionColors[i % _sectionColors.Length]);
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(160f, 160f));
            if (wheelIcons != null && i < wheelIcons.Length && wheelIcons[i] != null)
            {
                icon.sprite = wheelIcons[i];
                icon.color = Color.white;
                icon.preserveAspect = true;
            }

            var label = UIFactory.CreateText("Label", item.transform, _font, _sections[i], 38, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(280f, 60f));
            label.fontStyle = FontStyle.Bold;

            _wheelItems.Add((RectTransform)item.transform);
            _wheelGroups.Add(cg);
        }

        void OnWheelSwipe(SwipeDirection dir)
        {
            switch (dir)
            {
                case SwipeDirection.Tap:   EnterCurrentSection(); break;
                case SwipeDirection.Left:  SetWheel(_wheelIndex + 1); break;
                case SwipeDirection.Right: SetWheel(_wheelIndex - 1); break;
            }
        }

        void SetWheel(int index)
        {
            _wheelIndex = Mathf.Clamp(index, 0, _sections.Length - 1);
        }

        void EnterCurrentSection()
        {
            switch (_wheelIndex)
            {
                case 0: ShowMap(); break;
                case 1: ShowTeam(); break;
                case 3: ShowGacha(); break;
                default: ShowComingSoon(_sections[_wheelIndex]); break;
            }
        }

        void BuildMapScreen()
        {
            _mapScreen = MakeScreen("MapScreen", BgDark);
            var root = _mapScreen.transform;

            var mapImg = UIFactory.CreateImage("MapImage", root, new Color(0.16f, 0.32f, 0.22f, 1f));
            UIFactory.Place(mapImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1000f, 1400f));
            mapImg.raycastTarget = false;
            if (forestMapSprite != null)
            {
                mapImg.sprite = forestMapSprite;
                mapImg.color = Color.white;
                mapImg.preserveAspect = true;
            }
            else
            {
                var ph = UIFactory.CreateText("Placeholder", mapImg.transform, _font,
                    "[ Mapa del Bosque ]\nplaceholder", 40, new Color(1f, 1f, 1f, 0.5f), TextAnchor.UpperCenter);
                UIFactory.Place(ph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 160f));
            }

            foreach (var zone in _map.zones)
            {
                StoryZone captured = zone;
                var node = UIFactory.CreateButton("Zone_" + zone.id, mapImg.transform, _font, zone.displayName, 30, PurpleVivid, TextLight);
                var rt = (RectTransform)node.transform;
                rt.anchorMin = new Vector2(zone.mapX, zone.mapY);
                rt.anchorMax = new Vector2(zone.mapX, zone.mapY);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(300f, 130f);
                node.onClick.AddListener(() => ShowBattleList(captured));
            }

            AddHeader(root, "Historia - " + _map.mapName);
            AddBackButton(root, ShowWheel);
        }

        void BuildBattleListScreen()
        {
            _battleListScreen = MakeScreen("BattleListScreen", BgDark);
            var root = _battleListScreen.transform;

            AddHeader(root, "Batallas");
            AddBackButton(root, ShowMap);

            var content = UIFactory.NewUI("Content", root);
            UIFactory.Place((RectTransform)content.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(0f, 0f));
            _battleListContent = content.transform;
        }

        void PopulateBattleList(StoryZone zone)
        {
            for (int i = _battleListContent.childCount - 1; i >= 0; i--)
                Destroy(_battleListContent.GetChild(i).gameObject);

            float y = 0f;
            foreach (var entry in zone.entries)
            {
                StoryEntry captured = entry;
                string sub = entry.type == StoryEntryType.Video
                    ? "Video"
                    : "Energia " + entry.energyCost + "  -  Batallas " + entry.battleCount;
                string label = entry.title + "\n<size=22><color=#B9A9FF>" + sub + "</color></size>";

                var b = UIFactory.CreateButton("Entry_" + entry.id, _battleListContent, _font, label, 32, Purple, TextLight);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(940f, 150f));
                b.onClick.AddListener(() => ShowMission(captured));

                if (entry.type == StoryEntryType.Battle && PlayerProgress.IsCompleted(entry.id))
                {
                    var done = UIFactory.CreateText("Done", b.transform, _font, "Completado", 22, Green, TextAnchor.MiddleRight);
                    UIFactory.Place(done.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(220f, 40f));
                }

                y -= 170f;
            }
        }

        void BuildMissionScreen()
        {
            _missionScreen = MakeScreen("MissionScreen", BgDark);
            var root = _missionScreen.transform;

            AddBackButton(root, () => ShowBattleList(_currentZone));

            var content = UIFactory.NewUI("Content", root);
            UIFactory.Stretch((RectTransform)content.transform);
            _missionContent = content.transform;
        }

        void PopulateMission(StoryEntry entry)
        {
            for (int i = _missionContent.childCount - 1; i >= 0; i--)
                Destroy(_missionContent.GetChild(i).gameObject);

            var header = UIFactory.CreateText("Title", _missionContent, _font, entry.title, 46, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 90f));
            header.fontStyle = FontStyle.Bold;

            var mTitle = UIFactory.CreateText("MissionsTitle", _missionContent, _font, "Misiones", 36, Cyan, TextAnchor.UpperLeft);
            UIFactory.Place(mTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(880f, 50f));
            mTitle.fontStyle = FontStyle.Bold;

            var missionsBody = "- " + string.Join("\n- ", entry.missions);
            var mBody = UIFactory.CreateText("MissionsBody", _missionContent, _font, missionsBody, 30, TextLight, TextAnchor.UpperLeft);
            UIFactory.Place(mBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(880f, 300f));

            var rTitle = UIFactory.CreateText("RewardsTitle", _missionContent, _font, "Recompensas", 36, Green, TextAnchor.UpperLeft);
            UIFactory.Place(rTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -640f), new Vector2(880f, 50f));
            rTitle.fontStyle = FontStyle.Bold;

            var rewardsBody = "";
            foreach (var r in entry.rewards)
                rewardsBody += "- " + r.name + " x" + r.amount + "\n";
            var rBody = UIFactory.CreateText("RewardsBody", _missionContent, _font, rewardsBody, 30, TextLight, TextAnchor.UpperLeft);
            UIFactory.Place(rBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -700f), new Vector2(880f, 300f));

            if (entry.playerExp > 0 || entry.crystalReward > 0)
            {
                float noteY = -700f - entry.rewards.Count * 38f - 16f;
                var note = UIFactory.CreateText("ExpNote", _missionContent, _font,
                    "EXP de Sincronizador y Cristales se otorgan solo la primera vez.", 22,
                    new Color(1f, 1f, 1f, 0.5f), TextAnchor.UpperLeft);
                UIFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, noteY), new Vector2(880f, 40f));
            }

            bool playable = entry.type == StoryEntryType.Battle && !string.IsNullOrEmpty(entry.sceneToLoad);
            var start = UIFactory.CreateButton("Start", _missionContent, _font,
                playable ? "EMPEZAR" : "Proximamente", 40, playable ? PurpleVivid : Purple, TextLight);
            UIFactory.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(440f, 120f));
            start.interactable = playable;
            if (playable)
            {
                var warn = UIFactory.CreateText("EnergyWarn", _missionContent, _font, "", 24, Crimson, TextAnchor.MiddleCenter);
                UIFactory.Place(warn.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(800f, 40f));

                StoryEntry captured = entry;
                start.onClick.AddListener(() =>
                {
                    if (!PlayerProgress.SpendEnergy(captured.energyCost))
                    {
                        warn.text = "Energia insuficiente: necesitas " + captured.energyCost +
                                    ", tienes " + PlayerProgress.Energy;
                        return;
                    }
                    StorySelection.SelectedEntry = captured;
                    SceneManager.LoadScene(captured.sceneToLoad);
                });
            }
        }

        void BuildComingSoonScreen()
        {
            _comingSoonScreen = MakeScreen("ComingSoonScreen", BgDark);
            var root = _comingSoonScreen.transform;

            _comingSoonTitle = UIFactory.CreateText("Header", root, _font, "Seccion", 46, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(_comingSoonTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 90f));
            _comingSoonTitle.fontStyle = FontStyle.Bold;

            var body = UIFactory.CreateText("Body", root, _font, "Proximamente", 48, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 120f));

            _comingSoonBalance = UIFactory.CreateText("Balance", root, _font, "", 32, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(_comingSoonBalance.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(900f, 60f));

            AddBackButton(root, ShowWheel);
        }

        void ShowOnly(GameObject screen)
        {
            if (_wheelScreen != null) _wheelScreen.SetActive(screen == _wheelScreen);
            if (_mapScreen != null) _mapScreen.SetActive(screen == _mapScreen);
            if (_battleListScreen != null) _battleListScreen.SetActive(screen == _battleListScreen);
            if (_missionScreen != null) _missionScreen.SetActive(screen == _missionScreen);
            if (_comingSoonScreen != null) _comingSoonScreen.SetActive(screen == _comingSoonScreen);
            if (_teamScreen != null) _teamScreen.SetActive(screen == _teamScreen);
            if (_rosterScreen != null) _rosterScreen.SetActive(screen == _rosterScreen);
            if (_itemsScreen != null) _itemsScreen.SetActive(screen == _itemsScreen);
            if (_bagPickerScreen != null) _bagPickerScreen.SetActive(screen == _bagPickerScreen);
            if (_gachaScreen != null) _gachaScreen.SetActive(screen == _gachaScreen);
            if (_mejoraScreen != null) _mejoraScreen.SetActive(screen == _mejoraScreen);
        }

        void ShowWheel()
        {
            SaveSystem.Save();
            if (_wheelCrystals != null) _wheelCrystals.text = "Cristales: " + Wallet.Crystals;
            if (_wheelGold != null) _wheelGold.text = "Oro: " + Wallet.Gold;
            ShowOnly(_wheelScreen);
        }
        void ShowMap() => ShowOnly(_mapScreen);

        void ShowBattleList(StoryZone zone)
        {
            _currentZone = zone;
            PopulateBattleList(zone);
            ShowOnly(_battleListScreen);
        }

        void ShowMission(StoryEntry entry)
        {
            PopulateMission(entry);
            ShowOnly(_missionScreen);
        }

        void ShowComingSoon(string sectionName)
        {
            if (_comingSoonTitle != null) _comingSoonTitle.text = sectionName;
            if (_comingSoonBalance != null)
            {
                if (sectionName == "Tienda")
                {
                    _comingSoonBalance.text = "Oro: " + Wallet.Gold;
                    _comingSoonBalance.color = GoldColor;
                }
                else if (sectionName == "Gacha")
                {
                    _comingSoonBalance.text = "Cristales: " + Wallet.Crystals;
                    _comingSoonBalance.color = Cyan;
                }
                else
                {
                    _comingSoonBalance.text = "";
                }
            }
            ShowOnly(_comingSoonScreen);
        }

        static readonly Color SlotEmpty  = new Color(0.18f, 0.16f, 0.26f, 1f);
        static readonly Color SlotFriend = new Color(0.16f, 0.18f, 0.28f, 1f);

        void BuildTeamScreen()
        {
            _teamScreen = MakeScreen("TeamScreen", BgDark);
            var root = _teamScreen.transform;

            AddHeader(root, "Equipo");
            AddBackButton(root, ShowWheel);

            var itemsBtn = UIFactory.CreateButton("OpenItems", root, _font, "Items", 30, Purple, TextLight);
            UIFactory.Place((RectTransform)itemsBtn.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -50f), new Vector2(220f, 90f));
            itemsBtn.onClick.AddListener(ShowItems);

            var mejorarBtn = UIFactory.CreateButton("OpenMejora", root, _font, "Mejorar", 30, Purple, TextLight);
            UIFactory.Place((RectTransform)mejorarBtn.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-270f, -50f), new Vector2(220f, 90f));
            mejorarBtn.onClick.AddListener(ShowMejora);

            var partyLeft = UIFactory.CreateButton("PartyLeft", root, _font, "<", 36, Purple, TextLight);
            UIFactory.Place((RectTransform)partyLeft.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-230f, -165f), new Vector2(90f, 70f));
            partyLeft.onClick.AddListener(() => SetParty(TeamState.CurrentParty - 1));

            _partyLabel = UIFactory.CreateText("PartyLabel", root, _font, "Equipo 1", 32, Cyan, TextAnchor.MiddleCenter);
            UIFactory.Place(_partyLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(300f, 60f));
            _partyLabel.fontStyle = FontStyle.Bold;

            var partyRight = UIFactory.CreateButton("PartyRight", root, _font, ">", 36, Purple, TextLight);
            UIFactory.Place((RectTransform)partyRight.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230f, -165f), new Vector2(90f, 70f));
            partyRight.onClick.AddListener(() => SetParty(TeamState.CurrentParty + 1));

            var content = UIFactory.NewUI("Content", root);
            UIFactory.Place((RectTransform)content.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -255f), Vector2.zero);
            _teamContent = content.transform;

            _teamStatus = UIFactory.CreateText("Status", root, _font, "Toca un espacio para asignar una Vision", 26,
                new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
            UIFactory.Place(_teamStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1000f, 60f));
        }

        void PopulateTeam()
        {
            for (int i = _teamContent.childCount - 1; i >= 0; i--)
                Destroy(_teamContent.GetChild(i).gameObject);

            for (int i = 0; i < TeamState.PartySize; i++)
            {
                int slot = i;
                BuildHeroCard(_teamContent, i, 400f, TeamState.Party[i], false, () => ShowRoster(slot), 2);
            }
        }

        void BuildHeroCard(Transform parent, int gridIndex, float height, HeroInfo hero, bool isFriend, System.Action onClick, int numCols = 3)
        {
            int col = gridIndex % numCols;
            int row = gridIndex / numCols;
            float colWidth  = numCols == 2 ? 380f : 340f;
            float colOffset = (numCols - 1) / 2f;
            float x = (col - colOffset) * colWidth;
            float y = -row * (height + 40f);

            var go = UIFactory.NewUI("Card_" + gridIndex, parent, typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            img.color = hero != null ? Purple : (isFriend ? SlotFriend : SlotEmpty);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            UIFactory.Place((RectTransform)go.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(300f, height));
            btn.onClick.AddListener(() => onClick());

            FillHeroCard(go.transform, hero, isFriend);
        }

        void FillHeroCard(Transform card, HeroInfo hero, bool isFriend)
        {
            Color tileColor = hero != null ? hero.accent : new Color(0.3f, 0.3f, 0.38f, 1f);
            var tile = UIFactory.CreateImage("Tile", card, tileColor);
            tile.raycastTarget = false;
            UIFactory.Place(tile.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(190f, 190f));

            bool hasPortrait = hero != null && heroPortraits != null && hero.portraitIndex >= 0 &&
                               hero.portraitIndex < heroPortraits.Length && heroPortraits[hero.portraitIndex] != null;
            if (hasPortrait)
            {
                tile.sprite = heroPortraits[hero.portraitIndex];
                tile.color = Color.white;
                tile.preserveAspect = true;
            }
            else
            {
                string glyph = hero != null ? hero.name.Substring(0, 1).ToUpper() : "+";
                var initial = UIFactory.CreateText("Glyph", card, _font, glyph, 80, TextLight, TextAnchor.MiddleCenter);
                UIFactory.Place(initial.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(190f, 120f));
                initial.fontStyle = FontStyle.Bold;
            }

            string nameStr = hero != null ? hero.name : (isFriend ? "Amigo" : "Vacio");
            var nameT = UIFactory.CreateText("Name", card, _font, nameStr, 32, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(nameT.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(280f, 40f));
            nameT.fontStyle = FontStyle.Bold;

            if (hero != null)
            {
                string stars = "";
                for (int s = 1; s <= 5; s++) stars += s <= hero.stars ? "★" : "☆";
                var starsT = UIFactory.CreateText("Stars", card, _font, stars, 20, GoldColor, TextAnchor.MiddleCenter);
                UIFactory.Place(starsT.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -278f), new Vector2(280f, 26f));
            }

            string subText;
            if (hero != null)
            {
                string expStr = hero.level >= HeroLeveling.MaxLevel
                    ? "MAX"
                    : hero.exp + "/" + HeroLeveling.ExpToNext(hero.level);
                subText = "Nv " + hero.level + "  " + hero.role + "\nEXP " + expStr;
            }
            else
            {
                subText = isFriend ? "Bloqueado" : "Toca para anadir";
            }
            var subT = UIFactory.CreateText("SubInfo", card, _font, subText, 20,
                isFriend && hero == null ? new Color(1f, 0.7f, 0.3f, 1f) : new Color(1f, 1f, 1f, 0.65f),
                TextAnchor.MiddleCenter);
            UIFactory.Place(subT.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -308f), new Vector2(280f, 58f));
        }

        void BuildRosterScreen()
        {
            _rosterScreen = MakeScreen("RosterScreen", BgDark);
            var root = _rosterScreen.transform;

            _rosterTitle = UIFactory.CreateText("Header", root, _font, "Elegir Vision", 46, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(_rosterTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(940f, 90f));
            _rosterTitle.fontStyle = FontStyle.Bold;

            AddBackButton(root, ShowTeam);

            var scroll = UIFactory.CreateVerticalScroll("RosterScroll", root, out RectTransform content);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(40f, 200f);
            srt.offsetMax = new Vector2(-40f, -250f);
            _rosterContent = content;

            var remove = UIFactory.CreateButton("Remove", root, _font, "Quitar del equipo", 30, Crimson, TextLight);
            UIFactory.Place((RectTransform)remove.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(460f, 110f));
            remove.onClick.AddListener(() =>
            {
                if (_selectedSlot >= 0 && _selectedSlot < TeamState.PartySize)
                    TeamState.Party[_selectedSlot] = null;
                ShowTeam();
            });
        }

        void PopulateRoster(int slot)
        {
            if (_rosterTitle != null) _rosterTitle.text = "Elegir Vision  -  Espacio " + (slot + 1);

            for (int i = _rosterContent.childCount - 1; i >= 0; i--)
                Destroy(_rosterContent.GetChild(i).gameObject);

            for (int j = 0; j < _owned.Count; j++)
            {
                HeroInfo hero = _owned[j];
                bool inParty = IsInParty(hero);

                var go = UIFactory.NewUI("Unit_" + hero.id, _rosterContent, typeof(CanvasRenderer), typeof(Image), typeof(Button));
                var img = go.GetComponent<Image>();
                img.color = inParty ? new Color(0.16f, 0.2f, 0.32f, 1f) : Purple;
                var btn = go.GetComponent<Button>();
                btn.targetGraphic = img;

                int col = j % 3;
                int rowi = j / 3;
                float x = (col - 1) * 340f;
                float y = -rowi * 400f - 10f;
                UIFactory.Place((RectTransform)go.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(300f, 380f));

                FillHeroCard(go.transform, hero, false);

                if (inParty)
                {
                    var tag = UIFactory.CreateText("InParty", go.transform, _font, "En equipo", 20, Cyan, TextAnchor.MiddleCenter);
                    UIFactory.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(280f, 34f));
                }

                HeroInfo captured = hero;
                btn.onClick.AddListener(() =>
                {
                    AssignHeroToSlot(slot, captured);
                    ShowTeam();
                });
            }

            int rows = Mathf.CeilToInt(_owned.Count / 3f);
            ((RectTransform)_rosterContent).sizeDelta = new Vector2(0f, rows * 400f + 20f);
        }

        bool IsInParty(HeroInfo hero)
        {
            for (int i = 0; i < TeamState.PartySize; i++)
                if (TeamState.Party[i] == hero) return true;
            return false;
        }

        void AssignHeroToSlot(int slot, HeroInfo hero)
        {
            if (slot < 0 || slot >= TeamState.PartySize) return;

            int existing = -1;
            for (int i = 0; i < TeamState.PartySize; i++)
                if (TeamState.Party[i] == hero) { existing = i; break; }

            if (existing >= 0 && existing != slot)
                TeamState.Party[existing] = TeamState.Party[slot];

            TeamState.Party[slot] = hero;
            SaveSystem.Save();
        }

        void ShowTeam()
        {
            TeamState.EnsureInit();
            if (_teamStatus != null) _teamStatus.text = "Toca un espacio para asignar una Vision";
            if (_partyLabel != null) _partyLabel.text = "Equipo " + (TeamState.CurrentParty + 1);
            PopulateTeam();
            ShowOnly(_teamScreen);
        }

        void SetParty(int index)
        {
            int count = TeamState.PartyCount;
            TeamState.CurrentParty = (index % count + count) % count;
            if (_partyLabel != null) _partyLabel.text = "Equipo " + (TeamState.CurrentParty + 1);
            PopulateTeam();
        }

        void ShowRoster(int slot)
        {
            _selectedSlot = slot;
            PopulateRoster(slot);
            ShowOnly(_rosterScreen);
        }

        void BuildItemsScreen()
        {
            _itemsScreen = MakeScreen("ItemsScreen", BgDark);
            var root = _itemsScreen.transform;

            AddHeader(root, "Items");
            AddBackButton(root, ShowTeam);

            var scroll = UIFactory.CreateVerticalScroll("ItemsScroll", root, out RectTransform content);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(40f, 40f);
            srt.offsetMax = new Vector2(-40f, -160f);
            _itemsContent = content;
        }

        void PopulateItems()
        {
            for (int i = _itemsContent.childCount - 1; i >= 0; i--)
                Destroy(_itemsContent.GetChild(i).gameObject);

            float y = 0f;
            y = AddBagSection(y);
            y -= 20f;
            y = AddItemsSectionHeader("Consumibles", Cyan, y);

            bool anyItem = false;
            foreach (var kvp in Inventory.Items)
            {
                if (kvp.Key == null || kvp.Value <= 0) continue;
                y = AddItemRow(kvp.Key.itemName, kvp.Key.description, kvp.Value, y);
                anyItem = true;
            }
            if (!anyItem)
                y = AddItemsEmptyRow("Aun no tienes items. Se consiguen en aventuras o, mas adelante, en la Tienda.", y);

            y -= 30f;
            y = AddItemsSectionHeader("Materiales", Green, y);

            bool anyMaterial = false;
            foreach (var kvp in Inventory.Materials)
            {
                if (kvp.Key == null || kvp.Value <= 0) continue;
                y = AddItemRow(kvp.Key.materialName, kvp.Key.description, kvp.Value, y);
                anyMaterial = true;
            }
            if (!anyMaterial)
                y = AddItemsEmptyRow("Aun no hay materiales de mejora; llegaran con el sistema de mejora de Visiones.", y);

            ((RectTransform)_itemsContent).sizeDelta = new Vector2(0f, -y + 20f);
        }

        float AddItemsSectionHeader(string title, Color color, float y)
        {
            var t = UIFactory.CreateText("Section_" + title, _itemsContent, _font, title, 34, color, TextAnchor.UpperLeft);
            UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(900f, 50f));
            t.fontStyle = FontStyle.Bold;
            return y - 60f;
        }

        float AddItemsEmptyRow(string text, float y)
        {
            var t = UIFactory.CreateText("Empty", _itemsContent, _font, text, 24, new Color(1f, 1f, 1f, 0.55f), TextAnchor.UpperLeft);
            UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(900f, 70f));
            return y - 90f;
        }

        float AddItemRow(string name, string description, int count, float y)
        {
            var row = UIFactory.CreateImage("Row_" + name, _itemsContent, Purple);
            UIFactory.Place(row.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(940f, 110f));

            var nameT = UIFactory.CreateText("Name", row.transform, _font, name, 30, TextLight, TextAnchor.MiddleLeft);
            UIFactory.Place(nameT.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 10f), new Vector2(560f, 50f));
            nameT.fontStyle = FontStyle.Bold;

            if (!string.IsNullOrEmpty(description))
            {
                var descT = UIFactory.CreateText("Desc", row.transform, _font, description, 18, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft);
                UIFactory.Place(descT.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -22f), new Vector2(560f, 34f));
            }

            var countT = UIFactory.CreateText("Count", row.transform, _font, "x" + count, 32, Cyan, TextAnchor.MiddleRight);
            UIFactory.Place(countT.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(160f, 60f));
            countT.fontStyle = FontStyle.Bold;

            return y - 130f;
        }

        void ShowItems()
        {
            PopulateItems();
            ShowOnly(_itemsScreen);
        }

        float AddBagSection(float y)
        {
            y = AddItemsSectionHeader("Mochila de Batalla", PurpleVivid, y);

            const float slotW = 280f, slotH = 120f, colGap = 20f, rowGap = 15f;
            const float startX = 10f;

            for (int i = 0; i < BattleBag.MaxSlots; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x = startX + col * (slotW + colGap);
                float slotY = y - row * (slotH + rowGap);

                var itemInSlot = BattleBag.GetSlot(i);
                string label;
                Color bg;
                if (itemInSlot != null)
                {
                    int count = Inventory.GetItemCount(itemInSlot);
                    if (count > 0)
                    {
                        label = itemInSlot.itemName + "\n<size=18>x" + count + "</size>";
                        bg = Purple;
                    }
                    else
                    {
                        label = itemInSlot.itemName + "\n<size=18>sin stock</size>";
                        bg = new Color(0.28f, 0.18f, 0.28f, 1f);
                    }
                }
                else
                {
                    label = "+\nVacío";
                    bg = new Color(0.12f, 0.10f, 0.22f, 1f);
                }

                int capturedSlot = i;
                var btn = UIFactory.CreateButton("BagSlot" + i, _itemsContent, _font, label, 22, bg, TextLight);
                UIFactory.Place((RectTransform)btn.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, slotY), new Vector2(slotW, slotH));
                btn.onClick.AddListener(() => ShowBagPicker(capturedSlot));
            }

            y -= 2 * (slotH + rowGap);
            return y - 20f;
        }

        void BuildBagPickerScreen()
        {
            _bagPickerScreen = MakeScreen("BagPickerScreen", BgDark);
            var root = _bagPickerScreen.transform;

            _bagPickerTitle = UIFactory.CreateText("Header", root, _font, "Elegir item", 46, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Place(_bagPickerTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(940f, 90f));
            _bagPickerTitle.fontStyle = FontStyle.Bold;

            AddBackButton(root, ShowItems);

            var scroll = UIFactory.CreateVerticalScroll("PickerScroll", root, out RectTransform content);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(40f, 40f);
            srt.offsetMax = new Vector2(-40f, -160f);
            _bagPickerContent = content;
        }

        void PopulateBagPicker(int slot)
        {
            if (_bagPickerTitle != null)
                _bagPickerTitle.text = "Elegir item  -  Espacio " + (slot + 1);

            for (int i = _bagPickerContent.childCount - 1; i >= 0; i--)
                Destroy(_bagPickerContent.GetChild(i).gameObject);

            float y = 0f;
            int shown = 0;

            if (BattleBag.GetSlot(slot) != null)
            {
                int capturedSlot = slot;
                var clearBtn = UIFactory.CreateButton("Clear", _bagPickerContent, _font, "Vacío  (quitar item)", 28, Crimson, TextLight);
                UIFactory.Place((RectTransform)clearBtn.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(900f, 100f));
                clearBtn.onClick.AddListener(() => { BattleBag.ClearSlot(capturedSlot); ShowItems(); });
                y -= 120f;
                shown++;
            }

            foreach (var kvp in Inventory.Items)
            {
                if (kvp.Key == null || kvp.Value <= 0) continue;
                ItemData item = kvp.Key;
                int count = kvp.Value;
                int capturedSlot = slot;

                string desc = string.IsNullOrEmpty(item.description) ? "" :
                    ("\n<size=20><color=#FFFFFFAA>" + item.description + "</color></size>");
                string btnLabel = item.itemName + "   x" + count + desc;

                var btn = UIFactory.CreateButton("Pick_" + item.itemName, _bagPickerContent, _font, btnLabel, 28, Purple, TextLight);
                UIFactory.Place((RectTransform)btn.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(900f, 120f));
                btn.onClick.AddListener(() => { BattleBag.SetSlot(capturedSlot, item); ShowItems(); });
                y -= 140f;
                shown++;
            }

            if (shown == 0)
            {
                var empty = UIFactory.CreateText("Empty", _bagPickerContent, _font,
                    "No tienes items. Ganalos en aventuras.", 28,
                    new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter);
                UIFactory.Place(empty.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(900f, 80f));
                y -= 100f;
            }

            ((RectTransform)_bagPickerContent).sizeDelta = new Vector2(0f, -y + 20f);
        }

        void ShowBagPicker(int slot)
        {
            _bagPickerSlot = slot;
            PopulateBagPicker(slot);
            ShowOnly(_bagPickerScreen);
        }

        // ---------------------------------------------------------------- Gacha

        void BuildGachaScreen()
        {
            _gachaScreen = MakeScreen("GachaScreen", BgDark);
            var root = _gachaScreen.transform;

            AddHeader(root, "Invocacion");
            AddBackButton(root, ShowWheel);

            _gachaBalance = UIFactory.CreateText("Balance", root, _font, "", 32, Cyan, TextAnchor.MiddleCenter);
            UIFactory.Place(_gachaBalance.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(900f, 50f));
            _gachaBalance.fontStyle = FontStyle.Bold;

            var rates = UIFactory.CreateText("Rates", root, _font, HeroCatalog.RatesText(), 22,
                new Color(1f, 1f, 1f, 0.55f), TextAnchor.UpperCenter);
            UIFactory.Place(rates.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -245f), new Vector2(900f, 200f));

            var resultBg = UIFactory.CreateImage("ResultBg", root, new Color(0.12f, 0.10f, 0.22f, 1f));
            UIFactory.Place(resultBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 360f));

            _gachaResult = UIFactory.CreateText("Result", resultBg.transform, _font,
                "Invoca una Vision para comenzar.", 32, TextLight, TextAnchor.MiddleCenter);
            UIFactory.Stretch(_gachaResult.rectTransform);

            var summon = UIFactory.CreateButton("Summon", root, _font,
                "Invocar  (" + Gacha.SingleCost + " cristales)", 34, PurpleVivid, TextLight);
            UIFactory.Place((RectTransform)summon.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(680f, 120f));
            summon.onClick.AddListener(DoSummon);
        }

        void RefreshGachaBalance()
        {
            if (_gachaBalance != null) _gachaBalance.text = "Cristales: " + Wallet.Crystals;
        }

        static string StarString(int stars)
        {
            string s = "";
            for (int i = 1; i <= 5; i++) s += i <= stars ? "★" : "☆";
            return s;
        }

        void DoSummon()
        {
            var res = Gacha.SummonSingle();
            RefreshGachaBalance();

            if (res == null || !string.IsNullOrEmpty(res.error))
            {
                _gachaResult.text = res != null ? res.error : "Error";
                _gachaResult.color = Crimson;
                return;
            }

            string stars = "<color=#F2C757>" + StarString(res.hero.stars) + "</color>";
            if (res.isNew)
            {
                _gachaResult.text = "¡NUEVA VISION!\n\n" + res.hero.name + "\n" + stars + "\n<size=24>" + res.hero.role + "</size>";
                _gachaResult.color = TextLight;
            }
            else
            {
                string extra = res.dupeMaterial != null ? ("\n+" + res.dupeAmount + " " + res.dupeMaterial.materialName) : "";
                _gachaResult.text = "Duplicado\n\n" + res.hero.name + "\n" + stars +
                                    "\n<size=24>+" + res.crystalRefund + " cristales" + extra + "</size>";
                _gachaResult.color = new Color(1f, 1f, 1f, 0.85f);
            }
        }

        // --------------------------------------------------------------- Mejora

        void BuildMejoraScreen()
        {
            _mejoraScreen = MakeScreen("MejoraScreen", BgDark);
            var root = _mejoraScreen.transform;

            AddHeader(root, "Mejorar Visiones");
            AddBackButton(root, ShowTeam);

            var scroll = UIFactory.CreateVerticalScroll("MejoraScroll", root, out RectTransform content);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(40f, 40f);
            srt.offsetMax = new Vector2(-40f, -160f);
            _mejoraContent = content;
        }

        void PopulateMejora()
        {
            for (int i = _mejoraContent.childCount - 1; i >= 0; i--)
                Destroy(_mejoraContent.GetChild(i).gameObject);

            float y = 0f;
            foreach (var hero in TeamState.Owned)
            {
                if (hero == null) continue;
                y = AddMejoraCard(hero, y);
            }

            ((RectTransform)_mejoraContent).sizeDelta = new Vector2(0f, -y + 20f);
        }

        float AddMejoraCard(HeroInfo hero, float y)
        {
            const float h = 300f;

            var row = UIFactory.CreateImage("MejRow_" + hero.id, _mejoraContent, Purple);
            UIFactory.Place(row.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(940f, h));

            var nameT = UIFactory.CreateText("Name", row.transform, _font, hero.name + "   Nv " + hero.level, 30, TextLight, TextAnchor.UpperLeft);
            UIFactory.Place(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(560f, 40f));
            nameT.fontStyle = FontStyle.Bold;

            var starsT = UIFactory.CreateText("Stars", row.transform, _font, StarString(hero.stars), 30, GoldColor, TextAnchor.UpperLeft);
            UIFactory.Place(starsT.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -62f), new Vector2(300f, 40f));

            if (hero.stars >= StarUpgrade.MaxStars)
            {
                var max = UIFactory.CreateText("Max", row.transform, _font, "Estrellas al maximo", 28, Green, TextAnchor.MiddleCenter);
                UIFactory.Place(max.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, -20f), new Vector2(700f, 50f));
                return y - (h + 20f);
            }

            float ry = -118f;
            foreach (var r in StarUpgrade.Requirements(hero.stars))
            {
                string line = r.matName + "    " + r.have + " / " + r.need;
                var t = UIFactory.CreateText("Req_" + r.matName, row.transform, _font, line, 24, r.ok ? Green : Crimson, TextAnchor.UpperLeft);
                UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, ry), new Vector2(580f, 34f));
                ry -= 40f;
            }

            int goldCost = StarUpgrade.GoldCost(hero.stars);
            bool goldOk = Wallet.Gold >= goldCost;
            var goldT = UIFactory.CreateText("GoldReq", row.transform, _font, "Oro    " + Wallet.Gold + " / " + goldCost, 24, goldOk ? Green : Crimson, TextAnchor.UpperLeft);
            UIFactory.Place(goldT.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, ry), new Vector2(580f, 34f));

            bool can = StarUpgrade.CanUpgrade(hero);
            var btn = UIFactory.CreateButton("Up_" + hero.id, row.transform, _font, "Subir a " + (hero.stars + 1) + "★", 28,
                can ? PurpleVivid : new Color(0.25f, 0.22f, 0.34f, 1f), TextLight);
            UIFactory.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(290f, 120f));
            btn.interactable = can;

            HeroInfo captured = hero;
            btn.onClick.AddListener(() =>
            {
                if (StarUpgrade.DoUpgrade(captured)) PopulateMejora();
            });

            return y - (h + 20f);
        }

        void ShowGacha()
        {
            RefreshGachaBalance();
            if (_gachaResult != null)
            {
                _gachaResult.text = "Invoca una Vision para comenzar.";
                _gachaResult.color = TextLight;
            }
            ShowOnly(_gachaScreen);
        }

        void ShowMejora()
        {
            PopulateMejora();
            ShowOnly(_mejoraScreen);
        }
    }
}

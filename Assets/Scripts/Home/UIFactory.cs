using UnityEngine;
using UnityEngine.UI;

namespace VisionsOfGenesis.Home
{
    public static class UIFactory
    {
        public static Font DefaultFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            return f;
        }

        public static GameObject NewUI(string name, Transform parent, params System.Type[] comps)
        {
            var all = new System.Type[comps.Length + 1];
            all[0] = typeof(RectTransform);
            for (int i = 0; i < comps.Length; i++) all[i + 1] = comps[i];

            var go = new GameObject(name, all);
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = NewUI(name, parent, typeof(CanvasRenderer), typeof(Image));
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text CreateText(string name, Transform parent, Font font, string text,
            int size, Color color, TextAnchor align)
        {
            var go = NewUI(name, parent, typeof(CanvasRenderer), typeof(Text));
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateButton(string name, Transform parent, Font font, string label,
            int size, Color bg, Color textColor)
        {
            var go = NewUI(name, parent, typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            img.color = bg;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var label2 = CreateText("Text", go.transform, font, label, size, textColor, TextAnchor.MiddleCenter);
            Stretch(label2.rectTransform);

            return btn;
        }

        public static ScrollRect CreateVerticalScroll(string name, Transform parent, out RectTransform content)
        {
            var rootGo = NewUI(name, parent, typeof(ScrollRect));
            var scroll = rootGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewportGo = NewUI("Viewport", rootGo.transform, typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0f);
            vpImg.raycastTarget = true;
            var vpRt = (RectTransform)viewportGo.transform;
            Stretch(vpRt);

            var contentGo = NewUI("Content", viewportGo.transform);
            content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            content.sizeDelta = new Vector2(0f, 0f);
            content.anchoredPosition = Vector2.zero;

            scroll.viewport = vpRt;
            scroll.content = content;
            return scroll;
        }
    }
}

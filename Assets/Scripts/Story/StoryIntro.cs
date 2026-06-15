using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VisionsOfGenesis.Combat;

namespace VisionsOfGenesis.Story
{
    public class StoryIntro : MonoBehaviour
    {
        [Header("References")]
        public CanvasGroup overlayGroup;
        public Text        introLabel;
        public BattleManager battleManager;

        [Header("Text")]
        [TextArea] public string introText =
            "Synchronizer, a corrupt Echo approaches the Genesis Crystal. FIGHT!";

        [Header("Timings (seconds)")]
        public float fadeIn  = 1.0f;
        public float hold    = 2.5f;
        public float fadeOut = 1.0f;

        private void Start()
        {
            if (introLabel != null) introLabel.text = introText;
            if (overlayGroup != null) overlayGroup.alpha = 0f;
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            yield return Fade(0f, 1f, fadeIn);
            yield return new WaitForSeconds(hold);
            yield return Fade(1f, 0f, fadeOut);

            if (overlayGroup != null) overlayGroup.gameObject.SetActive(false);

            if (battleManager != null)
                battleManager.BeginBattle();
            else
                Debug.LogWarning("StoryIntro has no BattleManager reference â€” battle won't start.");
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (overlayGroup == null || duration <= 0f)
            {
                if (overlayGroup != null) overlayGroup.alpha = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                overlayGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            overlayGroup.alpha = to;
        }
    }
}

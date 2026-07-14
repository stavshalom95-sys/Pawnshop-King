using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Keeps a code-built button wide enough for its own label, in any language.
    /// Every button is sized in English at build time (e.g. "Reject" at 110px),
    /// but a localized string set later — by LocalizedLabel.Bind, or by one of
    /// the many screen-specific Loc.Set refresh calls — can need more room
    /// ("סרב" at the same font size). Rather than patching every call site that
    /// changes a button's text, this watches the label and self-corrects: a
    /// cheap per-frame text-changed check (same watchdog shape as the earlier
    /// music-toggle fix), only re-measuring TMP's actual preferred width when
    /// the string changed. Width only ever grows past the requested minimum,
    /// never shrinks below it.
    /// </summary>
    public class ButtonAutoWidth : MonoBehaviour
    {
        private const float HorizontalPadding = 24f;

        private TextMeshProUGUI label;
        private LayoutElement layoutElement;
        private float minWidth;
        private string lastMeasuredText;

        public static void Attach(TextMeshProUGUI label, LayoutElement layoutElement, float minWidth)
        {
            var watcher = label.gameObject.AddComponent<ButtonAutoWidth>();
            watcher.label = label;
            watcher.layoutElement = layoutElement;
            watcher.minWidth = minWidth;
            watcher.Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (label == null || layoutElement == null) return;
            if (label.text == lastMeasuredText) return;

            lastMeasuredText = label.text;
            float preferredTextWidth = label.GetPreferredValues(label.text, 0f, 0f).x;
            layoutElement.preferredWidth = Mathf.Max(minWidth, preferredTextWidth + HorizontalPadding);
        }
    }
}

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
    ///
    /// First measurement runs from OnEnable, not from Attach itself. Screens
    /// like Inventory/Upgrades rebuild their button rows while the screen root
    /// is still inactive (RebuildList runs before SetActive(true) in Open()) —
    /// Unity defers Awake/OnEnable for every component added under an inactive
    /// ancestor, including the label's own TextMeshProUGUI, until the hierarchy
    /// activates. Measuring immediately at Attach time called GetPreferredValues
    /// on a label that hadn't been through its own Awake yet and threw a
    /// NullReferenceException. OnEnable is guaranteed by Unity to fire only once
    /// the object is truly active — and since this component is always added
    /// after the label's TextMeshProUGUI, its OnEnable also always runs after
    /// TMP's own, so the label is fully initialized either way: immediately, if
    /// the hierarchy was already active when Attach was called, or deferred
    /// until SetActive(true), if it wasn't.
    ///
    /// Measurement itself: GetPreferredValues(text, 0, 0) queries a HYPOTHETICAL
    /// string against the font's metrics table, which can under-measure on a
    /// dynamically-generated atlas (UITheme.HebrewFont is built at runtime, not
    /// pre-baked) if the glyphs for that specific string haven't been rasterized
    /// into the atlas yet at the moment of the query — this was still leaving
    /// Hebrew labels like "סרב" a few pixels tight. ForceMeshUpdate + reading
    /// TMP's own preferredWidth instead measures the ACTUAL generated mesh for
    /// the text/font combination that's really assigned, not a prediction, so
    /// it can never disagree with what's actually rendered. Font is now also
    /// tracked alongside text, since a language switch changes both together and
    /// either one alone should trigger a re-measure.
    /// </summary>
    public class ButtonAutoWidth : MonoBehaviour
    {
        // Hebrew glyphs commonly carry more side-bearing than Latin at the same
        // point size — generous on purpose, since "still a little tight" is the
        // failure mode being fixed here.
        private const float HorizontalPadding = 36f;

        private TextMeshProUGUI label;
        private LayoutElement layoutElement;
        private float minWidth;
        private string lastMeasuredText;
        private TMP_FontAsset lastMeasuredFont;

        public static void Attach(TextMeshProUGUI label, LayoutElement layoutElement, float minWidth)
        {
            var watcher = label.gameObject.AddComponent<ButtonAutoWidth>();
            watcher.label = label;
            watcher.layoutElement = layoutElement;
            watcher.minWidth = minWidth;
        }

        private void OnEnable() => Refresh();

        private void Update() => Refresh();

        private void Refresh()
        {
            if (label == null || layoutElement == null) return;
            if (label.text == lastMeasuredText && label.font == lastMeasuredFont) return;

            lastMeasuredText = label.text;
            lastMeasuredFont = label.font;

            label.ForceMeshUpdate();
            float preferredTextWidth = label.preferredWidth;
            layoutElement.preferredWidth = Mathf.Max(minWidth, preferredTextWidth + HorizontalPadding);
        }
    }
}

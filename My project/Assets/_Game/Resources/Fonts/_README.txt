Hebrew font drop folder
=======================

Drop a Hebrew-capable TTF here named exactly:

    Hebrew.ttf

Recommended: Assistant or Heebo (both SIL Open Font License, free for
commercial use) from Google Fonts:

  1. Go to fonts.google.com, search "Assistant" (or "Heebo").
  2. Download the family, unzip, and take a STATIC weight file
     (e.g. static/Assistant-Regular.ttf) — prefer static over the
     variable "[wght]" file for maximum compatibility.
  3. Rename it Hebrew.ttf and drop it in this folder.

The game builds a dynamic TextMeshPro font asset from it at runtime, so
Hebrew glyphs populate on demand — no Font Asset Creator step needed.
Until a file is here, the game falls back to an OS font (Segoe UI /
Arial / Tahoma), which works on Windows but may differ per platform.

Optional (release optimization): bake a static TMP atlas via
Window > TextMeshPro > Font Asset Creator with character set
"Unicode Range (Hex)" = 0020-007E,0590-05FF — see the repo docs.
Keep the OFL license file alongside the font when you ship.

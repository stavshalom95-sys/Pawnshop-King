Hebrew font
===========

Hebrew.ttf in this folder is the source used to bake HebrewFontAsset.asset —
UITheme.HebrewFont loads that baked asset, not this file, at runtime.

To (re)bake: Tools > Pawnshop King > Bake Hebrew Font Asset. Manual only —
it does not run automatically on Editor load or on Play. Re-run it whenever
Hebrew.ttf changes or a new string needs a character not already covered
(FontAssetBaker.cs logs any missing glyphs after baking).

History: an earlier version of this pipeline built the font at runtime from
an OS system font (Segoe UI) instead of this file, and separately tried
auto-baking on every Editor load. Both were dropped — OS font loading
(Font.CreateDynamicFontFromOSFont) failed outright in this environment and
crashed the project on startup with a NullReferenceException. Baking once,
manually, from this checked-in TTF avoids OS font loading entirely.

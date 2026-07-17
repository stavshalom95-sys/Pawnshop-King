Hebrew font
===========

UITheme.HebrewFont no longer reads a font file from this folder. It builds
its dynamic TextMeshPro font asset directly from an OS system font (Segoe UI,
falling back to Arial then Tahoma), using TMP's plain default atlas settings.

Why: a bundled Hebrew.ttf was tried here first, with an increasingly
aggressive custom SDF atlas (padding, sampling size, atlas resolution) to fix
reported character overlap ("swallowing") at small UI sizes. The overlap
persisted regardless of atlas settings, which pointed at the font file's own
glyph metrics rather than the atlas — system fonts are hinted for small-scale
legibility and don't have this problem, so the project TTF path was removed
rather than tuned further.

Hebrew.ttf may still sit in this folder from the earlier setup; it is unused
and safe to delete.

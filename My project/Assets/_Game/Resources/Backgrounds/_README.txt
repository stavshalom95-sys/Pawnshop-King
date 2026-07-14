Shop background drop folder
============================

Drop the room art here named exactly:

    Shop.jpeg

(or .png/.jpg — if you change the extension, update
BackgroundResourcePath in ShopSceneBackdrop.cs to match, since
Resources.Load looks up the name without an extension.)

The image imports as a UI sprite automatically and displays full-bleed
behind every screen using a "cover" fit: it always fills the frame
without distortion, cropping the edges as needed on aspect ratios other
than the source image's own (currently authored at 1248x832, 3:2).
Keep the important content — the counter and its surroundings —
centered, since wider or narrower screens crop the top/bottom or
left/right symmetrically around the center.

Swapping the file replaces the room immediately on the next play
session — no code changes needed.

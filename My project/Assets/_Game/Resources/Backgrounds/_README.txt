Shop background drop folder
============================

The room now has four stock-level variants that cross-fade as the
player's inventory fills up. Drop each one here named exactly:

    Shop_Empty.jpeg      0 items
    Shop_Sparse.jpeg     1-3 items
    Shop_Stocked.jpeg    4-9 items
    Shop_Full.jpeg       10+ items

(thresholds are StockedAtCount / FullAtCount consts in
ShopSceneBackdrop.cs — retune there if a different pacing feels better
once you've seen real play sessions)

Any missing variant falls back to a single "Shop.jpeg" if present, so
you can drop the four in over time without breaking the scene. Every
file imports as a UI sprite automatically and displays full-bleed using
a "cover" fit: it always fills the frame without distortion, cropping
the edges as needed on aspect ratios other than the source art's own
(currently authored at 1248x832, 3:2). Keep the important content —
the counter and its surroundings — centered, since wider or narrower
screens crop symmetrically around the center.

Crossfades run over 2 seconds (CrossfadeDuration) whenever the player's
item count crosses a threshold. Swapping any file replaces that stock
level's image on the next play session — no code changes needed.

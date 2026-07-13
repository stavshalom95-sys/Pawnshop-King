# Release Candidate — Playtest Matrix

Run top to bottom in a build (not just the editor) where possible.
Check each box only when the **expected result** was observed. Anything
that deviates: note it, keep going, report the batch.

## A. Boot & Main Menu
- [ ] Fresh install (delete `%USERPROFILE%\AppData\LocalLow\<company>\<product>\pawnshop_king_save.json` + PlayerPrefs): main menu shows **New Game only** (no Continue).
- [ ] New Game starts Day 1 with $500 cash, $5,000 debt, first payment in 7 days.
- [ ] With a save present: menu shows **Continue — Day N** and **New Game (erases save)**.
- [ ] New Game over an existing save actually erases it (quit immediately → relaunch → no Continue).

## B. Core Deal Loop
- [ ] Next Customer brings 1–2 items with icons (photo for Watches/Electronics/Retro, glyph otherwise).
- [ ] Inspect 3× on one item: condition → value band → risk clue; button becomes "Inspected".
- [ ] Uncheck one of two items: asking price re-quotes lower; Buy label updates; unchecked item stays with customer after a deal ("They pocket what you passed on").
- [ ] Uncheck ALL items: Offer/Buy refuse with the "check at least one item" message.
- [ ] Offer with empty field: button pulses gold, input focused, typing works immediately; clicking elsewhere resets the pulse.
- [ ] Enter in the offer field submits the offer.
- [ ] Offer far above ask (e.g. ask $200, offer $2000): pays only the **ask**, never the typo amount.
- [ ] Lowball repeatedly: customer counters downward, then walks (offended or fed up); rep loss shows on offended exits.
- [ ] Reject: customer leaves, reject sound plays, controls disable. No double-click path pays twice (mash Buy).

## C. Inventory & Selling
- [ ] Every owned item shows three channel quotes; Collector disabled without tags; Black Market disabled during a raid closure.
- [ ] Selling removes the row, adds cash, shows the receipt line; consequences fire (heat for hot shopfront sales, rep loss for fakes).
- [ ] Inventory closes automatically when the day ends (no stale rows after seizures).

## D. Save / Load
- [ ] Finish Day 1 → quit at the summary → relaunch → Continue — Day 2 with cash/inventory/debt/heat/reputation intact (spot-check numbers).
- [ ] Quit MID-day → relaunch → resumes at that morning (day replays with a fresh queue — expected).
- [ ] Corrupt the save file (edit it to garbage) → menu shows "could not be read" notice + New Game only. No crash.
- [ ] Game Over and Victory both delete the save (relaunch → New Game only).

## E. Pause & Settings
- [ ] Esc opens pause on: counter, inventory open, upgrades open, day summary. Esc in Settings backs out to the pause view first.
- [ ] Esc does nothing while the main menu is showing.
- [ ] Master slider mutes/dims everything live; SFX slider changes click volume; Music slider changes music volume live.
- [ ] **Music toggle Off stops music immediately; On resumes; state survives relaunch** (regression check for the fixed stale-instance bug).
- [ ] **Music toggle Off silences even with the Music slider at 100%** (mute must override the slider, not depend on it).
- [ ] **Switch language while Settings is open**: all rows stay inside the panel — no clipping, overlap, or sliding off-screen; still correct after closing and reopening pause (regression check for the childControlHeight layout fix).
- [ ] Difficulty toggles Easy/Hard, persists in the save (relaunch → same setting), and Easy noticeably lowers asks on the next customers.
- [ ] Quit to Main Menu: timescale restored (customer panel animations run), Continue resumes last completed day.
- [ ] While paused: game visuals frozen but menu buttons animate and click.

## F. Localization & RTL (Hebrew)
- [ ] Toggle to עברית: every chrome label flips live (action button, Offer/Buy/Reject, Inspect, Inventory/Upgrades, pause menu, tips).
- [ ] Hebrew renders in the Assistant font (not boxes, not the OS fallback — console must show NO `[UITheme]` fallback warning).
- [ ] Tips bubble: Hebrew right-aligned, English left-aligned; button names inside tips read correctly.
- [ ] Day summary in Hebrew: right-aligned, colored deltas intact (no broken `<color>` tags visible), prices readable.
- [ ] Buy label in Hebrew shows the price correctly (digits not reversed).
- [ ] Switch back to English: header font returns on titles; nothing stays Hebrew.
- [ ] KNOWN GAP: dialogue/feedback prose is English in Hebrew mode — confirm acceptable for this release or escalate.

## G. Economy & Endings
- [ ] Day 7: payment auto-deducts when affordable; summary states it (localized).
- [ ] Payment unaffordable WITH inventory: creditors seize items at rock-bottom, debt +10%, rep -2; summary explains.
- [ ] Payment unaffordable with NOTHING: **GAME OVER** screen (localized), New Campaign works.
- [ ] End a day with $0 cash and empty inventory: **bankruptcy** game over fires immediately.
- [ ] Pay the debt to zero: **VICTORY** screen with the final ledger (localized), New Campaign works.
- [ ] Push heat above 50 (fence deals, hot shopfront sales): police visit seizes stolen goods / black market raid closes the channel for 3 days.

## H. Audio & Feel
- [ ] Every button clicks; deal close plays the accept stinger; reject/walk-out plays the buzz.
- [ ] Music (placeholder pad) loops without an audible seam and keeps playing while paused.
- [ ] Drop a real `Music.ogg` into `Resources/Audio` → it replaces the pad next session.

## I. Stability Sweep
- [ ] Play 3+ consecutive days without editor errors (watch the Console — any red = report).
- [ ] Resolution sanity: run at 16:9, 16:10, and ultrawide — no overlapping/offscreen panels.
- [ ] Alt-tab out/in during play and while paused — no stuck input or frozen UI.
- [ ] Second play session in the same editor run (domain reload disabled): everything above still works (statics survival check).

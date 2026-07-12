# 🕯️ Pawnshop King

**A noir pawnshop management game about survival, risk, and the art of the deal.**

The debt collector comes on schedule. The customers don't. Behind the counter of a struggling pawnshop in a city that never asks questions, every item slid across the glass is a gamble: a genuine heirloom, a convincing fake, or something still warm from someone else's home. Inspect carefully, haggle hard, and decide — item by item — what you're willing to touch. The wrong purchase brings heat. The wrong hesitation loses the deal. The debt clock ticks either way.

**Pawnshop King** is a single-player management sim where negotiation is the core verb: read your customer, appraise what they carry, and walk the line between profit and prison.

---

## 📸 Preview

<!-- TODO: Replace with a gameplay GIF or screenshot -->
> *Screenshot / gameplay GIF coming soon.*

---

## ✨ Key Features

- **Item-by-item negotiation** — Customers arrive with bundles, but you deal on *your* terms. Check or uncheck individual items on the counter; the customer re-quotes their asking price for exactly what's on the table. Buy the clean goods, pass on the stolen watch, and let them pocket what you refused.
- **Inspection under uncertainty** — Condition, value, and origin start hidden. Spend limited inspections to earn knowledge, and read between the lines of clue text — the game never hands you a "STOLEN" label, only hints you learn to trust.
- **Haggling with personality** — Every customer rolls patience, desperation, greed, and honesty. Lowball a desperate seller and they may cave; insult a proud one and they walk — and word gets around.
- **Risk & consequence systems** — Heat accumulates with every shady deal and can boil over into events. Debt payments land on a schedule with real teeth. Reputation shapes what walks through your door.
- **Fully procedural UI** — Every screen (HUD, inventory, upgrades, day summary) is built from code at runtime against a shared noir theme: no prefabs, no scene wiring, one palette change lands everywhere.
- **Noir pawnshop aesthetic** — Deep surfaces, neon-cyan accents, gold-lit ledger numbers, and dialogue that sounds like it was overheard through a rain-streaked window.

---

## 🛠️ Tech Stack

| | |
|---|---|
| **Engine** | Unity 6 (`6000.5.3f1`) |
| **Language** | C# |
| **UI** | uGUI + TextMeshPro, constructed 100% procedurally at runtime |
| **Architecture** | Custom **zero-editor-wiring** design |

### Zero-editor-wiring architecture

The entire game rig self-assembles from code — there is no Inspector configuration to maintain or break:

- A `SceneInitializer` bootstraps managers and UI in a deterministic order at startup.
- All UI (canvases, panels, buttons, item rows) is generated in C#, themed through a single `UITheme` token set.
- Game content (items, customer archetypes, upgrades) is data-driven: hand-authored assets loaded via `Resources.LoadAll`, so adding content never touches a scene.
- Gameplay logic lives in plain static systems (`NegotiationSystem`, `InspectionSystem`, `MarketSystem`, …) operating on a serializable `GameState` — easy to reason about, easy to test.

---

## 🚀 Getting Started

### Build & Run

1. **Clone** the repository:
   ```bash
   git clone https://github.com/stavshalom95-sys/Pawnshop-King.git
   ```
2. **Open** the `My project` folder in Unity Hub with **Unity 6000.5.x** (the project was authored on `6000.5.3f1`).
3. **Press Play.** The scene rig builds itself at runtime — no scene setup, no prefab wiring, no missing references.

A standalone build works the same way: `File → Build Profiles → Build` with default settings.

### Contributing

Contributions and playtesting feedback are welcome:

- **Keep the zero-wiring rule.** New UI and systems should self-assemble from code and pull theme values from `UITheme` — pull requests that add Inspector dependencies won't be merged.
- **New content, not new code paths.** Items, customers, and upgrades should arrive as data assets under `Resources`, picked up automatically by the existing loaders.
- Open an issue first for larger gameplay changes so we can talk balance before you build.

---

## 📜 License

*License to be determined — all rights reserved for now.*

---

*Every deal is a story. Most of them end badly for someone.*

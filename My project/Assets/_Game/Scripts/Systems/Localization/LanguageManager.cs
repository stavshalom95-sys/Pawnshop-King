using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawnshopKing.Systems.Localization
{
    public enum GameLanguage
    {
        English,
        Hebrew
    }

    /// <summary>
    /// Minimal string-table localization for the main UI chrome: two static
    /// dictionaries keyed by Keys constants, a persisted language choice, and a
    /// change event the code-built labels subscribe to. Prose (dialogue, deal
    /// feedback, day summaries) stays English for now — localizing it is a
    /// content pass, not a systems change.
    /// </summary>
    public static class LanguageManager
    {
        private const string PrefsKey = "language";

        private static GameLanguage? current;

        public static event Action LanguageChanged;

        public static GameLanguage Current
        {
            get
            {
                current ??= (GameLanguage)PlayerPrefs.GetInt(PrefsKey, (int)GameLanguage.English);
                return current.Value;
            }
            set
            {
                if (current == value) return;
                current = value;
                PlayerPrefs.SetInt(PrefsKey, (int)value);
                LanguageChanged?.Invoke();
            }
        }

        public static bool IsRtl => Current == GameLanguage.Hebrew;

        public static void Toggle() =>
            Current = Current == GameLanguage.English ? GameLanguage.Hebrew : GameLanguage.English;

        /// <summary>Active-language string for a key; falls back to English, then the key itself.</summary>
        public static string T(string key)
        {
            var table = Current == GameLanguage.Hebrew ? Hebrew : English;
            if (table.TryGetValue(key, out var value)) return value;
            if (English.TryGetValue(key, out value)) return value;
            return key;
        }

        /// <summary>UI string keys — the single vocabulary both tables must cover.</summary>
        public static class Keys
        {
            public const string Inventory = "inventory";
            public const string Upgrades = "upgrades";
            public const string NextCustomer = "next_customer";
            public const string CloseShop = "close_shop";
            public const string OpenDay = "open_day";           // {0} = day number
            public const string GameOver = "game_over";
            public const string Victory = "victory";
            public const string DayClosing = "day_closing";     // {0} = day number

            public const string Offer = "offer";
            public const string Buy = "buy";
            public const string BuyAmount = "buy_amount";       // {0} = price
            public const string Reject = "reject";
            public const string Inspect = "inspect";            // {0} = passes left
            public const string Inspected = "inspected";

            public const string Close = "close";
            public const string NewCampaign = "new_campaign";
            public const string InventoryTitle = "inventory_title"; // {0} = count
            public const string UpgradesTitle = "upgrades_title";   // {0}/{1} = owned/total
            public const string Installed = "installed";
            public const string ChannelShop = "channel_shop";
            public const string ChannelCollector = "channel_collector";
            public const string ChannelBlackMarket = "channel_black_market";
            public const string BlackMarketClosed = "black_market_closed";
            public const string NoCollector = "no_collector";

            public const string Paused = "paused";
            public const string Resume = "resume";
            public const string Settings = "settings";
            public const string QuitToMenu = "quit_to_menu";
            public const string Back = "back";
            public const string PauseNote = "pause_note";
            public const string Master = "master";
            public const string Sfx = "sfx";
            public const string Music = "music";
            public const string Language = "language";
            public const string LanguageName = "language_name"; // shows the ACTIVE language

            public const string NewGame = "new_game";
            public const string NewGameErases = "new_game_erases";
            public const string ContinueDay = "continue_day";   // {0} = day number
            public const string SaveUnreadable = "save_unreadable";

            public const string TipNextCustomer = "tip_next_customer";
            public const string TipCloseShop = "tip_close_shop";
            public const string TipInspect = "tip_inspect";
            public const string TipOpenLow = "tip_open_low";
            public const string TipHaggle = "tip_haggle";

            public const string SummaryProfitToday = "summary_profit";     // {0} = colored delta
            public const string SummaryReputation = "summary_reputation";  // {0} = delta, {1} = now
            public const string SummaryHeat = "summary_heat";              // {0} = delta, {1} = now
            public const string DebtRemaining = "debt_remaining";          // {0} = amount
            public const string DebtClear = "debt_clear";
            public const string DebtNext = "debt_next";                    // {0} = amount, {1} = days
            public const string DebtPaid = "debt_paid";                    // {0} paid, {1} remains, {2} next, {3} days
            public const string DebtFinal = "debt_final";                  // {0} = amount
            public const string DebtSeized = "debt_seized";                // {0} = amount, {1} = items
            public const string DebtNoAssets = "debt_no_assets";           // {0} = amount
            public const string Bankrupt = "bankrupt";
            public const string HeatPoliceSeized = "heat_police_seized";   // {0} = items, {1} = heat relief
            public const string HeatPoliceClean = "heat_police_clean";     // {0} = heat relief
            public const string HeatRaid = "heat_raid";                    // {0} = days, {1} = heat relief
            public const string VictoryNarrative = "victory_narrative";    // {0} = days
            public const string VictoryStats = "victory_stats";            // cash, rep, heat, items, tools, total, missed

            public const string StampPaid = "stamp_paid";
            public const string StampSeized = "stamp_seized";

            public const string MoodAskingLine = "mood_asking_line"; // {0} = mood, {1} = asking price
            public const string CustomerTypeHaggler = "customer_type_haggler";
            public const string CustomerTypeDesperate = "customer_type_desperate";
            public const string CustomerTypeHurryUp = "customer_type_hurryup";

            public const string On = "on";
            public const string Off = "off";
            public const string DifficultyLabel = "difficulty";
            public const string DifficultyEasy = "difficulty_easy";
            public const string DifficultyHard = "difficulty_hard";
        }

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            [Keys.Inventory] = "Inventory",
            [Keys.Upgrades] = "Upgrades",
            [Keys.NextCustomer] = "Next Customer",
            [Keys.CloseShop] = "Close Shop",
            [Keys.OpenDay] = "Open Day {0}",
            [Keys.GameOver] = "Game Over",
            [Keys.Victory] = "Campaign Complete",
            [Keys.DayClosing] = "Day {0} — Closing Time",

            [Keys.Offer] = "Offer",
            [Keys.Buy] = "Buy",
            [Keys.BuyAmount] = "Buy  ${0}",
            [Keys.Reject] = "Reject",
            [Keys.Inspect] = "Inspect ({0})",
            [Keys.Inspected] = "Inspected",

            [Keys.Close] = "Close",
            [Keys.NewCampaign] = "New Campaign",
            [Keys.InventoryTitle] = "Inventory — {0} item(s)",
            [Keys.UpgradesTitle] = "Upgrades — {0}/{1} tools installed",
            [Keys.Installed] = "Installed",
            [Keys.ChannelShop] = "Shop",
            [Keys.ChannelCollector] = "Collector",
            [Keys.ChannelBlackMarket] = "Black Mkt",
            [Keys.BlackMarketClosed] = "Raided — closed",
            [Keys.NoCollector] = "No collector",

            [Keys.Paused] = "PAUSED",
            [Keys.Resume] = "Resume",
            [Keys.Settings] = "Settings",
            [Keys.QuitToMenu] = "Quit to Main Menu",
            [Keys.Back] = "Back",
            [Keys.PauseNote] = "Progress since this morning isn't saved until the day ends.",
            [Keys.Master] = "Master",
            [Keys.Sfx] = "SFX",
            [Keys.Music] = "Music",
            [Keys.Language] = "Language",
            [Keys.LanguageName] = "English",

            [Keys.NewGame] = "New Game",
            [Keys.NewGameErases] = "New Game (erases save)",
            [Keys.ContinueDay] = "Continue — Day {0}",
            [Keys.SaveUnreadable] = "A previous save exists but could not be read.",

            [Keys.TipNextCustomer] = "Click Next Customer to call the next customer.",
            [Keys.TipCloseShop] = "Click Close Shop to end the day.",
            [Keys.TipInspect] = "Inspect an item before you buy it.",
            [Keys.TipOpenLow] = "Offer below the asking price — haggling is the whole profit.",
            [Keys.TipHaggle] = "Haggle again, buy at their price, or reject.",

            [Keys.SummaryProfitToday] = "Total profit today:  {0}",
            [Keys.SummaryReputation] = "Reputation:  {0}   (now {1})",
            [Keys.SummaryHeat] = "Heat:  {0}   (now {1})",
            [Keys.DebtRemaining] = "Debt remaining: ${0}",
            [Keys.DebtClear] = "You owe nothing. The shop is yours.",
            [Keys.DebtNext] = "Next payment: ${0} in {1} day(s).",
            [Keys.DebtPaid] = "Debt payment of ${0} made. ${1} remains — next payment ${2} in {3} days.",
            [Keys.DebtFinal] = "Final payment of ${0} made. The inherited debt is PAID OFF.",
            [Keys.DebtSeized] = "You couldn't cover the ${0} payment. Creditors seized {1} item(s) at rock-bottom prices. Debt penalty +10%. (Reputation -2)",
            [Keys.DebtNoAssets] = "The ${0} payment came due and there was nothing left to take. The shop is finished.",
            [Keys.Bankrupt] = "No cash and nothing left to sell — the shop is bankrupt.",
            [Keys.HeatPoliceSeized] = "POLICE VISIT — Officers seized {0} stolen item(s). No compensation. (Reputation -2, Heat -{1})",
            [Keys.HeatPoliceClean] = "POLICE VISIT — Officers turned the shop over and left empty-handed. (Heat -{0})",
            [Keys.HeatRaid] = "BLACK MARKET RAID — No black market sales for {0} days. (Heat -{1})",
            [Keys.VictoryNarrative] = "The inherited debt is history. In {0} days you turned a dying shop into your own — the Pawnshop King.",
            [Keys.VictoryStats] = "Final cash: ${0}\nReputation: {1}   ·   Heat: {2}\nInventory: {3} item(s) still on the shelves\nTools installed: {4}/{5}\nPayments missed along the way: {6}",

            [Keys.StampPaid] = "PAID",
            [Keys.StampSeized] = "SEIZED",

            [Keys.MoodAskingLine] = "Mood: {0}     Asking: {1}",
            [Keys.CustomerTypeHaggler] = "Haggler",
            [Keys.CustomerTypeDesperate] = "Desperate",
            [Keys.CustomerTypeHurryUp] = "In a Hurry",

            [Keys.On] = "On",
            [Keys.Off] = "Off",
            [Keys.DifficultyLabel] = "Difficulty",
            [Keys.DifficultyEasy] = "Easy",
            [Keys.DifficultyHard] = "Hard",
        };

        private static readonly Dictionary<string, string> Hebrew = new Dictionary<string, string>
        {
            [Keys.Inventory] = "מלאי",
            [Keys.Upgrades] = "שדרוגים",
            [Keys.NextCustomer] = "הלקוח הבא",
            [Keys.CloseShop] = "סגור את החנות",
            [Keys.OpenDay] = "פתח את יום {0}",
            [Keys.GameOver] = "המשחק נגמר",
            [Keys.Victory] = "הקמפיין הושלם",
            [Keys.DayClosing] = "יום {0} — סגירה",

            [Keys.Offer] = "הצע",
            [Keys.Buy] = "קנה",
            [Keys.BuyAmount] = "קנה  ${0}",
            [Keys.Reject] = "סרב",
            [Keys.Inspect] = "בדוק ({0})",
            [Keys.Inspected] = "נבדק",

            [Keys.Close] = "סגור",
            [Keys.NewCampaign] = "קמפיין חדש",
            [Keys.InventoryTitle] = "מלאי — {0} פריטים",
            [Keys.UpgradesTitle] = "שדרוגים — {0}/{1} כלים הותקנו",
            [Keys.Installed] = "הותקן",
            [Keys.ChannelShop] = "חנות",
            [Keys.ChannelCollector] = "אספן",
            [Keys.ChannelBlackMarket] = "שוק שחור",
            [Keys.BlackMarketClosed] = "נסגר בפשיטה",
            [Keys.NoCollector] = "אין אספן",

            [Keys.Paused] = "מושהה",
            [Keys.Resume] = "המשך משחק",
            [Keys.Settings] = "הגדרות",
            [Keys.QuitToMenu] = "יציאה לתפריט הראשי",
            [Keys.Back] = "חזרה",
            [Keys.PauseNote] = "ההתקדמות מהבוקר נשמרת רק בסוף היום.",
            [Keys.Master] = "עוצמה כללית",
            [Keys.Sfx] = "אפקטים",
            [Keys.Music] = "מוזיקה",
            [Keys.Language] = "שפה",
            [Keys.LanguageName] = "עברית",

            [Keys.NewGame] = "משחק חדש",
            [Keys.NewGameErases] = "משחק חדש (מוחק שמירה)",
            [Keys.ContinueDay] = "המשך — יום {0}",
            [Keys.SaveUnreadable] = "קיימת שמירה קודמת אך לא ניתן לקרוא אותה.",

            [Keys.TipNextCustomer] = "לחץ על 'הלקוח הבא' כדי לקרוא ללקוח הבא",
            [Keys.TipCloseShop] = "לחץ על 'סגור את החנות' כדי לסיים את היום",
            [Keys.TipInspect] = "לחץ על 'בדוק' כדי לבדוק את החפץ לפני שקונים",
            [Keys.TipOpenLow] = "הצע פחות מהמחיר המבוקש — מיקוח הוא כל הרווח",
            [Keys.TipHaggle] = "אפשר להתמקח שוב, לקנות במחיר המבוקש או לסרב",

            [Keys.SummaryProfitToday] = "רווח כולל היום:  {0}",
            [Keys.SummaryReputation] = "מוניטין:  {0}   (כעת {1})",
            [Keys.SummaryHeat] = "חום משטרתי:  {0}   (כעת {1})",
            [Keys.DebtRemaining] = "חוב שנותר: ${0}",
            [Keys.DebtClear] = "אין לך חובות. החנות שלך.",
            [Keys.DebtNext] = "התשלום הבא: ${0} בעוד {1} ימים.",
            [Keys.DebtPaid] = "שולם תשלום חוב של ${0}. נותרו ${1} — התשלום הבא ${2} בעוד {3} ימים.",
            [Keys.DebtFinal] = "התשלום האחרון של ${0} בוצע. החוב שולם במלואו!",
            [Keys.DebtSeized] = "לא הצלחת לכסות את התשלום של ${0}. הנושים החרימו {1} פריטים במחירי רצפה. קנס חוב +10%. (מוניטין -2)",
            [Keys.DebtNoAssets] = "הגיע מועד התשלום של ${0} ולא נשאר מה לקחת. החנות אבודה.",
            [Keys.Bankrupt] = "אין מזומן ואין מה למכור — החנות פשטה רגל.",
            [Keys.HeatPoliceSeized] = "ביקור משטרה — שוטרים החרימו {0} פריטים גנובים. ללא פיצוי. (מוניטין -2, חום -{1})",
            [Keys.HeatPoliceClean] = "ביקור משטרה — השוטרים הפכו את החנות ויצאו בידיים ריקות. (חום -{0})",
            [Keys.HeatRaid] = "פשיטה על השוק השחור — אין מכירות בשוק השחור למשך {0} ימים. (חום -{1})",
            [Keys.VictoryNarrative] = "החוב שירשת הוא היסטוריה. בתוך {0} ימים הפכת חנות גוססת לשלך — מלך העבוט.",
            [Keys.VictoryStats] = "מזומן סופי: ${0}\nמוניטין: {1}   ·   חום: {2}\nמלאי: {3} פריטים על המדפים\nכלים מותקנים: {4}/{5}\nתשלומים שהוחמצו בדרך: {6}",

            [Keys.StampPaid] = "שולם",
            [Keys.StampSeized] = "הוחרם",

            [Keys.MoodAskingLine] = "מצב רוח: {0}     מבוקש: {1}",
            [Keys.CustomerTypeHaggler] = "מתמקח",
            [Keys.CustomerTypeDesperate] = "נואש",
            [Keys.CustomerTypeHurryUp] = "ממהר",

            [Keys.On] = "פועל",
            [Keys.Off] = "כבוי",
            [Keys.DifficultyLabel] = "רמת קושי",
            [Keys.DifficultyEasy] = "קל",
            [Keys.DifficultyHard] = "קשה",
        };
    }
}

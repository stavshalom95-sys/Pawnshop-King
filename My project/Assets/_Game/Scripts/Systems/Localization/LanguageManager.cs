using System;
using System.Collections.Generic;
using PawnshopKing.Data;
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

        // ---- Enum value labels — every raw .ToString() a player can see (item
        // rows, mood line) gets a translation, not just the surrounding chrome. ----

        public static string CategoryLabel(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Watches: return T(Keys.CategoryWatches);
                case ItemCategory.Jewelry: return T(Keys.CategoryJewelry);
                case ItemCategory.Electronics: return T(Keys.CategoryElectronics);
                case ItemCategory.MusicalInstruments: return T(Keys.CategoryMusicalInstruments);
                case ItemCategory.RetroCollectibles: return T(Keys.CategoryRetroCollectibles);
                case ItemCategory.AntiquesCurios: return T(Keys.CategoryAntiquesCurios);
                case ItemCategory.LuxuryAccessories: return T(Keys.CategoryLuxuryAccessories);
                default: return T(Keys.CategoryToolsPracticalGoods);
            }
        }

        public static string ConditionLabel(ConditionState condition)
        {
            switch (condition)
            {
                case ConditionState.Pristine: return T(Keys.ConditionPristine);
                case ConditionState.Clean: return T(Keys.ConditionClean);
                case ConditionState.Worn: return T(Keys.ConditionWorn);
                case ConditionState.Damaged: return T(Keys.ConditionDamaged);
                default: return T(Keys.ConditionBroken);
            }
        }

        public static string MoodLabel(CustomerMood mood)
        {
            switch (mood)
            {
                case CustomerMood.Friendly: return T(Keys.MoodFriendly);
                case CustomerMood.Nervous: return T(Keys.MoodNervous);
                case CustomerMood.Impatient: return T(Keys.MoodImpatient);
                case CustomerMood.Desperate: return T(Keys.MoodDesperate);
                case CustomerMood.Offended: return T(Keys.MoodOffended);
                default: return T(Keys.MoodNeutral);
            }
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

            public const string Debt = "debt";
            public const string DebtPanelTitle = "debt_panel_title";       // {0} = amount owed
            public const string DebtPanelCash = "debt_panel_cash";         // {0} = cash on hand
            public const string DebtPanelPay = "debt_panel_pay";
            public const string DebtPanelEnterAmount = "debt_panel_enter_amount";
            public const string DebtPanelInsufficientCash = "debt_panel_insufficient_cash";
            public const string DebtPanelPaid = "debt_panel_paid";               // {0} = paid, {1} = remaining
            public const string DebtPanelPaidCleared = "debt_panel_paid_cleared"; // {0} = paid
            public const string OfferPlaceholder = "offer_placeholder";
            public const string DebtPayPlaceholder = "debt_pay_placeholder";

            public const string DayBarLabel = "day_bar_label";               // {0} = day number
            public const string CashBarLabel = "cash_bar_label";             // {0} = amount
            public const string ReputationBarLabel = "reputation_bar_label"; // {0} = value
            public const string HeatBarLabel = "heat_bar_label";             // {0} = value
            public const string DebtBarLabel = "debt_bar_label";             // {0} = owed, {1} = due, {2} = days
            public const string DebtBarCleared = "debt_bar_cleared";
            public const string ReputationTooltip = "reputation_tooltip";
            public const string HeatTooltip = "heat_tooltip";

            // Deal feedback (negotiation outcomes)
            public const string DealAccepted = "deal_accepted";                         // {0}=price {1}=leftover suffix
            public const string DealAcceptedReluctantly = "deal_accepted_reluctantly";   // {0}=price {1}=leftover suffix
            public const string DealCountered = "deal_countered";                        // {0}=price
            public const string DealOffended = "deal_offended";
            public const string DealGaveUp = "deal_gave_up";
            public const string DealCheckAtLeastOne = "deal_check_at_least_one";
            public const string DealNoCashForOffer = "deal_no_cash_for_offer";
            public const string DealNoCashForAsking = "deal_no_cash_for_asking";
            public const string DealBoughtAtAsking = "deal_bought_at_asking";            // {0}=price {1}=leftover suffix
            public const string DealRejected = "deal_rejected";
            public const string DealLeftoverSuffix = "deal_leftover_suffix";
            public const string OfferArmHint = "offer_arm_hint";

            // Market (selling in Inventory)
            public const string SellUnavailable = "sell_unavailable";
            public const string SellNotOwned = "sell_not_owned";
            public const string SoldFor = "sold_for";                                   // {0}=item name {1}=price
            public const string ConsequenceCollectorFakeAngry = "consequence_collector_fake_angry";
            public const string ConsequenceCollectorHotQuestions = "consequence_collector_hot_questions";
            public const string ConsequenceCollectorSatisfied = "consequence_collector_satisfied";
            public const string ConsequenceBlackMarketHotMoved = "consequence_bm_hot_moved";
            public const string ConsequenceBlackMarketCleanNoted = "consequence_bm_clean_noted";
            public const string ConsequenceShopfrontFakeReturned = "consequence_shop_fake_returned";
            public const string ConsequenceShopfrontHotRecognized = "consequence_shop_hot_recognized";

            // Upgrades (purchase outcomes)
            public const string UpgradeAlreadyOwned = "upgrade_already_owned";           // {0}=name
            public const string UpgradeCantAfford = "upgrade_cant_afford";               // {0}=name {1}=cost {2}=cash
            public const string UpgradeInstalled = "upgrade_installed";                  // {0}=name {1}=cost

            public const string ItemStatsLineCounter = "item_stats_line_counter";        // {0}=condition {1}=value
            public const string ItemStatsLineInventory = "item_stats_line_inventory";    // {0}=condition {1}=value {2}=paid

            public const string CategoryWatches = "category_watches";
            public const string CategoryJewelry = "category_jewelry";
            public const string CategoryElectronics = "category_electronics";
            public const string CategoryMusicalInstruments = "category_musical_instruments";
            public const string CategoryRetroCollectibles = "category_retro_collectibles";
            public const string CategoryAntiquesCurios = "category_antiques_curios";
            public const string CategoryLuxuryAccessories = "category_luxury_accessories";
            public const string CategoryToolsPracticalGoods = "category_tools_practical_goods";

            public const string ConditionPristine = "condition_pristine";
            public const string ConditionClean = "condition_clean";
            public const string ConditionWorn = "condition_worn";
            public const string ConditionDamaged = "condition_damaged";
            public const string ConditionBroken = "condition_broken";

            public const string MoodNeutral = "mood_neutral";
            public const string MoodFriendly = "mood_friendly";
            public const string MoodNervous = "mood_nervous";
            public const string MoodImpatient = "mood_impatient";
            public const string MoodDesperate = "mood_desperate";
            public const string MoodOffended = "mood_offended";

            public const string EffectConditionAccuracy = "effect_condition_accuracy";
            public const string EffectFakeDetection = "effect_fake_detection";
            public const string EffectValueAccuracy = "effect_value_accuracy";
            public const string EffectTool = "effect_tool";

            public const string ShopOpenName = "shop_open_name";
            public const string ShopClosedName = "shop_closed_name";
            public const string WaitingForFirstCustomer = "waiting_for_first_customer";
            public const string DayIsOver = "day_is_over";                     // {0} = day number
            public const string QueueCustomersToday = "queue_customers_today"; // {0} = count
            public const string QueueLastCustomer = "queue_last_customer";
            public const string QueueMoreWaiting = "queue_more_waiting";       // {0} = count

            public const string MoodAskingLine = "mood_asking_line"; // {0} = mood, {1} = asking price
            public const string MoodOnlyLine = "mood_only_line";     // {0} = mood
            public const string DealNothingToTrade = "deal_nothing_to_trade";
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

            [Keys.Debt] = "Debt",
            [Keys.DebtPanelTitle] = "Debt — ${0} owed",
            [Keys.DebtPanelCash] = "Cash on hand: ${0}",
            [Keys.DebtPanelPay] = "Pay",
            [Keys.DebtPanelEnterAmount] = "Enter an amount to pay first.",
            [Keys.DebtPanelInsufficientCash] = "You don't have that much cash.",
            [Keys.DebtPanelPaid] = "Paid ${0} toward the debt. ${1} remains.",
            [Keys.DebtPanelPaidCleared] = "Paid ${0}. The debt is clear!",
            [Keys.OfferPlaceholder] = "offer $",
            [Keys.DebtPayPlaceholder] = "amount $",

            [Keys.DayBarLabel] = "Day {0}",
            [Keys.CashBarLabel] = "Cash  ${0}",
            [Keys.ReputationBarLabel] = "Rep  {0}",
            [Keys.HeatBarLabel] = "Heat  {0}",
            [Keys.DebtBarLabel] = "Debt  ${0}  (${1} due in {2}d)",
            [Keys.DebtBarCleared] = "Debt  cleared",
            [Keys.ReputationTooltip] = "High reputation attracts wealthier customers.",
            [Keys.HeatTooltip] = "As heat rises, the risk of a police raid increases.",

            [Keys.DealAccepted] = "Deal. You hand over ${0}.{1}",
            [Keys.DealAcceptedReluctantly] = "“Fine. Just give me the money.” You pay ${0}.{1}",
            [Keys.DealCountered] = "“Make it ${0} and we're done.”",
            [Keys.DealOffended] = "“Insulting.” They storm out. Word gets around. (Reputation -1)",
            [Keys.DealGaveUp] = "“Forget it.” They pack up and leave.",
            [Keys.DealCheckAtLeastOne] = "Check at least one item to deal on.",
            [Keys.DealNoCashForOffer] = "You can't cover that offer.",
            [Keys.DealNoCashForAsking] = "You don't have the cash for their price.",
            [Keys.DealBoughtAtAsking] = "Bought at asking price — ${0}. Fair dealing. (Reputation +1){1}",
            [Keys.DealRejected] = "You wave them off. They take their goods elsewhere.",
            [Keys.DealLeftoverSuffix] = " They pocket what you passed on.",
            [Keys.OfferArmHint] = "Type your offer — press Enter or hit Offer again.",

            [Keys.SellUnavailable] = "No buyer wants this through that channel.",
            [Keys.SellNotOwned] = "That item is no longer on your shelves.",
            [Keys.SoldFor] = "{0} sold for ${1}.",
            [Keys.ConsequenceCollectorFakeAngry] = " The collector is furious — it was a fake. (Reputation -2)",
            [Keys.ConsequenceCollectorHotQuestions] = " The collector asks pointed questions about provenance. (Heat +2)",
            [Keys.ConsequenceCollectorSatisfied] = " A satisfied collector spreads the word.",
            [Keys.ConsequenceBlackMarketHotMoved] = " Moved quietly out of town, no questions asked. (Heat -1)",
            [Keys.ConsequenceBlackMarketCleanNoted] = " The fence's circle takes note of your business. (Heat +1)",
            [Keys.ConsequenceShopfrontFakeReturned] = " An angry buyer brought it back as counterfeit. (Reputation -1)",
            [Keys.ConsequenceShopfrontHotRecognized] = " Someone recognized it in the window — police are asking around. (Heat +3)",

            [Keys.UpgradeAlreadyOwned] = "You already own the {0}.",
            [Keys.UpgradeCantAfford] = "You can't afford the {0} — it's ${1} and you have ${2}.",
            [Keys.UpgradeInstalled] = "{0} installed. ${1} well spent — probably.",

            [Keys.ItemStatsLineCounter] = "Condition: {0} · Value: {1}",
            [Keys.ItemStatsLineInventory] = "Condition: {0} · Value: {1} · Paid ${2}",

            [Keys.CategoryWatches] = "Watches",
            [Keys.CategoryJewelry] = "Jewelry",
            [Keys.CategoryElectronics] = "Electronics",
            [Keys.CategoryMusicalInstruments] = "Musical Instruments",
            [Keys.CategoryRetroCollectibles] = "Retro Collectibles",
            [Keys.CategoryAntiquesCurios] = "Antiques & Curios",
            [Keys.CategoryLuxuryAccessories] = "Luxury Accessories",
            [Keys.CategoryToolsPracticalGoods] = "Tools & Practical Goods",

            [Keys.ConditionPristine] = "Pristine",
            [Keys.ConditionClean] = "Clean",
            [Keys.ConditionWorn] = "Worn",
            [Keys.ConditionDamaged] = "Damaged",
            [Keys.ConditionBroken] = "Broken",

            [Keys.MoodNeutral] = "Neutral",
            [Keys.MoodFriendly] = "Friendly",
            [Keys.MoodNervous] = "Nervous",
            [Keys.MoodImpatient] = "Impatient",
            [Keys.MoodDesperate] = "Desperate",
            [Keys.MoodOffended] = "Offended",

            [Keys.EffectConditionAccuracy] = "Inspection · Condition",
            [Keys.EffectFakeDetection] = "Inspection · Counterfeits",
            [Keys.EffectValueAccuracy] = "Inspection · Valuation",
            [Keys.EffectTool] = "Tool",

            [Keys.ShopOpenName] = "Shop is open",
            [Keys.ShopClosedName] = "Shop closed",
            [Keys.WaitingForFirstCustomer] = "Waiting for the first customer...",
            [Keys.DayIsOver] = "Day {0} is over. The debt clock ticks on.",
            [Keys.QueueCustomersToday] = "{0} customers in the queue today",
            [Keys.QueueLastCustomer] = "Last customer of the day",
            [Keys.QueueMoreWaiting] = "{0} more waiting outside",

            [Keys.MoodAskingLine] = "Mood: {0}     Asking: {1}",
            [Keys.MoodOnlyLine] = "Mood: {0}",
            [Keys.DealNothingToTrade] = "They have nothing you'd trade for.",
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

            [Keys.Debt] = "חוב",
            [Keys.DebtPanelTitle] = "חוב — ${0} לתשלום",
            [Keys.DebtPanelCash] = "מזומן בקופה: ${0}",
            [Keys.DebtPanelPay] = "שלם",
            [Keys.DebtPanelEnterAmount] = "הזן סכום לתשלום קודם.",
            [Keys.DebtPanelInsufficientCash] = "אין לך מספיק מזומן.",
            [Keys.DebtPanelPaid] = "שולם ${0} על חשבון החוב. נותרו ${1}.",
            [Keys.DebtPanelPaidCleared] = "שולם ${0}. החוב נפרע במלואו!",
            [Keys.OfferPlaceholder] = "הצעה $",
            [Keys.DebtPayPlaceholder] = "סכום $",

            [Keys.DayBarLabel] = "יום {0}",
            [Keys.CashBarLabel] = "מזומן  ${0}",
            [Keys.ReputationBarLabel] = "מוניטין  {0}",
            [Keys.HeatBarLabel] = "חום  {0}",
            [Keys.DebtBarLabel] = "חוב  ${0}  (${1} בעוד {2} ימים)",
            [Keys.DebtBarCleared] = "חוב  נפרע",
            [Keys.ReputationTooltip] = "מוניטין גבוה מושך לקוחות עשירים יותר",
            [Keys.HeatTooltip] = "ככל שהחום עולה, הסיכון לפשיטה משטרתית גובר",

            [Keys.DealAccepted] = "עסקה סגורה. אתה מעביר ${0}.{1}",
            [Keys.DealAcceptedReluctantly] = "“בסדר, רק תן לי את הכסף.” אתה משלם ${0}.{1}",
            [Keys.DealCountered] = "“שיהיה ${0} וסגרנו.”",
            [Keys.DealOffended] = "“עלבון.” הוא יוצא בכעס. השם שלך ייפגע. (מוניטין -1)",
            [Keys.DealGaveUp] = "“עזוב, לא משנה.” הוא אוסף את החפצים והולך.",
            [Keys.DealCheckAtLeastOne] = "סמן לפחות פריט אחד כדי לבצע עסקה.",
            [Keys.DealNoCashForOffer] = "אין לך מספיק מזומן לכסות את ההצעה.",
            [Keys.DealNoCashForAsking] = "אין לך מספיק מזומן במחיר המבוקש.",
            [Keys.DealBoughtAtAsking] = "נקנה במחיר המבוקש — ${0}. עסקה הוגנת. (מוניטין +1){1}",
            [Keys.DealRejected] = "אתה מסמן לו ללכת. הוא לוקח את הסחורה למקום אחר.",
            [Keys.DealLeftoverSuffix] = " הוא לוקח בחזרה את מה שלא קנית.",
            [Keys.OfferArmHint] = "הקלד את ההצעה שלך — לחץ Enter או לחץ שוב על הצע.",

            [Keys.SellUnavailable] = "אין קונה לפריט הזה בערוץ הזה.",
            [Keys.SellNotOwned] = "הפריט הזה כבר לא נמצא על המדפים שלך.",
            [Keys.SoldFor] = "{0} נמכר תמורת ${1}.",
            [Keys.ConsequenceCollectorFakeAngry] = " האספן זועם — זה היה זיוף. (מוניטין -2)",
            [Keys.ConsequenceCollectorHotQuestions] = " האספן שואל שאלות לגבי המקור. (חום +2)",
            [Keys.ConsequenceCollectorSatisfied] = " אספן מרוצה מפיץ את השם הטוב שלך.",
            [Keys.ConsequenceBlackMarketHotMoved] = " הוברח בשקט מהעיר, בלי שאלות. (חום -1)",
            [Keys.ConsequenceBlackMarketCleanNoted] = " החוג של הסוחר שם לב לעסק שלך. (חום +1)",
            [Keys.ConsequenceShopfrontFakeReturned] = " קונה כועס החזיר את זה כזיוף. (מוניטין -1)",
            [Keys.ConsequenceShopfrontHotRecognized] = " מישהו זיהה את זה בחלון הראווה — המשטרה שואלת סביב. (חום +3)",

            [Keys.UpgradeAlreadyOwned] = "כבר יש לך את {0}.",
            [Keys.UpgradeCantAfford] = "אין לך מספיק כסף בשביל {0} — זה עולה ${1} ויש לך ${2}.",
            [Keys.UpgradeInstalled] = "{0} הותקן. ${1} שהושקעו היטב — כנראה.",

            [Keys.ItemStatsLineCounter] = "מצב: {0} · שווי: {1}",
            [Keys.ItemStatsLineInventory] = "מצב: {0} · שווי: {1} · שולם ${2}",

            [Keys.CategoryWatches] = "שעונים",
            [Keys.CategoryJewelry] = "תכשיטים",
            [Keys.CategoryElectronics] = "אלקטרוניקה",
            [Keys.CategoryMusicalInstruments] = "כלי נגינה",
            [Keys.CategoryRetroCollectibles] = "אספנות רטרו",
            [Keys.CategoryAntiquesCurios] = "עתיקות ותשמישי נוי",
            [Keys.CategoryLuxuryAccessories] = "אביזרי יוקרה",
            [Keys.CategoryToolsPracticalGoods] = "כלים ומוצרים שימושיים",

            [Keys.ConditionPristine] = "מושלם",
            [Keys.ConditionClean] = "נקי",
            [Keys.ConditionWorn] = "שחוק",
            [Keys.ConditionDamaged] = "פגום",
            [Keys.ConditionBroken] = "שבור",

            [Keys.MoodNeutral] = "ניטרלי",
            [Keys.MoodFriendly] = "ידידותי",
            [Keys.MoodNervous] = "עצבני",
            [Keys.MoodImpatient] = "חסר סבלנות",
            [Keys.MoodDesperate] = "נואש",
            [Keys.MoodOffended] = "נעלב",

            [Keys.EffectConditionAccuracy] = "בדיקה · מצב",
            [Keys.EffectFakeDetection] = "בדיקה · זיופים",
            [Keys.EffectValueAccuracy] = "בדיקה · הערכת שווי",
            [Keys.EffectTool] = "כלי",

            [Keys.ShopOpenName] = "החנות פתוחה",
            [Keys.ShopClosedName] = "החנות סגורה",
            [Keys.WaitingForFirstCustomer] = "ממתין ללקוח הראשון...",
            [Keys.DayIsOver] = "יום {0} הסתיים. שעון החוב ממשיך לתקתק.",
            [Keys.QueueCustomersToday] = "{0} לקוחות בתור היום",
            [Keys.QueueLastCustomer] = "הלקוח האחרון של היום",
            [Keys.QueueMoreWaiting] = "עוד {0} מחכים בחוץ",

            [Keys.MoodAskingLine] = "מצב רוח: {0}     מבוקש: {1}",
            [Keys.MoodOnlyLine] = "מצב רוח: {0}",
            [Keys.DealNothingToTrade] = "אין לו כלום שכדאי לך לקנות.",
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

using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Market;
using UnityEngine;

namespace PawnshopKing.Systems.Debt
{
    public struct DebtTickResult
    {
        public bool paymentWasDue;
        public bool paid;
        public bool forcedSale;
        public int itemsSeized;
        public bool debtCleared;
        public bool gameOver;
        /// <summary>Ran out of cash AND inventory with debt still owed — no way forward.</summary>
        public bool bankrupt;
        /// <summary>What tonight's payment demanded (when one was due).</summary>
        public int amountDue;
        /// <summary>What was actually paid (when a payment went through).</summary>
        public int amountPaid;
        /// <summary>Human-readable summary line (English; the UI composes localized text from the fields above).</summary>
        public string message;
    }

    /// <summary>
    /// Tracks debt milestones and penalties (GDD 39). Runs at end of day: ticks the
    /// clock, auto-pays when due if cash covers it, and otherwise applies the teeth —
    /// reputation loss, creditors force-selling inventory at rock-bottom prices, and
    /// campaign failure when obligations can't be met at all (GDD 27.1).
    /// All numbers are tuning values; the GDD prescribes the pressure, not the rates.
    /// </summary>
    public static class DebtSystem
    {
        public const int PaymentIntervalDays = 7;
        private const float PaymentEscalation = 1.25f;   // each payment bites harder
        private const float MissedPenalty = 1.1f;        // missing adds 10% to the principal
        private const float ForcedSaleRate = 0.4f;       // creditors liquidate at 40% of shopfront

        public static DebtTickResult ProcessEndOfDay(GameState state)
        {
            var result = Tick(state);

            // Bankruptcy (GDD 27.1): no cash, nothing left to sell, debt still owed —
            // the campaign has no way forward, so end it now rather than weeks later.
            if (!result.gameOver && !result.debtCleared && state.debt.totalDebt > 0
                && state.cash <= 0 && state.inventory.Count == 0)
            {
                result.gameOver = true;
                result.bankrupt = true;
                result.message = "No cash and nothing left to sell — the shop is bankrupt.";
            }

            return result;
        }

        private static DebtTickResult Tick(GameState state)
        {
            var result = new DebtTickResult();

            if (state.debt.totalDebt <= 0)
            {
                result.message = "You owe nothing. The shop is yours.";
                return result;
            }

            if (state.debt.daysUntilPayment > 0) state.debt.daysUntilPayment--;
            if (state.debt.daysUntilPayment > 0)
            {
                result.message = $"Next payment: ${state.debt.nextPaymentAmount:N0} in {state.debt.daysUntilPayment} day{(state.debt.daysUntilPayment == 1 ? "" : "s")}.";
                return result;
            }

            // Payment is due tonight.
            result.paymentWasDue = true;
            int amount = Mathf.Min(state.debt.nextPaymentAmount, state.debt.totalDebt);
            result.amountDue = amount;

            if (state.cash >= amount)
            {
                Pay(state, amount, ref result);
                return result;
            }

            // Missed. The collectors don't wait (GDD 27.1).
            state.debt.missedPayments++;
            state.reputation -= 2;
            state.debt.totalDebt = Mathf.RoundToInt(state.debt.totalDebt * MissedPenalty / 5f) * 5;
            result.forcedSale = true;

            result.itemsSeized = LiquidateUntilCovered(state, amount);

            if (state.cash >= amount)
            {
                Pay(state, amount, ref result);
                result.message = $"You couldn't cover the ${amount:N0} payment. Creditors seized {result.itemsSeized} item{(result.itemsSeized == 1 ? "" : "s")} at rock-bottom prices and took their money. Debt penalty +10%. (Reputation -2)";
                return result;
            }

            // Everything is gone and it still isn't enough (GDD 27.1).
            result.gameOver = true;
            result.message = $"The ${amount:N0} payment came due and there was nothing left to take. The shop is finished.";
            return result;
        }

        private static void Pay(GameState state, int amount, ref DebtTickResult result)
        {
            state.cash -= amount;
            state.debt.totalDebt -= amount;
            result.paid = true;
            result.amountPaid = amount;

            if (state.debt.totalDebt <= 0)
            {
                state.debt.totalDebt = 0;
                state.debt.nextPaymentAmount = 0;
                state.debt.daysUntilPayment = 0;
                result.debtCleared = true;
                result.message = $"Final payment of ${amount:N0} made. The inherited debt is PAID OFF.";
                return;
            }

            state.debt.nextPaymentAmount = Mathf.Min(
                Mathf.RoundToInt(state.debt.nextPaymentAmount * PaymentEscalation / 25f) * 25,
                state.debt.totalDebt);
            state.debt.daysUntilPayment = PaymentIntervalDays;

            result.message = $"Debt payment of ${amount:N0} made. ${state.debt.totalDebt:N0} remains — next payment ${state.debt.nextPaymentAmount:N0} in {PaymentIntervalDays} days.";
        }

        /// <summary>Creditors take the most valuable pieces first, at rock-bottom prices.</summary>
        private static int LiquidateUntilCovered(GameState state, int target)
        {
            var byValue = new List<ItemInstance>(state.inventory);
            byValue.Sort((a, b) =>
                MarketSystem.GetQuote(state, b, SellChannel.Shopfront).price.CompareTo(
                    MarketSystem.GetQuote(state, a, SellChannel.Shopfront).price));

            int seized = 0;
            foreach (var item in byValue)
            {
                if (state.cash >= target) break;

                int salvage = Mathf.Max(5, Mathf.RoundToInt(
                    MarketSystem.GetQuote(state, item, SellChannel.Shopfront).price * ForcedSaleRate / 5f) * 5);
                state.inventory.Remove(item);
                state.cash += salvage;
                seized++;
            }

            return seized;
        }
    }
}

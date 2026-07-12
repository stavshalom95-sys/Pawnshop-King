using System;

namespace PawnshopKing.Data.Runtime
{
    /// <summary>Campaign debt pressure — the main timer (GDD 8.1, 38.2).</summary>
    [Serializable]
    public class DebtState
    {
        public int totalDebt;
        public int nextPaymentAmount;
        public int daysUntilPayment;
        public int missedPayments;
    }
}

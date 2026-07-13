namespace PawnshopKing.Data
{
    /// <summary>MVP sell channels (GDD 15.1, 30): local retail, tag-matched collectors, and the fence.</summary>
    public enum SellChannel
    {
        Shopfront,
        Collector,
        BlackMarket
    }

    /// <summary>Campaign difficulty — part of GameState so it saves with the run.</summary>
    public enum Difficulty
    {
        Easy,
        Hard
    }
}

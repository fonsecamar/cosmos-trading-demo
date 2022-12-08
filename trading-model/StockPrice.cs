namespace trading_model
{
    /// <summary>
    /// Stock price entity
    /// </summary>
    public class StockPrice
    {
        public string symbol { get; set; }
        public DateTime timestamp { get; set; }
        public decimal lastAskPrice { get; set; }
        public decimal lastBidPrice { get; set; }
        public decimal avgAskPrice { get; set; }
        public decimal avgBidPrice { get; set; }
        public decimal minAskPrice { get; set; }
        public decimal minBidPrice { get; set; }
        public decimal maxAskPrice { get; set; }
        public decimal maxBidPrice { get; set; }
    }
}
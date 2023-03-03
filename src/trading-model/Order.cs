using Newtonsoft.Json;

namespace trading_model
{
    /// <summary>
    /// Order entity
    /// </summary>
    public class Order
    {
        public string id { get { return this.orderId.ToString(); } }
        public string orderId { get; set; }
        public string customerId { get; set; }
        public int quantity { get; set; }
        public string symbol { get; set; }
        public decimal price { get; set; }
        public string action { get; set; }
        public string status { get; set; }
        public string assetClass { get { return "equities"; } }
        public string type { get { return "order"; } }
        public DateTime createdAt { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? lastModifiedAt { get; set; }
    }
}
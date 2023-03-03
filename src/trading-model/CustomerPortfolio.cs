using Newtonsoft.Json;

namespace trading_model
{
    /// <summary>
    /// Customer portfolio entity
    /// </summary>
    public class CustomerPortfolio
    {
        public string id { get { return $"{this.customerId}_{this.symbol}"; } }
        public string symbol { get; set; }
        public string customerId { get; set; }
        public int quantity { get; set; }
        public decimal price { get; set; }
        [JsonIgnore]
        public decimal position { get { return price * quantity; } }
        public string assetClass { get; set; }
        public DateTime createdAt { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? lastModifiedAt { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ttl { get; set; }
    }
}

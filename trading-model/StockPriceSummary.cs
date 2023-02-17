using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trading_model
{
    public class AvgPrices
    {
        public decimal avgAskPrice { get; set; }
        public decimal avgBidPrice { get; set; }
    }
    public class StockPriceSummary
    {
        public StockPriceSummary()
        {
            summary = new Dictionary<string, AvgPrices>();
        }
        public string id = "stockprice_summary";
        // String in this situation is the stock symbol
        public Dictionary<string, AvgPrices> summary { get; set; }
    }
}

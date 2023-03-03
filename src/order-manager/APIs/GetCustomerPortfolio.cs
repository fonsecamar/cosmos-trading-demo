using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.APIs
{
    /// <summary>
    /// Get customer portfolio Function API
    /// customerId value is provided directly to Cosmos input binding
    /// </summary>
    public static class GetCustomerPortfolio
    {
        [FunctionName("GetCustomerPortfolio")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customerPortfolio/{customerId}")] HttpRequest req,
            [CosmosDB(
                databaseName: "trading",
                containerName: "customerPortfolio",
                SqlQuery = "select * from c where c.customerId = {customerId}",
                Connection = "CosmosDBConnection")] IEnumerable<CustomerPortfolio> portfolios,
            ILogger log)
        {
            if (portfolios == null || portfolios.Count() == 0)
                return new NotFoundResult();

            //Portfolio enumerable returned to caller
            return new OkObjectResult(portfolios);
        }
    }
}

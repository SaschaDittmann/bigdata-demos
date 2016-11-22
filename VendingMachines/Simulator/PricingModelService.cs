using System.Collections.Generic;
using System.Linq;
using DeployR;

namespace Simulator
{
    public class PricingModelService
    {
        readonly string _deployrEndpointUri = $"http://{System.Configuration.ConfigurationManager.AppSettings["deployrIPorDNS"]}:8050/deployr";
        readonly string _deployrUser = System.Configuration.ConfigurationManager.AppSettings["deployrUser"];
        readonly string _deployrPassword = System.Configuration.ConfigurationManager.AppSettings["deployrPassword"];

        public double GetSuggestedPrice(int age, string gender, string productSelected)
        {
            var deployrEndpoint = _deployrEndpointUri;

            // Create an instance of RClient
            var rClient = RClientFactory.createClient(deployrEndpoint);

            // Creat an RBasicAuthentication token from the deployrUser and deployrPassword
            RAuthentication authToken = new RBasicAuthentication(_deployrUser, _deployrPassword);

            // Login to DeployR
            var rUser = rClient.login(authToken);

            // Configure the call to execute the PredictPricingService.r script for the admin user
            var exec = rClient.executeScript("PredictPricingService.r", "admin", "",
                new AnonymousProjectExecutionOptions()
                {
                    preloadDirectory =
                        new ProjectPreloadOptions()
                        {
                            filename = "pricingModel.rda,inputExample.rda",
                            author = "admin,admin",
                            directory = "root,root"
                        },
                    csvrinputs = "age," + age + ",gender," + gender + ",productSelected," + productSelected,
                    routputs = {"prediction"}
                });

            // Extract the suggested price from the data frame contained in the first workspaceObject in exe.about().
            var dataFrame = (RDataFrame)exec.about().workspaceObjects[0];
            var resultsVector = (List<RData>) dataFrame.Value;
            var suggestedPrice = (double) resultsVector.First().Value;

            return suggestedPrice;
        }
    }
}

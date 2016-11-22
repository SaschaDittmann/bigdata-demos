using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DeviceControlConsole
{
    class Program
    {
        static string _storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=<your account name>;AccountKey=<your account key>";
        static string _IoTHubConnectionString = System.Configuration.ConfigurationManager.AppSettings["IoTHubServiceConnectionsString"];
        static ServiceClient _serviceClient;

        static void Main(string[] args)
        {
            string deviceId = "112358";
            bool keepLooping = true;

            while (keepLooping)
            {
                Console.WriteLine("Which promo would you like to push out?");
                Console.WriteLine("1. Soda");
                Console.WriteLine("2. Water");
                Console.WriteLine("Press any other key to quit.");

                char selection = Console.ReadKey().KeyChar;
                Console.WriteLine();

                if (selection == '1' || selection == '2')
                {
                    PromoPackage promoPackage = CreatePromoPackage(selection);

                    PushPromo(deviceId, promoPackage).Wait();

                    Console.WriteLine("Command sent");
                }

                else
                {
                    keepLooping = false;
                }
            }
        }

        static PromoPackage CreatePromoPackage(char selection)
        {
            PromoPackage promoPackage;
            if (selection == '1')
            {
                promoPackage = new PromoPackage()
                {
                    ImageUri = GetUriForPromoImage("Soda.png"),
                    Price = 0.99,
                    ProductId = 1,
                    ProductTitle = "soda"
                };
            }
            else
            {
                promoPackage = new PromoPackage()
                {
                    ImageUri = GetUriForPromoImage("Water.png"),
                    Price = 0.75,
                    ProductId = 2,
                    ProductTitle = "water"
                };
            }

            return promoPackage;
        }

        static string GetUriForPromoImage(string imageName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("promo");

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(imageName);

            return GetBlobSasUri(blockBlob);
        }

        static async Task PushPromo(string deviceId, PromoPackage promoPackage)
        {
            // Create a Service Client instance provided the _IoTHubConnectionString
            _serviceClient = ServiceClient.CreateFromConnectionString(_IoTHubConnectionString);

            var promoPackageJson = JsonConvert.SerializeObject(promoPackage);

            Console.WriteLine("Sending Promo Package:");
            Console.WriteLine(promoPackageJson);

            var commandMessage = new Message(Encoding.ASCII.GetBytes(promoPackageJson));

            // Send the command
            await _serviceClient.SendAsync(deviceId, commandMessage);
        }

        private static string GetBlobSasUri(CloudBlob blob)
        {
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };
            var sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

            return blob.Uri + sasBlobToken;
        }
    }
}

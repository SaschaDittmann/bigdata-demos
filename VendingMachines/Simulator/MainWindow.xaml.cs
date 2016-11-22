using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Face;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;

namespace Simulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string _storageConnectionString = System.Configuration.ConfigurationManager.AppSettings["storageConnectionString"];
        readonly string _faceApiKey = System.Configuration.ConfigurationManager.AppSettings["faceAPIKey"];

        string _vendingMachineId = "112358";
        string _itemName = "coconut water";
        double _purchasePrice = 1.25;
        int _itemId;
        string _imagePath = Path.GetFullPath("images/CoconutWater.png");

        DeviceClient _deviceClient;
        private RegistryManager _registryManager;
        private string _iotHubSenderConnectionString;
        private string _iotHubManagerConnectionString;
        private string _deviceKey;

        public MainWindow()
        {
            InitializeComponent();

            Init();
        }

        private async void btnTakePicture_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                await UpdateDynamicPrice(filename);
            }
        }


        private async Task UpdateDynamicPrice(string filename)
        {
            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("photos");

            // Retrieve reference to a blob named with the value of fileName.
            var blobName = Guid.NewGuid() + Path.GetExtension(filename);
            var blockBlob = container.GetBlockBlobReference(blobName);

            // Create or overwrite the blob with contents from a local file.
            using (var fileStream = File.OpenRead(filename))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            // Acquire a SAS Uri for the blob
            var sasUri = GetBlobSasUri(blockBlob);

            // Provide the SAS Uri to blob to the Face API
            var d = await GetPhotoDemographics(sasUri);

            // Invoke ML Model
            var pricingModel = new PricingModelService();
            var gender = d.Gender == "Female" ? "F" : "M";
            var suggestedPrice = pricingModel.GetSuggestedPrice((int)d.Age, gender, _itemName);

            SetPromo(_itemName, suggestedPrice, _itemId, _imagePath);
        }

        private void SetPromo(string title, double price, int productId, string imagePath)
        {
            textPromoTitle.Text = title;
            textPromoPrice.Text = $"{price:c}";
            ImageSource imageSource = new BitmapImage(new Uri(imagePath));
            promotedImage.Source = imageSource;

            _purchasePrice = price;
            _itemName = title;
            _itemId = productId;
            _imagePath = imagePath;
        }



        private async Task<Demographics> GetPhotoDemographics(string sasUri)
        {
            Demographics d = null;

            // Invoke Face API with URI to photo
            IFaceServiceClient faceServiceClient = new FaceServiceClient(_faceApiKey);

            // Configure the desired attributes Age and Gender
            IEnumerable<FaceAttributeType> desiredAttributes = new[]
            {
                FaceAttributeType.Age,
                FaceAttributeType.Gender
            };

            // Invoke the Face API Detect operation
            var faces = await faceServiceClient.DetectAsync(sasUri, false, true, desiredAttributes);

            if (faces.Length > 0)
            {
                // Extract the age and gender from the Face API response
                var computedAge = faces[0].FaceAttributes.Age;
                var computedGender = faces[0].FaceAttributes.Gender;

                d = new Demographics()
                {
                    Age = computedAge,
                    Gender = computedGender
                };
            }

            return d;
        }

        static string GetBlobSasUri(CloudBlockBlob blob)
        {
            // Create a Read blob and Write blob Shared Access Policy that is effective 5 minutes ago and for 2 hours into the future
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };

            // Construct the full URI with SAS
            var sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);
            return blob.Uri + sasBlobToken;
        }

        private async void btnBuy_Click(object sender, RoutedEventArgs e)
        {
            bool result = await CompletePurchaseAsync();
            if (result)
            {
                MessageBox.Show("Enjoy!", "Purchase Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Unable to Complete Purchase", "Oh no!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TrackPurchaseEvent(Transaction t)
        {
            try
            {
                var transactionJson = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                TransmitEvent(transactionJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }
        }

        private async Task<bool> CompletePurchaseAsync()
        {
            bool success = false;
            try
            {
                TransactionsModel model = new TransactionsModel();

                Transaction t = new Transaction()
                {
                    ItemName = _itemName,
                    PurchasePrice = (decimal)_purchasePrice,
                    TransactionDate = DateTime.UtcNow,
                    TransactionStatus = 2,
                    VendingMachineId = _vendingMachineId,
                    ItemId = _itemId
                };
                model.Transactions.Add(t);

                await model.SaveChangesAsync();

                success = true;

                TrackPurchaseEvent(t);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }

            return success;
        }

        public async void Init()
        {
            try
            {
                _iotHubSenderConnectionString = System.Configuration.ConfigurationManager.AppSettings["IoTHubSenderConnectionsString"];
                _iotHubManagerConnectionString = System.Configuration.ConfigurationManager.AppSettings["IoTHubManagerConnectionsString"];
                _registryManager = RegistryManager.CreateFromConnectionString(_iotHubManagerConnectionString);

                await RegisterDeviceAsync();

                InitDeviceClient();

                ListenForControlMessages();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }

        }

        void InitDeviceClient()
        {
            var builder = Microsoft.Azure.Devices.IotHubConnectionStringBuilder.Create(_iotHubSenderConnectionString);
            string iotHubName = builder.HostName;

            _deviceClient = DeviceClient.Create(iotHubName,
                new DeviceAuthenticationWithRegistrySymmetricKey(_vendingMachineId, _deviceKey),
                Microsoft.Azure.Devices.Client.TransportType.Mqtt);


        }

        async Task RegisterDeviceAsync()
        {
            Device device = new Device(_vendingMachineId);
            device.Status = DeviceStatus.Disabled;

            try
            {
                device = await _registryManager.AddDeviceAsync(device);
            }
            catch (Microsoft.Azure.Devices.Common.Exceptions.DeviceAlreadyExistsException)
            {
                //Device already exists, get the registered device
                device = await _registryManager.GetDeviceAsync(_vendingMachineId);
            }

            try
            {
                //Ensure device is activated
                device.Status = DeviceStatus.Enabled;
                await _registryManager.UpdateDeviceAsync(device);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }

            _deviceKey = device.Authentication.SymmetricKey.PrimaryKey;
        }

        private void TransmitEvent(string datapoint)
        {
            Microsoft.Azure.Devices.Client.Message message;
            try
            {
                message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(datapoint));

                _deviceClient.SendEventAsync(message);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }
        }

        private async void ListenForControlMessages()
        {
            Task.Delay(3000).Wait();

            while (true)
            {
                // Receive messages intended for the device via the instance of _deviceClient.
                var receivedMessage = await _deviceClient.ReceiveAsync();

                // A null message may be received if the wait period expired, so ignore and call the receive operation again
                if (receivedMessage == null) continue;

                // Deserialize the received binary encoded JSON message into an instance of PromoPackage.
                var receivedJson = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                System.Diagnostics.Trace.TraceInformation("Received message: {0}", receivedJson);
                var promo = Newtonsoft.Json.JsonConvert.DeserializeObject<PromoPackage>(receivedJson);

                ApplyPromoPackageAsync(promo);

                // Acknowledge receipt of the message with IoT Hub
                await _deviceClient.CompleteAsync(receivedMessage);
            }
        }


        private async void ApplyPromoPackageAsync(PromoPackage promo)
        {
            var blob = new CloudBlockBlob(new Uri(promo.ImageUri));
            var path = Path.GetFullPath($"{promo.ProductId}-{promo.ProductTitle}.png");
            if (!File.Exists(path))
            {
                await blob.DownloadToFileAsync(path, FileMode.Create);
            }

            SetPromo(promo.ProductTitle, promo.Price, promo.ProductId, path);
        }
    }
}

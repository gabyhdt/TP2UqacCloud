using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace QueueFunction
{
    public class Function1
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        // Constructeur de la classe Function1 qui initialise le logger et le client de service Blob
        public Function1(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
            _blobServiceClient = blobServiceClient;
        }

        // Fonction Azure déclenchée par un message dans la file d'attente Service Bus
        [Function("Function1")]
        public void Run([ServiceBusTrigger("functionqueue", Connection = "AzureWebJobsServiceBus")] string myQueueItem)
        {
            // Log un message indiquant que la fonction a traité un message de la file d'attente
            _logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            // Supposons que myQueueItem contient le nom du blob
            var containerClient = _blobServiceClient.GetBlobContainerClient("blob");
            var blobClient = containerClient.GetBlobClient(myQueueItem);

            // Télécharger le contenu du blob
            var downloadInfo = blobClient.DownloadContent();
            var imageBytes = downloadInfo.Value.Content.ToArray();

            // Charger l'image à partir du tableau d'octets
            using (var ms = new MemoryStream(imageBytes))
            {
                using (var image = Image.FromStream(ms))
                {
                    // Redimensionner l'image (exemple : redimensionner à 100x100 pixels)
                    var resizedImage = new Bitmap(image, new Size(100, 100));

                    // Enregistrer l'image redimensionnée dans un nouveau flux de mémoire
                    using (var resizedMs = new MemoryStream())
                    {
                        resizedImage.Save(resizedMs, ImageFormat.Jpeg);
                        var resizedImageBytes = resizedMs.ToArray();

                        // Traiter l'image redimensionnée (par exemple, enregistrer sur le disque, télécharger dans le stockage blob, etc.)
                        File.WriteAllBytes($"./resized-{myQueueItem}", resizedImageBytes);
                        _logger.LogInformation($"Image {myQueueItem} has been resized and saved.");
                    }
                }
            }
        }
    }
}
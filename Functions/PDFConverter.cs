using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using WebsiteWatcher.Services;

namespace WebsiteWatcher;

public class PDFConverter(ILoggerFactory loggerFactory, PDFCreatorService pdfservice)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PDFConverter>();

    [Function(nameof(PDFConverter))]
 //   [BlobOutput("/pdfs/new.pdf", Connection = "WebsiteWatcherStorage")]
    public async Task Run(
        [SqlTrigger("[dbo].[Websites]", "ODBConnectionString")] SqlChange<Website>[] change)
    {
   //     Byte[]? buffer = null; 
        foreach (var changeItem in change)
        {
            if (changeItem.Operation == SqlChangeOperation.Insert)
            {
                var pdf = await pdfservice.ConvertPageToPDF(changeItem.Item.Url);
                //buffer = new byte[pdf.Length];
                //await pdf.ReadAsync(buffer);
                _logger.LogInformation($"PDF Stream Length : {pdf.Length}");


                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:WebsiteWatcherStorage");
                if (connectionString != null)
                {
                    var blobClient = new BlobClient(connectionString, "pdfs", $"{changeItem.Item.Id}.pdf");
                    var blobServiceClient = new BlobServiceClient(connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient("pdfs");
                    containerClient.CreateIfNotExists();
                    var result = await blobClient.UploadAsync(pdf);
                    _logger.LogInformation($"result {result.Value}, {result.GetRawResponse()}");
                }
            }
        }
   //     return buffer;
    }

   
}

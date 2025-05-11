using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PuppeteerSharp;
using Azure.Storage.Blobs;

namespace WebsiteWatcher;

public class PDFConverter(ILoggerFactory loggerFactory)
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
                var pdf = await ConvertPageToPDF(changeItem.Item.Url);
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

    private async Task<Stream> ConvertPageToPDF(string url)
    {
        var chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        if (!File.Exists(chromePath))
        {
            throw new FileNotFoundException($"Chrome not found at path: {chromePath}");
        }

        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = chromePath
        });

        await using (browser)
        {
            var page = await browser.NewPageAsync();
            await page.GoToAsync(url);

            await page.EvaluateFunctionAsync("() => document.fonts.ready.then(() => true)");

            var pdfStream = await page.PdfStreamAsync();
            pdfStream.Position = 0;
            return pdfStream;
        }
    }

}

using System;
using Azure.Storage.Blobs;
using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using static WebsiteWatcher.SnapShot;

namespace WebsiteWatcher;

public class Watcher
{
    private readonly ILogger _logger;

    public Watcher(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Watcher>();
    }

    private const string command = "SELECT w.Id, w.Url, w.XPathExpression, ss.Content as LatestContent FROM dbo.Websites AS w LEFT JOIN dbo.[Snapshot] AS ss  ON w.Id = ss.Id WHERE ss.[TimeStamp] = (SELECT MAX(TimeStamp) FROM dbo.[Snapshot] WHERE ID = W.ID);";

    [Function(nameof(Watcher))]
    [SqlOutput("dbo.SnapShot", "ODBConnectionString")]
    public async Task<SnapShotRecord> Run([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer,
        [SqlInput(command, "ODBConnectionString")] IReadOnlyList<WatcherModel> websites)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        SnapShotRecord? latestSnapShot = null;
        foreach (var website in websites)
        {
            HtmlWeb htmlWeb = new();
            HtmlDocument doc = htmlWeb.Load(website.Url);
            var docWithContent = doc.DocumentNode.SelectSingleNode(website.XPathExpression);
            var content = docWithContent != null ? docWithContent.InnerText.Trim() : "No Data Found";

            //Testing to change content
            content = content.Replace("Microsoft Entra", "Azure AD");

            bool contentHasChanged = content != website.LatestContent;
            if (contentHasChanged)
            {
                _logger.LogInformation($"AlERT Content Changed for this url {website.Url} ");
                var newPdf = await ConvertPageToPDF(website.Url);

                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:WebsiteWatcherStorage");
                if (connectionString != null)
                {
                    var blobClient = new BlobClient(connectionString, "pdfs", $"{website.Id} - {DateTime.UtcNow:MMddyyyyhhmmss}.pdf");
                    var blobServiceClient = new BlobServiceClient(connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient("pdfs");
                    containerClient.CreateIfNotExists();
                    var result = await blobClient.UploadAsync(newPdf);
                    _logger.LogInformation($"New PDF Uploaded.{result.Value}, {result.GetRawResponse()}");
                    latestSnapShot = new SnapShotRecord(website.Id, content);
                }
            }
        }
        return latestSnapShot;
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

    public class WatcherModel
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string? XPathExpression { get; set; }
        public string LatestContent { get; set; }
    }
}
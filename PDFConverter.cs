using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PuppeteerSharp;

namespace WebsiteWatcher;

public class PDFConverter(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PDFConverter>();

    [Function(nameof(PDFConverter))]
    [BlobOutput("/pdfs/new.pdf", Connection = "WebsiteWatcherStorage")]
    public async Task<byte[]?> Run(
        [SqlTrigger("[dbo].[Websites]", "ODBConnectionString")] SqlChange<Website>[] change)
    {
        Byte[]? buffer = null; 
        foreach (var changeItem in change)
        {
            if (changeItem.Operation == SqlChangeOperation.Insert)
            {
                var pdf = await ConvertPageToPDF(changeItem.Item.Url);
                buffer = new byte[pdf.Length];
                await pdf.ReadAsync(buffer);
                _logger.LogInformation($"PDF Stream Length : {pdf.Length}");
            }
        }
        return buffer;
    }

    private async Task<Stream> ConvertPageToPDF(string url)
    {
        var browserFetcher = new BrowserFetcher();
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url);
        await page.EvaluateExpressionHandleAsync("documents.fonts.ready");
        var result = await page.PdfStreamAsync();
        result.Position = 0;
        return result;
    }
}

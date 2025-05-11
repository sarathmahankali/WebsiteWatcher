using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebsiteWatcher.Services;

public class PDFCreatorService
{
    public async Task<Stream> ConvertPageToPDF(string url)
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

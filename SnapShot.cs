using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using System.Text.Json.Serialization;
using PuppeteerSharp;

namespace WebsiteWatcher;

public class SnapShot(ILogger<SnapShot> logger)
{
    [Function("SnapShot")]
    [SqlOutput("dbo.SnapShot" , "ODBConnectionString")]
    public SnapShotRecord? Run(
       [SqlTrigger("[dbo].[Websites]", "ODBConnectionString")] IReadOnlyList<SqlChange<Website>> changes)
    {
        SnapShotRecord snap = null;
        foreach (var change in changes)
        {
            if(change.Operation != SqlChangeOperation.Insert)
            {
                continue;
            }

            HtmlWeb htmlWeb = new();
            HtmlDocument doc = htmlWeb.Load(change.Item.Url);
            var docWithContent = doc.DocumentNode.SelectSingleNode(change.Item.XPathExpression);
            var content = docWithContent != null ? docWithContent.InnerText.Trim() : "No Data Found";

            logger.LogInformation(content);
            snap = new SnapShotRecord(change.Item.Id, content);
        }
        return snap;
    }

    public record SnapShotRecord ([property: JsonPropertyName("Id")] Guid id, [property: JsonPropertyName("Content")] string content);
    
}

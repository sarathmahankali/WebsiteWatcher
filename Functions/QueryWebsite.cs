using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace WebsiteWatcher;

public class QueryWebsite(ILogger<QueryWebsite> logger)
{
    private readonly ILogger<QueryWebsite> _logger = logger;

    private const string command = @"SELECT w.Id,w.Url, w.XPathExpression,ss.TimeStamp FROM WEBSITES AS W
                                    LEFT JOIN snapshot AS S ON W.ID = S.ID
                                    WHERE S.[TIMESTAMP] = (SELECT MAX(TIMESTAMP) FROM SNAPSHOT WHERE ID = W.ID)
                                    AND S.[TIMESTAMP] BETWEEN DATEADD(HOUR, -3, GETUTCDATE()) AND GETUTCDATE();";

    [Function(nameof(QueryWebsite))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req ,
        [SqlInput(command, "ODBConnectionString")] IReadOnlyList<dynamic> websites)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(websites);
    }
}
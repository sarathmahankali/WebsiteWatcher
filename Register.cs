using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WebsiteWatcher
{
    public static class Register
    {
        [Function(nameof(Register))]
        public static async Task<RegisterOutput> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
    FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Register));
            logger.LogInformation("Processing registration.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var website = JsonSerializer.Deserialize<Website>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            website.Id = Guid.NewGuid();

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(website);
            return new RegisterOutput
            {
                HttpResponse = response,
                Website = website
            };
        }

    }

    public class RegisterOutput
    {
        [SqlOutput("[dbo].[Websites]", "ODBConnectionString")]
        public Website Website { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }

    public class Website
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string? XPathExpression {  get; set; }
    }


}

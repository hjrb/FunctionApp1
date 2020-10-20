using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace FunctionApp1
{
    public static class Function1
    {
        [FunctionName("GetPersons")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;
            

            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = "tcp:dbserverhjrp.database.windows.net,1433",
                UserID = "hjrp",
                PersistSecurityInfo = false,
                IntegratedSecurity = false,
                Password = "BwbPPe46qwS",
                InitialCatalog = "dbhjrp",
                Encrypt = true

            };
            log.LogInformation(builder.ConnectionString);
            var persons = new List<object>();
            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (SqlException e)
                {
                    log.LogError(e, "Failed to access database");
                    return new BadRequestObjectResult("Database access failure");
                }
                var sql = "SELECT ID, LastName, FirstName FROM dbo.Persons Order by LastName, FirstName";
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var person = new
                    {
                        LastName = reader.GetString(1),
                        FirstName = reader.GetString(2),
                        ID = reader.GetInt32(0)
                    };
                    persons.Add(person);
                    // Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                }
            }
            log.LogInformation($"Found {persons.Count} persons.");
            var personsJson = JsonConvert.SerializeObject(persons);
            return new JsonResult(personsJson);
            // string responseMessage = string.IsNullOrEmpty(name)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //             : $"Hello, {name}. This HTTP triggered function executed successfully.";

            //         return new OkObjectResult(responseMessage);
        }
    }
}

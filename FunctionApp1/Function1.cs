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
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;

namespace FunctionApp1
{
    public static class Function1
    {
        //public static async Task<string> GetSecretValue(string keyName)
        //{
        //    var azureServiceTokenProvider = new AzureServiceTokenProvider();
        //    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        //    //slow without ConfigureAwait(false)    
        //    //keyvault should be keyvault DNS Name    
        //    var uri = Environment.GetEnvironmentVariable("VaultUri");
        //    var secretBundle = await keyVaultClient.GetSecretAsync(uri + keyName).ConfigureAwait(false);
        //    return secretBundle.Value;

        //}

        [FunctionName("GetPersons")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger logger)
        {
            logger.LogInformation("C# HTTP trigger function processed a request.");

            string ID  = req.Query["ID"];

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            ID ??= data?.ID;
            logger.LogInformation("ID: " + ID);

            var config = new ConfigurationBuilder()
              //.SetBasePath(context.FunctionAppDirectory)
              // .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .Build();

            // Access our secret setting, normally as any other setting
            // see https://www.youtube.com/watch?v=6HKj5hOuD00&ab_channel=MicrosoftAzure

            var connectionString = config["dbConnection"];
            //logger.LogDebug($"{connectionString}");

            //var builder = new SqlConnectionStringBuilder()
            //{
            //    DataSource = "tcp:dbserverhjrp.database.windows.net,1433",
            //    UserID = "hjrp",
            //    PersistSecurityInfo = false,
            //    IntegratedSecurity = false,
            //    Password = "BwbPPe46qwS",
            //    InitialCatalog = "dbhjrp",
            //    Encrypt = true

            //};
            // logger.LogInformation(builder.ConnectionString);
            var persons = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (SqlException e)
                {
                    logger.LogError(e, "Failed to access database");
                    return new BadRequestObjectResult("Database access failure");
                }
                var sql = "SELECT ID, LastName, FirstName FROM dbo.Persons ";
                if (int.TryParse(ID, out var intID)) sql += " WHERE ID=@ID";
                else sql += " Order by LastName, FirstName";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ID", intID);
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
                }
            }
            logger.LogInformation($"Found {persons.Count} persons.");
            var personsJson = JsonConvert.SerializeObject(persons);
            return new JsonResult(personsJson);
        }
    }
}

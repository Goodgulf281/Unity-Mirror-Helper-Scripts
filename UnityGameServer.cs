using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server;
using Azure.ResourceManager.Compute.Models;

namespace UnityGameServers
{
    public class UnityGameServer
    {
        private readonly ILogger<UnityGameServer> _logger;

        public UnityGameServer(ILogger<UnityGameServer> log)
        {
            _logger = log;
        }

        [FunctionName("UnityGameServer")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger _logger)
        {

            // The first step is this Azure Function is to look at the HTTP query string and extract a command from it.
            // If you look at the Unity C# code in you'll see I just appended the command to the webRequest string.
            // While this is the easier way of doing it, you may want to consider adding the command to the POST data instead since that
            // will be encrypted and the URL will be plainly visible in a network sniffer.


            //public async UniTaskVoid RegisterServer(ServerData serverData, ServerCallBack serverCallBack)
            //{
            //    string json = JsonUtility.ToJson(serverData);
            //    byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            //    UnityWebRequest webRequest = UnityWebRequest.PostWwwForm("*********==&cmd=RegisterServer", "POST");

            //    webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);

            string command = req.Query["cmd"];

            _logger.LogWarning($"UnityGameServer is processing a request <{command}>.");

            // Next we POST data into s string so each command can use it to (for example) deserialize an object. 
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string responseMessage = "Invalid command";

            // As you can see below I did comment out some account keys in the code I published to Github since these will allow anyone access
            // to the Azure Storage components. I already started to use a safer way: add the keys to an Azure Key Vault and link them to
            // the Visual Studio project by means of the environment variables.
            // This article describes it step by step.
            //
            // https://microsoft.github.io/AzureTipsAndTricks/blog/tip271.html
            //
            // Note: you'll see I took a lot of inspiration from that site since it also explains how to use an Azure Storage Table.
            //
            // Azure Storage Table: https://microsoft.github.io/AzureTipsAndTricks/blog/tip82.html
            // Azure Storage Blob: https://microsoft.github.io/AzureTipsAndTricks/blog/tip95.html


            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();

            _logger.LogInformation("Created ConfigurationBuilder.");

            if (command == "RegisterServer")
            {
                // So, this is where the server registers itself when starting up, by calling this command.

                _logger.LogInformation("RegisterServer: execute this command.");

                // In the POST data we expect a class ServerData which contains all the necessary data to put into the Azure Storage Table:
                var data = JsonConvert.DeserializeObject<ServerData>(requestBody);

                // Now establish teh connection to the Table:
                var serviceClient = new TableServiceClient("DefaultEndpointsProtocol=https;AccountName=*****************==;EndpointSuffix=core.windows.net");
                TableClient table = serviceClient.GetTableClient("Servers");
                table.CreateIfNotExists();

                _logger.LogInformation("RegisterServer: retrieved Table");

                if (GetServerRecord(table, "UnityGameServer", data.id, _logger) == null)
                {
                    // OK, so this server has not registered itself before so we simply create a new record with the data from ServerData:
                    _logger.LogInformation("RegisterServer: server does not exist.");

                    var nowUTC = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

                    CreateServerRecord(table, new ServerDataRecord(data.id, data.ipAddress, data.port, data.scene, data.playerCount, nowUTC),_logger);

                    _logger.LogInformation("RegisterServer: created a new record.");

                    return new OkObjectResult($"Created server record for {data.ipAddress}:{data.port} running scene {data.scene}.");
                }
                else
                {
                    // Server record does exist, so update it with the ServerData

                    _logger.LogInformation($"RegisterServer: server exists, updating IP {data.ipAddress} and port {data.port}.");

                    UpdateServerRecord(table, data, "UnityGameServer", data.id, _logger);

                    return new OkObjectResult($"Updated server record for {data.ipAddress}:{data.port} running scene {data.scene}.");

                }
            }
            else if(command=="UpdateServer")
            {
                // This command is mainly used to update the player count for the server:
                _logger.LogInformation("UpdateServer: execute this command.");

                var data = JsonConvert.DeserializeObject<ServerData>(requestBody);

                var serviceClient = new TableServiceClient("DefaultEndpointsProtocol=https;AccountName=*****************************==;EndpointSuffix=core.windows.net");
                TableClient table = serviceClient.GetTableClient("Servers");
                table.CreateIfNotExists();


                // _logger.LogInformation("UpdateServer: retrieved Table");

                if (GetServerRecord(table, "UnityGameServer", data.id, _logger) == null)
                {
                    _logger.LogError("UpdateServer: server record does not exist.");

                    return new NotFoundResult();
                }
                else
                {
                    // Server record does exist

                    _logger.LogInformation($"UpdateServer: server exists, updating server {data.id} with IP {data.ipAddress} and player count {data.playerCount}.");

                    UpdateServerRecord(table, data, "UnityGameServer", data.id, _logger);

                    return new OkObjectResult($"Updated server record for {data.ipAddress}:{data.port} running scene {data.scene} with player count {data.playerCount}.");
                }
            }
            else if (command == "ListServers")
            {
                // This command is used by the clients to get a list of all available servers.

                _logger.LogWarning("ListServers: execute this command.");

                // Establish a connection to the Azure Storage Table

                var serviceClient = new TableServiceClient("DefaultEndpointsProtocol=https;AccountName=**************************==;EndpointSuffix=core.windows.net");
                TableClient table = serviceClient.GetTableClient("Servers");
                table.CreateIfNotExists();

                // Retrieve all server self register records as a list of ServerData

                _logger.LogInformation("ListServers: retrieved Table");
                List<ServerData> serverDatas = GetServerRecords(table, "UnityGameServer", _logger);
                _logger.LogInformation($"ListServers: found {serverDatas.Count} servers.");

                // Convert the list of ServerData into a ServerDataList which we can serialize
                ServerDataList serverDataList = new ServerDataList();
                serverDataList.data.AddRange(serverDatas);

                // Serialize the serverDataList then return it as a result for the UnityWebRequest to process
                string json = JsonConvert.SerializeObject(serverDataList);
                return new OkObjectResult(json);

            }
            else if (command == "ForceShutdownServers")
            {
                return await ForceShutdownServers(_logger, config);
            }
            else if (command == "StartServers")
            {
                return await StartServers(_logger, config);
            }
            else if (command == "StartServersWait")
            {
                return await StartServers(_logger, config, false);
            }
            else if (command == "ConditionalShutdownServer")
            {
                _logger.LogWarning("ConditionalShutdownServer: execute this command.");

                System.Threading.Thread.Sleep(5000); // Give Azure Functions time to process last player count updates.

                _logger.LogInformation("ConditionalShutdownServer: ended sleep cycle.");

                // Iterate through the list of servers and see if the combined player count across all servers is zero

                var serviceClient = new TableServiceClient("DefaultEndpointsProtocol=https;AccountName=************************************==;EndpointSuffix=core.windows.net");
                TableClient table = serviceClient.GetTableClient("Servers");
                table.CreateIfNotExists();


                _logger.LogInformation("ConditionalShutdownServer: retrieved Table");

                List<ServerData> serverDatas = GetServerRecords(table, "UnityGameServer", _logger);

                _logger.LogInformation($"ConditionalShutdownServer: retrieved {serverDatas.Count} items");

                int playerCount = 0;

                foreach (ServerData serverData in serverDatas)
                {
                    if(serverData.playerCount>0)
                        playerCount += serverData.playerCount;
                }

                _logger.LogInformation($"ConditionalShutdownServer: playercount across all servers = {playerCount}");

                if (playerCount > 0)
                {
                    return new OkObjectResult($"ConditionalShutdownServer completed, no shutdown executed.");
                }

                _logger.LogInformation("ConditionalShutdownServer force shutdown since player count is zero.");
                return await ForceShutdownServers(_logger, config);
            }

            return new OkObjectResult(responseMessage);
        }

        private static async Task<IActionResult> StartServers(ILogger _logger, IConfigurationRoot config, bool async=true)
        {
            // Added IAM Role Assignment on the Server VM in the Azure Portal for this Azure Funtion: Power On Off Contributor
            // Wihout it, the Azure Function is not allowed to control the VM
            // https://learn.microsoft.com/en-us/azure/role-based-access-control/role-assignments-steps
            // https://learn.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal

            // Uses Managed Identity (from Function App) already created for getting data from Key Vault:
            // https://microsoft.github.io/AzureTipsAndTricks/blog/tip271.html


            _logger.LogWarning($"StartServers: execute this command in async mode={async}.");

            // This is where we access the Azure Vault instead of using the access strings in teh source code:

            var subscriptionId = config["subscriptionId"];
            var resourceGroupName = config["resourceGroupName"];
            var resourceName = config["servers"];

            // To support more than one server I use a string in the key vault which contains them all: server1;server2;server3
            // The server name is actually the VM name in the portal
            string[] serverList = resourceName.Split(';');

            _logger.LogInformation($"StartServers: retrieved info for {resourceGroupName}.");

            // Now we need to go through a couple of steps to get access to the virtual machine and get it started:
            // https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.armclient?view=azure-dotnet

            var armClient = new ArmClient(new DefaultAzureCredential());

            _logger.LogInformation("StartServers: created ARM client.");

            int serverCount = serverList.Length;

            for (int i = 0;i<serverCount;i++)
            {
                string server = serverList[i];
                _logger.LogInformation($"StartServers: process server {server} at index {i}.");


                // https://www.blueboxes.co.uk/how-to-use-azure-management-apis-in-c-with-azureidentity

                var id = VirtualMachineResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, server);

                _logger.LogInformation("StartServers: created Resource Identifier.");

                var vm = armClient.GetVirtualMachineResource(new ResourceIdentifier(id));

                _logger.LogInformation("StartServers: retrieved VM Resource.");

                // https://stackoverflow.com/questions/75116030/get-azure-vm-powerstate-in-c-dotnet-using-azure-resourcemanager

                VirtualMachineInstanceView instanceView = vm.InstanceView();
                string vmName = instanceView.ComputerName;

                _logger.LogInformation($"StartServers: retrieved {vmName}.");

                bool isRunning = false;

                var statuses = instanceView.Statuses;
                foreach(var status in statuses)
                {
                    string stat = status.DisplayStatus.ToString();
                    _logger.LogInformation($"StartServers: status {stat}.");

                    if(stat.Contains("VM running"))
                        isRunning = true;
                }

                if (isRunning)
                {
                    // This server is already running
                    _logger.LogWarning($"StartServers: skipping server {vmName} since it is already running.");
                }
                else
                {
                    // Based on whether we want to wait on the servers to be started or we need a different path to start them up
                    try
                    {
                        if (!async && i == serverCount - 1)
                        {
                            // If we decide to wait, we only wait on the last server to be started up
                            // assuming the others will start in parallel and be ready more or less at the same time.

                            _logger.LogInformation($"StartServers: power on and waiting until completed.");

                            // https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.compute.virtualmachineresource.poweronasync?view=azure-dotnet

                            await vm.PowerOnAsync(Azure.WaitUntil.Completed);
                        }
                        else await vm.PowerOnAsync(Azure.WaitUntil.Started);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("StartServers: " + ex.Message);
                        return new BadRequestObjectResult("Error: PowerOnAsync failed");
                    }
                }
            }


            return new OkObjectResult($"StartServers started the VMs.");
        }

        private static async Task<IActionResult> ForceShutdownServers(ILogger _logger, IConfigurationRoot config)
        {
            _logger.LogWarning("ForceShutdownServers: execute this task.");

            var subscriptionId = config["subscriptionId"];
            var resourceGroupName = config["resourceGroupName"];
            var resourceName = config["servers"];

            // TODO: This code has not been updated yet to have mulitple servers in the servers environment variable

            var armClient = new ArmClient(new DefaultAzureCredential());

            var id = VirtualMachineResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, resourceName);

            //_logger.LogInformation("ForceShutdownServers: created Resource Identifier.");

            var result = armClient.GetVirtualMachineResource(new ResourceIdentifier(id));

            _logger.LogInformation("ForceShutdownServers: retrieved VM Resource.");

            try
            {
                // https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.compute.virtualmachineresource.deallocateasync?view=azure-dotnet

                await result.DeallocateAsync(Azure.WaitUntil.Started);

            }
            catch (Exception ex)
            {
                _logger.LogError("ForceShutdownServers: " + ex.Message);
                return new BadRequestObjectResult("Error: DeallocateAsync failed");
            }
            return new OkObjectResult($"Started deallocation of VM.");
        }

        static void CreateServerRecord(TableClient table, ServerDataRecord serverrecord, ILogger log)
        {
            // https://microsoft.github.io/AzureTipsAndTricks/blog/tip83.html

            log.LogInformation("CreateServerRecord(): adding record for server "+serverrecord.ID);

            table.AddEntity(serverrecord);
        }

        static void DeleteServerRecord(TableClient table, string partitionKey, string rowKey, ILogger log)
        {
            // https://microsoft.github.io/AzureTipsAndTricks/blog/tip86.html


            log.LogInformation($"DeleteServerRecord(): deleting record for {partitionKey} key {rowKey}");

            table.DeleteEntity(partitionKey, rowKey);
        }

        static void UpdateServerRecord(TableClient table, ServerData data, string partitionKey, string rowKey, ILogger log)
        {
            // https://microsoft.github.io/AzureTipsAndTricks/blog/tip85.html

            var nowUTC = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

            log.LogInformation("UpdateServerRecord(): Retrieve record for " + partitionKey + ", " + rowKey + ".");

            ServerDataRecord entity = table.GetEntity<ServerDataRecord>(partitionKey, rowKey);

            entity.IPAddress = data.ipAddress;
            entity.Port = data.port;
            entity.Scene = data.scene;
            entity.PlayerCount = data.playerCount;

            log.LogWarning($"UpdateServerRecord(): player count = {data.playerCount} for server {data.id}");

            entity.LastUpdateDate = nowUTC;

            log.LogInformation("UpdateServerRecord(): Update Entity with LastSaveDate=" + nowUTC + ".");

            table.UpdateEntity(entity, ETag.All, TableUpdateMode.Replace);
        }


        static ServerDataRecord GetServerRecord(TableClient table, string partitionKey, string rowKey, ILogger log)
        {
            // Please refer to https://docs.microsoft.com/en-us/rest/api/storageservices/querying-tables-and-entities for more details about query syntax.

            // https://microsoft.github.io/AzureTipsAndTricks/blog/blog/tip84.html

            ServerDataRecord queryResult;

            log.LogInformation("GetServerRecord(): for " + partitionKey + ";" + rowKey + ".");

            try
            {
                Pageable<ServerDataRecord> serverRecords = table.Query<ServerDataRecord>(filter: $"PartitionKey eq '{partitionKey}' and RowKey eq '{rowKey}'");

                int count = serverRecords.Count<ServerDataRecord>();

                log.LogInformation("GetServerRecord(): record count = " + count + ".");

                if (count == 0)
                    return null;

                queryResult = serverRecords.Single();
            }
            catch (RequestFailedException e)
            {

                log.LogInformation("UnityGameServer:GetServerRecord exception: " + e.ErrorCode);
                return null;
            }

            log.LogInformation("GetServerRecord(): return query result.");
            return queryResult;
        }

        static List<ServerData> GetServerRecords(TableClient table, string partitionKey, ILogger log)
        {
            List<ServerData> result = new List<ServerData>();

            log.LogInformation($"GetServerRecords(): retrieve all server records");

            Pageable<ServerDataRecord> queryResults = table.Query<ServerDataRecord>("(PartitionKey eq '" + partitionKey + "')"); 

            log.LogInformation("GetServerRecords(): return query count = " + queryResults.Count<ServerDataRecord>());

            foreach (ServerDataRecord record in queryResults)
            {
                ServerData serverData = new ServerData();

                serverData.port = record.Port;
                serverData.scene = record.Scene;
                serverData.playerCount = record.PlayerCount;
                serverData.ipAddress = record.IPAddress;
                serverData.id = record.ID;

                result.Add(serverData);

                log.LogInformation($"GetServerRecords(): loop, added result for {serverData.id}, with player count {serverData.playerCount}");
            }

            return result;
        }

    }
}


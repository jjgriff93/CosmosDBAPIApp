using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CosmosDBAPIApp.Models;

namespace CosmosDBAPIApp.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class DatabaseController : Controller
    {
        // Dependency inject to get cosmos DB creds from appsettings.json
        private readonly CosmosDBOptions _cosmosDBOptions;
        private CosmosDBClient cosmosDBClient;

        public DatabaseController(IOptions<CosmosDBOptions> cosmosDBOptions)
        {
            _cosmosDBOptions = cosmosDBOptions.Value;
            this.cosmosDBClient = new CosmosDBClient(_cosmosDBOptions.URI, _cosmosDBOptions.Key);
        }
        
        // APIs to pull and push data from cosmos DB =======================================================================
        [HttpGet("{collection}/{documentId}/{partitionKey}", Name = "GetDocument")]
        public async Task<ActionResult> GetDocument(string collection, string documentId, string partitionKey)
        {
            return await cosmosDBClient.GetDocument(_cosmosDBOptions.DatabaseName, collection, documentId, partitionKey);
        }

        [HttpGet("{collection}/{query}/{partitionKey}", Name = "QueryDocuments")]
        public ActionResult QueryDocuments(string collection, string query, string partitionKey)
        {
            return cosmosDBClient.QueryDocuments(_cosmosDBOptions.DatabaseName, collection, query, partitionKey);
        }

        [HttpGet("{collection}/{query}", Name = "QueryDocumentsCrossPartition")]
        public ActionResult QueryDocumentsCrossPartition(string collection, string query)
        {
            return cosmosDBClient.QueryDocumentsCrossPartition(_cosmosDBOptions.DatabaseName, collection, query);
        }

        [HttpPost("{collection}/{partitionKey}", Name = "CreateIfNotExists")]
        public async Task<ActionResult> CreateIfNotExists(string collection, string partitionKey, [FromBody]JObject jsonContent)
        {
            return await cosmosDBClient.CreateDocumentIfNotExists(_cosmosDBOptions.DatabaseName, collection, jsonContent, partitionKey);
        }

        [HttpPost("{collection}/{partitionKey}", Name = "CreateOrUpdateDocument")]
        public async Task<ActionResult> CreateOrUpdateDocument(string collection, string partitionKey, [FromBody]JObject jsonContent)
        {
            return await cosmosDBClient.CreateOrUpdateDocument(_cosmosDBOptions.DatabaseName, collection, jsonContent, partitionKey);
        }

        [HttpDelete("{collection}/{documentId}/{partitionKey}", Name = "DeleteDocument")]
        public async Task<ActionResult> DeleteDocument(string collection, string documentId, string partitionKey)
        {
            return await cosmosDBClient.DeleteDocument(_cosmosDBOptions.DatabaseName, collection, documentId, partitionKey);
        }
    }

    // Cosmos DB methods ===================================================================================================
    public class CosmosDBClient
    {
        private DocumentClient documentClient;

        public CosmosDBClient(string uri, string key)
        {
            this.documentClient = new DocumentClient(new Uri(uri), key);
        }

        public async Task<ActionResult> CreateDocumentIfNotExists(string databaseName, string collectionName, JObject jsonDocument, string partitionKey)
        {
            try
            {
                if (jsonDocument.ContainsKey("id"))
                {
                    var result = await this.documentClient.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, jsonDocument.Value<string>("id")),
                        new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("No valid 'id' field found in the document provided.");
                }
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    try
                    {
                        var result = await this.documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), jsonDocument,
                            new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                        return new CreatedResult(result.ContentLocation, result.Resource);
                    }
                    catch(Exception ex)
                    {
                        return new BadRequestObjectResult(ex.Message);
                    }
                }
                else
                {
                    return new BadRequestObjectResult("Unable to create document. The document may already exist.");
                }
            }
        }

        public async Task<ActionResult> GetDocument(string databaseName, string collectionName, string documentId, string partitionKey)
        {
            try
            {
                var result = await this.documentClient.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentId),
                    new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                return new JsonResult(result.Resource);
            }
            catch (DocumentClientException de)
            {
                return new BadRequestObjectResult(de.Message);
            }
        }

        public async Task<ActionResult> CreateOrUpdateDocument(string databaseName, string collectionName, JObject jsonDocument, string partitionKey)
        {
            try
            {
                if (jsonDocument.ContainsKey("id"))
                {
                    var result = await this.documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, jsonDocument.Value<string>("id")),
                        jsonDocument,
                        new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult("No valid 'id' field found in the document provided.");
                }
            }
            catch (DocumentClientException de)
            {
                return new BadRequestObjectResult(de.Message);
            }
        }

        public ActionResult QueryDocuments(string databaseName, string collectionName, string query, string partitionKey)
        {
            try
            {
                var result = this.documentClient.CreateDocumentQuery(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                    query,
                    new FeedOptions { PartitionKey = new PartitionKey(partitionKey) });
                return new JsonResult(result);
            }
            catch (DocumentClientException de)
            {
                return new BadRequestObjectResult(de.Message);
            }
        }

        public ActionResult QueryDocumentsCrossPartition(string databaseName, string collectionName, string query)
        {
            try
            {
                var result = this.documentClient.CreateDocumentQuery(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                    query,
                    new FeedOptions { EnableCrossPartitionQuery = true });
                return new JsonResult(result);
            }
            catch (DocumentClientException de)
            {
                return new BadRequestObjectResult(de.Message);
            }
        }

        public async Task<ActionResult> DeleteDocument(string databaseName, string collectionName, string documentId, string partitionKey)
        {
            try
            {
                var result = await this.documentClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentId),
                    new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                return new OkResult();
            }
            catch (DocumentClientException de)
            {
                return new BadRequestObjectResult(de.Message);
            }
        }
    }
}

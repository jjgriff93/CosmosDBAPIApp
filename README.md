# Introduction 
A simple Cosmos DB API app built in ASP.NET Core for acting as a middle layer between web/mobile applications and a Cosmos DB backend.

# Getting Started
1. Create your Cosmos DB instance along with a database, collection and document: https://docs.microsoft.com/en-us/azure/cosmos-db/create-cosmosdb-resources-portal
2. Pull and open the API app code in VS Code
3. Update the Cosmos DB config in the appsettings.json file with your Cosmos DB instance secret, database name and URI
4. Hit Debug and use Postman to query your API (which should be running at `localhost:5001`), updating the suffix with the associated action you want to perform, for example `/database/GetDocument/<COLLECTION NAME>/<DOCUMENT ID>/<PARTITION KEY>`. You can find all of the actions you can perform in the comments in the `DatabaseController.cs` file.

Feel free to add more and contribute to make this repo better :)

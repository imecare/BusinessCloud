using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace BusinessCloud.Infrastructure.Persistence;

public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoConnection"));
        // El nombre de la DB puede ser fijo o uno por empresa si escalas mucho
        _database = client.GetDatabase("BusinessCloud_Logs");
    }

    // Para que los clientes vean su historial rápido [cite: 7, 32]
    public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);
}
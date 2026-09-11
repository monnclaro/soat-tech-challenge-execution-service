using Infrastructure.Database.Documents;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Database;

// Wrapper fino sobre IMongoDatabase/IMongoCollection — MongoDB é schemaless, então não
// existe aqui um equivalente a DbContext/migrations do EF Core (ver OsServiceDbContext no
// OS Service para contraste). IMongoClient/IMongoDatabase/IMongoCollection são thread-safe
// e reutilizáveis, por isso este wrapper é registrado como singleton.
public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IOptions<MongoSettings> options)
    {
        MongoConventions.Configure();

        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.Database);
    }

    public IMongoCollection<ExecucaoOrdemServicoDocument> Execucoes =>
        _database.GetCollection<ExecucaoOrdemServicoDocument>("execucoes");
}

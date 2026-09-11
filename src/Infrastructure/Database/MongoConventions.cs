using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Infrastructure.Database;

// A partir da versão 2.19 do driver oficial, Guid deixou de ter uma representação BSON
// implícita (era o legado do modo "CSharpLegacy") — sem isto, qualquer Guid (inclusive o
// _id) lança BsonSerializationException ao serializar/desserializar. Registrar uma vez,
// globalmente, evita anotar [BsonRepresentation] em cada propriedade Guid dos documentos.
public static class MongoConventions
{
    private static bool _configured;

    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        BsonSerializer.RegisterSerializer(new NullableSerializer<Guid>(new GuidSerializer(BsonType.String)));

        _configured = true;
    }
}

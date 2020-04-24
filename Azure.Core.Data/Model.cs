using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.Data
{
    public static class Model
    {
        public static ReadWriteModel Create(params (string propertyName, object propertyValue)[] properties)
            => new ReadWriteDictionaryData(properties);

        public static ReadOnlyModel CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
            => new ReadOnlyDictionaryModel(properties);

        public static ReadWriteModel Create(ModelSchema schema)
            => new ReadWriteDictionaryData(schema);

        public static ReadWriteModel CreateWithJsonSchema(string schemaFile)
            => Create(JsonSchemaParser.ParseFile(schemaFile));

        public static ReadWriteModel CreateFromDictionary(IDictionary<string, object> properties) => new ReadWriteDictionaryData(properties);
        public static ReadOnlyModel CreateFromReadOnlyDictionary(IReadOnlyDictionary<string, object> properties) => new ReadOnlyDictionaryModel(properties);
        public static ReadOnlyModel CreateFromJson(string jsonObject) => new ReadOnlyJson(jsonObject);
        public static ReadOnlyModel CreateFromJson(Stream jsonObject) => new ReadOnlyJson(jsonObject);

        public static async Task<ReadOnlyModel> CreateFromJsonAsync(Stream jsonObject, CancellationToken cancellationToken = default) => await ReadOnlyJson.CreateAsync(jsonObject, cancellationToken);
    }
}
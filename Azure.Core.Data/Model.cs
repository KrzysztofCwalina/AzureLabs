using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.Data
{
    public static class Model
    {
        public static Data Create(params (string propertyName, object propertyValue)[] properties)
        {
            var data = new Data();
            foreach (var property in properties)
            {
                data[property.propertyName] = property.propertyValue;
            }
            return data;
        }

        public static ReadOnlyModel CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
            => new ReadOnlyDictionaryModel(properties);

        public static Data Create(ModelSchema schema)
            => new Data(schema);

        public static Data CreateWithJsonSchema(string schemaFile)
            => Create(JsonSchemaParser.ParseFile(schemaFile));

        public static ReadOnlyModel CreateFromReadOnlyDictionary(IReadOnlyDictionary<string, object> properties) => new ReadOnlyDictionaryModel(properties);
        public static ReadOnlyModel CreateFromJson(string jsonObject) => new ReadOnlyJson(jsonObject);
        public static ReadOnlyModel CreateFromJson(Stream jsonObject) => new ReadOnlyJson(jsonObject);

        public static async Task<ReadOnlyModel> CreateFromJsonAsync(Stream jsonObject, CancellationToken cancellationToken = default) => await ReadOnlyJson.CreateAsync(jsonObject, cancellationToken);
    }
}
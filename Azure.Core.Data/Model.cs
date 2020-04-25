using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.Data
{
    public static class Model
    {
        public static Data Create(params (string propertyName, object propertyValue)[] properties)
            => new Data(properties, isReadOnly: false);

        public static Data CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
            => new Data(properties, isReadOnly: true);

        public static Data Create(ModelSchema schema)
            => new Data(schema);

        public static Data CreateWithJsonSchema(string schemaFile)
            => Create(JsonSchemaParser.ParseFile(schemaFile));
    }
}
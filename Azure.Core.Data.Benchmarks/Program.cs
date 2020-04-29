using Azure.Core.Data.DataStores;
using Azure.Data;
using Azure.Search.Documents.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.NetCoreApp30)]
public class DynamicJsonBench
{
    static string s_demo_payload =
    "{" +
    "\"Id\" : \"ID0001\"," +        // service defined
    "\"CreatedAt\" : 123," +        // service defined
    "\"Decomissioned\" : true," +   // service defined

    "\"Temperature\" : 72," +       // user defined
    "\"Unit\" : \"F\"" +            // user defined
    "}";

    static IReadOnlyDictionary<string, object> dictionary;

    static DynamicData jsonData;
    static dynamic dynamicJsonData;

    static SearchDocument searchDocument;
    static dynamic dynamicSearchDocument;

    static Payload staticObject;
    static dynamic dynamicObject;
        
    // TODO: this shoould also test read-only data and with perfect hash
    static DynamicData data;
    static dynamic dynamicData = data;

    static DynamicData handcraftedHash;
    static dynamic dynamicHandcraftedHash;

    static DynamicData perfectHash;
    static dynamic dynamicPerfectHash;

    [GlobalSetup]
    public void Setup()
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);

        dictionary = properties;

        jsonData = JsonData.Create(s_demo_payload);
        dynamicJsonData = jsonData;

        data = new DynamicData();
        dynamicData = data;

        foreach (var property in jsonData.PropertyNames)
        {
            properties.Add(property, jsonData[property]);
            data[property] = jsonData[property];
        }

        searchDocument = new SearchDocument(properties);
        dynamicSearchDocument = searchDocument;

        staticObject = (Payload)dynamicJsonData;
        dynamicObject = staticObject;

        handcraftedHash = new DynamicData(new PerfectStore(properties));
        dynamicHandcraftedHash = handcraftedHash;

        perfectHash = new DynamicData(PerfectHashStore.Create(properties));
        dynamicPerfectHash = perfectHash;
    }

    [Benchmark]
    public string IndexerHandcraftedHash() => (string)handcraftedHash["Id"];

    [Benchmark]
    public string IndexerPerfectHash() => (string)perfectHash["Id"];

    [Benchmark]
    public string IndexerDictionaryStore() => (string)data["Id"];

    [Benchmark]
    public string IndexerJsonDocument() => (string)jsonData["Id"];

    [Benchmark]
    public string IndexerSearchDocument() => (string)searchDocument["Id"];

    [Benchmark(Baseline = true)]
    public string IndexerDictionary() => (string)dictionary["Id"];

    //[Benchmark]
    //public string DynamicPerfectHash() => dynamicPerfectHash.Id;

    //[Benchmark]
    //public string DynamicJsonDocument() => dynamicJsonData.Id;

    //[Benchmark]
    //public string DynamicDictionaryStore() => dynamicData.Id;

    //[Benchmark]
    //public string DynamicSearchDocument() => (string)dynamicSearchDocument.Id;

    //[Benchmark]
    //public string DynamicObject() => (string)dynamicObject.Id;
}

public class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

class Payload
{
    public string Id { get; set; }
    public byte CreatedAt { get; set; }
    public bool Decomissioned { get; set; }

    public byte Temperature { get; set; }
    public string Unit { get; set; }
}

// This is manualy created perfect hash dictionary
// TODO: schema should move to the store, so that it can compute perfect hash
class PerfectStore : PropertyStore
{
    // CreatedAt 67 => 0
    // Decomissioned 68;
    // Id 73
    // Temperature 84
    // Unit 85 - 67 => 18

    object[] _values = new object[19];

    public PerfectStore(IReadOnlyDictionary<string, object> properties)
    {
        foreach (var property in properties)
        {
            _values[GetIndex(property.Key)] = property.Value;
        }
    }

    protected override bool IsReadOnly => true;

    private static int GetIndex(string key) => key[0] - 67;

    protected override bool TryGetValue(string propertyName, out object propertyValue)
    {
        propertyValue = _values[GetIndex(propertyName)];
        return propertyValue != null;
    }

    protected override DynamicData CreateDynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
        => throw new NotImplementedException();

    protected override void SetValue(string propertyName, object propertyValue)
        => throw new NotImplementedException();

    protected override bool TryConvertTo(Type type, out object converted)
        => throw new NotImplementedException();

    protected override bool TryGetValueAt(int index, out object item)
        => throw new NotImplementedException();

    protected override IEnumerable<string> PropertyNames 
        => throw new NotImplementedException();
}

using Azure.Data;
using Azure.Search.Documents.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

//[SimpleJob(RuntimeMoniker.Net472, baseline: true)]
[SimpleJob(RuntimeMoniker.NetCoreApp30)]
//[RPlotExporter]
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

    static IReadOnlyDictionary<string, object> dict;

    static ReadOnlyJson roj = new ReadOnlyJson(s_demo_payload);
    static dynamic droj = roj;

    static SearchDocument sdoc;
    static dynamic dsdoc;

    static Payload obj;
    static dynamic dobj;
        
    static ReadOnlyModel rom;
    static dynamic drom;

    static ReadOnlyModel perfect;

    static Data data;
    static dynamic ddata = data;

    [GlobalSetup]
    public void Setup()
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);
        data = new Data();
        ddata = data;
        foreach (var property in roj.PropertyNames)
        {
            properties.Add(property, roj[property]);
            data[property] = roj[property];
        }
        sdoc = new SearchDocument(properties);
        dsdoc = sdoc;
        dict = properties;

        obj = (Payload)droj;
        dobj = obj;

        rom = Model.CreateFromReadOnlyDictionary(properties);
        drom = rom;

        perfect = Model.CreateFromReadOnlyDictionary(new PerfectDictionary(properties));        
    }

    [Benchmark]
    public string IndexerPerfectHash() => (string)perfect["Id"];

    [Benchmark]
    public string IndexerData() => (string)data["Id"];

    [Benchmark]
    public string IndexerReadOnlyModel() => (string)rom["Id"];

    [Benchmark]
    public string IndexerReadOnlyJson() => (string)roj["Id"];

    [Benchmark]
    public string IndexerSearchDocument() => (string)sdoc["Id"];

    [Benchmark(Baseline=true)]
    public string IndexerDictionary() => (string)dict["Id"];

    [Benchmark]
    public string DynamicReadOnlyJson() => droj.Id;

    [Benchmark]
    public string DynamicData() => ddata.Id;

    [Benchmark]
    public string DynamicReadOnlyModel() => (string)drom.Id;

    [Benchmark]
    public string DynamicSearchDocument() => (string)dsdoc.Id;

    [Benchmark]
    public string DynamicObject() => (string)dobj.Id;
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
class PerfectDictionary : IReadOnlyDictionary<string, object>
{
    // CreatedAt 67 => 0
    // Decomissioned 68;
    // Id 73
    // Temperature 84
    // Unit 85 - 67 => 18

    object[] _values = new object[19];

    public PerfectDictionary(IReadOnlyDictionary<string, object> properties)
    {
        foreach(var property in properties)
        {
            _values[GetIndex(property.Key)] = property.Value;
        }
    }

    public object this[string key] {
        get {
            return _values[GetIndex(key)];
        }
    }

    private static int GetIndex(string key) => key[0] - 67;

    public IEnumerable<string> Keys => throw new System.NotImplementedException();

    public IEnumerable<object> Values => throw new System.NotImplementedException();

    public int Count => throw new System.NotImplementedException();

    public bool ContainsKey(string key)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        value = this[key];
        return value != null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

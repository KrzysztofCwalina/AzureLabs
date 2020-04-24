using Azure.Data;
using Azure.Search.Documents.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Collections.Generic;

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

    class Payload
    {
        public string Id { get; set; }
        public byte CreatedAt { get; set; }
        public bool Decomissioned { get; set; }

        public byte Temperature { get; set; }
        public string Unit { get; set; }
    }

    static ReadOnlyJson roj = new ReadOnlyJson(s_demo_payload);
    static dynamic droj = roj;
    static SearchDocument sdoc;
    static dynamic dsdoc;
    static IReadOnlyDictionary<string, object> dict;
    static dynamic dobj;
    static Payload obj;

    [GlobalSetup]
    public void Setup()
    {
        var properties = new Dictionary<string, object>();
        foreach (var property in roj.PropertyNames)
        {
            properties.Add(property, roj[property]);
        }
        sdoc = new SearchDocument(properties);
        dsdoc = sdoc;
        dict = properties;

        obj = (Payload)droj;
        dobj = obj;
    }

    [Benchmark]
    public string IndexerReadOnlyJson() => (string)roj["Id"];

    [Benchmark]
    public string IndexerSearchDocument() => (string)sdoc["Id"];

    [Benchmark(Baseline=true)]
    public string IndexerDictionary() => (string)dict["Id"];

    [Benchmark]
    public string DynamicReadOnlyJson() => droj.Id;

    [Benchmark]
    public string DynamicSearchDocument() => (string)dsdoc.Id;

    [Benchmark]
    public string DynamicObject() => (string)dobj.Id;
}

public class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

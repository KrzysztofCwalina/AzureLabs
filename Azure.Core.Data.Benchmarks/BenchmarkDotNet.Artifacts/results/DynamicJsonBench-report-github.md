``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                Method |       Mean |     Error |    StdDev | Ratio | RatioSD |
|---------------------- |-----------:|----------:|----------:|------:|--------:|
|   IndexerReadOnlyJson | 91.4866 ns | 1.8002 ns | 1.7681 ns | 4.824 |    0.11 |
| IndexerSearchDocument | 19.0245 ns | 0.2879 ns | 0.2404 ns | 1.008 |    0.03 |
|     IndexerDictionary | 18.9890 ns | 0.4215 ns | 0.4329 ns | 1.000 |    0.00 |
|   DynamicReadOnlyJson | 94.6148 ns | 0.6723 ns | 0.6288 ns | 4.998 |    0.11 |
| DynamicSearchDocument | 47.3536 ns | 0.4689 ns | 0.4157 ns | 2.507 |    0.05 |
|         DynamicObject |  6.4372 ns | 0.1927 ns | 0.2825 ns | 0.344 |    0.02 |
|          StaticObject |  0.0000 ns | 0.0000 ns | 0.0000 ns | 0.000 |    0.00 |

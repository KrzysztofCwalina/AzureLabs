``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                Method |      Mean |     Error |    StdDev | Ratio | RatioSD |
|---------------------- |----------:|----------:|----------:|------:|--------:|
|    IndexerPerfectHash |  5.468 ns | 0.0537 ns | 0.0502 ns |  0.28 |    0.00 |
|           IndexerData | 21.945 ns | 0.3147 ns | 0.2628 ns |  1.14 |    0.01 |
|  IndexerReadOnlyModel | 22.685 ns | 0.0826 ns | 0.0645 ns |  1.18 |    0.01 |
|   IndexerReadOnlyJson | 84.577 ns | 0.5952 ns | 0.5276 ns |  4.39 |    0.05 |
| IndexerSearchDocument | 17.757 ns | 0.2696 ns | 0.2390 ns |  0.92 |    0.01 |
|     IndexerDictionary | 19.276 ns | 0.2170 ns | 0.1924 ns |  1.00 |    0.00 |
|   DynamicReadOnlyJson | 88.762 ns | 0.5012 ns | 0.4443 ns |  4.61 |    0.05 |
|           DynamicData | 27.216 ns | 0.2625 ns | 0.2455 ns |  1.41 |    0.02 |
|  DynamicReadOnlyModel | 30.088 ns | 0.0919 ns | 0.0860 ns |  1.56 |    0.02 |
| DynamicSearchDocument | 47.399 ns | 0.1690 ns | 0.1320 ns |  2.46 |    0.03 |
|         DynamicObject |  5.927 ns | 0.0227 ns | 0.0201 ns |  0.31 |    0.00 |

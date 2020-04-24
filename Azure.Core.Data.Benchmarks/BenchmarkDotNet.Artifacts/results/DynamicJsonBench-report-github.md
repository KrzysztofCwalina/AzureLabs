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
|  IndexerReadOnlyModel | 20.808 ns | 0.0607 ns | 0.0507 ns |  1.16 |    0.01 |
|   IndexerReadOnlyJson | 83.379 ns | 0.2321 ns | 0.2171 ns |  4.65 |    0.03 |
| IndexerSearchDocument | 17.943 ns | 0.0938 ns | 0.0877 ns |  1.00 |    0.01 |
|     IndexerDictionary | 17.920 ns | 0.1080 ns | 0.1010 ns |  1.00 |    0.00 |
|   DynamicReadOnlyJson | 88.340 ns | 1.7804 ns | 1.5782 ns |  4.93 |    0.08 |
|  DynamicReadOnlyModel | 26.712 ns | 0.3166 ns | 0.2643 ns |  1.49 |    0.02 |
| DynamicSearchDocument | 46.463 ns | 0.1171 ns | 0.0977 ns |  2.59 |    0.02 |
|         DynamicObject |  5.836 ns | 0.0336 ns | 0.0281 ns |  0.33 |    0.00 |

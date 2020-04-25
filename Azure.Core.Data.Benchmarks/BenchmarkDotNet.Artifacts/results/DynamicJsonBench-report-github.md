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
|    IndexerPerfectHash |  4.786 ns | 0.1467 ns | 0.1372 ns |  0.24 |    0.01 |
|           IndexerData | 25.857 ns | 0.5211 ns | 0.4351 ns |  1.30 |    0.02 |
|       IndexerJsonData | 84.516 ns | 0.4519 ns | 0.4227 ns |  4.26 |    0.03 |
| IndexerSearchDocument | 17.794 ns | 0.0603 ns | 0.0535 ns |  0.90 |    0.01 |
|     IndexerDictionary | 19.842 ns | 0.1236 ns | 0.1156 ns |  1.00 |    0.00 |
|    DynamicPerfectHash |  7.021 ns | 0.0486 ns | 0.0406 ns |  0.35 |    0.00 |
|       DynamicJsonData | 89.379 ns | 0.5695 ns | 0.4446 ns |  4.51 |    0.04 |
|           DynamicData | 25.672 ns | 0.0282 ns | 0.0220 ns |  1.30 |    0.01 |
| DynamicSearchDocument | 48.226 ns | 0.1671 ns | 0.1481 ns |  2.43 |    0.01 |
|         DynamicObject |  6.283 ns | 0.0234 ns | 0.0196 ns |  0.32 |    0.00 |

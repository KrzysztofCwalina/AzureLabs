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
|    IndexerPerfectHash |  4.891 ns | 0.0239 ns | 0.0212 ns |  0.23 |    0.00 |
|  IndexerReadOnlyModel | 23.607 ns | 0.0698 ns | 0.0653 ns |  1.11 |    0.01 |
|   IndexerReadOnlyJson | 85.186 ns | 0.2994 ns | 0.2801 ns |  4.02 |    0.02 |
| IndexerSearchDocument | 19.040 ns | 0.4388 ns | 0.5054 ns |  0.90 |    0.02 |
|     IndexerDictionary | 21.192 ns | 0.1133 ns | 0.0946 ns |  1.00 |    0.00 |

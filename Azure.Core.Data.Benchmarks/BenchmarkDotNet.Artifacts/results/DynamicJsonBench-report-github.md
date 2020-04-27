``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
| IndexerHandcraftedHash |  4.297 ns | 0.0586 ns | 0.0548 ns |  0.20 |    0.00 |     - |     - |     - |         - |
|     IndexerPerfectHash |  5.153 ns | 0.0482 ns | 0.0428 ns |  0.24 |    0.01 |     - |     - |     - |         - |
| IndexerDictionaryStore | 22.269 ns | 0.0786 ns | 0.0613 ns |  1.03 |    0.02 |     - |     - |     - |         - |
|    IndexerJsonDocument | 84.473 ns | 0.5920 ns | 0.4944 ns |  3.90 |    0.11 |     - |     - |     - |         - |
|  IndexerSearchDocument | 17.783 ns | 0.1477 ns | 0.1381 ns |  0.82 |    0.02 |     - |     - |     - |         - |
|      IndexerDictionary | 21.521 ns | 0.4843 ns | 0.5182 ns |  1.00 |    0.00 |     - |     - |     - |         - |

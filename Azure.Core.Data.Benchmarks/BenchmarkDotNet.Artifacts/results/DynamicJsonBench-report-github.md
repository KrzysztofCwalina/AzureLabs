``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                Method |     Mean |    Error |   StdDev |
|---------------------- |---------:|---------:|---------:|
|   IndexerReadOnlyJson | 90.89 ns | 1.312 ns | 1.404 ns |
| IndexerSearchDocument | 18.13 ns | 0.403 ns | 0.357 ns |
|     IndexerDictionary | 18.57 ns | 0.289 ns | 0.241 ns |
|   DynamicReadOnlyJson | 97.32 ns | 1.110 ns | 0.984 ns |
| DynamicSearchDocument | 48.45 ns | 0.110 ns | 0.086 ns |

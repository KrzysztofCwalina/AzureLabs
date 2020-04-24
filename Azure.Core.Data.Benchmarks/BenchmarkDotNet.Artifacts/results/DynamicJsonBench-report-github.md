``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                Method |      Mean |    Error |   StdDev |
|---------------------- |----------:|---------:|---------:|
|   IndexerReadOnlyJson |  93.82 ns | 1.020 ns | 0.904 ns |
| IndexerSearchDocument |  17.98 ns | 0.083 ns | 0.078 ns |
|   DynamicReadOnlyJson | 104.45 ns | 0.388 ns | 0.344 ns |
| DynamicSearchDocument |  48.40 ns | 0.280 ns | 0.262 ns |

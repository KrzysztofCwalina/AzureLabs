``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                Method |        Mean |    Error |   StdDev |
|---------------------- |------------:|---------:|---------:|
|   ReadOnlyJsonIndexer | 1,045.76 ns | 5.378 ns | 5.030 ns |
|   ReadOnlyJsonDynamic | 1,081.82 ns | 5.573 ns | 4.941 ns |
| SearchDocumentIndexer |    17.84 ns | 0.113 ns | 0.106 ns |
| SearchDocumentDynamic |    48.42 ns | 0.261 ns | 0.244 ns |

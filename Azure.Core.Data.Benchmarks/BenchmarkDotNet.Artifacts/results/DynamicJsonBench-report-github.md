``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.778 (1903/May2019Update/19H1)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015115
  [Host]        : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  .NET Core 3.0 : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

Job=.NET Core 3.0  Runtime=.NET Core 3.0  

```
|                Method |      Mean |     Error |    StdDev |
|---------------------- |----------:|----------:|----------:|
|   IndexerReadOnlyJson | 91.141 ns | 1.3245 ns | 1.1742 ns |
| IndexerSearchDocument | 19.077 ns | 0.1891 ns | 0.1579 ns |
|     IndexerDictionary | 18.433 ns | 0.1229 ns | 0.1149 ns |
|   DynamicReadOnlyJson | 95.144 ns | 0.5716 ns | 0.4773 ns |
| DynamicSearchDocument | 46.815 ns | 0.1257 ns | 0.1176 ns |
|         DynamicObject |  5.989 ns | 0.0518 ns | 0.0459 ns |

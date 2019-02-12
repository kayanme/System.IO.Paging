``` ini

BenchmarkDotNet=v0.10.14, OS=debian 9
Intel Xeon CPU 2.30GHz, 1 CPU, 1 logical core and 1 physical core
.NET Core SDK=2.1.503
  [Host] : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT


```
|                  Method | WriteMethod |         Mean |      Error |  Version |  Group |
|------------------------ |------------ |-------------:|-----------:|--------- |------- |
|               **AddRecord** |   **FixedSize** |     **3.673 us** |  **0.0438 us** | **0.0.12.0** | **Change** |
|      AddRecordWithFlush |   FixedSize | 1,066.949 us | 76.0870 us | 0.0.12.0 | Change |
| AddRecordGroupWithFlush |   FixedSize |   308.887 us |  3.0503 us | 0.0.12.0 | Change |
|               **AddRecord** |       **Naive** |     **3.105 us** |  **0.0367 us** | **0.0.12.0** | **Change** |
|      AddRecordWithFlush |       Naive |     3.139 us |  0.0389 us | 0.0.12.0 | Change |
| AddRecordGroupWithFlush |       Naive |   151.284 us |  0.9070 us | 0.0.12.0 | Change |
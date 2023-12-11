// See https://aka.ms/new-console-template for more information


using Axis.Pulsar.Core.Benchmarks.Json;
using BenchmarkDotNet.Running;

//var soloBenchmarker = new SoloPulsarBenchmark();
//soloBenchmarker.ParseJsonBool();
//soloBenchmarker.ParseJsonDecimal();
//soloBenchmarker.ParseJsonInt();
//soloBenchmarker.ParseJsonBool();
//soloBenchmarker.ParseJsonString();
//soloBenchmarker.ParseJsonScientificDecimal();
//soloBenchmarker.ParseJsonList();
//soloBenchmarker.ParseJsonList2();


BenchmarkRunner.Run<SoloPulsarBenchmark>();

Console.ReadKey(false);
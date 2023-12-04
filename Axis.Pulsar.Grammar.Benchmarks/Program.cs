// See https://aka.ms/new-console-template for more information

using Axis.Pulsar.Grammar.Benchmarks.Json;
using BenchmarkDotNet.Running;

var soloBenchmarker = new SoloPulsarBenchmark();
soloBenchmarker.ParseJson();

BenchmarkRunner.Run<SoloPulsarBenchmark>();

Console.ReadKey();
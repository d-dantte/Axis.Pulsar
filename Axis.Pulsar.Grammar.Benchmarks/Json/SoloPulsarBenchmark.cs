using BenchmarkDotNet.Attributes;

namespace Axis.Pulsar.Grammar.Benchmarks.Json
{
    [MemoryDiagnoser(false)]
    public class SoloPulsarBenchmark
    {
        [Benchmark]
        public void ParseJson()
        {
            _ = LangUtil.Grammar.RootRecognizer().TryRecognize(
                LangUtil.SampleJson,
                out var result);
        }
    }
}

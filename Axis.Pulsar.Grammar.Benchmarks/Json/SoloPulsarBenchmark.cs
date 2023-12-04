using BenchmarkDotNet.Attributes;

namespace Axis.Pulsar.Grammar.Benchmarks.Json
{
    public class SoloPulsarBenchmark
    {
        [Benchmark]
        public void ParseJson()
        {
            var success = LangUtil.Grammar.RootRecognizer().TryRecognize(
                LangUtil.SampleJson,
                out var result);

        }
    }
}

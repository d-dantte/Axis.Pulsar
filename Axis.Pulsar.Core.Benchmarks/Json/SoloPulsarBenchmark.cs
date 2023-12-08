using BenchmarkDotNet.Attributes;

namespace Axis.Pulsar.Core.Benchmarks.Json
{
    [MemoryDiagnoser(false)]
    public class SoloPulsarBenchmark
    {
        [Benchmark]
        public void ParseJson()
        {
            var result = LangUtil.LanguageContext.Recognize(LangUtil.SampleJson);

        }
    }
}

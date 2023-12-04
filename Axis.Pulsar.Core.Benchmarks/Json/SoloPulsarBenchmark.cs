using BenchmarkDotNet.Attributes;

namespace Axis.Pulsar.Core.Benchmarks.Json
{
    public class SoloPulsarBenchmark
    {
        [Benchmark]
        public void ParseJson()
        {
            var result = LangUtil.LanguageContext.Recognize(LangUtil.SampleJson);

        }
    }
}

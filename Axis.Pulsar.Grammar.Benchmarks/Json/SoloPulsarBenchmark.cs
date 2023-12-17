using Axis.Luna.Extensions;
using BenchmarkDotNet.Attributes;
using System.Diagnostics;

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

        public static void ParseJsonManualBenchmark(int callCount)
        {
            // warmup
            for (int cnt = 0; cnt < 10; cnt++)
            {
                _ = LangUtil.Grammar.RootRecognizer().TryRecognize(
                    LangUtil.SampleJson,
                    out var result);
            }

            // benchmark
            var counter = Stopwatch.StartNew();
            for (int cnt = 0; cnt < callCount; cnt++)
            {
                _ = LangUtil.Grammar.RootRecognizer().TryRecognize(
                    LangUtil.SampleJson,
                    out var result);
            }
            counter.Stop();

            var averageTicks = counter.ElapsedTicks / callCount;

            Console.WriteLine($"Total time: {new TimeSpan(counter.ElapsedTicks)}");
            Console.WriteLine($"Average time: {new TimeSpan(averageTicks)}, for call-count: {callCount}");
        }


        private static readonly string JsonStringSample = "the quick brown fox jumps".WrapIn("\"");
        private static readonly string JsonIntSample = "345435";
        private static readonly string JsonDecimalSample = "345435.654";
        private static readonly string JsonScientificSample = "-4.9403E-12";
        private static readonly string JsonBoolSample = "false";
        private static readonly string JsonListSample1 = "[]";
        private static readonly string JsonListSample2 = "[true, 43, [], -32.600001,[null,true,0.0E0  , []]]";

        [Benchmark]
        public void ParseJsonString()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-string");
            var success = rule.TryRecognize(
                JsonStringSample,
                out var result);
        }

        //[Benchmark]
        public void ParseJsonInt()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-number");
            var success = rule.TryRecognize(
                JsonIntSample,
                out var result);
        }

        [Benchmark]
        public void ParseJsonDecimal()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-number");
            var success = rule.TryRecognize(
                JsonDecimalSample,
                out var result);
        }

        [Benchmark]
        public void ParseJsonScientificDecimal()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-number");
            var success = rule.TryRecognize(
                JsonScientificSample,
                out var result);
        }

        [Benchmark]
        public void ParseJsonBool()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-boolean");
            var success = rule.TryRecognize(
                JsonBoolSample,
                out var result);
        }

        [Benchmark]
        public void ParseJsonList()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-list");
            var success = rule.TryRecognize(
                JsonListSample1,
                out var result);
        }

        [Benchmark]
        public void ParseJsonList2()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-list");
            var success = rule.TryRecognize(
                JsonListSample2,
                out var result);
        }

        [Benchmark]
        public void ParseJsonNull()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-null");
            var success = rule.TryRecognize(
                "null",
                out var result);
        }

        [Benchmark]
        public void ParseJsonNull_Fail()
        {
            var rule = LangUtil.Grammar.GetRecognizer("json-null");
            var success = rule.TryRecognize(
                "not-null",
                out var result);
        }


        //[Benchmark]
        public void TokenReader_ReadTokens10()
        {
            BufferedTokenReader reader = JsonListSample2;
            var success = reader.TryNextTokens(10, out var tokens);
        }


        //[Benchmark]
        public void TokenReader_ReadTokens40()
        {
            BufferedTokenReader reader = JsonListSample2;
            var success = reader.TryNextTokens(40, out var tokens);
        }


        //[Benchmark]
        public void TokenReader_ReadTokens2()
        {
            BufferedTokenReader reader = JsonListSample2;
            var success = reader.TryNextTokens(2, out var tokens);
        }
    }

}

using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using BenchmarkDotNet.Attributes;
using System.Diagnostics;
using System.Text;

namespace Axis.Pulsar.Core.Benchmarks.Json
{
    [MemoryDiagnoser(false)]
    public class SoloPulsarBenchmark
    {
        private static readonly SymbolPath ParentPath = SymbolPath.Of("Parent");
        private static readonly string JsonStringSample = "the quick brown fox jumps".WrapIn("\"");
        private static readonly string JsonIntSample = "345435";
        private static readonly string JsonDecimalSample = "345435.654";
        private static readonly string JsonScientificSample = "-4.9403E-12";
        private static readonly string JsonBoolSample = "false";
        private static readonly string JsonListSample1 = "[]";
        private static readonly string JsonListSample2 = "[true, 43, [], -32.600001,[null,true,0.0E0  , []]]";


        [Benchmark]
        public void ParseJson()
        {
            var result = LangUtil.LanguageContext.Recognize(LangUtil.SampleJson);
        }

        public static void ParseJsonManualBenchmark(int callCount)
        {
            // warmup
            for (int cnt = 0; cnt < 10; cnt++)
            {
                _ = LangUtil.LanguageContext.Recognize(LangUtil.SampleJson);
            }

            // benchmark
            var counter = Stopwatch.StartNew();
            for (int cnt = 0; cnt < callCount; cnt++)
            {
                _ = LangUtil.LanguageContext.Recognize(LangUtil.SampleJson);
            }
            counter.Stop();

            var averageTicks = counter.ElapsedTicks / callCount;

            Console.WriteLine($"Total time: {new TimeSpan(counter.ElapsedTicks)}");
            Console.WriteLine($"Average time: {new TimeSpan(averageTicks)}, for call-count: {callCount}");
        }
        public static string ToString(IReadOnlyDictionary<string, int> exceptionMap)
        {
            return exceptionMap
                .Aggregate(
                    func: (_sb, kvp) => _sb.AppendLine($"{kvp.Key}: {kvp.Value}"),
                    seed: new StringBuilder())
                .ToString();
        }

        //[Benchmark]
        public void InstantiateExceptionError()
        {
            var fre = new FailedRecognitionError("stuff", 54);
            var pre = new PartialRecognitionError("stuff", 2, 556);
        }

        //[Benchmark]
        public void InstantiatePlainError()
        {
            var fre = FailedRecognitionError.Of("stuff", 54);
            var pre = PartialRecognitionError.Of("stuff", 2, 556);
        }

        [Benchmark]
        public void ParseJsonString()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-string"];
            var success = rule.TryRecognize(
                JsonStringSample,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        [Benchmark]
        public void ParseJsonInt()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-number"];
            var success = rule.TryRecognize(
                JsonIntSample,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        [Benchmark]
        public void ParseJsonDecimal()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-number"];
            var success = rule.TryRecognize(
                JsonDecimalSample,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        //[Benchmark]
        public void ParseJsonScientificDecimal()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-number"];
            var success = rule.TryRecognize(
                JsonScientificSample,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        //[Benchmark]
        public void ParseJsonBool()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-boolean"];
            var success = rule.TryRecognize(
                JsonBoolSample,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        [Benchmark]
        public void ParseJsonList()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-list"];
            var success = rule.TryRecognize(
                JsonListSample1,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        [Benchmark]
        public void ParseJsonList2()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-list"];
            var success = rule.TryRecognize(
                JsonListSample2,
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        [Benchmark]
        public void ParseJsonNull()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-null"];
            var success = rule.TryRecognize(
                "null",
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }

        [Benchmark]
        public void ParseJsonNull_Fail()
        {
            var rule = LangUtil.LanguageContext.Grammar["json-null"];
            var success = rule.TryRecognize(
                "not-null",
                ParentPath,
                LangUtil.LanguageContext,
                out var result);
        }


        //[Benchmark]
        public void TokenReader_ReadTokens10()
        {
            TokenReader reader = JsonListSample2;
            var success = reader.TryGetTokens(10, out var tokens);
        }


        //[Benchmark]
        public void TokenReader_ReadTokens40()
        {
            TokenReader reader = JsonListSample2;
            var success = reader.TryGetTokens(40, out var tokens);
        }


        //[Benchmark]
        public void TokenReader_ReadTokens2()
        {
            TokenReader reader = JsonListSample2;
            var success = reader.TryGetTokens(2, out var tokens);
        }

        //[Benchmark]
        public void CreateExceptionFailedError()
        {
            FailedRecognitionError.Of("symbol", 45 - 4);
        }

        //[Benchmark]
        public void CreateStructFailedError()
        {
            FailedRecognitionError.Of("symbol", 45 - 4);
        }

        //[Benchmark]
        public void CreateExceptionPartialError()
        {
            PartialRecognitionError.Of("symbol", 45 - 4, 20);
        }

        //[Benchmark]
        public void CreateStructPartialError()
        {
            PartialRecognitionError.Of("symbol", 45 - 4, 20);
        }

    }
}

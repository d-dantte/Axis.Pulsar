using Axis.Pulsar.Grammar;
using Axis.Pulsar.Languages.xBNF;
using System.Text;

namespace Axis.Pulsar.Languages.IO.Tests.xBnf
{
    [TestClass]
    public class ImporterTests
    {
        [TestMethod]
        public void MiscImportTests()
        {
            try
            {
                #region others
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                var x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF1)));
                var result = x.RootRecognizer().Recognize(new BufferedTokenReader("foO"));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF2)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);




                timer.Restart();
                ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF3)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF4)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF5)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF6)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);
                #endregion


                timer.Restart();
                ruleImporter = new Importer();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                using var sampleGrammarStream = typeof(ImporterTests).Assembly
                    .GetManifestResourceStream($"{typeof(ImporterTests).Namespace}.TestGrammar.xbnf");
                x = ruleImporter.ImportGrammar(sampleGrammarStream);
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);
            }
            catch (Exception e)
            {
                throw;
            }
        }


        [TestMethod]
        public void SampleGrammarTest()
        {
            try
            {
                using var sampleGrammarStream = typeof(ImporterTests).Assembly
                    .GetManifestResourceStream($"{typeof(ImporterTests).Namespace}.TestGrammar.xbnf");

                var ruleImporter = new Importer();
                var x = ruleImporter.ImportGrammar(sampleGrammarStream);

                var result = x.RootRecognizer().Recognize(new BufferedTokenReader("1_000_000_000"));

                Assert.IsNotNull(x);
            }
            catch (Exception e)
            {
                throw;
            }
        }


        public static readonly string SampleBNF1 =
@"$grama -> +[?[$stuff $other-stuff.2 $more-stuff 'foo'] EOF]
# comments occupy a whole line.
$more-stuff -> $stuff

$stuff ::= /bleh///.i.5
$other-stuff ::= ""meh""
";

        public static readonly string SampleBNF2 =
@"$grama -> ?[#[$other-stuff $main-stuff].1,4 $nothing  $stuff ]>2
$stuff -> /\w+/
$other-stuff -> ""meh""

$main-stuff -> '34'

$nothing -> 'moja hiden'
";

        public static readonly string SampleBNF3 =
@"$grama ::= ?[$stuff $other-stuff].?>3
$stuff ::= /bleh+/.4,6
$other-stuff ::= ""meh""
";

        public static readonly string SampleBNF4 =
@"$grama ::= ?[$stuff $other-stuff $main-stuff].*
$stuff ::= /bleh+/.4,6
$other-stuff ::= ""meh""
$main-stuff ::= ""hem""
";

        public static readonly string SampleBNF5 =
@"
# some
# comments
# to
# kick start
# things
$grama ::= ?[$stuff $other-stuff $main-stuff].+>1
$stuff ::= /bleh+/.4,6
$other-stuff ::= ""meh""
$main-stuff ::= ""hem""
";

        public static readonly string SampleBNF6 =
@"
# only
# comments
# to
# kick start
# things";
    }
}

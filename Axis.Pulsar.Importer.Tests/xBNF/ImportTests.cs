using Axis.Pulsar.Parser.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace Axis.Pulsar.Importer.Tests.xBNF
{
    [TestClass]
    public class ImportTests
    {
        [TestMethod]
        public void MiscTest()
        {
            try
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var ruleImporter = new Common.xBNF.GrammarImporter();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                var x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF1)));
                var result = x.RootParser().Parse(new Parser.Input.BufferedTokenReader("mehmehbleh/"));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Common.xBNF.GrammarImporter();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF2)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);




                timer.Restart();
                ruleImporter = new Common.xBNF.GrammarImporter();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF3)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Common.xBNF.GrammarImporter();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF4)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);



                timer.Restart();
                ruleImporter = new Common.xBNF.GrammarImporter();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF5)));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);


                timer.Restart();
                ruleImporter = new Common.xBNF.GrammarImporter();
                timer.Stop();
                Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

                timer.Restart();
                Assert.ThrowsException<GrammarValidationException>(() => ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleBNF6))));
                timer.Stop();
                Console.WriteLine("Time to Import: " + timer.Elapsed);
            }
            catch(Exception e)
            {
                throw;
            }
        }


        [TestMethod]
        public void SampleGrammarTest()
        {
            try
            {
                using var sampleGrammarStream = typeof(ImportTests).Assembly
                    .GetManifestResourceStream($"{typeof(ImportTests).Namespace}.TestGrammar.xbnf");

                var ruleImporter = new Common.xBNF.GrammarImporter();
                var x = ruleImporter.ImportGrammar(sampleGrammarStream);

                var result = x.RootParser().Parse(new Parser.Input.BufferedTokenReader("meh"));

                Assert.IsNotNull(x);
            }
            catch(Exception e)
            {
                throw;
            }
        }


        public static readonly string SampleBNF1 =
@"$grama -> +[?[$stuff $other-stuff.2 $more-stuff] EOF]
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

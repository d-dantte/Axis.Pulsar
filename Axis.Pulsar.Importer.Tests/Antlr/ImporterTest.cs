using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace Axis.Pulsar.Importer.Tests.Antlr
{
    [TestClass]
    public class ImporterTest
    {
        [TestMethod]
        public void ImportGrammarTest()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var ruleImporter = new Common.Antlr.GrammarImporter();
            timer.Stop();
            Console.WriteLine("Time to Create Importer: " + timer.Elapsed);

            timer.Restart();
            var x = ruleImporter.ImportGrammar(new MemoryStream(Encoding.UTF8.GetBytes(SampleAntlr)));
            timer.Stop();
            Console.WriteLine("Time to Import: " + timer.Elapsed);
        }

        public static readonly string SampleAntlr =
@"
# my vague grammar described using antlr
grammar
    : more-stuff
    | other-stuff
    | 'meh'
    ;

# second production
# bleh
#

other-stuff
    : /bleh/
    ;



more-stuff
    : stuff
    ;

stuff
    : '.'
    ;
";


    }
}

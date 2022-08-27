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
# some comment
# some other comment
grammar
    : more-stuff
    | other-stuff
    | 'meh'
    ;

#    comment placed in between

# again

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

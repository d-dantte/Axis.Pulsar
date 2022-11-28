using System.Text;
using Axis.Pulsar.Languages.xAntlr;

namespace Axis.Pulsar.Languges.IO.Tests.xAntlr
{
    [TestClass]
    public class ImporterTests
    {
        [TestMethod]
        public void ImportGrammarTest()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var ruleImporter = new Importer();
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
    > -threshold 5
    : more-stuff $EOF
    | other-stuff $EOF
    | 'meh' $EOF
    ;

#    comment placed in between

# again

other-stuff
    : /bleh/.simn
    ;

more-stuff
    : (stuff xstuff)+
    ;

stuff
    : '.'
    ;

xstuff
    : '+'
    ;
";
    }
}

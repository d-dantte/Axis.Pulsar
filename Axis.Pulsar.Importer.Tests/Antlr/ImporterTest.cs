﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    > -threshold 5 -stuff 'abc' -other true
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
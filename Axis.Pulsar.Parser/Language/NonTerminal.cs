using System;

namespace Axis.Pulsar.Parser.Language
{
    public class NonTerminal: IRule
    {
        public Production Production { get; }

        public bool IsRoot { get; }

        public string Name { get; }


        public NonTerminal(string name, bool isRoot, Production production)
        {
            Name = name;
            IsRoot = isRoot;
            Production = production;

            Validate();
        }

        public NonTerminal(string name, Production production) 
            :this(name, false, production)
        { }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception("Invalid Name");

            else if (Production == null)
                throw new Exception("Invalid Production");
        }
    }
}

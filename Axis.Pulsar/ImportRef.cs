using System;

namespace Axis.Pulsar
{
    public class ImportRef : ICloneable
    {
        public string LanguageId { get; set; }
        public string Prefix { get; set; }

        public object Clone() => Copy();
        public ImportRef Copy() => new ImportRef
        {
            LanguageId = this.LanguageId,
            Prefix = this.Prefix
        };

        public override string ToString() => this.Prefix + ":" + this.LanguageId;
    }
}

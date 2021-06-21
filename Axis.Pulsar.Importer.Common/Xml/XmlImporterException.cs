using System;
using System.Linq;
using System.Xml.Schema;

namespace Axis.Pulsar.Importer.Common.Xml
{
    public class XmlImporterException: Exception
    {
        private readonly Info[] _errors;

        public Info[] Errors => _errors.ToArray();


        public XmlImporterException(params Info[] errors)
            :base("Some xml validation errors were encountered")
        {
            _errors = errors ?? Array.Empty<Info>();
        }


        public class Info
        {
            public XmlSchemaException Exception { get; }
            public string Message { get; }
            public XmlSeverityType Severity { get; }


            public Info(string message, XmlSchemaException exception, XmlSeverityType severity)
            {
                Exception = exception;
                Severity = severity;
                Message = message;
            }
        }
    }
}

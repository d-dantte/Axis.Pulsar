using System;
using System.Linq;
using System.Xml.Schema;

namespace Axis.Pulsar.Languages.Xml
{
    public class XmlValidationException: Exception
    {
        private ErrorInfo[] _validationErrors;

        public ErrorInfo[] ValidationErrors => _validationErrors.ToArray();

        public XmlValidationException(params ErrorInfo[] errors)
            :base("Xml Schema Validation errors occured")
        {
            _validationErrors = errors.ToArray();
        }

        public record ErrorInfo
        {
            public XmlSeverityType SeverityType { get; }

            public XmlSchemaException Exception { get; }

            public string Message { get; }

            public ErrorInfo(
                string message,
                XmlSchemaException exception,
                XmlSeverityType severityType)
            {
                SeverityType = severityType;
                Exception = exception;
                Message = message;
            }
        }
    }
}

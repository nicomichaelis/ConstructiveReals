using System;
using System.Runtime.Serialization;

namespace ConstructiveReals;

[Serializable]
internal class PrecisionOverflowException : ArithmeticException
{
    public PrecisionOverflowException()
    {
    }

    public PrecisionOverflowException(string message) : base(message)
    {
    }

    public PrecisionOverflowException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected PrecisionOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

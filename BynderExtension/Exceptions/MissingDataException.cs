using System;
using System.Runtime.Serialization;

namespace Bynder.Exceptions
{
    [Serializable]
    public class MissingDataException : Exception
    {
        #region Constructors

        public MissingDataException()
        {
        }

        public MissingDataException(string message) : base(message)
        {
        }

        public MissingDataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Constructors
    }
}
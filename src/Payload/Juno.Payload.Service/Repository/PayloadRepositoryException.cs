using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juno.Payload.Service.Repository
{
    public class PayloadRepositoryException : Exception
    {
        public PayloadRepositoryException(string message) : base(message)
        {
        }

        public PayloadRepositoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

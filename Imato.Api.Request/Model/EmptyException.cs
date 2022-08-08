using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imato.Api.Request.Model
{
    public class EmptyException : ApplicationException
    {
        public EmptyException() : base("Result is empty")
        {
        }
    }
}
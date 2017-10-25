using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoveCollection.Application.Dto
{
    public class RequestMessage
    {
        public string Message { get; set; }
        public bool IsSuccess { get; set; } = true;
    }
}

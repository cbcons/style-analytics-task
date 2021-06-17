using System;
using System.Collections.Generic;

namespace BadProject.Services
{
    public class ErrorService
    {
        public Queue<DateTime> HttpErrors { get; set; }
    }
}
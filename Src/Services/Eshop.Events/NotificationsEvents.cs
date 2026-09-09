using System;
using System.Collections.Generic;
using System.Text;

namespace Eshop.Events
{
    public record SendEmailEvent
    {
        public string toEmail;
        public string subject;
        public string body;
        public CancellationToken ct;
    }
}

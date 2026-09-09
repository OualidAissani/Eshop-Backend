using System;
using System.Collections.Generic;
using System.Text;

namespace Eshop.Events;

public record SendEmailEvent(

     string toEmail,
     string subject,
     string body
);

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    // מחלקה לאחסון הגדרות המייל מהקובץ appsettings.json
    public class EmailSettings
    {
            public string SmtpServer { get; set; }
            public int Port { get; set; }
            public string SenderEmail { get; set; }
            public string SenderPassword { get; set; }
        }
    }


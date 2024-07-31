using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailReader.Models;

public class EmailModel
{
    public DateTime CreateDate { get; set; }
    public string Subject { get; set; }
    public string UId { get; set; }
    public string From { get; set; }
    public string Body { get; set; }
    public string TextBody { get; set; }

}

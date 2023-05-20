using System;
using System.Collections.Generic;
using System.Text;

namespace Abstraction.Models;

public class OpenAiSettings
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public bool IsGoogleAccount { get; set; }
}

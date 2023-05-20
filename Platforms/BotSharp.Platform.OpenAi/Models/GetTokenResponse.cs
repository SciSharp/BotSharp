using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGPT.Models;

public class GetTokenResponse
{
    public DateTime Expires {  get; set; }
    public string AccessToken { get; set; }
}

using BotSharp.Core.Engines.Articulate;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Articulate
{
    public class DomainPageViewModel
    {
        public List<DomainModel> Domains { get; set; }

        public int Total { get; set; }
    }
}

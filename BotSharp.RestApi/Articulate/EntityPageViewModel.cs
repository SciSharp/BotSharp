using BotSharp.Core.Engines.Articulate;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Articulate
{
    public class EntityPageViewModel
    {
        public List<EntityModel> Entities { get; set; }

        public int Total { get; set; }
    }
}

using BotSharp.Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Articulate.ViewModels
{
    public class EntityPageViewModel
    {
        public List<EntityModel> Entities { get; set; }

        public int Total { get; set; }
    }
}

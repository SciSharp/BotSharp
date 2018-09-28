using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.ViewModels
{
    public class EntityPageViewModel
    {
        public List<EntityModel> Entities { get; set; }

        public int Total { get; set; }
    }
}

using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.ViewModels
{
    public class DomainPageViewModel
    {
        public List<DomainModel> Domains { get; set; }

        public int Total { get; set; }
    }
}

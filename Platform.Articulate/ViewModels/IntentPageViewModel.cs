using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.ViewModels
{
    public class IntentPageViewModel
    {
        public List<IntentModel> Intents { get; set; }

        public int Total { get; set; }
    }
}

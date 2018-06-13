using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface INlpProvider
    {
        void LoadModel();
        Object GetDoc();
    }
}

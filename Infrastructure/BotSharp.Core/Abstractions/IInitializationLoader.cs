using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Abstractions
{
    public interface IInitializationLoader
    {
        int Priority { get; }
        void Initialize(IConfiguration config, IWebHostEnvironment env);
    }
}

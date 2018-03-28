using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Loader;

namespace Microsoft.AspNetCore.Builder
{
    public static class InitLoaderBuilderExtensions
    {
        /// <summary>
        /// Initialize Loader
        /// </summary>
        /// <param name="app"></param>
        public static void UseInitLoader(this IApplicationBuilder app)
        {
            new InitializationLoader().Load();
        }
    }
}

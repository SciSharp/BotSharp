using BotSharp.Platform.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.ContextStorage
{
    public class ContextStorageServiceRegister
    {
        public static void Register<T>(IServiceCollection services)
        {
            services.AddSingleton<IContextStorageFactory<T>, ContextStorageFactory<T>>();

            services.AddSingleton<ContextStorageInFile<T>>();

            services.AddSingleton(factory =>
            {
                Func<string, IContextStorage<T>> accesor = key =>
                {
                    if (key.Equals("ContextStorageInFile"))
                    {
                        return factory.GetService<ContextStorageInFile<T>>();
                    }
                    else
                    {
                        throw new ArgumentException($"Not Support key : {key}");
                    }
                };

                return accesor;
            });
        }
    }
}

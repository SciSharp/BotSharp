using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Builder
{
    public static class EntityDbContextBuilderExtensions
    {
        /// <summary>
        /// Use CustomEntityFoundation
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configuration"></param>
        /// <param name="contentRootPath"></param>
        /// <param name="assembles"></param>
        public static void UseEntityDbContext(this IApplicationBuilder app, IConfiguration configuration, String contentRootPath, String[] assembles)
        {
            Database.Configuration = configuration;
            Database.Assemblies = assembles;
            Database.ContentRootPath = contentRootPath;
        }
    }
}

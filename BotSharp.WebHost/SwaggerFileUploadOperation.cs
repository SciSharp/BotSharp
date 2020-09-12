using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.WebHost
{
    public class SwaggerFileUploadOperation : IOperationFilter
    {

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if(context.MethodInfo.Name == "Import")
            {
                if(operation.RequestBody.Content.FirstOrDefault().Value?.Schema?.Properties.ContainsKey("uploadedFile") == true)
                {
                    var p = operation.RequestBody.Content.FirstOrDefault();
                    p.Value.Schema.Properties["uploadedFile"].Description = "Upload Zip File， meta.json is the extra data for customized function.";
                }
            }

        }
    }
}

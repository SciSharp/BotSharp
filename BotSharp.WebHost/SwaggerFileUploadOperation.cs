using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.WebHost
{
    //public class SwaggerFileUploadOperation : IOperationFilter
    //{

    //    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    //    {
    //        if (operation.OperationId == "Import")
    //        {
    //            operation.Parameters.Clear();

    //            operation.Parameters.Add(new OpenApiParameter
    //            {
    //                Name = "uploadedFile",
    //                In = "formData",
    //                Description = "Upload Zip File， meta.json is the extra data for customized function.",
    //                Required = true,
    //                Type = "file"
    //            });
    //            operation.Consumes.Add("multipart/form-data");
    //            operation.
    //        }
    //    }
    //}
}

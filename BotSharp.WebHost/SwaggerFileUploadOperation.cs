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
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.OperationId == "Import")
            {
                operation.Parameters.Clear();

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "uploadedFile",
                    In = "formData",
                    Description = "Upload Zip File， meta.json is the extra data for customized function.",
                    Required = true,
                    Type = "file"
                });
                operation.Consumes.Add("multipart/form-data");
            }
        }
    }
}

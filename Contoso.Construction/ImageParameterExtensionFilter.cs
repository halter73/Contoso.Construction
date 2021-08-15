
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection.Metadata;

namespace Contoso.Construction;

public class ImageParameterExtensionFilter : IOperationFilter
{
    internal const string UPLOAD_SITE_PHOTO_OPERATION_ID = "UploadSitePhoto";
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.OperationId == UPLOAD_SITE_PHOTO_OPERATION_ID)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Description = "File to upload",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "multipart/form-data", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Required = new HashSet<String>{ "file" },
                                Properties = new Dictionary<String, OpenApiSchema>
                                {
                                    {
                                        "file", new OpenApiSchema()
                                        {
                                            Type = "string",
                                            Format = "binary",
                                            Extensions = new Dictionary<string, IOpenApiExtension>
                                            {
                                              { "x-ms-media-kind", new OpenApiString("image")}
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            };
        }
    }
}

using System.Threading.RateLimiting;
using ASPNETCoreStatusCodes.Middlewares;
using ASPNETCoreStatusCodes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

namespace ASPNETCoreStatusCodes;
/*
  1xx Informational Codes - Indicates the request is being processed and the client should wait for a final response

  2xx Success Codes - Indicates the request was successfully received, understood, and accepted
    - 200 OK - Used for successful GET requests (retrieving users, successful updates)
    - 201 Created - Used when successfully creating a new user via POST
    - 204 No Content - Used for successful DELETE operations and empty PATCH operations

  3xx Redirection Codes - Indicates that further action needs to be taken by the user agent to complete the request

  4xx Client Error Codes - Indicates the client seems to have made an error - the request contains bad syntax or cannot be fulfilled
    - 400 Bad Request - Used for invalid request data (invalid IDs, null request bodies)
    - 401 Unauthorized - Used in ApiKeyMiddleware for missing or invalid API keys
    - 403 Forbidden - Used when trying to delete system accounts (authorization check)
    - 404 Not Found - Used when requested user doesn't exist
    - 409 Conflict - Used for duplicate email addresses during create/update operations
    - 415 Unsupported Media Type - Returned by ASP.NET Core when Content-Type is not application/json for endpoints expecting JSON
    - 422 Unprocessable Entity - Used for model validation failures
    - 429 Too Many Requests - Used by rate limiting middleware when limits are exceeded

  5xx Server Error Codes - Indicates the server failed to fulfill an apparently valid request so server is aware it has encountered an error
    - 500 Internal Server Error - Used for unexpected server errors and exceptions
 */
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "User Management API", Version = "v1" });
            c.AddSecurityDefinition("ApiKey", new()
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key needed to access the endpoints"
            });
            c.AddSecurityRequirement(new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new() { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed-rate-limit", opt =>
            {
                opt.PermitLimit = 2;
                opt.Window = TimeSpan.FromSeconds(100);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0; // No queuing - immediate rejection
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded!", cancellationToken: token);
            };
        });

        builder.Services.AddSingleton<IUserService, UserService>();


        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            // Disable automatic 400 Bad Request responses from built-in DataAnnotation validation
            // to allow custom 422 Unprocessable Entity handling in controller actions
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddLogging();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API V1");
                c.RoutePrefix = string.Empty;
            });
        }

        app.UseHttpsRedirection();

        app.UseMiddleware<ApiKeyMiddleware>();

        app.UseRateLimiter();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
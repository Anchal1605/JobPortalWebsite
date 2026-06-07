//this middleware is used to handle global exceptions
//it is used to catch all exceptions and return a response to the client

using System.Net;
using System.Text.Json;

namespace JobPortal.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            //if no exception, pass the request to the next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            //if exception, log the exception and return a response to the client
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.ContentType = "application/json"; //set the content type to json   
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; //set the status code to 500 (internal server error) 

            var response = new
            {
                success = false,
                message = "An unexpected error occurred.",
                error = ex.Message // later in prod, hide detailed message
            }; //create a response object

            var json = JsonSerializer.Serialize(response); //serialize the response object to json
            await context.Response.WriteAsync(json); //write the response to the client
        }
    }
}

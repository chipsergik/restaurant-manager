using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RestaurantManager.API.Filters;

internal class GlobalExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        var result = new StatusCodeResult((int)HttpStatusCode.InternalServerError);

        _logger.LogError("Unhandled exception occurred while executing request: {Exception}", context.Exception);

        // Set the result
        context.Result = result;
    }
}
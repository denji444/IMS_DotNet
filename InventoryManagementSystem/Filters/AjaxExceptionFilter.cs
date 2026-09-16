using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using InventoryManagementSystem.Exceptions;

namespace InventoryManagementSystem.Filters
{
    public class AjaxExceptionFilter : IExceptionFilter
    {
        private readonly IHostEnvironment _env;
        private readonly ILogger<AjaxExceptionFilter> _logger;

        public AjaxExceptionFilter(IHostEnvironment env, ILogger<AjaxExceptionFilter> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var request = context.HttpContext.Request;
            bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (request.ContentType != null && request.ContentType.Contains("application/json"));

            if (isAjax)
            {
                context.ExceptionHandled = true;
                var response = context.HttpContext.Response;

                if (context.Exception is BusinessException businessEx)
                {
                    _logger.LogWarning(businessEx, "Business exception occurred during AJAX request to {Path}", request.Path);
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Result = new JsonResult(new { success = false, message = businessEx.Message });
                }
                else
                {
                    _logger.LogError(context.Exception, "Unhandled exception occurred during AJAX request to {Path}", request.Path);
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    
                    string message = _env.IsDevelopment()
                        ? context.Exception.Message
                        : "An unexpected server error occurred. Please try again or contact system support.";

                    context.Result = new JsonResult(new { success = false, message });
                }
            }
        }
    }
}

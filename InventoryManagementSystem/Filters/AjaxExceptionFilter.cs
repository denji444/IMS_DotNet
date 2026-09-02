using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using InventoryManagementSystem.Exceptions;

namespace InventoryManagementSystem.Filters
{
    public class AjaxExceptionFilter : IExceptionFilter
    {
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
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Result = new JsonResult(new { success = false, message = businessEx.Message });
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    // In development, we can pass the real message, but for security, keep it general in production.
                    context.Result = new JsonResult(new { success = false, message = context.Exception.Message });
                }
            }
        }
    }
}

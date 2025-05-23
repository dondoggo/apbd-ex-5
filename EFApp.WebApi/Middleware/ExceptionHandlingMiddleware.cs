using System.Net;
using EFApp.Application.Exceptions;
using System.Text.Json;

namespace EFApp.WebApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext http)
        {
            try
            {
                await _next(http);
            }
            catch (NotFoundException nf)
            {
                http.Response.StatusCode = (int)HttpStatusCode.NotFound;
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(JsonSerializer.Serialize(new { error = nf.Message }));
            }
            catch (BadRequestException br)
            {
                http.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(JsonSerializer.Serialize(new { error = br.Message }));
            }
            catch (ConcurrencyException ce)
            {
                http.Response.StatusCode = (int)HttpStatusCode.Conflict;
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(JsonSerializer.Serialize(new { error = ce.Message }));
            }
            catch (Exception ex)
            {
                http.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Wewnętrzny błąd serwera" }));
            }
        }
    }
}
using System.Text;
using MobyPark.Models;
using MobyPark.Models.DbContext;

namespace MobyPark.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        //dit moet omdat de json body maar 1 keer leesbaar is en anders nooit door zou worden gegeven aan de controller 
        context.Request.EnableBuffering(); 

        string body = "";
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                leaveOpen: true
            );

            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; //zet de leespositie weer bovenaan zodat controller de juiste info leest
        }
        
        await _next(context);
        
        var log = new ApiLoggingModel
        {
            InputBody = body,
            Path = context.Request.Path,
            StatusCode = context.Response.StatusCode,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ApiLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
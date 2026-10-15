using Microsoft.Extensions.Localization;

namespace English.Services;

// Listening-only guard must precede authorization, whose ActiveAccount handler reads the database.
public sealed class ListeningDatabaseSafetyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, ListeningDatabaseGuard guard,
        IWebHostEnvironment environment, IStringLocalizer<SharedResource> localizer)
    {
        var path = httpContext.Request.Path;
        var isVoa = path.StartsWithSegments("/AdminListeningVoa");
        var isListening = isVoa || path.StartsWithSegments("/AdminListening")
            || path.StartsWithSegments("/Listening") || path.StartsWithSegments("/vi/listening")
            || path.StartsWithSegments("/ListeningAudio");
        if (isVoa && !environment.IsDevelopment())
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (isListening && !guard.IsConfiguredTarget())
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            httpContext.Response.ContentType = "text/plain; charset=utf-8";
            await httpContext.Response.WriteAsync(localizer["ListeningDatabaseTarget"].Value, httpContext.RequestAborted);
            return;
        }
        await next(httpContext);
    }
}

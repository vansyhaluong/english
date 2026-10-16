namespace English.Services;

public sealed class BooksDatabaseSafetyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, BooksDatabaseGuard guard)
    {
        // Run before authorization: ActiveAccount itself reads the configured database.
        if ((context.Request.Path.StartsWithSegments("/books")
            || context.Request.Path.StartsWithSegments("/AdminBooks")) && !guard.IsConfiguredTarget())
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { code = "BooksDatabaseTarget" }, context.RequestAborted);
            return;
        }
        await next(context);
    }
}

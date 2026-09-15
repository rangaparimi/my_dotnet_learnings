using BookHub.Mvc.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
// ORDER IS EXECUTION ORDER. Each app.Use... wraps everything registered after it,
// like nested `with` blocks -- the first one registered is the outermost layer.

// 1) Outermost: times EVERY request, including ones later middleware short-circuits
//    or ones that end in an unhandled exception.
app.UseRequestTiming();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// 2) A hand-rolled inline middleware (no class needed) that gates one path and
//    SHORT-CIRCUITS by returning without calling next() -- routing and the
//    controller action never run for a blocked request.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/HttpDemo/Secret") &&
        !context.Request.Headers.ContainsKey("X-Api-Key"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Missing X-Api-Key header" });
        return; // <-- no call to next(): the pipeline stops here
    }

    await next(context);
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

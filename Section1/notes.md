# .NET / ASP.NET Core MVC Learning Notes — Batch 1

**Covers:** Course Sections 2–6 (Getting Started, HTTP, Middleware, Routing, Controllers & IActionResult)
**Project:** `BookHub.Mvc` — one evolving ASP.NET Core MVC app, built incrementally instead of disconnected demos
**Repo:** https://github.com/rangaparimi/my_dotnet_learnings
**Audience note:** written for someone fluent in Python (Flask/FastAPI, ML/DL/agents) but new to C#/.NET — every new concept is anchored to a Python equivalent.

---

## Table of contents

1. [How this project is organized](#1-how-this-project-is-organized)
2. [Section 2 — Getting Started](#2-section-2--getting-started)
3. [Section 3 — HTTP](#3-section-3--http)
4. [Section 4 — Middleware](#4-section-4--middleware)
5. [Section 5 — Routing](#5-section-5--routing)
6. [Section 6 — Controllers & IActionResult](#6-section-6--controllers--iactionresult)
7. [Python → C#/.NET cheat sheet](#7-python--cnet-cheat-sheet)
8. [What's next](#8-whats-next)

---

## 1. How this project is organized

Instead of 30 disconnected demo apps (one per course section), everything lives in **one evolving project**, `BookHub.Mvc`, so later sections build on earlier ones the way a real app would. Each course section either adds a new controller/concept to this project, or — for later sections (Clean Architecture, Web API, Angular, JWT) — splits it into more projects that reuse what came before.

```
DotNetMastery.slnx                     solution file (list of projects)
src/
  BookHub.Mvc/
    BookHub.Mvc.csproj                 project file (deps + build config)
    Program.cs                         entry point + app configuration
    appsettings.json / .Development.json
    Properties/launchSettings.json     local dev launch profile
    Controllers/
    Models/
    Views/
    Middleware/
    wwwroot/                           static files (css/js/vendored libs)
```

| .NET concept | Closest Python equivalent |
|---|---|
| `.slnx` solution | a monorepo workspace grouping deployable projects |
| `.csproj` project file | `requirements.txt` + `pyproject.toml` combined |
| NuGet package | pip package |
| `dotnet build` / `dotnet run` | compiles your code into one `.dll`, then runs it (no separate "install" step for your own code) |
| Kestrel (built-in web server) | `uvicorn` / `gunicorn` |
| `wwwroot/` | Flask's `static/` folder |

---

## 2. Section 2 — Getting Started

Scaffolded with:
```bash
dotnet new sln -n DotNetMastery
dotnet new mvc -n BookHub.Mvc --no-https -f net10.0
dotnet sln add src/BookHub.Mvc/BookHub.Mvc.csproj
```

`Program.cs` is the single entry point — everything else gets wired up from here. It has two distinct halves:

```csharp
var builder = WebApplication.CreateBuilder(args);   // 1. register services (DI container)
builder.Services.AddControllersWithViews();

var app = builder.Build();
// 2. configure the HTTP pipeline (middleware, order matters — see Section 4)
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(...);

app.Run();                                          // blocks, like uvicorn.run()
```

- **Part 1** registers services into .NET's built-in **dependency injection container** — comparable to nothing in typical Flask, closer to FastAPI's `Depends()` system but centralized. Deep dive in Section 12.
- **Part 2** builds the **middleware pipeline** — each `app.Use...` call is a layer requests pass through. Deep dive in Section 4.

**MVC folder conventions** (convention over configuration, same spirit as Django/Rails app layout):
- `Controllers/{Name}Controller.cs` — request handlers, one class per resource area.
- `Views/{ControllerName}/{ActionName}.cshtml` — the framework finds the view for an action purely from this folder path, no explicit registration needed.
- `Models/` — plain data classes (DTOs / view models), like Python `@dataclass` without built-in validation.

**Run it:**
```bash
cd src/BookHub.Mvc
dotnet run --urls http://localhost:5080
```

---

## 3. Section 3 — HTTP

**File:** [`src/BookHub.Mvc/Controllers/HttpDemoController.cs`](../src/BookHub.Mvc/Controllers/HttpDemoController.cs)

### Theory

Every request gets one `HttpContext`, created fresh by Kestrel and discarded once the response is sent — no shared/global state between requests, the same guarantee Flask's `request` object gives you, except here it's an explicit object on the controller (`Request`/`Response`), not a thread-local.

`IActionResult` decouples **what to return** from **how it's written to the response**. Helper methods (`Ok`, `NotFound`, `StatusCode`, `Created`) build different concrete result objects; there's no Python-style `return body, status` tuple — the return value itself carries everything.

### Code walkthrough

```csharp
[HttpGet]
public IActionResult Info()
{
    var info = new
    {
        Method = Request.Method,
        Path = Request.Path.Value,
        QueryString = Request.QueryString.Value,
        Query = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
        Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
    };
    Response.Headers["X-Demo-Header"] = "hello-from-server";
    return Ok(info); // 200 + JSON via System.Text.Json
}

[HttpGet("HttpDemo/Status/{code:int}")]
public IActionResult Status(int code)
    => StatusCode(code, new { requestedCode = code, message = $"You asked for {code}" });

[HttpGet]
public IActionResult ResourceCreated()
    => Created("/HttpDemo/Info", new { id = 42, message = "Pretend resource created" }); // 201 + Location header

[HttpGet]
public IActionResult GoElsewhere() => Redirect("/Home/Privacy"); // 302 + Location header

[HttpGet]
public IActionResult Secret() => NotFound(new { message = "This resource does not exist (404)" });

[HttpPost]
public async Task<IActionResult> Echo()
{
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();
    return Ok(new { youSent = body, contentType = Request.ContentType, length = Request.ContentLength });
}
```

> **Bug caught live:** an action was first named `Created()`, which collides with `ControllerBase.Created()` — a protected helper already in scope. The compiler warned it was *hiding* the inherited member. Renamed to `ResourceCreated`. Lesson: `Controller`/`ControllerBase` bring a bunch of helper methods into scope (`Ok`, `NotFound`, `Created`, `File`, `View`, `Redirect`, `Content`...) and picking a colliding action name is a real footgun.

### Practical results (curl)

```
GET /HttpDemo/Info?q=hello&x=1
  200 OK, X-Demo-Header: hello-from-server
  {"method":"GET","path":"/HttpDemo/Info","query":{"q":"hello","x":"1"}, ...}

GET /HttpDemo/Status/418
  418 I'm a teapot
  {"requestedCode":418,"message":"You asked for 418"}

GET /HttpDemo/ResourceCreated
  201 Created, Location: /HttpDemo/Info
  {"id":42,"message":"Pretend resource created"}

GET /HttpDemo/GoElsewhere
  302 Found, Location: /Home/Privacy   (curl did NOT auto-follow, unlike a browser)

GET /HttpDemo/Secret
  404 Not Found
  {"message":"This resource does not exist (404)"}

POST /HttpDemo/Echo  (body: "hello from curl", Content-Type: text/plain)
  200 OK
  {"youSent":"hello from curl","contentType":"text/plain","length":15}
```

### Key takeaways
- Status code and response body are independent knobs — `StatusCode(code, body)` proves it.
- A plain 404 has no automatic HTML page unless you configure `UseStatusCodePages` (Section 22).
- `curl -I` sends `HEAD`, not `GET` — `[HttpGet]` does **not** implicitly also handle `HEAD` in ASP.NET Core (bit us again in Section 6).

---

## 4. Section 4 — Middleware

**Files:** [`src/BookHub.Mvc/Middleware/RequestTimingMiddleware.cs`](../src/BookHub.Mvc/Middleware/RequestTimingMiddleware.cs), [`src/BookHub.Mvc/Program.cs`](../src/BookHub.Mvc/Program.cs)

### Theory

Middleware is a pipeline of nested `with`-block-like layers, not a flat list of hooks. **Registration order = execution order going in, reverse order going out.** Code before `await next()` runs on the way in; code after runs on the way out, once everything nested inside has finished — directly analogous to a Python context manager's `__enter__`/`__exit__`, or ASGI middleware's before/after `call_next()`.

Two ways to write middleware:
- **Class-based** — constructor-injected `RequestDelegate next` + anything else DI provides. One instance is built at startup and reused for every request, so **never store per-request state on instance fields** — use locals inside `InvokeAsync`.
- **Inline lambda** — `app.Use(async (context, next) => {...})`, good for quick one-offs with no dependencies.

**Short-circuiting** = simply not calling `next()`. Everything downstream (routing, model binding, the controller) never runs.

### Code walkthrough

```csharp
// Middleware/RequestTimingMiddleware.cs
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await _next(context);                 // <-- rest of the pipeline runs here
        stopwatch.Stop();
        _logger.LogInformation("{Method} {Path} -> {StatusCode} ({ElapsedMs}ms)",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
}

public static class RequestTimingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTimingMiddleware>();
}
```

```csharp
// Program.cs pipeline, in registration order:
app.UseRequestTiming();                 // 1) outermost — measures EVERY request

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.Use(async (context, next) =>        // 2) inline short-circuit gate
{
    if (context.Request.Path.StartsWithSegments("/HttpDemo/Secret") &&
        !context.Request.Headers.ContainsKey("X-Api-Key"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Missing X-Api-Key header" });
        return;                          // no next() call -> pipeline stops here
    }
    await next(context);
});

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
```

### Practical results (server log)

```
GET /HttpDemo/Info    -> 200 (80ms)   full pipeline: routing -> controller
GET /HttpDemo/Secret  -> 403 (4ms)    stopped at the gate, never reached routing (no X-Api-Key)
GET /HttpDemo/Secret  -> 404 (7ms)    passed the gate (X-Api-Key present), reached the controller, real 404
```

All three were logged by the *same* outer middleware, proving it wraps the entire rest of the pipeline regardless of where — or whether — a response was actually produced downstream.

### Key takeaways
- Order isn't stylistic. If `UseRequestTiming()` were registered *after* the gate, 403 responses would never be logged — timing would start too late.
- Short-circuiting is how real concerns work in production: rate limiting, maintenance mode, and (Section 25) authentication gates are all "middleware that decides whether the pipeline continues at all."

---

## 5. Section 5 — Routing

**Files:** [`src/BookHub.Mvc/Controllers/RoutingDemoController.cs`](../src/BookHub.Mvc/Controllers/RoutingDemoController.cs), [`src/BookHub.Mvc/Views/RoutingDemo/`](../src/BookHub.Mvc/Views/RoutingDemo/)

### Theory

Two routing systems coexist **per action**, not per controller:
1. **Conventional routing** — one pattern in `Program.cs` (`{controller=Home}/{action=Index}/{id?}`) matches any action with no route template of its own.
2. **Attribute routing** — `[HttpGet("template")]` on an action overrides that; the action is reachable *only* through its own template.

**Constraints filter matches, they don't validate.** `{id:int}` only matches all-digit segments. A non-matching request doesn't error — it just doesn't match *that* candidate, and the router tries others (or 404s if nothing matches). Different from throwing a type error.

**Endpoint routing splits matching from execution.** `UseRouting()` finds the best-matching endpoint and stashes it on `HttpContext` (`SetEndpoint()`) — it does **not** invoke the controller. Execution happens later, once the pipeline reaches the mapped endpoint. This is *why* `UseAuthorization()` sits between `UseRouting()` and the endpoint mapping in `Program.cs`: it can inspect the matched endpoint's metadata (e.g. an `[Authorize]` attribute) via `context.GetEndpoint()` and short-circuit with a 401 — using the exact same "don't call `next()`" mechanism from Section 4. Routing and middleware are not separate systems; routing *is* middleware.

**Matching is specificity-based, not registration-order-based.** ASP.NET Core scores every candidate template (literal segments > constrained parameters > unconstrained parameters > catch-all) and picks the most specific match. Two templates with identical specificity that both match → `AmbiguousMatchException` (a 500) at request time, not a silent pick.

**Reverse routing** — `Url.Action(actionName, routeValues)` builds a URL from the route table instead of a hardcoded string, same problem Flask's `url_for()` solves the same way.

### Code walkthrough

```csharp
// Conventional routing: no template -> falls back to {controller}/{action}/{id?}
[HttpGet]
public IActionResult Greet(string? id) { ... }

// Route constraints: two candidates for the same URL shape
[HttpGet("RoutingDemo/Book/{id:int}")]
public IActionResult BookById(int id) { ... }

[HttpGet("RoutingDemo/Book/{slug:alpha}")]
public IActionResult BookBySlug(string slug) { ... }

// Multiple templates, one action
[HttpGet("RoutingDemo/Ping")]
[HttpGet("/ping")]
public IActionResult Ping() { ... }

// Catch-all: slurps every remaining segment, incl. slashes (like Flask's <path:...>)
[HttpGet("RoutingDemo/Files/{**path}")]
public IActionResult Files(string path) { ... }

// Reverse routing
[HttpGet("RoutingDemo/Links")]
public IActionResult Links()
{
    return View(new LinksViewModel(
        LinkToBook42: Url.Action(nameof(BookById), new { id = 42 })!,
        LinkToPing: Url.Action(nameof(Ping))!,
        LinkToHomeIndex: Url.Action("Index", "Home")!));
}
```

Every action here also returns `View(model)` and renders a real `.cshtml` page under `Views/RoutingDemo/` — the routing demo is a clickable multi-page site (`/RoutingDemo/Index` as the landing page), not just JSON.

### Practical results (curl)

```
/RoutingDemo/Greet            200  conventional route, {id?} absent
/RoutingDemo/Greet/Sai        200  conventional route, id="Sai"
/RoutingDemo/Book/42          200  :int constraint -> BookById
/RoutingDemo/Book/mybook      200  :alpha constraint -> BookBySlug
/RoutingDemo/Book/42abc       404  neither constraint matches
/RoutingDemo/Ping             200  \ same action,
/ping                         200  / two templates
/RoutingDemo/Files/a/b/c.txt  200  catch-all -> requestedPath = "a/b/c.txt"
/RoutingDemo/Links            200  {"linkToBook42":"/RoutingDemo/Book/42","linkToPing":"/RoutingDemo/Ping","linkToHomeIndex":"/"}
```

### Key takeaways
- Constraint reference beyond `:int`/`:alpha`: `:guid`, `:bool`, `:datetime`, `:min(1)`, `:max(100)`, `:range(1,50)`, `:length(2,10)`, `:regex(...)` — chainable, e.g. `{id:int:min(1)}`.
- Model binding pulls from route values, then query string, then form body, matched by **parameter name** — not by "which one you meant." `[FromRoute]`/`[FromQuery]`/`[FromBody]` pin a parameter to one explicit source (Section 7).
- Routing is case-insensitive and trailing-slash-tolerant by default for matching; generated links come out in a canonical form regardless of how controller/action names were typed in C#.

---

## 6. Section 6 — Controllers & IActionResult

**File:** [`src/BookHub.Mvc/Controllers/ActionResultsController.cs`](../src/BookHub.Mvc/Controllers/ActionResultsController.cs)

### Theory

`Controller` = `ControllerBase` + `View()`/`PartialView()`/`ViewComponent()` support. Anything that never renders Razor should inherit the leaner `ControllerBase` — the split Section 26's dedicated Web API project standardizes on for every controller.

`IActionResult` has exactly one member: `ExecuteResultAsync(ActionContext)`. Every `Ok()`/`NotFound()`/`Content()`/`File()` call builds a different **concrete class** implementing that interface — "what result to produce" is fully decoupled from "how it's written," the same shape as the Command pattern. You can return the concrete type (`ContentResult`, `FileContentResult`) directly when an action only ever produces one kind of result.

`ActionResult<T>` lets one method return **either** a `T` directly **or** any `IActionResult` (`NotFound()`, `BadRequest()`, ...), via an implicit conversion. Its real payoff: Swagger/OpenAPI tooling (Section 27) can read the declared `T` and generate an accurate response schema — a bare `IActionResult` return type erases the success payload's shape entirely.

### Code walkthrough

```csharp
public class ActionResultsController : ControllerBase   // no View() needed here
{
    [HttpGet("ActionResults/PlainText")]
    public ContentResult PlainText()
        => Content("just plain text, no JSON wrapper", "text/plain");

    [HttpGet("ActionResults/Download")]
    public FileContentResult Download()
    {
        var bytes = Encoding.UTF8.GetBytes("id,title\n1,Dune\n2,Foundation\n");
        return File(bytes, "text/csv", "books.csv"); // sets Content-Disposition: attachment
    }

    [HttpDelete("ActionResults/NoBody/{id:int}")]
    public IActionResult NoBody(int id) => NoContent(); // 204, deliberately empty body

    [HttpGet("ActionResults/Typed/{id:int}")]
    public ActionResult<BookViewModel> Typed(int id)
    {
        if (id <= 0)
            return NotFound(new { message = $"No book with id {id}" }); // IActionResult branch
        return new BookViewModel(id.ToString(), "ActionResult<T> implicit conversion", "n/a");
        // ^ T branch, implicitly wrapped as 200 + JSON
    }

    // SECURITY: open redirect (CWE-601) prevention
    [HttpGet("ActionResults/SafeRedirect")]
    public IActionResult SafeRedirect(string target)
    {
        if (!Url.IsLocalUrl(target))
            return BadRequest(new { error = "Only local, same-app redirect targets are allowed." });
        return LocalRedirect(target);
    }
}
```

### Security note: open redirects (CWE-601)

Handing user-controlled input straight to `Redirect(target)` is a real vulnerability: an attacker crafts `yourtrustedsite.com/...?target=https://evil.example` — a link that *looks* like your domain right up until the 302 sends the victim's browser somewhere else. `Url.IsLocalUrl()` validates the target is same-app-relative before redirecting; this is the same pattern ASP.NET Core Identity uses for its own post-login "return URL" handling — not a toy check, the real one.

### Practical results (curl)

```
GET /ActionResults/PlainText
  200, Content-Type: text/plain
  "just plain text, no JSON wrapper"

GET /ActionResults/Download
  200, Content-Type: text/csv
  Content-Disposition: attachment; filename=books.csv
  (file saved: id,title / 1,Dune / 2,Foundation)

DELETE /ActionResults/NoBody/5
  204 No Content (empty body)

GET /ActionResults/Typed/5
  200  {"identifier":"5","matchedVia":"ActionResult<T> implicit conversion","routeUsed":"n/a"}

GET /ActionResults/Typed/0
  404  {"message":"No book with id 0"}

GET /ActionResults/SafeRedirect?target=/Home/Privacy
  302, Location: /Home/Privacy

GET /ActionResults/SafeRedirect?target=https://evil.example
  400  {"error":"Only local, same-app redirect targets are allowed."}
```

> **Bug caught live (again):** `curl -I` on `/ActionResults/Download` returned 404 — because `-I` sends `HEAD`, and `[HttpGet]` doesn't implicitly also handle `HEAD`. A real `GET` confirmed the endpoint was correct all along. Same lesson as Section 3, worth remembering: `GET`/`HEAD` are not silently interchangeable here.

---

## 7. Python → C#/.NET cheat sheet

| Python / Flask / FastAPI | ASP.NET Core MVC equivalent |
|---|---|
| `uvicorn` / `gunicorn` | Kestrel |
| Flask `request` object | `HttpContext.Request` (per-request, framework-managed lifetime) |
| `return body, status` tuple | `IActionResult` (`Ok()`, `NotFound()`, `StatusCode()`, ...) |
| `@app.route("/x")` | conventional routing (`{controller}/{action}`) or `[HttpGet("/x")]` attribute routing |
| Flask `<int:id>` converter | `{id:int}` route constraint |
| Flask `<path:...>` converter | `{**path}` catch-all route |
| `url_for('endpoint', id=5)` | `Url.Action(nameof(Action), new { id = 5 })` |
| ASGI middleware / `@app.middleware("http")` | `app.Use(...)` / class-based middleware with `InvokeAsync` |
| FastAPI `Depends()` | built-in DI container (Section 12) |
| Jinja2 `{{ }}` / `{% extends %}` | Razor `@` expressions / `_Layout.cshtml` + `@RenderBody()` |
| `@dataclass` / Pydantic model (no validation) | plain C# `record`/class as a view model |
| `pip install X` | NuGet package reference in `.csproj` |
| `.env` | `appsettings.json` + `appsettings.{Environment}.json` |

---

## 8. What's next

**Section 7 — Model Binding and Validation** (the largest section so far: 23 items). This is where request data — route values, query string, form fields, JSON body — becomes typed C# objects automatically, and where `[Required]`/DataAnnotations validation lives. It directly extends the model-binding preview from Section 5 (parameter-name-based binding across sources) and the `[FromRoute]`/`[FromQuery]`/`[FromBody]` attributes mentioned there.

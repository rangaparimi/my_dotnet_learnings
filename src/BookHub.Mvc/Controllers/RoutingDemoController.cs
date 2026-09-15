using BookHub.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookHub.Mvc.Controllers;

// ASP.NET Core has TWO routing systems that coexist per-ACTION, not per-controller:
//   1) Conventional routing -- one pattern in Program.cs (`{controller}/{action}/{id?}`)
//      matches any action that has no route template of its own.
//   2) Attribute routing -- a [HttpGet("template")] on the action overrides that,
//      and that action is ONLY reachable through its own template.
// Both kinds can live side-by-side in the same controller, as they do here.
//
// Every action now returns View(model) instead of Ok(json) -- View() looks up
// Views/RoutingDemo/{ActionName}.cshtml by convention and renders it with the model.
public class RoutingDemoController : Controller
{
    // Landing page: real, clickable navigation instead of hand-typed URLs in curl.
    [HttpGet]
    public IActionResult Index() => View();

    // No template on the attribute -> falls back to conventional routing:
    // Program.cs pattern is {controller=Home}/{action=Index}/{id?}
    // so this responds at /RoutingDemo/Greet and /RoutingDemo/Greet/{id}
    [HttpGet]
    public IActionResult Greet(string? id)
    {
        var model = new GreetViewModel(
            Message: id is null ? "Hello, stranger" : $"Hello, {id}",
            MatchedVia: "conventional route ({controller}/{action}/{id?})",
            RequestedId: id);
        return View(model);
    }

    // Route CONSTRAINT: {id:int} only matches all-digit segments.
    // A non-matching request doesn't error here -- it simply doesn't match THIS
    // action, and the router tries other candidates (see BookBySlug below).
    [HttpGet("RoutingDemo/Book/{id:int}")]
    public IActionResult BookById(int id)
    {
        var model = new BookViewModel(
            Identifier: id.ToString(),
            MatchedVia: "attribute route with :int constraint",
            RouteUsed: "RoutingDemo/Book/{id:int}");
        return View("Book", model); // action name != view name, so name it explicitly
    }

    // A second candidate for the same URL shape, constrained to letters only.
    // /RoutingDemo/Book/42      -> matches BookById   (all digits)
    // /RoutingDemo/Book/mybook  -> matches BookBySlug (all letters)
    // /RoutingDemo/Book/42abc   -> matches NEITHER constraint -> 404
    [HttpGet("RoutingDemo/Book/{slug:alpha}")]
    public IActionResult BookBySlug(string slug)
    {
        var model = new BookViewModel(
            Identifier: slug,
            MatchedVia: "attribute route with :alpha constraint",
            RouteUsed: "RoutingDemo/Book/{slug:alpha}");
        return View("Book", model);
    }

    // ONE action, TWO independent route templates -- either URL reaches the same code.
    [HttpGet("RoutingDemo/Ping")]
    [HttpGet("/ping")]
    public IActionResult Ping()
    {
        var model = new PingViewModel("pong", "one of two attribute routes on the same action");
        return View(model);
    }

    // Catch-all segment: {**path} slurps every remaining path segment into one
    // string, including slashes. Equivalent to Flask's <path:...> converter.
    [HttpGet("RoutingDemo/Files/{**path}")]
    public IActionResult Files(string path)
    {
        var model = new FilesViewModel(path, "catch-all route ({**path})");
        return View(model);
    }

    // Reverse routing: build a URL FROM route values instead of hardcoding a string,
    // so renaming a route template doesn't silently break links elsewhere.
    // Directly equivalent to Flask's url_for('endpoint', id=5).
    [HttpGet("RoutingDemo/Links")]
    public IActionResult Links()
    {
        var model = new LinksViewModel(
            LinkToBook42: Url.Action(nameof(BookById), new { id = 42 })!,
            LinkToPing: Url.Action(nameof(Ping))!,
            LinkToHomeIndex: Url.Action("Index", "Home")!);
        return View(model);
    }
}

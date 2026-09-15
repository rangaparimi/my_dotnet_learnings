using System.Text;
using BookHub.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookHub.Mvc.Controllers;

// Inherits ControllerBase, not Controller. Controller = ControllerBase + View()/
// PartialView()/ViewComponent() support for rendering Razor. This controller never
// renders a view, so ControllerBase is the leaner, correct base class -- exactly the
// split Section 26's dedicated Web API project will lean on for every controller.
//
// Every Ok()/NotFound()/View()/Content() helper you've used so far is a convenience
// method that builds a CONCRETE class implementing IActionResult. IActionResult has
// exactly one member -- ExecuteResultAsync(ActionContext) -- which is what actually
// writes status code + headers + body onto the real HTTP response. "What result to
// produce" is fully decoupled from "how it gets written", the same shape as the
// classic Command pattern.
public class ActionResultsController : ControllerBase
{
    [HttpGet("ActionResults/PlainText")]
    public ContentResult PlainText()
        // Return type here is the CONCRETE ContentResult, not IActionResult --
        // fine, even preferable, when an action only ever produces one kind of result.
        => Content("just plain text, no JSON wrapper", "text/plain");

    [HttpGet("ActionResults/Download")]
    public FileContentResult Download()
    {
        var csv = "id,title\n1,Dune\n2,Foundation\n";
        var bytes = Encoding.UTF8.GetBytes(csv);
        // 3rd arg sets Content-Disposition: attachment; filename=... so a browser
        // downloads it instead of rendering it inline.
        return File(bytes, "text/csv", "books.csv");
    }

    [HttpDelete("ActionResults/NoBody/{id:int}")]
    public IActionResult NoBody(int id)
        => NoContent(); // 204: success, deliberately empty body -- the idiomatic DELETE response

    // ActionResult<T> lets one method return EITHER a T directly OR any IActionResult
    // (NotFound(), BadRequest(), ...), with an implicit conversion doing the wrapping.
    // Its real payoff shows up in Section 27 (Swagger): tooling can read the declared
    // T and generate accurate API docs, something a bare IActionResult return type
    // can't express since it erases the success payload's type.
    [HttpGet("ActionResults/Typed/{id:int}")]
    public ActionResult<BookViewModel> Typed(int id)
    {
        if (id <= 0)
            return NotFound(new { message = $"No book with id {id}" }); // IActionResult branch

        return new BookViewModel(id.ToString(), "ActionResult<T> implicit conversion", "n/a");
        // ^ T branch -- implicitly wrapped in a 200 + JSON ObjectResult for you
    }

    // SECURITY NOTE (CWE-601, open redirect): if `target` comes from user input,
    // never hand it straight to Redirect(target) -- that 302s the browser to
    // WHATEVER url an attacker puts in the query string, on a link that still
    // looks like it points at your trusted domain. Always validate a
    // user-supplied redirect target is local first (Url.IsLocalUrl), which is
    // exactly what ASP.NET Core Identity's own login "return URL" handling does.
    [HttpGet("ActionResults/SafeRedirect")]
    public IActionResult SafeRedirect(string target)
    {
        if (!Url.IsLocalUrl(target))
            return BadRequest(new { error = "Only local, same-app redirect targets are allowed." });

        return LocalRedirect(target);
    }
}

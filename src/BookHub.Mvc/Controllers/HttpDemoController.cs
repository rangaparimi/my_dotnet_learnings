using Microsoft.AspNetCore.Mvc;

namespace BookHub.Mvc.Controllers;

// A controller dedicated to poking at the raw HTTP request/response.
// Nothing here is "real" app logic -- it's a lab bench for Section 3: HTTP.
public class HttpDemoController : Controller
{
    // GET /HttpDemo/Info
    // HttpContext is created fresh by Kestrel for every request and thrown away
    // when the response finishes -- there is no shared state between requests,
    // same as a Flask `request` object living only for the duration of a view function.
    [HttpGet]
    public IActionResult Info()
    {
        var info = new
        {
            Method = Request.Method,                 // GET, POST, PUT, DELETE...
            Path = Request.Path.Value,                // /HttpDemo/Info
            QueryString = Request.QueryString.Value,  // ?foo=bar
            Query = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
            Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Protocol = Request.Protocol,               // HTTP/1.1
            IsHttps = Request.IsHttps,
            ContentType = Request.ContentType
        };

        // Response headers are just a dictionary you write to before the body is sent.
        Response.Headers["X-Demo-Header"] = "hello-from-server";

        return Ok(info); // Ok() = HTTP 200 + serializes the object to JSON via System.Text.Json
    }

    // GET /HttpDemo/Status/201  (explicit attribute route -- more on routing in Section 5)
    [HttpGet("HttpDemo/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        // StatusCode() lets you return ANY status code with a body, useful for
        // demonstrating that "the status code" and "the body" are independent knobs.
        return StatusCode(code, new { requestedCode = code, message = $"You asked for {code}" });
    }

    // GET /HttpDemo/ResourceCreated
    // (Named ResourceCreated, not Created -- ControllerBase already defines a
    //  `Created(...)` helper method, and shadowing it just to name an action is a trap.)
    [HttpGet]
    public IActionResult ResourceCreated()
    {
        // 201 Created conventionally carries a Location header pointing at the new resource.
        return Created("/HttpDemo/Info", new { id = 42, message = "Pretend resource created" });
    }

    // GET /HttpDemo/GoElsewhere
    [HttpGet]
    public IActionResult GoElsewhere()
    {
        return Redirect("/Home/Privacy"); // 302 Found + Location header
    }

    // GET /HttpDemo/Secret
    [HttpGet]
    public IActionResult Secret()
    {
        return NotFound(new { message = "This resource does not exist (404)" });
    }

    // POST /HttpDemo/Echo
    // Reads the raw request body -- same idea as Flask's request.data / FastAPI's await request.body()
    [HttpPost]
    public async Task<IActionResult> Echo()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        return Ok(new { youSent = body, contentType = Request.ContentType, length = Request.ContentLength });
    }
}

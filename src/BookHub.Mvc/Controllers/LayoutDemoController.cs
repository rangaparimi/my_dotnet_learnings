using Microsoft.AspNetCore.Mvc;

namespace BookHub.Mvc.Controllers;

// Section 9: Layout Views. Every action here renders a real page, each exercising
// a different layout composition technique -- nested layouts, optional vs required
// sections, and opting out of the site layout entirely. The routing/IActionResult
// mechanics are unchanged from Sections 5-6; what's new is which Layout each view
// picks, and how.
public class LayoutDemoController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult TwoColumn() => View();

    [HttpGet]
    public IActionResult TwoColumnNoSidebar() => View();

    [HttpGet]
    public IActionResult Minimal() => View();

    [HttpGet]
    public IActionResult RequiredSectionDemo() => View();
}

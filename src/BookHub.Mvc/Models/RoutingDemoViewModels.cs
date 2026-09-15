namespace BookHub.Mvc.Models;

// Plain view models -- just typed data carriers passed from a controller action into
// a Razor view (Section 8 goes deep on Views; this file exists now because turning
// the routing demo into real pages needs *something* strongly typed to bind @model to).
public record GreetViewModel(string Message, string MatchedVia, string? RequestedId);

public record BookViewModel(string Identifier, string MatchedVia, string RouteUsed);

public record PingViewModel(string Message, string MatchedVia);

public record FilesViewModel(string RequestedPath, string MatchedVia);

public record LinksViewModel(string LinkToBook42, string LinkToPing, string LinkToHomeIndex);

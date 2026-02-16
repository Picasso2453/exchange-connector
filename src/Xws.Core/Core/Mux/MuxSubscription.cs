namespace Xws.Data.Mux;

public sealed record MuxSubscription(string Exchange, string? Market, string[] Symbols);

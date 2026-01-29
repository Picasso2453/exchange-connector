namespace xws.Core.Mux;

public sealed record MuxSubscription(string Exchange, string? Market, string[] Symbols);

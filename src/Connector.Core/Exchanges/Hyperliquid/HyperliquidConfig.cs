namespace Connector.Core.Exchanges.Hyperliquid;

public sealed class HyperliquidConfig
{
    public string Network { get; init; } = "mainnet";
    public string? UserAddress { get; init; }
    public string? PrivateKey { get; init; }

    public string WsUrl => Network == "testnet"
        ? "wss://api.hyperliquid-testnet.xyz/ws"
        : "wss://api.hyperliquid.xyz/ws";

    public string HttpUrl => Network == "testnet"
        ? "https://api.hyperliquid-testnet.xyz"
        : "https://api.hyperliquid.xyz";

    public static HyperliquidConfig FromEnvironment() => new()
    {
        Network = Environment.GetEnvironmentVariable("HL_NETWORK") ?? "mainnet",
        UserAddress = Environment.GetEnvironmentVariable("HL_USER_ADDRESS"),
        PrivateKey = Environment.GetEnvironmentVariable("HL_PRIVATE_KEY")
    };
}

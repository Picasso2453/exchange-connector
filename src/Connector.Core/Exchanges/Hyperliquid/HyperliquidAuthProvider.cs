using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Connector.Core.Abstractions;
using Connector.Core.Transport;

namespace Connector.Core.Exchanges.Hyperliquid;

/// <summary>
/// Hyperliquid auth provider. Uses wallet address for WS private subscriptions.
/// Signing for REST exchange API calls uses EIP-712 typed data (not implemented here;
/// would require Ethereum signing library for production use).
/// </summary>
public sealed class HyperliquidAuthProvider : IAuthProvider
{
    private readonly HyperliquidConfig _config;

    public HyperliquidAuthProvider(HyperliquidConfig config)
    {
        _config = config;
    }

    public bool IsAuthenticated => _config.UserAddress is not null;

    public Task<TransportWsMessage?> GetWsAuthMessageAsync(CancellationToken ct)
    {
        // HL WebSocket doesn't require an auth handshake message.
        // Private subscriptions use the user address in the subscription itself.
        return Task.FromResult<TransportWsMessage?>(null);
    }

    public void ApplyRestAuth(TransportRestRequest request)
    {
        // HL REST /info endpoint doesn't require authentication for reads.
        // Exchange API (place/cancel orders) requires EIP-712 signing.
        // This is a stub for the signing path.
        if (_config.PrivateKey is null)
            throw new InvalidOperationException("HL_PRIVATE_KEY required for authenticated REST calls");

        // For exchange endpoints, the signature would be applied to the request body.
        // Full implementation requires keccak256 + secp256k1 signing (Nethereum or similar).
        request.Headers ??= new Dictionary<string, string>();
        request.Headers["X-HL-Address"] = _config.UserAddress ?? "";
    }
}

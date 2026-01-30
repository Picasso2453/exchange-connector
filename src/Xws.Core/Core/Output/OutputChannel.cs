using System.Threading.Channels;

namespace xws.Core.Output;

public sealed class OutputChannel
{
    private readonly Channel<string> _channel;

    public OutputChannel()
    {
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelReader<string> Reader => _channel.Reader;
    public ChannelWriter<string> Writer => _channel.Writer;

    public void Complete(Exception? error = null)
    {
        _channel.Writer.TryComplete(error);
    }
}

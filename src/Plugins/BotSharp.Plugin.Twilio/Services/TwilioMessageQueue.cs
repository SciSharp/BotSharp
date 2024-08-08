using BotSharp.Plugin.Twilio.Models;
using System.Threading.Channels;

namespace BotSharp.Plugin.Twilio.Services
{
    public class TwilioMessageQueue
    {
        private readonly Channel<CallerMessage> _queue;
        internal ChannelReader<CallerMessage> Reader => _queue.Reader;
        public TwilioMessageQueue()
        {
            BoundedChannelOptions options = new(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<CallerMessage>(options);
        }

        public async ValueTask EnqueueAsync(CallerMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            Console.WriteLine($"[{DateTime.UtcNow}] Enqueue {request}");
            await _queue.Writer.WriteAsync(request);
        }

        internal void Stop()
        {
            _queue.Writer.TryComplete();
        }
    }
}

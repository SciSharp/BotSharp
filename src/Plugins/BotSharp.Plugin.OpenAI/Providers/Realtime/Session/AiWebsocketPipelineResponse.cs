using System.ClientModel.Primitives;
using System.Net;
using System.Net.WebSockets;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime.Session;

public class AiWebsocketPipelineResponse : PipelineResponse
{

    public AiWebsocketPipelineResponse()
    {
        
    }

    private int _status;
    public override int Status => _status;

    private string _reasonPhrase;
    public override string ReasonPhrase => _reasonPhrase;

    private MemoryStream _contentStream = new();
    public override Stream? ContentStream
    {
        get
        {
            return _contentStream != null ? _contentStream : new MemoryStream();
        }
        set => throw new NotImplementedException();
    }

    private BinaryData _content;
    public override BinaryData Content
    {
        get
        {
            if (_content == null)
            {
                _content = new(_contentStream.ToArray());
            }
            return _content;
        }
    }

    protected override PipelineResponseHeaders HeadersCore => throw new NotImplementedException();

    public bool IsComplete { get; private set; } = false;


    public void CollectReceivedResult(WebSocketReceiveResult receivedResult, BinaryData receivedBytes)
    {
        if (ContentStream.Length == 0)
        {
            _status = ConvertWebsocketCloseStatusToHttpStatus(receivedResult.CloseStatus ?? WebSocketCloseStatus.Empty);
            _reasonPhrase = receivedResult.CloseStatusDescription?? (receivedResult.CloseStatus ?? WebSocketCloseStatus.Empty).ToString();
        }
        else if (receivedResult.MessageType != WebSocketMessageType.Text)
        {
            throw new NotImplementedException($"{nameof(AiWebsocketPipelineResponse)} currently supports only text messages.");
        }

        var rawBytes = receivedBytes.ToArray();
        _contentStream.Position = _contentStream.Length;
        _contentStream.Write(rawBytes, 0, rawBytes.Length);
        _contentStream.Position = 0;
        IsComplete = receivedResult.EndOfMessage;
    }

    public override BinaryData BufferContent(CancellationToken cancellationToken = default)
    {
        return Content;
    }

    public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<BinaryData>(Task.FromResult(Content));
    }

    public override void Dispose()
    {
        ContentStream?.Dispose();
    }

    private static int ConvertWebsocketCloseStatusToHttpStatus(WebSocketCloseStatus status)
    {
        int res;

        switch (status)
        {
            case WebSocketCloseStatus.Empty:
            case WebSocketCloseStatus.NormalClosure:
                res = (int)HttpStatusCode.OK;
                break;
            case WebSocketCloseStatus.EndpointUnavailable:
            case WebSocketCloseStatus.ProtocolError:
            case WebSocketCloseStatus.InvalidMessageType:
            case WebSocketCloseStatus.InvalidPayloadData:
            case WebSocketCloseStatus.PolicyViolation:
                res = (int)HttpStatusCode.BadRequest;
                break;
            case WebSocketCloseStatus.MessageTooBig:
                res = (int)HttpStatusCode.RequestEntityTooLarge;
                break;
            case WebSocketCloseStatus.MandatoryExtension:
                res = 418;
                break;
            case WebSocketCloseStatus.InternalServerError:
                res = (int)HttpStatusCode.InternalServerError;
                break;
            default:
                res = (int)HttpStatusCode.InternalServerError;
                break;
        }

        return res;
    }
}

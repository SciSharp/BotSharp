using NAudio.Wave;

namespace BotSharp.Test.RealtimeVoice.Audio;

internal class AudioInStream : Stream
{
    private const int SAMPLE_RATE = 16000;
    private const int BYTES_PER_SAMPLE = 2;
    private const int CHANNELS = 1;
    private const int SAMPLING_SECONDS = 10;
    private const int TIMEOUT_SECONDS = 100;

    private readonly byte[] _buffer = new byte[SAMPLE_RATE * BYTES_PER_SAMPLE * CHANNELS * SAMPLING_SECONDS];
    private readonly object _lock = new();
    private int _bufferReadPtr = 0;
    private int _bufferWritePtr = 0;
    private readonly WaveInEvent _waveInEvent;

    private AudioInStream()
    {
        _waveInEvent = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SAMPLE_RATE, BYTES_PER_SAMPLE * 8, CHANNELS),
            DeviceNumber = 0
        };

        _waveInEvent.DataAvailable += (_, e) =>
        {
            lock (_lock)
            {
                var bytesToCopy = e.BytesRecorded;
                if (_bufferWritePtr + bytesToCopy >= _buffer.Length)
                {
                    var chunkLength = _buffer.Length - _bufferWritePtr;
                    Array.Copy(e.Buffer, 0, _buffer, _bufferWritePtr, chunkLength);
                    bytesToCopy -= chunkLength;
                    _bufferWritePtr = 0;
                }
                Array.Copy(e.Buffer, e.BytesRecorded - bytesToCopy, _buffer, _bufferWritePtr, bytesToCopy);
                _bufferWritePtr += bytesToCopy;
            }
        };

        _waveInEvent.StartRecording();
    }

    public static AudioInStream Init() => new();

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var total = count;

        while (GetAvailableBytes() < count)
        {
            Thread.Sleep(TIMEOUT_SECONDS);
        }

        lock (_lock)
        {
            if (_bufferReadPtr + count >= _buffer.Length)
            {
                var chunkLength = _buffer.Length - _bufferReadPtr;
                Array.Copy(_buffer, _bufferReadPtr, buffer, offset, chunkLength);
                _bufferReadPtr = 0;
                count -= chunkLength;
                offset += chunkLength;
            }
            Array.Copy(_buffer, _bufferReadPtr, buffer, offset, count);
            _bufferReadPtr += count;
        }

        return total;
    }

    private int GetAvailableBytes()
    {
        if (_bufferWritePtr >= _bufferReadPtr)
        {
            return _bufferWritePtr - _bufferReadPtr;
        }
        else
        {
            return _buffer.Length - _bufferReadPtr + _bufferWritePtr;
        }
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing = true)
    {
        _waveInEvent?.Dispose();
        base.Dispose(disposing);
    }
}

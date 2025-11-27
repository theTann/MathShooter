using System;
using System.Buffers;
using System.Threading;

public class NetworkBuffer : IBufferWriter<byte>, IPoolObject {
    private readonly int _logicalSize;
    private readonly byte[] _buffer;
    
    private int _writePos;
    private int _readPos;
    private int _writtenSize;

    private int _refCount;
    
    public NetworkBuffer(int size, bool doubleSize = true) {
        // send buffer로 사용될 때에는 doubleSize일 필요가 없는듯하다.
        int bufferSize = doubleSize == true ? size * 2 : size;
        _buffer = new byte[bufferSize];
        _logicalSize = size;
        resetBufferPosition();
    }

    public void addRefCount() {
        Interlocked.Increment(ref _refCount);
    }

    public int decRefCount() {
        int result = Interlocked.Decrement(ref _refCount);
        return result;
    }

    private int getRemainSize() {
        return _logicalSize - _writtenSize;
    }

    public int getWrittenSize() {
        return _writtenSize;
    }

    public ArraySegment<byte> getBufferAsArrSegment() {
        int remainSize = getRemainSize();
        if (remainSize <= 0) {
            return ArraySegment<byte>.Empty;
        }

        ArraySegment<byte> buffer = new ArraySegment<byte>(_buffer, _writePos, remainSize);
        return buffer;
    }

    public Memory<byte> getBuffer() {
        int remainSize = getRemainSize();
        if (remainSize <= 0) {
            return Memory<byte>.Empty;
        }

        Memory<byte> buffer = new Memory<byte>(_buffer, _writePos, remainSize);
        return buffer;
    }

    public Span<byte> getBufferAsSpan() {
        int remainSize = getRemainSize();
        if (remainSize <= 0) {
            return Span<byte>.Empty;
        }

        Span<byte> buffer = new Span<byte>(_buffer, _writePos, remainSize);
        return buffer;
    }

    public void written(int size) {
        _writePos += size;
        if (_writePos > _logicalSize) {
            _writePos -= _logicalSize;
        }
        _writtenSize += size;
    }

    public Span<byte> peekBufferAsSpan(int offset, int length) {
        if (_writtenSize < offset + length) {
            return Span<byte>.Empty;
        }

        if (getRemainSize() < offset + length) {
            throw new ArgumentOutOfRangeException("out of buffer");
        }

        if (_logicalSize < offset + length) {
            throw new ArgumentOutOfRangeException($"buffer size {_logicalSize}. but you request {length}");
        }

        Span<byte> buffer = new Span<byte>(_buffer, _readPos + offset, length);
        return buffer;
    }

    public Span<byte> peekBufferAsSpan(int length) {
        return peekBufferAsSpan(0, length);
    }

    public Memory<byte> peekBuffer() {
        return peekBuffer(0, _writtenSize);
    }

    public Memory<byte> peekBuffer(int offset, int length) {
        if (_writtenSize < offset + length) {
            return Memory<byte>.Empty;
        }

        if (getRemainSize() < offset + length) {
            throw new ArgumentOutOfRangeException("out of buffer");
        }

        if (_logicalSize < offset + length) {
            throw new ArgumentOutOfRangeException($"buffer size {_logicalSize}. but you request {length}");
        }

        Memory<byte> buffer = new Memory<byte>(_buffer, _readPos + offset, length);
        return buffer;
    }

    public Memory<byte> peekBuffer(int length) {
        return peekBuffer(0, length);
    }

    public void flushBuffer(int size) {
        int newReadPos = _readPos + size;
        if(newReadPos > _logicalSize) {
            newReadPos -= _logicalSize;
        }

        if(newReadPos > _writePos) {
            throw new ArgumentOutOfRangeException($"invalid flush size.{size}");
        }

        _readPos = newReadPos;
        _writtenSize -= size;

        if(_readPos == _writePos) {
            resetBufferPosition();
        }
    }

    public void reset() {
        if (_refCount != 0) {
            Console.Error.WriteLine($"invalid ref count!");
        }

        resetBufferPosition();
    }

    void resetBufferPosition() {
        _writtenSize = 0;
        _writePos = 0;
        _readPos = 0;
    }
    
    void IBufferWriter<byte>.Advance(int count) {
        written(count);
    }

    Memory<byte> IBufferWriter<byte>.GetMemory(int sizeHint) {
        if(getRemainSize() < sizeHint) {
            throw new Exception("not enough buffer");
        }
        return getBuffer();
    }

    Span<byte> IBufferWriter<byte>.GetSpan(int sizeHint) {
        if (getRemainSize() < sizeHint) {
            throw new Exception("not enough buffer");
        }
        return getBufferAsSpan();
    }
}

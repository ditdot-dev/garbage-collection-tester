using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarbageCollectionTester
{
    internal class ResourceManager : IDisposable
    {
        private readonly ArrayPool<byte> _pool;
        private byte[] _buffer;
        private bool _disposed;

        public ResourceManager(int bufferSize)
        {
            _pool = ArrayPool<byte>.Shared;
            _buffer = _pool.Rent(bufferSize);
        }

        public void UseResource()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceManager));

            Span<byte> span = _buffer.AsSpan();
            span.Fill(0xFF); // Simulate usage
            Console.WriteLine("Resource is in use.");
        }

        // The public Dispose method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The protected Dispose method follows the dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Release managed resources if necessary
            }

            // Release unmanaged resources
            if (_buffer != null)
            {
                _pool.Return(_buffer);
                _buffer = null;
            }

            _disposed = true;
        }

        // Finalizer, only if necessary (e.g., for unmanaged resources)
        ~ResourceManager()
        {
            Dispose(false);
        }
    }
}

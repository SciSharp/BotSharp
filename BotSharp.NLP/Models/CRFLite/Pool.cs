using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BotSharp.Models.CRFLite
{
    /// <summary>
    /// Represents general purpose pool that has no restrictions (e.g. grows if it's required)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class Pool<T>
    {
        private int _totalCount;
        private readonly ConcurrentStack<T> _container = new ConcurrentStack<T>();
        private readonly Func<Pool<T>, T> _creator;
        private readonly Action<T> _cleaner;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public Pool(Func<Pool<T>, T> creator, Action<T> cleaner = null)
        {
            _creator = creator;
            _cleaner = cleaner;
        }

        /// <summary>
        /// Gets item from pool or creates a new item
        /// </summary>
        /// <returns></returns>
        public PoolItem<T> GetOrCreate()
        {
            T item;
            if (_container.TryPop(out item))
            {
                return new PoolItem<T>(item, _cleaner, this);
            }
            var newItem = _creator(this);
            if (newItem == null)
            {
                throw new ApplicationException("Unable to create new pool item");
            }
            Interlocked.Increment(ref _totalCount);
            return new PoolItem<T>(newItem, _cleaner, this);
        }

        /// <summary>
        /// Returns amount of free items in the bag
        /// </summary>
        public int FreeCount { get { return _container.Count; } }

        /// <summary>
        /// Returns amount items created by pool
        /// </summary>
        public int TotalCount { get { return _totalCount; } }

        private void Return(T item)
        {
            _container.Push(item);
        }

        /// <summary>
        /// Pool item that is return when pool request is processed
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        internal struct PoolItem<T1> : IDisposable
        {
            /// <summary>
            /// Pooled item
            /// </summary>
            public readonly T1 Item;
            private readonly Pool<T1> _owner;
            private readonly Action<T1> _cleaner;

            /// <summary>
            /// Creates a new pool item
            /// </summary>
            /// <param name="item"></param>
            /// <param name="cleaner"></param>
            /// <param name="owner"></param>
            internal PoolItem(T1 item, Action<T1> cleaner, Pool<T1> owner)
            {
                Item = item;
                _cleaner = cleaner;
                _owner = owner;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _cleaner?.Invoke(Item);
                _owner.Return(Item);
            }
        }
    }
}
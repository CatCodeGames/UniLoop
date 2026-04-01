using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace CatCode.Collections
{
    /// <summary>
    /// Dense array with deferred add/remove operations.
    /// Changes are applied later so iteration stays stable.
    /// </summary>
    public sealed class DeferredDenseArray<T>
    {
        private struct Entry
        {
            public T Element;
            public ElementHandle Handle;

            public Entry(T element, ElementHandle handle)
            {
                Element = element;
                Handle = handle;
            }
        }

        private bool _addRequested;
        private bool _removeRequested;

        private int _count;
        private int _size;
        private Entry[] _entries;

        private readonly int _growSize;
        private readonly IObjectPool<ElementHandle> _handlesPool;

        private readonly List<Entry> _pendingAdd = new();

        public int Count => _count;
        public int PendingCount => _pendingAdd.Count;
        public int TotalCount => _count + _pendingAdd.Count;

        public T this[int index] => _entries[index].Element;
        public T this[ElementHandle handle] => handle.IsPendingAdd
            ? _pendingAdd[handle.Index].Element
            : _entries[handle.Index].Element;

        public DeferredDenseArray(int startSize, int growSize)
        {
            _size = startSize;
            _growSize = growSize;

            _entries = new Entry[_size];

            _handlesPool = new ObjectPool<ElementHandle>(
                createFunc: () => new(),
                actionOnRelease: instance => instance.Reset(),
                collectionCheck: false,
                defaultCapacity: startSize);


            _pendingAdd.Capacity = startSize;
        }

        public ElementHandle Add(T item)
        {
            _addRequested = true;

            var handle = _handlesPool.Get();

            handle.Index = _pendingAdd.Count;
            handle.IsPendingAdd = true;

            var entry = new Entry(item, handle);
            _pendingAdd.Add(entry);

            return handle;
        }

        public bool Remove(ElementHandle handle)
        {
            if (handle.IsPendingRemove)
                return false;

            _removeRequested = true;

            handle.IsPendingRemove = true;
            handle.Generation++;

            return true;
        }

        public void RemoveAt(int index)
        {
            var handle = _entries[index].Handle;
            Remove(handle);
        }


        public void ApplyChanges()
        {
            ApplyAdd();
            ApplyRemove();
        }

        public void ApplyAdd()
        {
            if (!_addRequested)
                return;
            _addRequested = false;

            var newCount = _count + _pendingAdd.Count;
            if (newCount > _size)
            {
                _size = (newCount / _growSize + 1) * _growSize;
                Array.Resize(ref _entries, _size);
            }

            var count = _pendingAdd.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = _pendingAdd[i];
                var handle = entry.Handle;

                if (handle.IsPendingRemove)
                {
                    _handlesPool.Release(handle);
                    continue;
                }
                handle.Index = _count;
                handle.IsPendingAdd = false;

                _entries[_count] = entry;
                _count++;
            }
            _pendingAdd.Clear();
        }

        public void ApplyRemove()
        {
            if (!_removeRequested)
                return;
            _removeRequested = false;

            var span = _entries.AsSpan(0, _count);

            var lastLiveIndex = 0;
            int rangeStartIndex;
            int rangeEndIndex;

            while (lastLiveIndex < _count && !span[lastLiveIndex].Handle.IsPendingRemove)
                lastLiveIndex++;

            rangeStartIndex = lastLiveIndex;

            while (rangeStartIndex < _count)
            {
                while (rangeStartIndex < _count && span[rangeStartIndex].Handle.IsPendingRemove)
                {
                    _handlesPool.Release(span[rangeStartIndex].Handle);
                    rangeStartIndex++;
                }

                rangeEndIndex = rangeStartIndex;
                var realIndex = lastLiveIndex;
                while (rangeEndIndex < _count && !span[rangeEndIndex].Handle.IsPendingRemove)
                {
                    span[rangeEndIndex].Handle.Index = realIndex;
                    rangeEndIndex++;
                    realIndex++;
                }

                if (rangeEndIndex - rangeStartIndex > 0)
                    span[rangeStartIndex..rangeEndIndex].CopyTo(span[lastLiveIndex..]);

                lastLiveIndex += (rangeEndIndex - rangeStartIndex);
                rangeStartIndex = rangeEndIndex;
            }
            _count = lastLiveIndex;
        }
    }
}
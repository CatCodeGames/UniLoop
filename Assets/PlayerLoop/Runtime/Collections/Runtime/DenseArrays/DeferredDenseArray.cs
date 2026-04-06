using System;
using System.Collections.Generic;

namespace CatCode.Collections
{
    /// <summary>
    /// Dense array with deferred add/remove operations.
    /// Changes are applied later so iteration stays stable.
    /// </summary>
    public sealed class DeferredDenseArray<T>
    {
        private struct HandleSlot
        {
            public int denseIndex;
            public int generation;
            public bool isPendingAdd;

            public HandleSlot(int index)
            {
                denseIndex = index;
                generation = 0;
                isPendingAdd = false;
            }
        }

        private struct DenseEntry
        {
            public T element;
            public int slotID;
            public bool isPendingRemove;

            public DenseEntry(T element, int slotID)
            {
                this.element = element;
                this.slotID = slotID;
                isPendingRemove = false;
            }
        }

        private bool _hasPendingAdd;
        private bool _hasPendingRemove;

        private int _count;
        private int _size;

        private DenseEntry[] _entries;
        private HandleSlot[] _slots;

        private int[] _freeIDs;
        private int _freeTop;

        private readonly List<DenseEntry> _pendingAdds = new();

        public int Count => _count;
        public int PendingCount => _pendingAdds.Count;
        public int TotalCount => _pendingAdds.Count + _count;

        public T this[int index] => _entries[index].element;

        public T this[ElementHandle handle]
        {
            get
            {
                var id = handle.id;
                ref var slot = ref _slots[id];
                var index = slot.denseIndex;
                if (slot.isPendingAdd)
                    return _pendingAdds[index].element;
                else
                    return _entries[index].element;
            }
        }

        public DeferredDenseArray(int startSize)
        {
            _size = startSize;
            _entries = new DenseEntry[startSize];
            _slots = new HandleSlot[startSize];
            _freeIDs = new int[startSize];
            for (int i = 0; i < startSize; i++)
            {
                _slots[i] = new HandleSlot(i);
                _freeIDs[_count] = i;
                _count++;
            }
            _freeTop = startSize;
            _count = 0;
        }


        private int AllocateID()
        {
            if (_freeTop == 0)
                Resize();

            _freeTop--;
            return _freeIDs[_freeTop];
        }

        public ElementHandle Add(T item)
        {
            _hasPendingAdd = true;

            int slotID = AllocateID();
            ref var slot = ref _slots[slotID];

            slot.denseIndex = _pendingAdds.Count;
            slot.isPendingAdd = true;

            var pending = new DenseEntry(item, slotID);
            _pendingAdds.Add(pending);

            return new ElementHandle(slotID, slot.generation);
        }

        public void ApplyAdd()
        {
            if (!_hasPendingAdd)
                return;
            _hasPendingAdd = false;

            var count = _pendingAdds.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = _pendingAdds[i];
                var slotID = entry.slotID;

                ref var slot = ref _slots[slotID];
                slot.isPendingAdd = false;
                slot.denseIndex = _count;
                _entries[_count] = entry;
                _count++;
            }

            _pendingAdds.Clear();
        }

        public void Remove(ElementHandle handle)
        {
            _hasPendingRemove = true;

            var denseIndex = _slots[handle.id].denseIndex;
            ref var entry = ref _entries[denseIndex];
            entry.isPendingRemove = true;
        }

        public void ApplyRemove()
        {
            if (!_hasPendingRemove)
                return;
            _hasPendingRemove = false;

            var span = _entries.AsSpan(0, _count);

            var lastLiveIndex = 0;
            int rangeStartIndex;
            int rangeEndIndex;

            while (lastLiveIndex < _count && !_entries[lastLiveIndex].isPendingRemove)
                lastLiveIndex++;

            rangeStartIndex = lastLiveIndex;

            while (rangeStartIndex < _count)
            {

                while (rangeStartIndex < _count)
                {
                    ref var entry = ref span[rangeStartIndex];
                    if (!entry.isPendingRemove)
                        break;

                    var slotID = span[rangeStartIndex].slotID;

                    _slots[slotID].generation++;
                    _slots[slotID].denseIndex = -1;

                    _freeIDs[_freeTop] = slotID;
                    _freeTop++;

                    rangeStartIndex++;
                }

                rangeEndIndex = rangeStartIndex;
                var realIndex = lastLiveIndex;

                while (rangeEndIndex < _count)
                {
                    ref var entry = ref span[rangeEndIndex];
                    if (entry.isPendingRemove)
                        break;

                    var slotID = span[rangeEndIndex].slotID;
                    _slots[slotID].denseIndex = realIndex;

                    rangeEndIndex++;
                    realIndex++;
                }

                if (rangeEndIndex - rangeStartIndex > 0)
                    span[rangeStartIndex..rangeEndIndex].CopyTo(span[lastLiveIndex..]);

                lastLiveIndex += (rangeEndIndex - rangeStartIndex);
                rangeStartIndex = rangeEndIndex;
            }

            Array.Clear(_entries, lastLiveIndex, _count - lastLiveIndex);
            _count = lastLiveIndex;
        }

        private void Resize()
        {
            var newCount = _count + _pendingAdds.Count;
            if (newCount < _size)
                return;

            _size = (_size > int.MaxValue / 2)
                ? int.MaxValue
                : _size * 2;

            Array.Resize(ref _entries, _size);
            Array.Resize(ref _slots, _size);
            Array.Resize(ref _freeIDs, _size);

            for (int i = newCount; i < _size; i++)
            {
                _slots[i] = new HandleSlot(i);
                _freeIDs[_freeTop] = i;
                _freeTop++;
            }
        }


        public void ApplyChanges()
        {
            ApplyAdd();
            ApplyRemove();
        }

        public bool IsValid(ElementHandle handle)
        {
            ref var slot = ref _slots[handle.id];
            return slot.generation == handle.generation;
        }

        public void RemoveAt(int i)
        {
            _hasPendingRemove = true;

            ref var entry = ref _entries[i];
            entry.isPendingRemove = true;
        }
    }
}
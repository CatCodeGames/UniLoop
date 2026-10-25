using System;

namespace CatCode.PlayerLoops.Collections
{
    /// <summary>
    /// Dense array with deferred add/remove operations.
    /// Changes are applied later so iteration stays stable.
    /// </summary>
    /// <remarks>
    /// Elements are stored in one contiguous block: [0, Count) are active, [Count, TotalCount) are pending adds.
    /// A handle points to a slot and the slot points to the dense index,
    /// so elements can move during compaction without invalidating handles.
    /// </remarks>
    public sealed class DeferredDenseArray<T>
    {
        private const int MinCapacity = 4;

        private struct Slot
        {
            // -1 while the slot is free.
            public int Index;
            // Starts from 1, so default(ElementHandle) is never valid.
            public int Generation;
        }

        private struct ElementMeta
        {
            public int SlotId;
            public bool IsPendingRemove;
        }

        private T[] _elements;
        private ElementMeta[] _meta;
        private Slot[] _slots;

        private int[] _freeSlotIds;
        private int _freeCount;

        private int _count;
        private int _pendingCount;
        private bool _hasPendingRemove;


        public int Count => _count;
        public int PendingCount => _pendingCount;
        public int TotalCount => _count + _pendingCount;

        public ref T this[int index] => ref _elements[index];

        public ref T this[ElementHandle handle]
        {
            get
            {
                if (!IsValid(handle))
                    throw new ArgumentException("The handle is not valid for this array.", nameof(handle));

                return ref _elements[_slots[handle.Id].Index];
            }
        }


        public DeferredDenseArray(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            capacity = Math.Max(capacity, MinCapacity);

            _elements = new T[capacity];
            _meta = new ElementMeta[capacity];
            _slots = new Slot[capacity];
            _freeSlotIds = new int[capacity];

            AddFreeSlots(0, capacity);
        }


        public ElementHandle Add(T item)
        {
            if (_freeCount == 0)
                Resize();

            _freeCount--;
            var slotId = _freeSlotIds[_freeCount];
            var index = _count + _pendingCount;

            ref var slot = ref _slots[slotId];
            slot.Index = index;

            _elements[index] = item;
            _meta[index] = new ElementMeta { SlotId = slotId };
            _pendingCount++;

            return new ElementHandle(slotId, slot.Generation);
        }

        public void Remove(ElementHandle handle)
        {
            if (IsValid(handle))
                RemoveAt(_slots[handle.Id].Index);
        }

        public void RemoveAt(int index)
        {
            _meta[index].IsPendingRemove = true;
            _hasPendingRemove = true;
        }


        public void ApplyChanges()
        {
            ApplyAdd();
            ApplyRemove();
        }

        public void ApplyAdd()
        {
            _count += _pendingCount;
            _pendingCount = 0;
        }

        public void ApplyRemove()
        {
            if (!_hasPendingRemove)
                return;
            _hasPendingRemove = false;

            var oldTotalCount = TotalCount;

            var write = 0;
            while (write < _count && !_meta[write].IsPendingRemove)
                write++;

            var read = write;
            while (read < _count)
            {
                while (read < _count && _meta[read].IsPendingRemove)
                {
                    FreeSlot(_meta[read].SlotId);
                    read++;
                }

                var liveStart = read;
                while (read < _count && !_meta[read].IsPendingRemove)
                    read++;

                var liveLength = read - liveStart;
                MoveRange(liveStart, liveLength, write);
                write += liveLength;
            }

            var pendingStart = write;
            MoveRange(_count, _pendingCount, pendingStart);
            _count = write;
                        
            for (int i = pendingStart; i < pendingStart + _pendingCount; i++)
            {
                if (_meta[i].IsPendingRemove)
                {
                    _hasPendingRemove = true;
                    break;
                }
            }

            Array.Clear(_elements, TotalCount, oldTotalCount - TotalCount);
        }


        public bool IsValid(ElementHandle handle)
        {
            if (handle.Id < 0 || handle.Id >= _slots.Length)
                return false;

            ref var slot = ref _slots[handle.Id];
            return slot.Index >= 0 && slot.Generation == handle.Generation;
        }

        public bool TryGetIndex(ElementHandle handle, out int index)
        {
            if (IsValid(handle))
            {
                index = _slots[handle.Id].Index;
                return true;
            }

            index = -1;
            return false;
        }

        public bool IsPendingRemove(ElementHandle handle)
            => TryGetIndex(handle, out var index) && IsPendingRemoveAt(index);

        public bool IsPendingRemoveAt(int index)
            => _meta[index].IsPendingRemove;

                
        private void MoveRange(int from, int length, int to)
        {
            if (length == 0 || from == to)
                return;

            Array.Copy(_elements, from, _elements, to, length);
            Array.Copy(_meta, from, _meta, to, length);

            for (int i = to; i < to + length; i++)
            {
                var slotId = _meta[i].SlotId;
                _slots[slotId].Index = i;
            }
        }

        private void FreeSlot(int slotId)
        {
            ref var slot = ref _slots[slotId];
            slot.Index = -1;
            slot.Generation++;

            _freeSlotIds[_freeCount] = slotId;
            _freeCount++;
        }

        private void Resize()
        {
            var oldCapacity = _elements.Length;
            var newCapacity = oldCapacity * 2;

            Array.Resize(ref _elements, newCapacity);
            Array.Resize(ref _meta, newCapacity);
            Array.Resize(ref _slots, newCapacity);
            Array.Resize(ref _freeSlotIds, newCapacity);

            AddFreeSlots(oldCapacity, newCapacity);
        }

        private void AddFreeSlots(int from, int to)
        {
            for (int id = to - 1; id >= from; id--)
            {
                _slots[id] = new Slot { Index = -1, Generation = 1 };

                _freeSlotIds[_freeCount] = id;
                _freeCount++;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Graphs;
using UnityEngine;

namespace CatCode.PlayerLoops.Collections
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

            public HandleSlot(int index)
            {
                denseIndex = index;
                generation = 0;
            }
        }
        private struct ElementMeta
        {
            public int slotID;
            public bool isPendingRemove;

            public ElementMeta(int ID)
            {
                slotID = ID;
                isPendingRemove = false;
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

        private T[] _elements;
        private ElementMeta[] _meta;
        private HandleSlot[] _slots;

        private int[] _freeIDs;
        private int _freeTop;

        private int _pendingCount;


        public int Count => _count;
        public int PendingCount => _pendingCount;
        public int TotalCount => _pendingCount + _count;

        public ref T this[int index] => ref _elements[index];

        public ref T this[ElementHandle handle]
        {
            get
            {
                var id = handle.id;
                ref var slot = ref _slots[id];
                var index = slot.denseIndex;

                return ref _elements[index];
            }
        }

        public DeferredDenseArray(int startSize)
        {
            _size = startSize;

            _elements = new T[startSize];
            _meta = new ElementMeta[startSize];
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

            _pendingCount = 0;
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

            var index = _count + _pendingCount;
            slot.denseIndex = index;

            _elements[index] = item;
            _meta[index] = new ElementMeta(slotID);

            _pendingCount++;

            return new ElementHandle(slotID, slot.generation);
        }

        public void ApplyAdd()
        {
            if (!_hasPendingAdd)
                return;
            _hasPendingAdd = false;

            _count += _pendingCount;
            _pendingCount = 0;
        }

        public void Remove(ElementHandle handle)
        {
            _hasPendingRemove = true;
            var denseIndex = _slots[handle.id].denseIndex;
            _meta[denseIndex].isPendingRemove = true;
        }

        public void ApplyRemove()
        {
            if (!_hasPendingRemove)
                return;
            _hasPendingRemove = false;

            var elementSpan = _elements.AsSpan(0, _count + _pendingCount);
            var metaSpan = _meta.AsSpan(0, _count + _pendingCount);

            var lastLiveIndex = 0;
            int rangeStartIndex;
            int rangeEndIndex;

            while (lastLiveIndex < _count && !_meta[lastLiveIndex].isPendingRemove)
                lastLiveIndex++;

            rangeStartIndex = lastLiveIndex;

            while (rangeStartIndex < _count)
            {
                while (rangeStartIndex < _count)
                {
                    ref var meta = ref _meta[rangeStartIndex];
                    if (!meta.isPendingRemove)
                        break;

                    var slotID = meta.slotID;
                    ref var slot = ref _slots[slotID];

                    slot.generation++;
                    slot.denseIndex = -1;

                    _freeIDs[_freeTop] = slotID;
                    _freeTop++;

                    rangeStartIndex++;
                }

                rangeEndIndex = rangeStartIndex;
                var realIndex = lastLiveIndex;

                while (rangeEndIndex < _count)
                {
                    ref var meta = ref _meta[rangeEndIndex];
                    if (meta.isPendingRemove)
                        break;

                    var slotID = meta.slotID;
                    _slots[slotID].denseIndex = realIndex;

                    rangeEndIndex++;
                    realIndex++;
                }

                if (rangeEndIndex - rangeStartIndex > 0)
                {
                    elementSpan[rangeStartIndex..rangeEndIndex].CopyTo(elementSpan[lastLiveIndex..]);
                    metaSpan[rangeStartIndex..rangeEndIndex].CopyTo(metaSpan[lastLiveIndex..]);
                }

                lastLiveIndex += (rangeEndIndex - rangeStartIndex);
                rangeStartIndex = rangeEndIndex;
            }

            if (_pendingCount > 0)
            {
                var pendingStart = _count;
                var pendingEnd = _count + _pendingCount;

                elementSpan[pendingStart..pendingEnd].CopyTo(elementSpan[lastLiveIndex..]);
                metaSpan[pendingStart..pendingEnd].CopyTo(metaSpan[lastLiveIndex..]);

                pendingStart = lastLiveIndex;
                pendingEnd = lastLiveIndex + _pendingCount;

                for (int i = pendingStart; i < pendingEnd; i++)
                {
                    ref var meta = ref _meta[i];
                    var slotID = meta.slotID;
                    _slots[slotID].denseIndex = i;
                }
            }

            var freeStart = lastLiveIndex + _pendingCount;
            var freeLength = _count + _pendingCount - freeStart;
            Array.Clear(_elements, freeStart, freeLength);
            Array.Clear(_meta, freeStart, freeLength);

            _count = lastLiveIndex;
        }

        private void Resize()
        {
            var newCount = _count + _pendingCount;
            if (newCount < _size)
                return;

            _size = (_size > int.MaxValue / 2)
                ? int.MaxValue
                : _size * 2;

            Array.Resize(ref _elements, _size);
            Array.Resize(ref _meta, _size);

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

            ref var entry = ref _meta[i];
            entry.isPendingRemove = true;
        }
    }
}
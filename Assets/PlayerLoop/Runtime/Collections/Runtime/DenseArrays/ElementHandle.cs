using System;

namespace CatCode.Collections
{
    /// <summary>
    /// Lightweight handle referencing an element inside the dense array.
    /// </summary>
    public sealed class ElementHandle : IComparable<ElementHandle>
    {
        public int Index;
        public uint Generation;

        public bool IsPendingAdd;
        public bool IsPendingRemove;

        public int CompareTo(ElementHandle other)
            => Index.CompareTo(other.Index);

        public void Reset()
        {
            Index = -1;
            IsPendingRemove = false;
            IsPendingAdd = false;
        }
    }
}
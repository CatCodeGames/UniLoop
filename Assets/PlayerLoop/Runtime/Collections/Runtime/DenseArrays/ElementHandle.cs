
namespace CatCode.Collections
{
    /// <summary>
    /// Lightweight handle referencing an element inside the dense array.
    /// </summary>
    public struct ElementHandle
    {
        public int id;
        public int generation;

        public ElementHandle(int id, int generation)
        {
            this.id = id;
            this.generation = generation;
        }
    }
}
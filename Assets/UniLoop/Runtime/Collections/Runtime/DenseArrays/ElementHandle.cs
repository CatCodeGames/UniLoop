
namespace CatCode.PlayerLoops.Collections
{
    public readonly struct ElementHandle
    {
        public readonly int Id;
        public readonly int Generation;

        public ElementHandle(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }
    }
}

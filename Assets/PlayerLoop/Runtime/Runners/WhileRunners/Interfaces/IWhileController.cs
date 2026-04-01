using CatCode.Collections;

namespace CatCode.PlayerLoops
{
    public interface IWhileController : IWhileRegistry
    {
        public void Cancel(ElementHandle handle);
    }
}
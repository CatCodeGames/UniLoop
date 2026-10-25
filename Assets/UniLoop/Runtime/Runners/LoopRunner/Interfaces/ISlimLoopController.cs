using CatCode.PlayerLoops.Collections;

namespace CatCode.PlayerLoops
{
    public interface ISlimLoopController
    {
        bool IsValid(ElementHandle handle);
        void Cancel(ElementHandle handle);
        bool IsActive(ElementHandle handle);
    }
}
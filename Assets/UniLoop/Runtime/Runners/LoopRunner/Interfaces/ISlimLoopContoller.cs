using CatCode.PlayerLoops.Collections;

namespace CatCode.PlayerLoops
{
    public interface ISlimLoopContoller
    {
        bool IsValid(ElementHandle handle);
        void Cancel(ElementHandle handle);
    }
}
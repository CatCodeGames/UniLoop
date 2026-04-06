using CatCode.Collections;

namespace CatCode.PlayerLoops
{
    public interface ISlimLoopContoller
    {
        bool IsValid(ElementHandle handle);
        void Cancel(ElementHandle handle);
    }
}
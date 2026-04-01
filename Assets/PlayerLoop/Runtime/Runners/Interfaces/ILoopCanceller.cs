using CatCode.Collections;

namespace CatCode.PlayerLoops
{
    public interface ILoopCanceller
    {
        void Cancel(ElementHandle handle);
    }
}
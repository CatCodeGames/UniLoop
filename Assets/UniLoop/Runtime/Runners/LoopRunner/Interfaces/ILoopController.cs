using CatCode.PlayerLoops.Collections;

namespace CatCode.PlayerLoops
{     
    public interface ILoopController : ILoopRegistry
    {
        void Cancel(ElementHandle handle);
    }
}
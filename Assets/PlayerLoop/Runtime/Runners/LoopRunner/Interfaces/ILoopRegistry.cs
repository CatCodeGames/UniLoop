using CatCode.Collections;
using System;

namespace CatCode.PlayerLoops
{
    public interface ILoopRegistry
    {
        public void SetOnCanceled(ElementHandle handle, Action onCanceled);
        public void SetOnCanceled<T>(ElementHandle handle, Action<T> onCanceled, T state);
    }
}
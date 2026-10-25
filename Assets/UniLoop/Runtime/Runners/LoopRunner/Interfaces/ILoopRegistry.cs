using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    public interface ILoopRegistry
    {
        public void SetOnCanceled(ElementHandle handle, Action onCanceled);
        public void SetOnCanceled<T>(ElementHandle handle, T state, Action<T> onCanceled) where T : class;

        public bool IsValid(ElementHandle handle);
        public bool IsActive(ElementHandle handle);
    }
}
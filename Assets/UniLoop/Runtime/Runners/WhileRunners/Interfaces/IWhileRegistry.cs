using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    public interface IWhileRegistry
    {
        public void SetOnCompleted(ElementHandle handle, Action onCompleted);
        public void SetOnCanceled(ElementHandle handle, Action onCanceled);

        public void SetOnCompleted<T>(ElementHandle handle, T state, Action<T> onCompleted) where T:class;
        public void SetOnCanceled<T>(ElementHandle handle, T state, Action<T> onCanceled) where T : class;

        public bool IsValid(ElementHandle handle);
    }
}
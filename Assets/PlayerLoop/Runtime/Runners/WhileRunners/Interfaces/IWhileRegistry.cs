using CatCode.Collections;
using System;

namespace CatCode.PlayerLoops
{
    public interface IWhileRegistry
    {
        public void SetOnCompleted(ElementHandle handle, Action onCompleted);
        public void SetOnCompleted<T>(ElementHandle handle, Action<T> onCompleted, T state);
        public void SetOnCanceled(ElementHandle handle, Action onCanceled);
        public void SetOnCanceled<T>(ElementHandle handle, Action<T> onCanceled, T state);
    }
}
namespace CatCode.PlayerLoops
{
    public interface IStatefulCallback
    {
        void Invoke();
        void Release();
        void InvokeAndRelease();
    }
}
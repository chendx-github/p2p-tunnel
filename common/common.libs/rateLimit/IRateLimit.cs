namespace common.libs.rateLimit
{
    public interface IRateLimit
    {
        void Init(int rate);
        bool TryGet(bool wait);
        void Disponse();
    }
}

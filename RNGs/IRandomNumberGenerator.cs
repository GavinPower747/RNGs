namespace RNGs.RNGs
{
    public interface IRandomNumberGenerator
    {
        string DisplayName { get; }
        double Next();
    }
}

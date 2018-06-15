using System;

namespace RNGs.RNGs
{
    public class SysRandomRng : IRandomNumberGenerator
    {
        public string DisplayName => "System.Random Generation";

        public double Next() => new Random().NextDouble();
    }
}

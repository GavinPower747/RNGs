using System;

namespace RNGs.RNGs
{
    public class SysRandomRng : IRandomNumberGenerator
    {
        public string DisplayName => "System.Random Generation";
        public Random rng;

        public SysRandomRng() 
        {
            rng = new Random();
        }

        public double Next() => rng.NextDouble();
    }
}

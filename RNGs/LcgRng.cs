using System;

namespace RNGs.RNGs
{
    public class LcgRng : IRandomNumberGenerator
    {
        public string DisplayName => "LCG RNG";
        private const long m = 4294967296; // aka 2^32
        private const long a = 1664525;
        private const long c = 1013904223;
        private long _last;

        public LcgRng()
        {
            _last = DateTime.Now.Ticks % m;
        }

        public double Next()
        {
            _last = ((a * _last) + c) % m;

            return _last;
        }
    }
}
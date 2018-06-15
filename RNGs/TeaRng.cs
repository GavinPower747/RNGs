using System;

namespace RNGs.RNGs
{
    public class TeaRng : IRandomNumberGenerator
    {
        public string DisplayName => "Tiny Encryption Algorithm RNG";

        private const uint Max = uint.MaxValue;
        private const uint Delta = 0x9e3779b9;
        private static readonly uint[] Key = {
            0x0C7D7A8B4,
            0x09ABFB3B6,
            0x073DC1683,
            0x017B7BE43
        };

        private uint x;
        private uint y;

        public TeaRng()
        {
            x = (uint)DateTime.UtcNow.Second;
            y = (uint)DateTime.UtcNow.Millisecond;
        }

        public TeaRng(uint seedX, uint seedY)
        {
            x = seedX;
            y = seedY;
        }

        public double Next()
        {
            uint sum = 0;

            for (int i = 0; i < 32; i++)
            {
                sum += Delta;
                x += ((y << 4) + Key[0]) ^ (y + sum) ^ ((y >> 5) + Key[1]);
                y += ((x << 4) + Key[2]) ^ (x + sum) ^ ((x >> 5) + Key[3]);
            }

            return (double)(x + y / Max) / Max;
        }
    }
}

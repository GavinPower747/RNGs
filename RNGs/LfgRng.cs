using System;
using System.Collections.Generic;

namespace RNGs.RNGs
{
    public class LfgRng : IRandomNumberGenerator
    {
        public string DisplayName => "Lagged Fibonacci Generator";

        private const int k = 10;
        private const int j = 7;
        private const int m = 2147483647;

        private const int a = 48271;
        private const int q = 44488;
        private const int r = 3399;

        private List<int> vals = null;
        private int curr;
        private object _lock = new object();

        public LfgRng() : this((int)DateTime.UtcNow.Ticks) {}

        public LfgRng(int seed)
        {
            vals = new List<int>();
            if (seed == 0) seed = 1;
            int lCurr = seed; 

            for (int i = 0; i < k + 1; ++i) 
            {
                int hi = lCurr / q; 
                int lo = lCurr % q;
                int t = (a * lo) - (r * hi);

                if (t > 0)
                    lCurr = t;
                else
                    lCurr = t + m;

                vals.Add(lCurr);
            }

            for (int ct = 0; ct < 1000; ++ct)
            {
                double dummy = this.Next();
            }
        }

        public double Next()
        {
            lock (_lock)
            {
                int left = vals[0] % m;
                int right = vals[k - j] % m;
                long sum = (long)left + (long)right;

                curr = (int)(sum % m);
                vals.Insert(k + 1, curr);
                vals.RemoveAt(0);
                return (1.0 * curr) / m;
            }
        }
    }
}

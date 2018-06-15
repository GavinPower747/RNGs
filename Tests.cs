using RNGs.RNGs;
using System;

namespace RNGs
{
    public class Tests
    {
        public static void KSTest(IRandomNumberGenerator rng)
        {
            int numReps = 1000;
            double failureProbability = 0.001; // probability of test failing with normal input
            int j;
            double[] samples = new double[numReps];

            for (j = 0; j != numReps; ++j)
                samples[j] = rng.Next();

            System.Array.Sort(samples);

            double CDF;
            double temp;
            int j_minus = 0, j_plus = 0;
            double K_plus = -double.MaxValue;
            double K_minus = -double.MaxValue;

            for (j = 0; j != numReps; ++j)
            {
                CDF = samples[j];
                temp = (j + 1.0) / numReps - CDF;
                if (K_plus < temp)
                {
                    K_plus = temp;
                    j_plus = j;
                }
                temp = CDF - (j + 0.0) / numReps;
                if (K_minus < temp)
                {
                    K_minus = temp;
                    j_minus = j;
                }
            }

            double sqrtNumReps = Math.Sqrt((double)numReps);
            K_plus *= sqrtNumReps;
            K_minus *= sqrtNumReps;

            // We divide the failure probability by four because we have four tests:
            // left and right tests for K+ and K-.
            double p_low = 0.25 * failureProbability;
            double p_high = 1.0 - 0.25 * failureProbability;
            double cutoff_low = Math.Sqrt(0.5 * Math.Log(1.0 / (1.0 - p_low))) - 1.0 / (6.0 * sqrtNumReps);
            double cutoff_high = Math.Sqrt(0.5 * Math.Log(1.0 / (1.0 - p_high))) - 1.0 / (6.0 * sqrtNumReps);

            Console.WriteLine("\n\nTesting the random number distribution");
            Console.WriteLine("using the Kolmogorov-Smirnov (KS) test.\n");

            Console.WriteLine("K+ statistic: {0}", K_plus);
            Console.WriteLine("K+ statistic: {0}", K_minus);
            Console.WriteLine("Acceptable interval: [{0}, {1}]", cutoff_low, cutoff_high);
            Console.WriteLine("K+ max at {0} {1}", j_plus, samples[j_plus]);
            Console.WriteLine("K- max at {0} {1}", j_minus, samples[j_minus]);

            if (cutoff_low <= K_plus && K_plus <= cutoff_high && cutoff_low <= K_minus && K_minus <= cutoff_high)
                Console.WriteLine("\nKS test passed\n");
            else
                Console.WriteLine("\nKS test failed\n");
        }
    }
}

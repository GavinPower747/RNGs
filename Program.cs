using RNGs.RNGs;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RNGs
{
    class Program
    {
        const int PerformanceIterations = 50000;

        static IRandomNumberGenerator[] rngs = new IRandomNumberGenerator[]
        {
            new SysRandomRng(),
            new LcgRng(),           
            new LfgRng(),
            new MtRng(),
            new TeaRng()            
        };

        static void Main(string[] args)
        {
            foreach (var rng in rngs)
            {
                Console.WriteLine($"\nRunning tests for {rng.DisplayName}");

                Tests.KSTest(rng);
                ProfileSync($"Sync profile of {rng.DisplayName}", () => rng.Next());
                ProfileParallel($"Parallel profile of {rng.DisplayName}", () => rng.Next());
                Console.WriteLine("----------------------------------------------------------------------");
            }

            Console.ReadLine();
        }

        static double ProfileSync(string description, Action func)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            func(); //Omit first run due to JIT overhead

            var watch = new Stopwatch();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            watch.Start();

            for (int i = 0; i < PerformanceIterations; i++)
            {
                func();
            }

            watch.Stop();
            Console.Write(description);
            Console.WriteLine(" Time Elapsed {0} ms", watch.Elapsed.TotalMilliseconds);
            return watch.Elapsed.TotalMilliseconds;
        }

        static double ProfileParallel(string description, Action func)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            

            func(); //Omit first run due to JIT overhead

            var watch = new Stopwatch();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            watch.Start();
            Parallel.For(0, PerformanceIterations, (index) =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                func();
            });
            watch.Stop();
            Console.Write(description);
            Console.WriteLine(" Time Elapsed {0} ms", watch.Elapsed.TotalMilliseconds);
            return watch.Elapsed.TotalMilliseconds;
        }    
    }
}

//C# implementation from a port of a C library, Source lost to time....
//Not actually using a lot of this code, to be honest it was left in the talk for comic effect.

using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace RNGs.RNGs
{
    public class MtRng : IRandomNumberGenerator
    {
        public string DisplayName => "Mersenne Twister";

        #if VB2010orLater
        // Used by genrand_int128SignedInt():
        private BigInteger int128SignedIntMaxValue;
        #endif
        private BigInteger twoToThe128power;

        // a class (within the MTRandom class) to hold the MT generator state
        [Serializable()]
        public class MTState
        {
            public int mti;

            public uint[] mt = new uint[MtRng.Nuplim + 1];
            public MTState()
            {
                this.mti = MtRng.Nplus1;
            }
        }

        // Here lies the state of a generator so it can be saved to an XML file
        // and loaded later.

        public MTState state = new MTState();
        // a VB Random Class PRNG used in init_random() to seed MTRandom based
        // on the system clock
        private Random rng = null;
        //_________________________________________________________________________________________


        //#include <stdio.h>
        //
        ///* Period parameters */
        //#define N 624
        //#define M 397
        //#define MATRIX_A 0x9908b0dfUL   /* constant vector a */
        //#define UPPER_MASK 0x80000000UL /* most significant w-r bits */
        //#define LOWER_MASK 0x7fffffffUL /* least significant r bits */
        private const int N = 624;
        private const int M = 397;
            ///* constant vector a */
        private const uint MATRIX_A = 0x9908b0dfu;
            ///* most significant w-r bits */
        private const uint UPPER_MASK = 0x80000000u;
            ///* least significant r bits */
        private const uint LOWER_MASK = 0x7fffffffu;

        //To avoid unnecesary operations while using the Visual Basic interpreter:
        private const int kDiffMN = M - N;
        private const int Nuplim = N - 1;
        private const int Muplim = M - 1;
        private const int Nplus1 = N + 1;
        private const int NuplimLess1 = Nuplim - 1;

        private const int NuplimLessM = Nuplim - M;
        //static unsigned long mt[N];  /* the array for the state vector */
        //static int mti=N+1;          /* mti==N+1 means mt[N] is not initialized */
        // The following two VBA lines are replaced by code at the beginning of
        // class MTRandom.
        //'Dim mt(0 To Nuplim) As Integer	 '/* the array for the state vector */
        //'Dim mti As Integer = Nplus1

        //In the C original version the following array, mag01(), is declared within
        //the function genrand_int32(). In VBA I had to declare it global for performance
        //considerations, and because there is no way in VBA to emulate the use of the word
        //"static" in C:
        //
        //static unsigned long mag01[2]={0x0UL, MATRIX_A};
        ///* mag01[x] = x * MATRIX_A  for x=0,1 */
        private uint[] mag01 = {
            0u,
            MATRIX_A

        };
        //Other constants defined to be used in this Visual Basic version:

        //Powers of 2: k2_X means 2^X
        private const int k2_8 = 256;
        private const int k2_16 = 65536;

        private const int k2_24 = 16777216;
            //2^31   ==  2147483648 == 80000000
        private const double k2_31 = 2147483648.0;
            //2^32   ==  4294967296 == 0
        private const double k2_32 = 2.0 * k2_31;
            //2^32-1 ==  4294967295 == FFFFFFFF == -1
        private const double k2_32b = k2_32 - 1.0;

        //The following constant has its value defined by the authors of the
        //Mersenne Twister algorithm

        private const uint kDefaultSeed = 5489;

        //The following constant, is used within genrand_real1(), which returns values in [0,1]

        private const double kMT_1 = 1.0 / k2_32b;
        //The following constant, is used within genrand_real2(), which returns values in [0,1)

        private const double kMT_2 = 1.0 / k2_32;
        //The following constant, is used within genrand_real3(), which returns values in (0,1)

        private const double kMT_3 = kMT_2;
        //The following constant, used within genrand_res53(), is needed, because the Visual
        //Basic interpreter cannot read real LITERALS with the the same precision as a C compiler,
        //and so ends up truncating the least significant decimal digit(s), a '2' in this case.
        //The original factor used in the C code is: 9007199254740992.0
            //add lost digit '2'
        private const double kMT_res53 = 1.0 / (9.00719925474099E+15 + 2.0);


        //The following constants, are used within the ADDITIONAL functions genrand_real2b() and
        //genrand_real3b(), equivalent to genrand_real() and genrand_real3(), but that return
        //evenly distributed values in the ranges [0, 1-kMT_Gap] and [0+kMT_Gap, 1-kMT_Gap],
        //respectively. A similar statement is valid also for genrand_real2c(), genrand_real4b()
        //and genrand_real5b(). See the section "Functions and procedures implemented" above,
        //for more details.
        //
        //If you want to change the value of kMT_Gap, it is suggested to do it so that:
        //   5e-15 <= kMT_Gap <= 5e-2

            //5.0E-13
        private const double kMT_Gap = 5E-13;
            //1.0E-12
        private const double kMT_Gap2 = 2.0 * kMT_Gap;
            //0.9999999999990
        private const double kMT_GapInterval = 1.0 - kMT_Gap2;

        private const double kMT_2b = kMT_GapInterval / k2_32b;
        private const double kMT_2c = kMT_2b;
        private const double kMT_3b = kMT_2b;
        private const double kMT_4b = 2.0 / k2_32b;
            //1.999999999998/k2_32b
        private const double kMT_5b = (2.0 * kMT_GapInterval) / k2_32b;





        //_________________________________________________________________________________________
        // For Visual Basic .NET
        // ----------[MTRandom Contructors]-----------

        // initialize the PRNG with the default seed
        public MtRng()
        {
            //  init_genrand(5489UL); /* a default initial seed is used */
            this.init_genrand(kDefaultSeed);
            #if VB2010orLater
            this.init_int128SignedInt();
            // initialize constants for genrand_int128SignedInt()
            #endif
        }

        // initialize the MTRandom PRNG with seed
        public MtRng(uint seed)
        {
            this.init_genrand(seed);
            #if VB2010orLater
            this.init_int128SignedInt();
            // initialize constants for genrand_int128SignedInt()
            #endif
        }

        // initialize the MTRandom PRNG with an unsigned integer array
        public MtRng(ref uint[] array)
        {
            this.init_by_array(ref array);
            #if VB2010orLater
            this.init_int128SignedInt();
            // initialize constants for genrand_int128SignedInt()
            #endif
        }

        // Initialize the MTRandom PRNG with a pseudo-random seed.  Variable dummy
        // is used only to distinguish this overload from the others.
        public MtRng(bool dummy)
        {
            this.init_random(true);
            #if VB2010orLater
            this.init_int128SignedInt();
            // initialize constants for genrand_int128SignedInt()
            #endif
        }

        // Initialize the MTRandom PRNG with a cryptographically secure set of numbers.
        // THIS DOES NOT MAKE MTRandom CRYPTOGRAPHICALLY SECURE. It provides a good
        // set of initializers for general pseudorandom number use.
        public MtRng(double dummy)
        {
            this.init_by_crypto(0.0);
            #if VB2010orLater
            this.init_int128SignedInt();
            // initialize constants for genrand_int128SignedInt()
            #endif
        }


        // initialize the MTRandom PRNG with an XML file created by MTRandom.saveState()
        public MtRng(string fileName)
        {
            this.loadState(fileName);
            #if VB2010orLater
            this.init_int128SignedInt();
            // initialize constants for genrand_int128SignedInt()
            #endif
        }
        // ----------[End of MTRandom Contructors]-----------


        // Initialize the MTRandom PRNG with a pseudo-random seed.
        // reSeedFromClock: True - reseed the INITIALIZER RNG from the system clock
        //				   False - use the next RN from the INITIALIZER RNG

        public void init_random(bool reSeedFromClock)
        {
            // If this is the first call of init_random() or user asks for reseed
            // of rng from the system clock.
            // (Must check for rng Is Nothing because user might re-initialize
            // the MTRandom instance when it was first initialized another way.)
            if (rng == null | reSeedFromClock) {
                // seed rng from the system clock by making a new instance
                rng = new Random();
            }

            // initialize MTRandom with a pseudo-random Integer from rng
            this.init_genrand(Convert.ToUInt32(Convert.ToInt64(rng.Next(Int32.MinValue, Int32.MaxValue)) - Convert.ToInt64(Int32.MinValue)));
        }

        // Initialize the MTRandom PRNG with a cryptographically secure set of numbers.
        // THIS DOES NOT MAKE MTRandom CRYPTOGRAPHICALLY SECURE. It provides the full
        // range of approximately 2^19937 possible initial states/sequences.
        public void init_by_crypto(double dummy)
        {
            const int bytesPerUInteger = 4;
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            // Create a byte array to hold the random values.
            byte[] bytes = new byte[bytesPerUInteger * state.mt.Length + 1];
            // Fill the array with random values.
            rngCsp.GetBytes(bytes);

            // Fill state.mt() with the random values in bytes().
            // For all purposes--practical and impractical--the chance
            // is 0 that state.mt() will initialize to all zeroes.
            for (state.mti = 0; state.mti <= state.mt.GetUpperBound(0); state.mti++) {
                int baseByte = state.mti * bytesPerUInteger;
                state.mt[state.mti] = Convert.ToUInt32(bytes[baseByte + 3]) << 24 | Convert.ToUInt32(bytes[baseByte + 2]) << 16 | Convert.ToUInt32(bytes[baseByte + 1]) << 8 | Convert.ToUInt32(bytes[baseByte + 0]);
            }
            // Cause genrand_int32() to generate another N words when it
            // is called.
            state.mti = N;
        }

        // Save the PRNG state to a file as XML
        public void saveState(string fileName)
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(MTState));
                FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                serializer.Serialize(fs, state);
                fs.Close();
            } catch (Exception ex) {
                throw new MTRandomSaveStateException(ex.Message, Environment.StackTrace);
            }
        }

        // Load the PRNG state from a file created by MTRandom.saveState()
        public void loadState(string fileName)
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(MTState));
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                state = (MTState)serializer.Deserialize(fs);
                fs.Close();
            } catch (Exception ex) {
                throw new MTRandomLoadStateException(ex.Message, Environment.StackTrace);
            }

        }

        #if VB2010orLater
        // initialize constants needed by genrand_int128SignedInt()
        private void init_int128SignedInt()
        {
            int128SignedIntMaxValue = BigInteger.Parse("170141183460469231731687303715884105727");
            twoToThe128power = BigInteger.Parse("340282366920938463463374607431768211456");
        }
        #endif
        //_________________________________________________________________________________________


        //void init_genrand(unsigned long s)
        public void init_genrand(uint seed)
        {
            ///* initializes mt[N] with a seed */
            //mt[0]= s & 0xffffffffUL;
            //for (mti=1; mti<N; mti++) {
            //    mt[mti] =
            //    (1812433253UL * (mt[mti-1] ^ (mt[mti-1] >> 30)) + mti);
            //    /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
            //    /* In the previous versions, MSBs of the seed affect   */
            //    /* only MSBs of the array mt[].                        */
            //    /* 2002/01/09 modified by Makoto Matsumoto             */
            //    mt[mti] &= 0xffffffffUL;
            //    /* for >32 bit machines */

            uint tt = 0;

            state.mt[0] = (seed & 0xffffffffu);
            for (state.mti = 1; state.mti <= Nuplim; state.mti++) {
                //original expression, rearranged in one line:
                //mt[mti] = (1812433253UL * (mt[mti-1] ^ (mt[mti-1] >> 30)) + mti);

                tt = state.mt[state.mti - 1];
                state.mt[state.mti] = Convert.ToUInt32((1812433253uL * Convert.ToUInt64(tt ^ (tt >> 30)) + Convert.ToUInt64(state.mti)) & 0xffffffffuL);
                // The next statement is incorporated into the previous statement.
                //state.mt(state.mti) = state.mt(state.mti) And &HFFFFFFFFUI	 '/* for >32 bit machines */
            }

        }
        //init_genrand

        public void init_by_array(ref uint[] init_key)
        {
            //void init_by_array(unsigned long init_key[], int key_length)

            ///* initialize by an array with array-length */
            ///* init_key is the array for initializing keys */
            ///* key_length is its length */
            ///* slight change for C++, 2004/2/26 */

            //int i, j, k;
            int i = 0;
            int j = 0;
            int k = 0;
            int key_length = init_key.Length;
            uint tt = 0;


            //init_genrand(19650218UL);
            //i=1; j=0;
            //k = (N>key_length ? N : key_length);
            init_genrand(19650218u);
            i = 1;
            j = 0;
            k = Convert.ToInt32(((N > key_length) ? N : key_length));


            //for (; k; k--) {
            //while k<>0, that is: while k>0
            for (k = k; k >= 1; k += -1) {
                //original expression, rearranged in one line:
                //mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1664525UL)) + init_key[j] + j;

                tt = state.mt[i - 1];
                state.mt[i] = Convert.ToUInt32((Convert.ToUInt64((state.mt[i] ^ ((tt ^ (tt >> 30))) * 1664525uL)) + Convert.ToUInt64(init_key[j]) + Convert.ToUInt64(j)) & 0xffffffffuL);

                //mt[i] &= 0xffffffffUL;          /* for WORDSIZE > 32 machines */
                //'unnecesary, due to previous statement

                //i++; j++;
                //if (i>=N) { mt[0] = mt[N-1]; i=1; }
                //if (j>=key_length) j=0;
                i = i + 1;
                j = j + 1;
                if (i >= N){state.mt[0] = state.mt[Nuplim];i = 1;}
                if (j >= key_length)
                    j = 0;
            }


            //for (k=N-1; k; k--) {
            for (k = Nuplim; k >= 1; k += -1) {
                //original expression, rearranged in one line:
                //mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1566083941UL)) - i;  /* non linear */

                tt = state.mt[i - 1];
                state.mt[i] = Convert.ToUInt32(((Convert.ToUInt64(state.mt[i]) ^ Convert.ToUInt64((tt ^ (tt >> 30)) * 1566083941uL)) - Convert.ToUInt64(i)) & 0xffffffffuL);

                //mt[i] &= 0xffffffffUL;          /* for WORDSIZE > 32 machines */
                //'unnecesary, due to previous statement

                //i++;
                //if (i>=N) { mt[0] = mt[N-1]; i=1; }
                i = i + 1;
                if (i >= N){state.mt[0] = state.mt[Nuplim];i = 1;}
            }


            //mt[0] = 0x80000000UL;   /* MSB is 1; assuring non-zero initial array */
            state.mt[0] = 0x80000000u;
            ///* MSB is 1; assuring non-zero initial array */
        }
        //init_by_array


        // genrand_int32SignedInt() is only for compatiblity with the VBA version. It does not
        // serve the same purpose as in the VBA version.
        public int genrand_int32SignedInt()
        {
            long tmp = genrand_int32();

            if (tmp > Int32.MaxValue) {
                tmp -= UInt32.MaxValue + 1L;
            }

            return Convert.ToInt32(tmp);
        }

        public uint genrand_int32()
        {
            //unsigned long genrand_int32(void)
            ///* generates a random number on [0,0xffffffff]-interval */

            //unsigned long y;
            uint y = 0;

            //The below lines were replaced by another approach. See section "On performance" for details:
            //static unsigned long mag01[2]={0x0UL, MATRIX_A};
            ///* mag01[x] = x * MATRIX_A  for x=0,1 */

            //{ /* generate N words at one time */
            if ((state.mti >= N)) {
                //int kk;
                int kk = 0;

                //if (mti == N+1)   /* if sgenrand() has not been called, */
                //  init_genrand(5489UL); /* a default initial seed is used */
                if (state.mti == Nplus1)
                    init_genrand(kDefaultSeed);

                //for (kk=0;kk<N-M;kk++) {
                //    y = (mt[kk]&UPPER_MASK)|(mt[kk+1]&LOWER_MASK);
                //    mt[kk] = mt[kk+M] ^ (y >> 1) ^ mag01[y & 0x1UL];
                //}
                for (kk = 0; kk <= (NuplimLessM); kk++) {
                    y = (state.mt[kk] & UPPER_MASK) | (state.mt[kk + 1] & LOWER_MASK);
                    state.mt[kk] = state.mt[kk + M] ^ (y >> 1) ^ mag01[Convert.ToInt32(y & 1u)];
                }

                //for (;kk<N-1;kk++) {
                //    y = (mt[kk]&UPPER_MASK)|(mt[kk+1]&LOWER_MASK);
                //    mt[kk] = mt[kk+(M-N)] ^ (y >> 1) ^ mag01[y & 0x1UL];
                //}
                for (kk = kk; kk <= NuplimLess1; kk++) {
                    y = (state.mt[kk] & UPPER_MASK) | (state.mt[kk + 1] & LOWER_MASK);
                    state.mt[kk] = state.mt[kk + (M - N)] ^ (y >> 1) ^ mag01[Convert.ToInt32(y & 1u)];
                }

                //y = (mt[N-1]&UPPER_MASK)|(mt[0]&LOWER_MASK);
                //mt[N-1] = mt[M-1] ^ (y >> 1) ^ mag01[y & 0x1UL];
                y = (state.mt[Nuplim] & UPPER_MASK) | (state.mt[0] & LOWER_MASK);
                state.mt[N - 1] = state.mt[M - 1] ^ (y >> 1) ^ mag01[Convert.ToInt32(y & 1u)];
                //mti = 0;
                state.mti = 0;
            }


            y = state.mt[state.mti];
            state.mti = state.mti + 1;
            ///* Tempering */
            //y ^= (y >> 11);
            y = y ^ (y >> 11);

            //y ^= (y << 7) & 0x9d2c5680UL;
            y = y ^ (y << 7) & 0x9d2c5680u;

            //y ^= (y << 15) & 0xefc60000UL;
            y = y ^ (y << 15) & 0xefc60000u;

            //y ^= (y >> 18);
            //return y;
            return y ^ (y >> 18);
        }
        //genrand_int32

        public int genrand_int31()
        {
            //long genrand_int31(void)
            ///* generates a random number on [0,0x7fffffff]-interval */
            //return (long)(genrand_int32()>>1);
            return Convert.ToInt32(genrand_int32() >> 1);
        }
        //genrand_int31

        public double Next()
        {
            //double genrand_real1(void)
            ///* generates a random number on [0,1]-real-interval */
            //return genrand_int32()*(1.0/4294967295.0);     '/* divided by 2^32-1 */
            return genrand_int32() * kMT_1;
        }
        //genrand_real1

        public double genrand_real2()
        {
            //double genrand_real2(void)
            ///* generates a random number on [0,1)-real-interval */
            //return genrand_int32()*(1.0/4294967296.0);     '/* divided by 2^32 */
            return genrand_int32() * kMT_2;
        }
        //genrand_real2

        public double genrand_real3()
        {
            //double genrand_real3(void)
            ///* generates a random number on (0,1)-real-interval */
            //return (((double)genrand_int32()) + 0.5)*(1.0/4294967296.0);   '/* divided by 2^32 */
            return (Convert.ToDouble(genrand_int32()) + 0.5) * kMT_3;
        }
        //genrand_real3

        public double genrand_res53()
        {
            //double genrand_res53(void)
            ///* generates a random number on [0,1) with 53-bit resolution*/
            //unsigned long a=genrand_int32()>>5, b=genrand_int32()>>6;
            //return(a*67108864.0+b)*(1.0/9007199254740992.0);
            return kMT_res53 * ((genrand_int32() >> 5) * 67108864.0 + (genrand_int32() >> 6));
        }
        //genrand_res53

        ///* These (PREVIOUS) real versions are due to Isaku Wada, 2002/01/09 added */


        //The following functions are present only in the Visual Basic version, not in the
        //C version. See more comments in the definition of the constants used as factors:

        public double genrand_real2b()
        {
            //Returns results in the range [0,1) == [0, 1-kMT_Gap2]
            //Its lowest value is : 0.0
            //Its highest value is: 0.9999999999990
            return genrand_int32() * kMT_2b;
        }
        //genrand_real2b

        public double genrand_real2c()
        {
            //Returns results in the range (0,1] == [0+kMT_Gap2, 1.0]
            //Its lowest value is : 0.0000000000010  (1E-12)
            //Its highest value is: 1.0
            return kMT_Gap2 + (genrand_int32() * kMT_2c);
            //==kMT_Gap2+genrand_real2b()
        }
        //genrand_real2c

        public double genrand_real3b()
        {
            //double genrand_real3(void)
            //Returns results in the range (0,1) == [0+kMT_Gap, 1-kMT_Gap]
            //Its lowest value is : 0.0000000000005  (5E-13)
            //Its highest value is: 0.9999999999995
            return kMT_Gap + (genrand_int32() * kMT_3b);
        }
        //genrand_real3b

        //Mr. Kenneth C. Ives sent me some code and the idea in which I based genrand_real4b() and
        //genrand_real5b(). Added on 2005-Sep-12:

        public double genrand_real4b()
        {
            //Returns results in the range [-1,1] == [-1.0, 1.0]
            //Its lowest value is : -1.0
            //Its highest value is: 1.0
            return (genrand_int32() * kMT_4b) - 1.0;
        }
        //genrand_real4b

        public double genrand_real5b()
        {
            //Returns results in the range (-1,1) == [-kMT_GapInterval, kMT_GapInterval]
            //Its lowest value is : -0.9999999999990
            //Its highest value is: 0.9999999999990
            return kMT_Gap2 + ((genrand_int32() * kMT_5b) - 1.0);
        }
        //genrand_real5b

        //__________________________________________________________________________________________
        // The following functions were added by Ron Charlton 2008-09-23 for Visual Basic .NET.

        public uint genrand_intMax(uint N)
        {
            // Returns a UInteger in [0,n] for 0 <= n < 2^32
            // Its lowest value is : 0
            // Its highest value is: 4294967295 but <= N

            // Translated by Ron Charlton from C++ file 'MersenneTwister.h' where it is named
            // MTRand::randInt(const uint32& n), and has the following comments:
            //-----
            // Mersenne Twister random number generator -- a C++ class MTRand
            // Based on code by Makoto Matsumoto, Takuji Nishimura, and Shawn Cokus
            // Richard J. Wagner  v1.0  15 May 2003  rjwagner@writeme.com

            // Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
            // Copyright (C) 2000 - 2003, Richard J. Wagner
            // All rights reserved.                          
            //-----
            // MersenneTwister.h can be found at
            // http://www-personal.umich.edu/~wagnerr/MersenneTwister.html.

            // Find which bits are used in N
            // Optimized by Magnus Jonsson (magnus@smartelectronix.com)
            uint used = N;
            used = used | (used >> 1);
            used = used | (used >> 2);
            used = used | (used >> 4);
            used = used | (used >> 8);
            used = used | (used >> 16);

            // Draw numbers until one is found in [0,n]
            uint i = 0;
            do {
                i = genrand_int32() & used;
                // toss unused bits to shorten search
            } while (i > N);

            return i;
        }

        public uint genrand_intRange(uint lower, uint upper)
        {
            // Generate a pseudo-random integer between lower inclusive and upper inclusive for
            // 0 <= lower <= upper <= 4294967295.
            // Returns a UInteger in the range [lower,upper].
            // Its lowest value is : 0 but >= lower
            // Its highest value is: 4294967295 but <= upper
            //
            // Written by Ron Charlton, 2008-09-23.
            if (lower > upper) {
                // swap lower and upper
                uint temp = lower;
                lower = upper;
                upper = temp;
            }

            return lower + genrand_intMax(upper - lower);
        }

        public ulong genrand_int64()
        {
            // Returns an unsigned long in the range [0,2^64-1]
            // Its lowest value is : 0
            // Its highest value is: 18446744073709551615
            //
            // Written by Ron Charlton, 2008-09-23.

            return Convert.ToUInt64(genrand_int32()) | (Convert.ToUInt64(genrand_int32()) << 32);
        }

        #if VB2010orLater
        // The following function was added by Ron Charlton 2011-01-31 for Visual Basic .NET (.NET).

        public BigInteger genrand_int128SignedInt()
        {
            // Returns a signed, 128-bit BigInteger in the range [-2^127, 2^127-1]

            // Make a 128-bit UNsigned integer in the range [0,2^128-1]
            // (Using four calls to genrand_in32() is slower.)
            BigInteger tmp = ((BigInteger)genrand_int64() << 64) | (BigInteger)genrand_int64();

            // Convert to a signed 128-bit BigInteger.  (The "constants" are
            // initialized automatically in init_int128SignedInt().)
            if (tmp > int128SignedIntMaxValue) {
                tmp = tmp - twoToThe128power;
            }

            return tmp;
        }
        #endif
    }



    // ----------[EXCEPTION CLASSES]----------

    // The "unable to save the PRNG's state" exception for MTRandom.
    // Properties:
    //	Message		- an error description
    //	StackTrace	- a stack trace
    class MTRandomSaveStateException : Exception
    {

        private string msg;
            // stack trace
        private string stkTrace;

        public MTRandomSaveStateException(string MyBaseMessage, string stackTrace)
        {
            const string NL = "\r\n";

            msg = "Failed to save generator state in MTRandom.saveState:" + NL + MyBaseMessage;

            stkTrace = stackTrace;
        }

        public override string StackTrace {
            get { return stkTrace; }
        }

        public override string Message {
            get { return msg; }
        }

    }



    // The "unable to load the PRNG's state" exception for MTRandom.
    // Properties:
    //	Message		- an error description
    //	StackTrace	- a stack trace
    class MTRandomLoadStateException : Exception
    {

        private string msg;
            // stack trace
        private string stkTrace;

        public MTRandomLoadStateException(string MyBaseMessage, string stackTrace)
        {
            const string NL = "\r\n";

            msg = "Failed to load generator state in MTRandom.loadState:" + NL + MyBaseMessage;

            stkTrace = stackTrace;
        }

        public override string StackTrace {
            get { return stkTrace; }
        }

        public override string Message {
            get { return msg; }
        }

    }
}
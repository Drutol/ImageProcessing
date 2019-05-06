using System;
using System.Numerics;

namespace POID.ImageProcessingApp.Operations
{
    public class FourierTransformation
    {
        public enum Direction
        {
            Forward = 1,
            Backward = -1
        };

        public static void FFT(Complex[] data, Direction direction)
        {
            var n = data.Length;
            var m = Log2(n);

            // reorder data first
            ReorderData(data);

            // compute FFT
            int tn = 1;

            for (var k = 1; k <= m; k++)
            {
                var rotation = GetComplexRotation(k, direction);

                var tm = tn;
                tn <<= 1;

                for (var i = 0; i < tm; i++)
                {
                    var t = rotation[i];

                    for (var even = i; even < n; even += tn)
                    {
                        var odd = even + tm;
                        var ce = data[even];
                        var co = data[odd];

                        var tr = co.Real * t.Real - co.Imaginary * t.Imaginary;
                        var ti = co.Real * t.Imaginary + co.Imaginary * t.Real;

                        data[even] += new Complex(tr, ti);
                        data[odd] = new Complex(ce.Real - tr, ce.Imaginary - ti);
                    }
                }
            }

            if (direction == Direction.Forward)
            {
                for (var i = 0; i < data.Length; i++)
                    data[i] /= (double)n;
            }
        }


        public static void FFT2(Complex[,] data, Direction direction)
        {
            var k = data.GetLength(0);
            var n = data.GetLength(1);

            // check data size
            if (!((k > 0) && ((k & (k - 1)) == 0)) || !((n > 0) && ((n & (n - 1)) == 0)))
                throw new ArgumentException("The matrix rows and columns must be a power of 2.");

            if (k < minLength || k > maxLength || n < minLength || n > maxLength)
                throw new ArgumentException("Incorrect data length.");

            // process rows
            var row = new Complex[n];

            for (var i = 0; i < k; i++)
            {
                // copy row
                for (var j = 0; j < row.Length; j++)
                    row[j] = data[i, j];

                // transform it
                FFT(row, direction);

                // copy back
                for (var j = 0; j < row.Length; j++)
                    data[i, j] = row[j];
            }

            // process columns
            var col = new Complex[k];

            for (var j = 0; j < n; j++)
            {
                // copy column
                for (var i = 0; i < k; i++)
                    col[i] = data[i, j];

                // transform it
                FFT(col, direction);

                // copy back
                for (var i = 0; i < k; i++)
                    data[i, j] = col[i];
            }
        }

 

        private const int minLength = 2;
        private const int maxLength = 16384;
        private const int minBits = 1;
        private const int maxBits = 14;
        private static int[][] reversedBits = new int[maxBits][];
        private static Complex[,][] complexRotation = new Complex[maxBits, 2][];

        // Get array, indicating which data members should be swapped before FFT
        private static int[] GetReversedBits(int numberOfBits)
        {
            if ((numberOfBits < minBits) || (numberOfBits > maxBits))
                throw new ArgumentOutOfRangeException();

            // check if the array is already calculated
            if (reversedBits[numberOfBits - 1] == null)
            {
                var n = Pow2(numberOfBits);
                var rBits = new int[n];

                // calculate the array
                for (var i = 0; i < n; i++)
                {
                    var oldBits = i;
                    var newBits = 0;

                    for (var j = 0; j < numberOfBits; j++)
                    {
                        newBits = (newBits << 1) | (oldBits & 1);
                        oldBits = (oldBits >> 1);
                    }
                    rBits[i] = newBits;
                }
                reversedBits[numberOfBits - 1] = rBits;
            }
            return reversedBits[numberOfBits - 1];
        }

        // Get rotation of complex number
        private static Complex[] GetComplexRotation(int numberOfBits, Direction direction)
        {
            var directionIndex = (direction == Direction.Forward) ? 0 : 1;

            // check if the array is already calculated
            if (complexRotation[numberOfBits - 1, directionIndex] == null)
            {
                var n = 1 << (numberOfBits - 1);
                var uR = 1.0;
                var uI = 0.0;
                var angle = System.Math.PI / n * (int)direction;
                var wR = System.Math.Cos(angle);
                var wI = System.Math.Sin(angle);
                double t;
                var rotation = new Complex[n];

                for (var i = 0; i < n; i++)
                {
                    rotation[i] = new Complex(uR, uI);
                    t = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = t;
                }

                complexRotation[numberOfBits - 1, directionIndex] = rotation;
            }
            return complexRotation[numberOfBits - 1, directionIndex];
        }

        // Reorder data for FFT using
        private static void ReorderData(Complex[] data)
        {
            var len = data.Length;

            // check data length
            if ((len < minLength) || (len > maxLength) || (!((len > 0) && ((len & (len - 1)) == 0))))
                throw new ArgumentException("Incorrect data length.");

            var rBits = GetReversedBits(Log2(len));

            for (var i = 0; i < len; i++)
            {
                var s = rBits[i];

                if (s > i)
                {
                    var t = data[i];
                    data[i] = data[s];
                    data[s] = t;
                }
            }
        }
        public static int Log2(int x)
        {
            if (x <= 65536)
            {
                if (x <= 256)
                {
                    if (x <= 16)
                    {
                        if (x <= 4)
                        {
                            if (x <= 2)
                            {
                                if (x <= 1)
                                    return 0;
                                return 1;
                            }
                            return 2;
                        }
                        if (x <= 8)
                            return 3;
                        return 4;
                    }
                    if (x <= 64)
                    {
                        if (x <= 32)
                            return 5;
                        return 6;
                    }
                    if (x <= 128)
                        return 7;
                    return 8;
                }
                if (x <= 4096)
                {
                    if (x <= 1024)
                    {
                        if (x <= 512)
                            return 9;
                        return 10;
                    }
                    if (x <= 2048)
                        return 11;
                    return 12;
                }
                if (x <= 16384)
                {
                    if (x <= 8192)
                        return 13;
                    return 14;
                }
                if (x <= 32768)
                    return 15;
                return 16;
            }

            if (x <= 16777216)
            {
                if (x <= 1048576)
                {
                    if (x <= 262144)
                    {
                        if (x <= 131072)
                            return 17;
                        return 18;
                    }
                    if (x <= 524288)
                        return 19;
                    return 20;
                }
                if (x <= 4194304)
                {
                    if (x <= 2097152)
                        return 21;
                    return 22;
                }
                if (x <= 8388608)
                    return 23;
                return 24;
            }
            if (x <= 268435456)
            {
                if (x <= 67108864)
                {
                    if (x <= 33554432)
                        return 25;
                    return 26;
                }
                if (x <= 134217728)
                    return 27;
                return 28;
            }
            if (x <= 1073741824)
            {
                if (x <= 536870912)
                    return 29;
                return 30;
            }
            return 31;
        }

        public static int Pow2(int power)
        {
            return ((power >= 0) && (power <= 30)) ? (1 << power) : 0;
        }
    }
}

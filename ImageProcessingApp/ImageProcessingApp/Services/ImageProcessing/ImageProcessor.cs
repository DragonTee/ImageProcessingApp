using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessingApp.Mobile.Services.ImageProcessing
{
    public static class ImageProcessor
    {
        private static object imageLock = new object();

        struct seed
        {
            public static uint x = 1;
            public static uint y = 123;
            public static uint z = 456;
            public static uint w = 768;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RandomXorShift128()
        {
            uint t = seed.x ^ (seed.x << 11);
            seed.x = seed.y;
            seed.y = seed.z;
            seed.z = seed.w;
            seed.w = (seed.w ^ (seed.w >> 19)) ^ (t ^ (t >> 8));
            return seed.w;
        }

        public static void ApplySaltPepperNoise(SKBitmap bitmap, double probability)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint probabilityInt = (uint)Math.Max(0, Math.Floor(probability * 127));
                    for (int i = 0; i < pixelCount; i++)
                    {
                        uint rnd = RandomXorShift128() % 128;
                        if (rnd <= probabilityInt)
                        {
                            byte value = (byte)(255 * (rnd % 2));
                            *ptrOut++ = MakePixel(value, value, value, 255);
                        }
                        else
                        {
                            ptrOut++;
                        }
                    }
                }
            }
        }

        private static readonly int[] borderPixelMap =
        {
            0b1001,
            0b0001,
            0b0011,
            0b0010,
            0b0110,
            0b0100,
            0b1100,
            0b1000
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetPixels(uint pixel, out byte r, out byte g, out byte b)
        {
            r = (byte)(pixel & 255);
            g = (byte)((pixel >> 8) & 255);
            b = (byte)((pixel >> 16) & 255);
        }
        private static unsafe void LinearFilterStep(uint* ptrIn, uint* ptrOut, uint width, byte bordersInverted)
        {
            uint sumR = (*ptrIn & 255);
            uint sumG = ((*ptrIn >> 8) & 255);
            uint sumB = ((*ptrIn >> 16) & 255);
            uint count = 1;
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i])
                {
                    count++;
                    sumR += *(ptrIn + shiftH + shiftV * width) & 255;
                    sumG += ((*(ptrIn + shiftH + shiftV * width) >> 8) & 255);
                    sumB += ((*(ptrIn + shiftH + shiftV * width) >> 16) & 255);
                }
            }
            sumR /= count;
            sumG /= count;
            sumB /= count;
            *ptrOut = MakePixel((byte)sumR, (byte)sumG, (byte)sumB, 255);
        }
        public static void LinearFilter (SKBitmap bitmap, SKBitmap source)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrIn = (uint*)source.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte bordersInverted =
                            (byte)
                            ((i < width ? 0 : 1) +
                            ((((i + 1) % width) == 0 ? 0 : 1) << 1) +
                            ((i >= ((height - 1) * width) ? 0 : 1) << 2) +
                            (((i % width) == 0 ? 0 : 1) << 3));
                        LinearFilterStep(ptrIn, ptrOut, width, bordersInverted);
                        ptrIn++;
                        ptrOut++;
                    }
                }
            }
        }

        class MedianFilterPixel
        {
            public int shiftH;
            public int shiftV;
            public MedianFilterPixel(int shiftH, int shiftV)
            {
                this.shiftH = shiftH;
                this.shiftV = shiftV;
            }
        }
        private static unsafe void StatEdgesStep(uint* ptrGray, uint* ptrOut, uint width, byte bordersInverted)
        {
            var sum = 0;
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i] && i % 2 == 1)
                {
                    sum += (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                }
            }
            var current = (int)(*ptrGray & 255);
            sum += current;
            var average = sum / 9d;
            var result = 0d;
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i] && i % 2 == 1)
                {
                    var newElem = (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                    result += (newElem - average) * (newElem - average);
                }
            }
            result += (current - average) * (current - average);
            result /= 9;
            int resultInt = (int)Math.Round(Math.Sqrt(result) * current / 50);
            resultInt = Math.Min(Math.Max(resultInt, 0), 255);
            byte res = (byte)(resultInt);
            *ptrOut = MakePixel(res, res, res, 255);
        }
        public static void StatEdges(SKBitmap bitmap, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                StatEdgesStep(localPtrGray, localPtrOut, width, bordersInverted);
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrOut += step;
                        ptrGray += step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }
        private static unsafe void WallaceStep(uint* ptrGray, uint* ptrOut, uint width, byte bordersInverted)
        {
            var result = 1d;
            var f = (int)(*ptrGray & 255);
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i] && i % 2 == 1)
                {
                    result = result * f / (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                }
            }
            result = (int)Math.Round(Math.Log(result) / 4 * 400);
            result = Math.Min(Math.Max(result, 0), 255);
            byte res = (byte)(result);
            *ptrOut = MakePixel(res, res, res, 255);
        }
        public static void Wallace(SKBitmap bitmap, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                WallaceStep(localPtrGray, localPtrOut, width, bordersInverted);
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrOut += step;
                        ptrGray += step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }
        private static unsafe void SobelStep(uint* ptrGray, uint* ptrOut, uint width, byte bordersInverted)
        {
            var result = 0;
            int[] f = { 0, 0, 0, 0, 0, 0, 0, 0};
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i])
                {
                    f[i] = (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                }
            }
            var x = (f[0] + 2 * f[1] + f[2]) - (f[6] + 2 * f[5] + f[4]);
            var y = (f[0] + 2 * f[7] + f[6]) - (f[2] + 2 * f[3] + f[4]);
            result = (int)Math.Round(Math.Sqrt(x * x + y * y));
            result = Math.Min(Math.Max(result, 0), 255);
            byte res = (byte)(result);
            *ptrOut = MakePixel(res, res, res, 255);
        }
        public static void Sobel(SKBitmap bitmap, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                SobelStep(localPtrGray, localPtrOut, width, bordersInverted);
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrOut += step;
                        ptrGray += step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }
        private static unsafe void RobertsStep(uint* ptrGray, uint* ptrOut, uint width, byte bordersInverted)
        {
            var result = 0;
            int[] f = { 0, 0, 0 , 0};
            for (int i = 3; i < 6; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i])
                {
                    f[i - 3] = (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                }
            }
            f[3] = (int)(*ptrGray & 255);
            result = (int)Math.Round(Math.Sqrt((f[3] - f[1]) * (f[3] - f[1]) + (f[0] - f[2]) * (f[0] - f[2])));
            result = Math.Min(Math.Max(result, 0), 255);
            byte res = (byte)(result);
            *ptrOut = MakePixel(res, res, res, 255);
        }
        public static void Roberts(SKBitmap bitmap, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                RobertsStep(localPtrGray, localPtrOut, width, bordersInverted);
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrOut += step;
                        ptrGray += step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }
        private static readonly int[] LaplaceCoef = { -1, -2, -1, -2, -1, -2, -1, -2 };
        private static unsafe void LaplaceStep(uint* ptrGray, uint* ptrOut, uint width, byte bordersInverted)
        {
            var result = 0;
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i])
                {
                    result += (int)(*(ptrGray + shiftH + shiftV * width) & 255) * LaplaceCoef[i];
                }
            }
            result += (int)(*ptrGray & 255) * 12;
            result /= 12;
            result = Math.Max(0, result);
            byte res = (byte)(result);
            *ptrOut = MakePixel(res, res, res, 255);
        }
        public static void Laplace(SKBitmap bitmap, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                LaplaceStep(localPtrGray, localPtrOut, width, bordersInverted);
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrOut += step;
                        ptrGray += step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }
        private static unsafe void KirschStep(uint* ptrGray, uint* ptrOut, uint width, byte bordersInverted)
        {
            byte[] values = new byte[9];
            values[0] = (byte)(*ptrGray & 255);
            var result = 0;
            for (int i = 0; i < 8; i++)
            {
                var s = 0;
                for (int j = 0; j < 3; j++)
                {
                    int shiftH = (borderPixelMap[(i + j) % 8] & 2) - (borderPixelMap[(i + j) % 8] & 8);
                    int shiftV = (borderPixelMap[(i + j) % 8] & 4) - (borderPixelMap[(i + j) % 8] & 1);
                    if ((borderPixelMap[(i + j) % 8] & bordersInverted) == borderPixelMap[(i + j) % 8])
                    {
                        s += (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                    }
                }
                var t = 0;
                for (int j = 3; j < 8; j++)
                {
                    int shiftH = (borderPixelMap[(i + j) % 8] & 2) - (borderPixelMap[(i + j) % 8] & 8);
                    int shiftV = (borderPixelMap[(i + j) % 8] & 4) - (borderPixelMap[(i + j) % 8] & 1);
                    if ((borderPixelMap[(i + j) % 8] & bordersInverted) == borderPixelMap[(i + j) % 8])
                    {
                        t += (int)(*(ptrGray + shiftH + shiftV * width) & 255);
                    }
                }
                result = Math.Max(result, Math.Min(Math.Max(Math.Abs((5 * s) - (3 * t)) / 15, 0), 255));
            }
            byte res = (byte)(result);
            *ptrOut = MakePixel(res, res, res, 255);
        }
        public static void Kirsch(SKBitmap bitmap, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                KirschStep(localPtrGray, localPtrOut, width, bordersInverted);
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrOut += step;
                        ptrGray += step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }
        private static unsafe void MedianFilterStep(uint* ptrIn, uint* ptrOut, uint* ptrGray, uint width, byte bordersInverted)
        {
            uint count = 1;
            byte[] values = new byte[9];
            MedianFilterPixel[] keys = new MedianFilterPixel[9];
            values[0] = (byte)(*ptrGray & 255);
            keys[0] = new MedianFilterPixel(0, 0);
            for (int i = 0; i < 8; i++)
            {
                int shiftH = (borderPixelMap[i] & 2) - (borderPixelMap[i] & 8);
                int shiftV = (borderPixelMap[i] & 4) - (borderPixelMap[i] & 1);
                if ((borderPixelMap[i] & bordersInverted) == borderPixelMap[i])
                {
                    values[count] = (byte)(*(ptrGray + shiftH + shiftV * width) & 255);
                    keys[count] = new MedianFilterPixel(shiftH, shiftV);
                    count++;
                }
            }
            Array.Sort(values, keys, 0, (int)count);
            var pixel = keys[count / 2];
            int shiftHNew = pixel.shiftH;
            int shiftVNew = pixel.shiftV;
            byte r = (byte)(*(ptrIn + shiftHNew + shiftVNew * width) & 255);
            byte g = (byte)((*(ptrIn + shiftHNew + shiftVNew * width) >> 8) & 255);
            byte b = (byte)((*(ptrIn + shiftHNew + shiftVNew * width) >> 16) & 255);
            *ptrOut = MakePixel(r, g, b, 255);
        }
        public static void MedianFilter(SKBitmap bitmap, SKBitmap source, SKBitmap grayscale)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrIn = (uint*)source.GetPixels().ToPointer();
                    uint* ptrGray = (uint*)grayscale.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;
                    uint width = (uint)bitmap.Width;
                    uint height = (uint)bitmap.Height;
                    GetPixels(*ptrIn, out byte r, out byte g, out byte b);
                    var threads = new List<Task>();
                    var step = (int)(width * 50);
                    for (int i = 0; i < pixelCount; i += step)
                    {
                        var localPtrIn = ptrIn;
                        var localPtrOut = ptrOut;
                        var localPtrGray = ptrGray;
                        var localCount = Math.Min(pixelCount - i, step);
                        var newI = i;
                        var newTask = Task.Factory.StartNew(() =>
                        {
                            for (int j = 0; j < localCount; j++)
                            {

                                byte bordersInverted =
                                    (byte)
                                    (((newI + j) < width ? 0 : 1) +
                                    ((((newI + j + 1) % width) == 0 ? 0 : 1) << 1) +
                                    (((newI + j) >= ((height - 1) * width) ? 0 : 1) << 2) +
                                    ((((newI + j) % width) == 0 ? 0 : 1) << 3));
                                MedianFilterStep(localPtrIn, localPtrOut, localPtrGray, width, bordersInverted);
                                localPtrIn++;
                                localPtrOut++;
                                localPtrGray++;
                            }
                        });
                        threads.Add(newTask);
                        ptrIn+=step;
                        ptrOut+=step;
                        ptrGray+=step;
                    }
                    Task.WaitAll(threads.ToArray());
                }
            }
        }

        public static int[] GetRedChannelStats(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    int[] result = new int[256];
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        result[*ptrIn & 255]++;
                        ptrIn++;
                    }
                    return result;
                }
            }
        }

        public static int[] GetGreenChannelStats(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    int[] result = new int[256];
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        result[(*ptrIn >> 8) & 255]++;
                        ptrIn++;
                    }
                    return result;
                }
            }
        }

        public static int[] GetBlueChannelStats(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    int[] result = new int[256];
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        result[(*ptrIn >> 16) & 255]++;
                        ptrIn++;
                    }
                    return result;
                }
            }
        }

        public static int[] GetCombinedChannelStats(SKBitmap bitmapGrayscaled)
        {
            lock (imageLock)
            {
                unsafe
                {
                    int[] result = new int[256];
                    uint* ptrIn = (uint*)bitmapGrayscaled.GetPixels().ToPointer();
                    int pixelCount = bitmapGrayscaled.Width * bitmapGrayscaled.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        result[*ptrIn & 255]++;
                        ptrIn++;
                    }
                    return result;
                }
            }
        }

        public static int[] GetMiddleSliceStats(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    int[] result = new int[bitmap.Width * 3];
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width;
                    ptrIn += (bitmap.Height / 2) * pixelCount;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        result[i * 3] = (int)(*ptrIn & 255);
                        result[i * 3 + 1] = (int)((*ptrIn >> 8) & 255);
                        result[i * 3 + 2] = (int)((*ptrIn >> 16) & 255);
                        ptrIn++;
                    }
                    return result;
                }
            }
        }

        public static void Grayscale(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte value = (byte)((*ptrIn & 255) * 3 / 10 + ((*ptrIn >> 8) & 255) * 59 / 100 + ((*ptrIn >> 16) & 255) * 11 / 100);
                        ptrIn++;
                        *ptrOut++ = MakePixel(value, value, value, 255);
                    }
                }
            }
        }

        public static void Brighten(SKBitmap bitmap, int value)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte valueR = (byte)(*ptrIn & 255);
                        byte valueG = (byte)((*ptrIn >> 8) & 255);
                        byte valueB = (byte)((*ptrIn >> 16) & 255);
                        if (valueR + value < 0)
                            valueR = 0;
                        else
                        if (valueR + value > 255)
                            valueR = 255;
                        else
                            valueR = (byte)(valueR + value);

                        if (valueG + value < 0)
                            valueG = 0;
                        else
                       if (valueG + value > 255)
                            valueG = 255;
                        else
                            valueG = (byte)(valueG + value);

                        if (valueB + value < 0)
                            valueB = 0;
                        else
                       if (valueB + value > 255)
                            valueB = 255;
                        else
                            valueB = (byte)(valueB + value);
                        ptrIn++;
                        *ptrOut++ = MakePixel(valueR, valueG, valueB, 255);
                    }
                }
            }
        }

        public static void Contrast(SKBitmap bitmap, int multiplier, int divisor)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint[] averages = GetAverageBrightness(bitmap);
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte valueR = (byte)(*ptrIn & 255);
                        byte valueG = (byte)((*ptrIn >> 8) & 255);
                        byte valueB = (byte)((*ptrIn >> 16) & 255);
                        var newValueR = valueR * multiplier / divisor - averages[0] * multiplier / divisor + averages[0];
                        var newValueG = valueG * multiplier / divisor - averages[1] * multiplier / divisor + averages[1];
                        var newValueB = valueB * multiplier / divisor - averages[2] * multiplier / divisor + averages[2];
                        if (newValueR < 0)
                            valueR = 0;
                        else
                        if (newValueR > 255)
                            valueR = 255;
                        else
                            valueR = (byte)(newValueR);

                        if (newValueG < 0)
                            valueG = 0;
                        else
                       if (newValueG > 255)
                            valueG = 255;
                        else
                            valueG = (byte)(newValueG);

                        if (newValueB < 0)
                            valueB = 0;
                        else
                       if (newValueB > 255)
                            valueB = 255;
                        else
                            valueB = (byte)(newValueB);
                        ptrIn++;
                        *ptrOut++ = MakePixel(valueR, valueG, valueB, 255);
                    }
                }
            }
        }

        private static uint[] GetAverageBrightness(SKBitmap bitmap)
        {            
            unsafe
            {
                uint[] result = new uint[3];
                uint[] newSum = new uint[3];
                uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                int pixelCount = bitmap.Width * bitmap.Height;

                for (int i = 0; i < pixelCount; i++)
                {
                    if (i % bitmap.Width == 0)
                    {
                        result[0] += (uint)(newSum[0] / bitmap.Width);
                        result[1] += (uint)(newSum[1] / bitmap.Width);
                        result[2] += (uint)(newSum[2] / bitmap.Width);
                        newSum[0] = 0;
                        newSum[1] = 0;
                        newSum[2] = 0;
                    }
                    byte valueR = (byte)(*ptrIn & 255);
                    byte valueG = (byte)((*ptrIn >> 8) & 255);
                    byte valueB = (byte)((*ptrIn >> 16) & 255);
                    newSum[0] += valueR;
                    newSum[1] += valueG;
                    newSum[2] += valueB;                    
                    ptrIn++;
                }
                result[0] += (uint)(newSum[0] / bitmap.Width);
                result[1] += (uint)(newSum[1] / bitmap.Width);
                result[2] += (uint)(newSum[2] / bitmap.Width);
                result[0] = (uint)(result[0] / bitmap.Height);
                result[1] = (uint)(result[1] / bitmap.Height);
                result[2] = (uint)(result[2] / bitmap.Height);
                return result;
            }
        }

        public static void PasterizeBitmap(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        *ptrOut++ = *ptrIn++ & 0xE0E0E0FF;
                    }
                }
            }
        }

        public static void BinarizeBitmap(SKBitmap bitmap, SKBitmap bitmapGrayscaled, byte treshold, Color color1, Color color2)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint colorL = MakePixel(color1.R, color1.G, color1.B, 255);
                    uint colorH = MakePixel(color2.R, color2.G, color2.B, 255);
                    uint* ptrIn = (uint*)bitmapGrayscaled.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmapGrayscaled.Width * bitmapGrayscaled.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte valueR = (byte)((*ptrIn & 255) > treshold ? color2.R : color1.R);
                        byte valueG = (byte)((*ptrIn & 255) > treshold ? color2.G : color1.G);
                        byte valueB = (byte)((*ptrIn & 255) > treshold ? color2.B : color1.B);
                        ptrIn++;
                        *ptrOut++ = MakePixel(valueR, valueG, valueB, 255);
                    }
                }
            }
        }

        public static void InvertBitmap(SKBitmap bitmap)
        {
            lock (imageLock)
            {
                unsafe
                {
                    uint* ptrIn = (uint*)bitmap.GetPixels().ToPointer();
                    uint* ptrOut = (uint*)bitmap.GetPixels().ToPointer();
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte r = (byte)(255 - (*ptrIn & 255));
                        byte g = (byte)(255 - ((*ptrIn >> 8) & 255));
                        byte b = (byte)(255 - ((*ptrIn >> 16) & 255));
                        ptrIn++;
                        *ptrOut++ = MakePixel(r, g, b, 255);
                    }
                }
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
           (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
    }
}

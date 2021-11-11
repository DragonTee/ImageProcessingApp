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

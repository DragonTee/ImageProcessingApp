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
            Console.WriteLine(color1);
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

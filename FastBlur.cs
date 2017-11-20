/*
License for the original source code:
-------------------------------------

The MIT License (MIT)

Copyright (c) 2015 Michal Dymel

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Drawing;
using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BrushFilter
{
    /// <summary>
    /// Perform Gaussian blur on a Bitmap using parallelized 1-d convolutions.
    /// </summary>
    public static class FastBlur
    {
        #region Members
        /// <summary>
        /// Stores all alpha values in an image.
        /// </summary>
        private static int[] alpha;

        /// <summary>
        /// Stores all red values in an image.
        /// </summary>
        private static int[] red;

        /// <summary>
        /// Stores all green values in an image.
        /// </summary>
        private static int[] green;

        /// <summary>
        /// Stores all blue values in an image.
        /// </summary>
        private static int[] blue;

        /// <summary>
        /// Stores the width of an image.
        /// </summary>
        private static int width;

        /// <summary>
        /// Stores the height of an image.
        /// </summary>
        private static int height;

        /// <summary>
        /// Sets parallelization options to maximize use of concurrency.
        /// </summary>
        private static ParallelOptions parallelOps;
        #endregion

        #region Methods
        /// <summary>
        /// Returns a Gaussian-blurred image.
        /// </summary>
        /// <param name="image">
        /// The image to be modified.
        /// </param>
        /// <param name="radius">
        /// The radius (kernel size) to perform Gaussian blur with.
        /// </param>
        public static Bitmap Apply(Bitmap image, double radius)
        {
            CopyChannelsToArray(image);

            int[] newAlpha = new int[width * height];
            int[] newRed = new int[width * height];
            int[] newGreen = new int[width * height];
            int[] newBlue = new int[width * height];
            int[] dest = new int[width * height];

            Parallel.Invoke(
                () => GaussBlur_4(alpha, newAlpha, radius),
                () => GaussBlur_4(red, newRed, radius),
                () => GaussBlur_4(green, newGreen, radius),
                () => GaussBlur_4(blue, newBlue, radius));

            Parallel.For(0, dest.Length, parallelOps, i =>
            {
                if (newAlpha[i] > 255)
                {
                    newAlpha[i] = 255;
                }
                if (newRed[i] > 255)
                {
                    newRed[i] = 255;
                }
                if (newGreen[i] > 255)
                {
                    newGreen[i] = 255;
                }
                if (newBlue[i] > 255)
                {
                    newBlue[i] = 255;
                }

                if (newAlpha[i] < 0)
                {
                    newAlpha[i] = 0;
                }
                if (newRed[i] < 0)
                {
                    newRed[i] = 0;
                }
                if (newGreen[i] < 0)
                {
                    newGreen[i] = 0;
                }
                if (newBlue[i] < 0)
                {
                    newBlue[i] = 0;
                }

                dest[i] = (int)((uint)(newAlpha[i] << 24)
                    | (uint)(newRed[i] << 16)
                    | (uint)(newGreen[i] << 8)
                    | (uint)newBlue[i]);
            });

            Rectangle rct = new Rectangle(0, 0, image.Width, image.Height);
            Bitmap newImage = new Bitmap(rct.Width, rct.Height);
            BitmapData bits2 = newImage.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
            newImage.UnlockBits(bits2);

            return newImage;
        }

        /// <summary>
        /// Performs an optimized Gaussian blur across an image.
        /// </summary>
        /// <param name="image">
        /// The image to be modified.
        /// </param>
        /// <param name="radius">
        /// The radius (kernel size) to perform Gaussian blur with.
        /// </param>
        public static void ProcessInPlace(Bitmap image, double radius)
        {
            CopyChannelsToArray(image);

            int[] newAlpha = new int[width * height];
            int[] newRed = new int[width * height];
            int[] newGreen = new int[width * height];
            int[] newBlue = new int[width * height];
            int[] dest = new int[width * height];

            Parallel.Invoke(
                () => GaussBlur_4(alpha, newAlpha, radius),
                () => GaussBlur_4(red, newRed, radius),
                () => GaussBlur_4(green, newGreen, radius),
                () => GaussBlur_4(blue, newBlue, radius));

            Parallel.For(0, dest.Length, parallelOps, i =>
            {
                if (newAlpha[i] > 255)
                {
                    newAlpha[i] = 255;
                }
                if (newRed[i] > 255)
                {
                    newRed[i] = 255;
                }
                if (newGreen[i] > 255)
                {
                    newGreen[i] = 255;
                }
                if (newBlue[i] > 255)
                {
                    newBlue[i] = 255;
                }

                if (newAlpha[i] < 0)
                {
                    newAlpha[i] = 0;
                }
                if (newRed[i] < 0)
                {
                    newRed[i] = 0;
                }
                if (newGreen[i] < 0)
                {
                    newGreen[i] = 0;
                }
                if (newBlue[i] < 0)
                {
                    newBlue[i] = 0;
                }

                dest[i] = (int)((uint)(newAlpha[i] << 24)
                    | (uint)(newRed[i] << 16)
                    | (uint)(newGreen[i] << 8)
                    | (uint)newBlue[i]);
            });

            Rectangle rct = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bits2 = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
            image.UnlockBits(bits2);
        }

        /// <summary>
        /// Separates the image channels into separate arrays so the Gaussian
        /// kernel can be separated into one-dimensional convolutions operating
        /// on each channel individually.
        /// </summary>
        private static void CopyChannelsToArray(Bitmap image)
        {
            Rectangle rct = new Rectangle(0, 0, image.Width, image.Height);
            int[] source = new int[rct.Width * rct.Height];

            parallelOps = new ParallelOptions();
            parallelOps.MaxDegreeOfParallelism = 16;

            BitmapData bits = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(bits.Scan0, source, 0, source.Length);
            image.UnlockBits(bits);

            width = image.Width;
            height = image.Height;

            alpha = new int[width * height];
            red = new int[width * height];
            green = new int[width * height];
            blue = new int[width * height];

            Parallel.For(0, source.Length, parallelOps, i =>
            {
                alpha[i] = (int)((source[i] & 0xff000000) >> 24);
                red[i] = (source[i] & 0xff0000) >> 16;
                green[i] = (source[i] & 0x00ff00) >> 8;
                blue[i] = (source[i] & 0x0000ff);
            });
        }

        /// <summary>
        /// Performs cumulative box blurs across three image regions to
        /// approximate a Gaussian blur with high accuracy.
        /// </summary>
        private static void GaussBlur_4(int[] source, int[] dest, double r)
        {
            int[] bxs = BoxesForGauss(r, 3);
            BoxBlur_4(source, dest, width, height, (bxs[0] - 1) / 2);
            BoxBlur_4(dest, source, width, height, (bxs[1] - 1) / 2);
            BoxBlur_4(source, dest, width, height, (bxs[2] - 1) / 2);
        }

        /// <summary>
        /// Determines the areas to perform a blur across.
        /// </summary>
        private static int[] BoxesForGauss(double sigma, int n)
        {
            double wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            double wl = Math.Floor(wIdeal);

            if (wl % 2 == 0)
            {
                wl--;
            }

            double wu = wl + 2;

            double mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            double m = Math.Round(mIdeal);

            int[] sizes = new int[n];
            for (int i = 0; i < n; i++)
            {
                if (i < m)
                {
                    sizes[i] = (int)wl;
                }
                else
                {
                    sizes[i] = (int)wu;
                }
            }

            return sizes;
        }

        /// <summary>
        /// Performs both the horizontal, then vertical box blurs for the given
        /// region.
        /// </summary>
        private static void BoxBlur_4(int[] source, int[] dest, int w, int h, int r)
        {
            for (int i = 0; i < source.Length; i++)
            {
                dest[i] = source[i];
            }
            BoxBlurH_4(dest, source, w, h, r);
            BoxBlurT_4(source, dest, w, h, r);
        }

        /// <summary>
        /// Performs a horizontal box blur for the given region.
        /// </summary>
        private static void BoxBlurH_4(int[] source, int[] dest, int w, int h, int r)
        {
            double iar = 1d / (r + r + 1);
            Parallel.For(0, h, parallelOps, i =>
            {
                int ti = i * w;
                int li = ti;
                int ri = ti + r;
                int fv = source[ti];
                int lv = source[ti + w - 1];
                int val = (r + 1) * fv;
                for (int j = 0; j < r; j++)
                {
                    val += source[ti + j];
                }
                for (int j = 0; j <= r; j++)
                {
                    val += source[ri++] - fv;
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (int j = r + 1; j < w - r; j++)
                {
                    val += source[ri++] - dest[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (int j = w - r; j < w; j++)
                {
                    val += lv - source[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
            });
        }

        /// <summary>
        /// Performs a vertical box blur for the given region.
        /// </summary>
        private static void BoxBlurT_4(int[] source, int[] dest, int w, int h, int r)
        {
            double iar = 1d / (r + r + 1);
            Parallel.For(0, w, parallelOps, i =>
            {
                int ti = i;
                int li = ti;
                int ri = ti + r * w;
                int fv = source[ti];
                int lv = source[ti + w * (h - 1)];
                int val = (r + 1) * fv;
                for (int j = 0; j < r; j++)
                {
                    val += source[ti + j * w];
                }
                for (int j = 0; j <= r; j++)
                {
                    val += source[ri] - fv;
                    dest[ti] = (int)Math.Round(val * iar);
                    ri += w;
                    ti += w;
                }
                for (int j = r + 1; j < h - r; j++)
                {
                    val += source[ri] - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ri += w;
                    ti += w;
                }
                for (int j = h - r; j < h; j++)
                {
                    val += lv - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ti += w;
                }
            });
        }
        #endregion
    }
}
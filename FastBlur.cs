using System.Drawing;
using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace BrushFilter
{
    /// <summary>
    /// Quickly blur a bitmap with 1d kernels in linear time.
    /// Based on Leon's open-sourced work at lionroar7.com.
    /// </summary>
    public static class FastBlur
    {
        /// <summary>
        /// Performs a box blur using 1-d kernels on a 32bppArgb-formatted image.
        /// </summary>
        private unsafe static Bitmap FastBoxBlur(Bitmap img, int radius)
        {
            int kSize = radius;

            //Pads the kernel size to a multiple of 2.
            if (kSize % 2 == 0)
            {
                kSize++;
            }

            using (Bitmap imgHBlur = new Bitmap(img))
            {
                float avg = (float)1 / kSize;

                BitmapData imgHBlurData = imgHBlur.LockBits(
                    new Rectangle(0, 0,
                        imgHBlur.Width,
                        imgHBlur.Height),
                    ImageLockMode.ReadWrite,
                    imgHBlur.PixelFormat);

                //Navigates to top-left pixel.
                byte* srcRowH = (byte*)imgHBlurData.Scan0;

                //Avoids concurrent object access.
                int imgHBlurHeight = imgHBlur.Height;
                int imgHBlurWidth = imgHBlur.Width;
                int imgHBlurStride = imgHBlurData.Stride;

                //Iterates through each vertical pixel first.
                Parallel.For(0, imgHBlurHeight, (y) =>
                {
                    float[] hSum = new float[] { 0f, 0f, 0f, 0f };
                    float[] iAvg = new float[] { 0f, 0f, 0f, 0f };
                    int ptr = 0;

                    //Computes the sum.
                    for (int x = 0; x < kSize; x++)
                    {
                        ptr = y * imgHBlurStride + x * 4;

                        hSum[0] += srcRowH[ptr];
                        hSum[1] += srcRowH[ptr + 1];
                        hSum[2] += srcRowH[ptr + 2];
                        hSum[3] += srcRowH[ptr + 3];
                    }

                    //Computes averages.
                    iAvg[0] = hSum[0] * avg;
                    iAvg[1] = hSum[1] * avg;
                    iAvg[2] = hSum[2] * avg;
                    iAvg[3] = hSum[3] * avg;

                    //Blurs horizontally.
                    for (int x = 0; x < imgHBlurWidth; x++)
                    {
                        if (x - kSize / 2 >= 0 &&
                            x + 1 + kSize / 2 < imgHBlurWidth)
                        {
                            ptr = y * imgHBlurStride + (x - kSize / 2) * 4;
                            hSum[0] -= srcRowH[ptr];
                            hSum[1] -= srcRowH[ptr + 1];
                            hSum[2] -= srcRowH[ptr + 2];
                            hSum[3] -= srcRowH[ptr + 3];

                            ptr = y * imgHBlurStride + (x + 1 + kSize / 2) * 4;
                            hSum[0] += srcRowH[ptr];
                            hSum[1] += srcRowH[ptr + 1];
                            hSum[2] += srcRowH[ptr + 2];
                            hSum[3] += srcRowH[ptr + 3];

                            iAvg[0] = hSum[0] * avg;
                            iAvg[1] = hSum[1] * avg;
                            iAvg[2] = hSum[2] * avg;
                            iAvg[3] = hSum[3] * avg;
                        }

                        ptr = y * imgHBlurStride + x * 4;
                        srcRowH[ptr] = (byte)iAvg[0];
                        srcRowH[ptr + 1] = (byte)iAvg[1];
                        srcRowH[ptr + 2] = (byte)iAvg[2];
                        srcRowH[ptr + 3] = (byte)iAvg[3];
                    }
                });

                //Performs a vertical blur after the horizontal blur.
                imgHBlur.UnlockBits(imgHBlurData);
                Bitmap imgHVBlur = new Bitmap(imgHBlur);

                BitmapData imgHVBlurData = imgHVBlur.LockBits(
                    new Rectangle(0, 0,
                        imgHVBlur.Width,
                        imgHVBlur.Height),
                    ImageLockMode.ReadWrite,
                    imgHVBlur.PixelFormat);

                //Navigates to top-left pixel.
                byte* srcRowHV = (byte*)imgHVBlurData.Scan0;

                //Avoids concurrent object access.
                int imgHVBlurHeight = imgHVBlur.Height;
                int imgHVBlurWidth = imgHVBlur.Width;
                int imgHVBlurStride = imgHVBlurData.Stride;

                //Iterates through each horizontal pixel first.
                Parallel.For(0, imgHVBlurWidth, (x) =>
                {
                    float[] tSum = new float[] { 0f, 0f, 0f, 0f };
                    float[] iAvg = new float[] { 0f, 0f, 0f, 0f };
                    int ptr = 0;

                    //Computes the sum.
                    for (int y = 0; y < kSize; y++)
                    {
                        ptr = y * imgHVBlurStride + x * 4;

                        tSum[0] += srcRowHV[ptr];
                        tSum[1] += srcRowHV[ptr + 1];
                        tSum[2] += srcRowHV[ptr + 2];
                        tSum[3] += srcRowHV[ptr + 3];
                    }

                    //Computes averages.
                    iAvg[0] = tSum[0] * avg;
                    iAvg[1] = tSum[1] * avg;
                    iAvg[2] = tSum[2] * avg;
                    iAvg[3] = tSum[3] * avg;

                    //Blurs vertically.
                    for (int y = 0; y < imgHVBlurHeight; y++)
                    {
                        if (y - kSize / 2 >= 0 &&
                            y + 1 + kSize / 2 < imgHVBlurHeight)
                        {
                            ptr = (y - kSize / 2) * imgHVBlurStride + x * 4;
                            tSum[0] -= srcRowHV[ptr];
                            tSum[1] -= srcRowHV[ptr + 1];
                            tSum[2] -= srcRowHV[ptr + 2];
                            tSum[3] -= srcRowHV[ptr + 3];

                            ptr = (y + 1 + kSize / 2) * imgHVBlurStride + x * 4;
                            tSum[0] += srcRowHV[ptr];
                            tSum[1] += srcRowHV[ptr + 1];
                            tSum[2] += srcRowHV[ptr + 2];
                            tSum[3] += srcRowHV[ptr + 3];

                            iAvg[0] = tSum[0] * avg;
                            iAvg[1] = tSum[1] * avg;
                            iAvg[2] = tSum[2] * avg;
                            iAvg[3] = tSum[3] * avg;
                        }

                        ptr = y * imgHVBlurStride + x * 4;
                        srcRowHV[ptr] = (byte)iAvg[0];
                        srcRowHV[ptr + 1] = (byte)iAvg[1];
                        srcRowHV[ptr + 2] = (byte)iAvg[2];
                        srcRowHV[ptr + 3] = (byte)iAvg[3];
                    }
                });

                //Unlocks bits and returns.
                imgHVBlur.UnlockBits(imgHVBlurData);

                return imgHVBlur;
            }
        }

        /// <summary>
        /// Computes the number of Gaussian boxes.
        /// </summary>
        private static int[] BoxesForGaussian(double sigma, int n)
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
        /// Approximates a Gaussian blur with speed 6*O(n).
        /// </summary>
        public static Bitmap FastGaussianBlur(Bitmap src, int radius)
        {
            double sigma = radius / 4d; //Counteracts the cumulative blur effect so all radii can be represented.
            var boxes = BoxesForGaussian(sigma, 3);
            using (Bitmap img = FastBoxBlur(src, boxes[0]))
            {
                using (Bitmap img2 = FastBoxBlur(img, boxes[1]))
                {
                    return FastBoxBlur(img2, boxes[2]);
                }
            }
        }
    }
}
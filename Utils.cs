using PaintDotNet;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrushFilter
{
    /// <summary>
    /// Provides common functionality shared across multiple classes.
    /// </summary>
    static class Utils
    {
        #region Methods
        /// <summary>
        /// If the given value is out of range, it's clamped to the nearest
        /// bound (low or high). Example: 104 in range 0 - 100 becomes 100.
        /// </summary>
        public static int Clamp(int value, int low, int high)
        {
            if (value < low)
            {
                value = low;
            }
            else if (value > high)
            {
                value = high;
            }

            return value;
        }

        /// <summary>
        /// If the given value is out of range, it's clamped to the nearest
        /// bound (low or high). Example: -0.1 in range 0 - 1 becomes 0.
        /// </summary>
        public static float ClampF(float value, float low, float high)
        {
            if (value < low)
            {
                value = low;
            }
            else if (value > high)
            {
                value = high;
            }

            return value;
        }

        /// <summary>
        /// Strictly copies all data from one bitmap over the other. They
        /// must have the same size and pixel format. The image can be made
        /// fully transparent without reducing color, which is used for an
        /// "uncovering" effect. Returns success.
        /// </summary>
        /// <param name="srcImg">
        /// The image to copy from.
        /// </param>
        /// <param name="dstImg">
        /// The image to be overwritten.
        /// </param>
        public static unsafe bool CopyBitmapPure(Bitmap srcImg, Bitmap dstImg)
        {
            //Formats and size must be the same.
            if (srcImg.PixelFormat != PixelFormat.Format32bppArgb ||
            dstImg.PixelFormat != PixelFormat.Format32bppArgb ||
            srcImg.Width != dstImg.Width ||
            srcImg.Height != dstImg.Height)
            {
                return false;
            }

            BitmapData srcData = srcImg.LockBits(
                new Rectangle(0, 0,
                    srcImg.Width,
                    srcImg.Height),
                ImageLockMode.ReadOnly,
                srcImg.PixelFormat);

            BitmapData destData = dstImg.LockBits(
                new Rectangle(0, 0,
                    dstImg.Width,
                    dstImg.Height),
                ImageLockMode.WriteOnly,
                dstImg.PixelFormat);

            //Copies each pixel.
            byte* srcRow = (byte*)srcData.Scan0;
            byte* dstRow = (byte*)destData.Scan0;

            int srcImgHeight = srcImg.Height;
            int srcImgWidth = srcImg.Width;
            Parallel.For(0, srcImgHeight, (y) =>
            {
                for (int x = 0; x < srcImgWidth; x++)
                {
                    int ptr = y * srcData.Stride + x * 4;

                    dstRow[ptr] = srcRow[ptr];
                    dstRow[ptr + 1] = srcRow[ptr + 1];
                    dstRow[ptr + 2] = srcRow[ptr + 2];
                    dstRow[ptr + 3] = srcRow[ptr + 3];
                }
            });

            srcImg.UnlockBits(srcData);
            dstImg.UnlockBits(destData);

            return true;
        }

        /// <summary>
        /// Returns the original bitmap data in another format by drawing it.
        /// All transparency is removed and made white.
        /// </summary>
        public static Bitmap FormatImage(Bitmap img, PixelFormat format)
        {
            Bitmap clone = new Bitmap(img.Width, img.Height, format);
            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.SmoothingMode = SmoothingMode.None;
                gr.DrawImage(img, 0, 0, img.Width, img.Height);
            }

            return clone;
        }

        /// <summary>
        /// Constructs an outline of the given region with the given bounds
        /// and scaling factor.
        /// </summary>
        /// <param name="region">
        /// The selection to approximate.
        /// </param>
        /// <param name="bounds">
        /// The boundaries of the image.
        /// </param>
        /// <param name="scalingMultiplier">
        /// The amount to scale the size of the outline by.
        /// </param>
        public static PdnRegion ConstructOutline(
            this PdnRegion region,
            RectangleF bounds,
            float scalingMultiplier)
        {
            GraphicsPath path = new GraphicsPath();
            PdnRegion newRegion = region.Clone();

            //The size to scale the region by.
            Matrix scalematrix = new Matrix(
                bounds,
                new PointF[]{
                    new PointF(bounds.Left, bounds.Top),
                    new PointF(bounds.Right * scalingMultiplier, bounds.Top),
                    new PointF(bounds.Left, bounds.Bottom * scalingMultiplier)
                });

            newRegion.Transform(scalematrix);

            //Makes the new region slightly larger by inflating rectangles.
            foreach (RectangleF rect in newRegion.GetRegionScans())
            {
                path.AddRectangle(RectangleF.Inflate(rect, 1, 1));
            }

            //Subtracts the old region, leaving an outline from the expansion.
            PdnRegion result = new PdnRegion(path);
            result.Exclude(newRegion);

            return result;
        }

        /// <summary>
        /// Pads the given bitmap to be square.
        /// </summary>
        /// <param name="img">
        /// The image to pad. The original is untouched.
        /// </param>
        public static Bitmap MakeBitmapSquare(Bitmap img)
        {
            //Exits if it's already square.
            if (img.Width == img.Height)
            {
                return new Bitmap(img);
            }

            //Creates a new bitmap with the minimum square size.
            int size = Math.Max(img.Height, img.Width);
            Bitmap newImg = new Bitmap(size, size);

            using (Graphics graphics = Graphics.FromImage(newImg))
            {
                graphics.FillRectangle(Brushes.White,
                    new Rectangle(0, 0, newImg.Width, newImg.Height));

                graphics.DrawImage(img,
                    (size - img.Width) / 2,
                    (size - img.Height) / 2,
                    img.Width, img.Height);
            }

            return newImg;
        }

        /// <summary>
        /// Returns a copy of the image, rotated about its center.
        /// </summary>
        /// <param name="origBmp">
        /// The image to clone and change.
        /// </param>
        /// <param name="angle">
        /// The angle in degrees; positive or negative.
        /// </param>
        public static Bitmap RotateImage(Bitmap origBmp, float angle)
        {
            //Performs nothing if there is no need.
            if (angle == 0)
            {
                return origBmp;
            }

            //Places the angle in the range 0 <= x < 360.
            while (angle < 0)
            {
                angle += 360;
            }
            while (angle >= 360)
            {
                angle -= 360;
            }

            //Calculates the new bounds of the image with trigonometry.
            double radAngle = angle * Math.PI / 180;
            double cos = Math.Abs(Math.Cos(radAngle));
            double sin = Math.Abs(Math.Sin(radAngle));
            int newWidth = (int)Math.Ceiling(origBmp.Width * cos + origBmp.Height * sin);
            int newHeight = (int)Math.Ceiling(origBmp.Width * sin + origBmp.Height * cos);

            //Creates the new image and a graphic canvas to draw the rotation.
            Bitmap newBmp = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(newBmp))
            {
                //Fills the image with white.
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, newBmp.Width, newBmp.Height));

                //Uses matrices to centrally-rotate the original image.
                g.TranslateTransform(
                    (float)(newWidth - origBmp.Width) / 2,
                    (float)(newHeight - origBmp.Height) / 2);

                g.TranslateTransform(
                    (float)origBmp.Width / 2,
                    (float)origBmp.Height / 2);

                g.RotateTransform(angle);

                //Undoes the transform.
                g.TranslateTransform(-(float)origBmp.Width / 2, -(float)origBmp.Height / 2);

                //Draws the image.
                g.DrawImage(origBmp, 0, 0, origBmp.Width, origBmp.Height);
            }

            return newBmp;
        }

        /// <summary>
        /// Creates and returns a Winforms control for the given property
        /// based on its type. When the control is modified, the property
        /// is automatically updated.
        /// </summary>
        public static Control CreateControl(this Property prop, int propIndex, ControlInfo dlg)
        {
            if (prop is BooleanProperty)
            {
                var control = new CheckBox();
                control.Checked = (bool)prop.Value;
                control.CheckedChanged += (a, b) => { prop.Value = control.Checked; };
                return control;
            }
            else if (prop is DoubleProperty propKnown)
            {
                var control = new TrackBar();
                control.TickStyle = TickStyle.None;
                control.Minimum = (int)(propKnown.MinValue * 100);
                control.Maximum = (int)(propKnown.MaxValue * 100);
                control.Value = (int)(propKnown.Value * 100);
                control.ValueChanged += (a, b) => { prop.Value = control.Value / 100d; };
                return control;
            }
            else if (prop is Int32Property propKnown2)
            {
                var control = new TrackBar();
                control.TickStyle = TickStyle.None;
                control.Minimum = propKnown2.MinValue;
                control.Maximum = propKnown2.MaxValue;
                control.Value = propKnown2.Value;
                control.ValueChanged += (a, b) => { prop.Value = control.Value; };
                return control;
            }
            else if (prop is StaticListChoiceProperty propKnown3)
            {
                var control = new ComboBox();

                //Adds each option to the combobox as a tuple.
                for (int i = 0; i < propKnown3.ValueChoices.Length; i++)
                {
                    string name = propKnown3.Value.ToString();
                    var value = propKnown3.ValueChoices[i].ToString();
                    control.Items.Add(new Tuple<string, object>(name, value));

                    //Sets the default item in the combobox.
                    if (name == value)
                    {
                        control.SelectedIndex = i;
                    }
                }

                control.ValueMember = "Item1";
                control.DisplayMember = "Item2";

                control.SelectedValueChanged += (a, b) =>
                {
                    prop.Value = propKnown3.ValueChoices[control.SelectedIndex];
                };

                return control;
            }

            return null;
        }

        /// <summary>
        /// Creates and returns a Winforms label for the given property
        /// based on its type. The label's Tag attribute contains the
        /// name of the property.
        /// </summary>
        public static Label CreateLabel(this Property prop, int propIndex, ControlInfo dlg)
        {
            var control = new Label();
            control.Tag = prop.Name;

            //Probes for the display name or description of the property to use.
            if (dlg.ChildControls.Count > propIndex)
            {
                var properties = dlg?.ChildControls[propIndex]?.ControlProperties?.Properties;

                if (properties != null)
                {
                    for (int i = 0; i < properties.Count(); i++)
                    {
                        var candidate = properties.ElementAt(i);

                        if (candidate.Name ==
                            ControlInfoPropertyNames.DisplayName.ToString() &&
                            !String.IsNullOrEmpty(candidate.Value.ToString()))
                        {
                            control.Tag = candidate.Value.ToString();
                        }
                        else if (candidate.Name ==
                            ControlInfoPropertyNames.Description.ToString() &&
                            !String.IsNullOrEmpty(candidate.Value.ToString()))
                        {
                            control.Tag = candidate.Value.ToString();
                        }
                    }
                }
            }

            //The label always displays the value of the property.
            control.Text = (string)control.Tag + ": " + prop.Value.ToString();
            prop.ValueChanged += (a, b) =>
            {
                control.Text = (string)control.Tag + ": " + prop.Value.ToString();
            };

            return control;
        }
        #endregion
    }
}
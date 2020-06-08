using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PngToIco.Drawing
{
    public static class ImageEx
    {
        /// <summary>
        ///     Determines whether the specified pixel size indicator is within the allowed
        ///     range, depending on the specified pixel format.
        /// </summary>
        /// <param name="pixelIndicator">
        ///     The pixel size indicator to check.
        ///     <para>
        ///         The pixel size indicator represents the maximum value between the width
        ///         and height of an image.
        ///     </para>
        /// </param>
        /// <param name="pixelFormat">
        ///     The pixel format.
        /// </param>
        public static bool SizeIsValid(int pixelIndicator, PixelFormat pixelFormat)
        {
#if x86
            const double memoryLimit = 0x40000000;
#elif x64
            const double memoryLimit = 0x80000000;
#else
            var memoryLimit = Environment.Is64BitProcess ? 0x80000000d : 0x40000000;
#endif
            double bit;
            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    bit = 1d;
                    break;
                case PixelFormat.Format4bppIndexed:
                    bit = 4d;
                    break;
                case PixelFormat.Format8bppIndexed:
                    bit = 8d;
                    break;
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                    bit = 16d;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    bit = 32d;
                    break;
                case PixelFormat.Format48bppRgb:
                    bit = 48d;
                    break;
                default:
                    bit = 64d;
                    break;
            }
            var absolutRange = (int)Math.Ceiling(Math.Sqrt(memoryLimit / (bit * .125d)));
            return pixelIndicator >= 1 && pixelIndicator <= absolutRange;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Image"/> size is within the
        ///     allowed range, depending on the specified pixel format.
        /// </summary>
        /// <param name="width">
        ///     The image width to check.
        /// </param>
        /// <param name="height">
        ///     The image height to check.
        /// </param>
        /// <param name="pixelFormat">
        ///     The pixel format.
        /// </param>
        public static bool SizeIsValid(int width, int height, PixelFormat pixelFormat)
        {
            var indicator = Math.Max(width, height);
            return SizeIsValid(indicator, pixelFormat);
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Image"/> size is within the
        ///     allowed range, depending on the specified pixel format.
        /// </summary>
        /// <param name="size">
        ///     The image size to check.
        /// </param>
        /// <param name="pixelFormat">
        ///     The pixel format.
        /// </param>
        public static bool SizeIsValid(Size size, PixelFormat pixelFormat)
        {
            var indicator = Math.Max(size.Width, size.Height);
            return SizeIsValid(indicator, pixelFormat);
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Image"/> size is within the
        ///     allowed range.
        /// </summary>
        /// <param name="image">
        ///     The image to check.
        /// </param>
        public static bool SizeIsValid(Image image)
        {
            if (!(image is Image img))
                return false;
            var indicator = Math.Max(img.Width, img.Height);
            return SizeIsValid(indicator, img.PixelFormat);
        }

        /// <summary>
        ///     Redraws the specified <see cref="Image"/> with the specified size and with
        ///     the specified rendering quality.
        /// </summary>
        /// <param name="image">
        ///     The image to draw.
        /// </param>
        /// <param name="width">
        ///     The new width of the image.
        /// </param>
        /// <param name="heigth">
        ///     The new height of the image.
        /// </param>
        /// <param name="quality">
        ///     The rendering quality for the image.
        /// </param>
        public static Image Redraw(this Image image, int width, int heigth, SmoothingMode quality = SmoothingMode.HighQuality)
        {
            if (!(image is Image img))
                return default;
            try
            {
                if (!SizeIsValid(width, heigth, PixelFormat.Format32bppArgb))
                    throw new Exception($"{nameof(width)}+{nameof(heigth)}");
                var bmp = new Bitmap(width, heigth);
                bmp.SetResolution(img.HorizontalResolution, img.VerticalResolution);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    switch (quality)
                    {
                        case SmoothingMode.AntiAlias:
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            break;
                        case SmoothingMode.HighQuality:
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            break;
                        case SmoothingMode.HighSpeed:
                            g.CompositingQuality = CompositingQuality.HighSpeed;
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                            g.SmoothingMode = SmoothingMode.HighSpeed;
                            break;
                    }
                    using (var ia = new ImageAttributes())
                    {
                        ia.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(img, new Rectangle(0, 0, width, heigth), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
                    }
                }
                return bmp;
            }
            catch
            {
                // Log.Write(ex);
                return img;
            }
        }

        /// <summary>
        ///     Redraws the specified <see cref="Image"/> with the specified maximum size
        ///     indicator and with the specified rendering quality.
        /// </summary>
        /// <param name="image">
        ///     The image to draw.
        /// </param>
        /// <param name="quality">
        ///     The rendering quality for the image.
        /// </param>
        /// <param name="indicator">
        ///     Specifies the maximal size indicator, which determines when the image gets
        ///     a new size.
        /// </param>
        public static Image Redraw(this Image image, SmoothingMode quality = SmoothingMode.HighQuality, int indicator = 1024)
        {
            if (!(image is Image img))
                return default;
            int[] size =
            {
                img.Width,
                img.Height
            };
            if (indicator <= 0 || indicator >= size.First() && indicator >= size.Last())
                goto Return;
            for (var i = 0; i < size.Length; i++)
            {
                if (size[i] <= indicator)
                    continue;
                var percent = (int)Math.Floor(100d / size[i] * indicator);
                size[i] = (int)(size[i] * (percent / 100d));
                size[i == 0 ? 1 : 0] = (int)(size[i == 0 ? 1 : 0] * (percent / 100d));
                break;
            }
        Return:
            return img.Redraw(size.First(), size.Last(), quality);
        }

        /// <summary>
        ///     Redraws the specified <see cref="Image"/> with the specified maximum size
        ///     indicator and with the highest available rendering quality.
        /// </summary>
        /// <param name="image">
        ///     The image to draw.
        /// </param>
        /// <param name="indicator">
        ///     Specifies the maximal size indicator, which determines when the image gets
        ///     a new size.
        /// </param>
        public static Image Redraw(this Image image, int indicator) =>
            image.Redraw(SmoothingMode.HighQuality, indicator);

    }
}

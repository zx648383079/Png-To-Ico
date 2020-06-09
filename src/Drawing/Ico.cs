using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PngToIco.Drawing
{
    public static class Ico
    {
        const int bitmapSize = 40;
        const int colorMode = 0;
        const int directorySize = 16;
        const int headerSize = 6;

        public static Bitmap ResizeImage(int width, int height, Image source)
        {
            return ResizeImage(width, height, source, SmoothingMode.HighQuality);
        }
        /// <summary>
        /// 修改图片尺寸
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Bitmap ResizeImage(int width, int height, Image source, SmoothingMode quality)
        {
            var bmp = new Bitmap(width, height);
            bmp.SetResolution(source.HorizontalResolution, source.VerticalResolution);
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
                    g.DrawImage(source, new System.Drawing.Rectangle(0, 0, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return bmp;
        }

        public static bool Converter(IEnumerable<Image> images, Stream stream)
        {
            var bw = new BinaryWriter(stream);
            CreateHeader(images.Count(), bw);
            var offset = headerSize + (directorySize * images.Count());

            var cahce = new Dictionary<int, byte[]>();
            var i = 0;
            foreach (var item in images) {
                var length = GetImageSize(item);
                //if (item.Width >= 256)
                //{
                //    var png = ImageCorrection(item);
                //    var bytes = CreateBuffer(png);
                //    cahce[i] = bytes;
                //    length = bytes.Length;
                //    if (png != item)
                //    {
                //        png.Dispose();
                //    }
                //}
                CreateDirectory(offset, item, length, bw);
                offset += length + bitmapSize;
                i++;
            }
            i = 0;
            foreach (var item in images) {
                if (cahce.ContainsKey(i))
                {
                    CreateBitmap(item, colorMode, cahce[i].Length, bw);
                    foreach (var byt in cahce[i])
                    {
                        bw.Write(byt);
                    }
                } else
                {
                    CreateBitmap(item, colorMode, GetImageSize(item), bw);
                    CreateDib(item, bw);
                }
                
                i++;
            }
            bw.Dispose();
            return true;
        }

        /// <summary>
        /// 根据一张图，生成不同尺寸
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sizes"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool Converter(Image source, int[] sizes, Stream stream)
        {
            return Converter(source, sizes, SmoothingMode.HighQuality, stream);
        }

        public static bool Converter(Image source, int[] sizes, SmoothingMode quality, Stream stream)
        {
            var items = new List<Bitmap>();
            foreach (var item in sizes)
            {
                items.Add(ResizeImage(item, item, source, quality));
            }
            return Converter(items, stream);
        }

        /// <summary>
        /// 传入多张图片，自动根据较大尺寸选择
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sizes"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool Converter(IEnumerable<Image> data, int[] sizes, Stream stream)
        {
            return Converter(data, sizes, SmoothingMode.HighQuality, stream);
        }

        public static bool Converter(IEnumerable<Image> data, int[] sizes, SmoothingMode quality, Stream stream)
        {
            data = data.OrderBy(x => x.Width).ThenBy(x => x.Height);
            var items = new List<Bitmap>();
            foreach (var item in sizes)
            {
                var image = GetThanImage(item, data);
                items.Add(ResizeImage(item, item, image, quality));
            }
            return Converter(items, stream);
        }

        private static Image GetThanImage(int width, IEnumerable<Image> data)
        {
            foreach (var item in data)
            {
                if (item.Width >= width)
                {
                    return item;
                }
            }
            return data.Last();
        }

        /// <summary>
        /// 写头
        /// </summary>
        /// <param name="count"></param>
        /// <param name="writer"></param>
        private static void CreateHeader(int count, BinaryWriter writer)
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)count); // 
        }


        private static int GetImageSize(Image image)
        {
            return image.Height * image.Width * 4;// * Image.GetPixelFormatSize(image.PixelFormat) / 1024 / 1024;
        }

        private static void CreateDirectory(int offset, Image image, int imageLength, BinaryWriter writer)
        {
            var size = imageLength + bitmapSize;
            var width = image.Width >= 256 ? 0 : image.Width;
            var height = image.Height >= 256 ? 0 : image.Height;
            var bpp = Image.GetPixelFormatSize(image.PixelFormat);

            writer.Write((byte)width);
            writer.Write((byte)height);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)bpp);
            writer.Write((uint)size);
            writer.Write((uint)offset);
        }

        private static void CreateBitmap(Image image, int compression, int imageLength, BinaryWriter writer)
        {
            writer.Write((uint)bitmapSize);
            writer.Write((uint)image.Width);
            writer.Write((uint)image.Height * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)Image.GetPixelFormatSize(image.PixelFormat));
            writer.Write((uint)compression);
            writer.Write((uint)imageLength);
            writer.Write(0);
            writer.Write(0);
            writer.Write((uint)0);
            writer.Write((uint)0);
        }

        private static void CreateDib(Image image, BinaryWriter writer)
        {
            for (int i = 0; i < image.Height; i++)
            {
                for (int j = 0; j < image.Width; j++)
                {
                    var color = (image as Bitmap).GetPixel(j, i);
                    writer.Write(color.B);
                    writer.Write(color.G);
                    writer.Write(color.R);
                    writer.Write(color.A);
                }
            }
        }

        private static byte[] CreateBuffer(Image image) {
            byte[] ba;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                ba = ms.ToArray();
            }
            return ba;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Image ImageCorrection(Image image)
        {
            var dispose = false;
            try
            {
                var img = image;
                if (!img.PixelFormat.Equals(PixelFormat.Format32bppArgb))
                {
                    var bitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(bitmap))
                        g.DrawImage(img, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    img = bitmap;
                }
                if (!img.RawFormat.Guid.Equals(ImageFormat.Png.Guid))
                {
                    using (var ms = new MemoryStream())
                    {
                        img.Save(ms, ImageFormat.Png);
                        ms.Position = 0;
                        img = Image.FromStream(ms);
                    }
                }
                if (image != img)
                {
                    dispose = true;
                }
                return img;
            }
            finally
            {
                //if (dispose)
                //    image?.Dispose();
            }
        }

    }
}

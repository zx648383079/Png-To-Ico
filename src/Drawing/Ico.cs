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
        /// <summary>
        /// 修改图片尺寸
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Bitmap ResizeImage(int width, int height, Image source)
        {
            var bmp = new Bitmap(width, height);
            bmp.SetResolution(source.HorizontalResolution, source.VerticalResolution);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
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

            foreach (var item in images) {
                CreateDirectory(offset, item, bw);
                offset += GetImageSize(item) + bitmapSize;
            }

            foreach (var item in images) {
                CreateBitmap(item, colorMode, bw);
                CreateDib(item, bw);
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
        public static bool Coverter(Image source, int[] sizes, Stream stream)
        {
            var items = new List<Bitmap>();
            foreach (var item in sizes)
            {
                items.Add(ResizeImage(item, item, source));
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
            data = data.OrderBy(x => x.Width).ThenBy(x => x.Height);
            var items = new List<Bitmap>();
            foreach (var item in sizes)
            {
                var image = GetThanImage(item, data);
                items.Add(ResizeImage(item, item, image));
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

        private static void CreateDirectory(int offset, Image image, BinaryWriter writer)
        {
            var size = GetImageSize(image) + bitmapSize;
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

        private static void CreateBitmap(Image image, int compression, BinaryWriter writer)
        {
            writer.Write((uint)bitmapSize);
            writer.Write((uint)image.Width);
            writer.Write((uint)image.Height * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)Image.GetPixelFormatSize(image.PixelFormat));
            writer.Write((uint)compression);
            writer.Write((uint)GetImageSize(image));
            writer.Write(0);
            writer.Write(0);
            writer.Write((uint)0);
            writer.Write((uint)0);
        }

        private static void CreateDib(Image image, BinaryWriter writer)
        {
            //var bpp = Image.GetPixelFormatSize(image.PixelFormat);
            //var cols = image.Width * bpp;
            //var rows = image.Height * cols;
            //var end = rows - cols;

            for (int i = 0; i < image.Height; i++)
            {
                for (int j = 0; j < image.Width; j++)
                {
                    var color = (image as Bitmap).GetPixel(j, i);
                    // var newColor = color.B | (color.G << 8) | (color.R << 16) | (color.A << 24);
                    writer.Write(color.B);
                    writer.Write(color.G);
                    writer.Write(color.R);
                    writer.Write(color.A);
                }
            }
        }
    }
}

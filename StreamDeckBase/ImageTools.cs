using IronSoftware.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using SKBitmap = SixLabors.ImageSharp.Image;
using SKColor = SixLabors.ImageSharp.Color;
using SKRectangle = SixLabors.ImageSharp.Rectangle;
using SKPoint = SixLabors.ImageSharp.Point;
using SKFont = SixLabors.Fonts.Font;
using SixLabors.Fonts;

namespace StreamDeckBase
{
    public static class ImageTools
    {
        public static AnyBitmap DownloadFromUrl(String url)
        {
            WebClient wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-gb");
            wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36");

            if (url.Contains("?"))
                url = url.Substring(0, url.IndexOf("?"));

            AnyBitmap bmp = null;
            try
            {
                bmp = new AnyBitmap(new Uri(url));//

            }
            catch (Exception ex)
            {
                bmp = new AnyBitmap(200, 200);
            }


            var b2 = bmp.Clone();

            bmp.Dispose();
            bmp = null;

            return b2;
        }

        ///// <summary>
        ///// Source: https://gist.github.com/brwnx/191c79b6c2b3befbfc7d
        ///// </summary>
        ///// <param name="bitmap"></param>
        ///// <returns></returns>
        //public static bool isDark(Bitmap bitmap)
        //{
        //    bool dark = false;

        //    float darkThreshold = bitmap.Width * bitmap.Height * 0.7f;
        //    int darkPixels = 0;

        //    //int[] pixels = new int[bitmap.Width * bitmap.Height];

        //    for (int x = 0; x < bitmap.Width; x++)
        //    {
        //        for (int y = 0; y < bitmap.Height; y++)
        //        {
        //            System.Drawing.Color pxl = bitmap.GetPixel(x, y);
        //            int r = pxl.R;
        //            int g = pxl.G;
        //            int b = pxl.B;
        //            double luminance = (0.299 * r + 0.0f + 0.587 * g + 0.0f + 0.114 * b + 0.0f);
        //            if (luminance < 150)
        //            {
        //                darkPixels++;
        //            }
        //        }
        //    }

        //    if (darkPixels >= darkThreshold)
        //    {
        //        dark = true;
        //    }
        //    return dark;
        //}

        /// <summary>
        /// Source: https://gist.github.com/brwnx/191c79b6c2b3befbfc7d
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static bool isDark(this AnyBitmap bitmap)
        {
            bool dark = false;

            float darkThreshold = bitmap.Width * bitmap.Height * 0.7f;
            int darkPixels = 0;

            //int[] pixels = new int[bitmap.Width * bitmap.Height];

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var pxl = bitmap.GetPixel(x, y);
                    int r = pxl.R;
                    int g = pxl.G;
                    int b = pxl.B;
                    double luminance = (0.299 * r + 0.0f + 0.587 * g + 0.0f + 0.114 * b + 0.0f);
                    if (luminance < 150)
                    {
                        darkPixels++;
                    }
                }
            }

            if (darkPixels >= darkThreshold)
            {
                dark = true;
            }
            return dark;
        }

        //public static Bitmap Blur(Bitmap image, Int32 blurSize)
        //{
        //    return Blur(image, new System.Drawing.Rectangle(0, 0, image.Width, image.Height), blurSize);
        //}

        //public static Bitmap Blur(Bitmap image, System.Drawing.Rectangle rectangle, Int32 blurSize)
        //{
        //    Bitmap blurred = new Bitmap(image.Width, image.Height);

        //    // make an exact copy of the bitmap provided
        //    using (Graphics graphics = Graphics.FromImage(blurred))
        //        graphics.DrawImage(image, new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
        //            new System.Drawing.Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

        //    // look at every pixel in the blur rectangle
        //    for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
        //    {
        //        for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
        //        {
        //            int avgR = 0, avgG = 0, avgB = 0;
        //            int blurPixelCount = 0;

        //            // average the color of the red, green and blue for each pixel in the
        //            // blur size while making sure you don't go outside the image bounds
        //            for (int x = xx; (x < xx + blurSize && x < image.Width); x++)
        //            {
        //                for (int y = yy; (y < yy + blurSize && y < image.Height); y++)
        //                {
        //                    System.Drawing.Color pixel = blurred.GetPixel(x, y);

        //                    avgR += pixel.R;
        //                    avgG += pixel.G;
        //                    avgB += pixel.B;

        //                    blurPixelCount++;
        //                }
        //            }

        //            avgR = avgR / blurPixelCount;
        //            avgG = avgG / blurPixelCount;
        //            avgB = avgB / blurPixelCount;

        //            // now that we know the average for the blur size, set each pixel to that color
        //            for (int x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++)
        //                for (int y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++)
        //                    blurred.SetPixel(x, y, System.Drawing.Color.FromArgb(avgR, avgG, avgB));
        //        }
        //    }

        //    return blurred;
        //}

        public static AnyBitmap Blur(this AnyBitmap original, float sigma)
        {
            return original.Blur(new SixLabors.ImageSharp.Rectangle(0, 0, original.Width, original.Height), sigma);
        }

        public static AnyBitmap Blur(this AnyBitmap original, SixLabors.ImageSharp.Rectangle rectangle, float sigma)
        {
            SixLabors.ImageSharp.Image img = original;

            img.Mutate(a => a.GaussianBlur(sigma, rectangle));

            return (AnyBitmap)img;
        }

        ///// <summary>
        ///// Resize the image to the specified width and height.
        ///// </summary>
        ///// <param name="image">The image to resize.</param>
        ///// <param name="width">The width to resize to.</param>
        ///// <param name="height">The height to resize to.</param>
        ///// <returns>The resized image.</returns>
        //public static Bitmap ResizeImage(Image image, int width, int height)
        //{
        //    var destRect = new System.Drawing.Rectangle(0, 0, width, height);
        //    var destImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        //    destImage.SetResolution(width, height);

        //    using (var graphics = Graphics.FromImage(destImage))
        //    {
        //        graphics.CompositingMode = CompositingMode.SourceCopy;
        //        graphics.CompositingQuality = CompositingQuality.HighQuality;
        //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        graphics.SmoothingMode = SmoothingMode.HighQuality;
        //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        //        using (var wrapMode = new ImageAttributes())
        //        {
        //            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        //            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        //        }
        //    }

        //    return destImage;
        //}

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="new_width"></param>
        /// <returns></returns>
        public static AnyBitmap ResizeImage(this AnyBitmap original, int width, int height)
        {
            SixLabors.ImageSharp.Image img = original;

            img.Mutate(a => a.Resize(width, height));

            return (AnyBitmap)img;
        }

        ///// <summary>
        ///// Resize the image to the specified width and height.
        ///// </summary>
        ///// <param name="image">The image to resize.</param>
        ///// <param name="width">The width to resize to.</param>
        ///// <param name="height">The height to resize to.</param>
        ///// <returns>The resized image.</returns>
        //public static Bitmap ResizeImage(Image image, float new_width)
        //{
        //    int width = (int)new_width;
        //    int height = (int)(image.Height / (image.Width / new_width));


        //    var destRect = new System.Drawing.Rectangle(0, 0, width, height);
        //    var destImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        //    destImage.SetResolution(width, height);

        //    using (var graphics = Graphics.FromImage(destImage))
        //    {
        //        graphics.CompositingMode = CompositingMode.SourceCopy;
        //        graphics.CompositingQuality = CompositingQuality.HighQuality;
        //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        graphics.SmoothingMode = SmoothingMode.HighQuality;
        //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        //        using (var wrapMode = new ImageAttributes())
        //        {
        //            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        //            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        //        }
        //    }

        //    return destImage;
        //}

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="new_width"></param>
        /// <returns></returns>
        public static AnyBitmap ResizeImage(this AnyBitmap original, float new_width)
        {
            int width = (int)new_width;
            int height = (int)(original.Height / (original.Width / new_width));

            SixLabors.ImageSharp.Image img = original;

            img.Mutate(a => a.Resize(width, height));

            return (AnyBitmap)img;
        }

        //public static Bitmap MakeGrayscale3(Bitmap original)
        //{
        //    //create a blank bitmap the same size as original
        //    Bitmap newBitmap = new Bitmap(original.Width, original.Height);

        //    //get a graphics object from the new image
        //    using (Graphics g = Graphics.FromImage(newBitmap))
        //    {

        //        //create the grayscale ColorMatrix
        //        ColorMatrix colorMatrix = new ColorMatrix(
        //           new float[][]
        //           {
        //     new float[] {.3f, .3f, .3f, 0, 0},
        //     new float[] {.59f, .59f, .59f, 0, 0},
        //     new float[] {.11f, .11f, .11f, 0, 0},
        //     new float[] {0, 0, 0, 1, 0},
        //     new float[] {0, 0, 0, 0, 1}
        //           });

        //        //create some image attributes
        //        using (ImageAttributes attributes = new ImageAttributes())
        //        {

        //            //set the color matrix attribute
        //            attributes.SetColorMatrix(colorMatrix);

        //            //draw the original image on the new image
        //            //using the grayscale color matrix
        //            g.DrawImage(original, new System.Drawing.Rectangle(0, 0, original.Width, original.Height),
        //                        0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
        //        }
        //    }
        //    return newBitmap;
        //}

        public static AnyBitmap MakeGrayscale3(this AnyBitmap original)
        {
            SKBitmap img = original;

            img.Mutate(a => a.Grayscale());

            return (AnyBitmap)img;
        }

        public static AnyBitmap Fill(this AnyBitmap original, SKColor color)
        {
            SKBitmap img = original;

            img.Mutate(a => a.Fill(color));

            return img;
        }

        public static AnyBitmap DrawImageScaled(this AnyBitmap original, AnyBitmap image_to_draw, SKRectangle rectangle)
        {
            SKBitmap img = original;

            int width = rectangle.Width;
            int height = rectangle.Height;

            var b = image_to_draw.Clone().ResizeImage(width, height);

            img.Mutate(a => a.DrawImage(b, new SKPoint(rectangle.Left, rectangle.Top), 1)); // new SKRectangle(5 * factor, 5 * factor, 24 * factor, 24 * factor)

            return img;
        }

        public static void DrawImageScaled(this SKBitmap original, AnyBitmap image_to_draw, SKRectangle rectangle)
        {
            int width = rectangle.Width;
            int height = rectangle.Height;

            var b = image_to_draw.Clone().ResizeImage(width, height);

            original.Mutate(a => a.DrawImage(b, new SKPoint(rectangle.Left, rectangle.Top), 1)); // new SKRectangle(5 * factor, 5 * factor, 24 * factor, 24 * factor)
        }


        public static void DrawText(this SKBitmap original, SKFont font, String text, SKColor color, SKPoint point, HorizontalAlignment aligment = HorizontalAlignment.Center)
        {

            original.Mutate(a => a.DrawText(new RichTextOptions(font) { HorizontalAlignment = aligment, Origin = point }, text, color));

        }

        public static float MeasureMaxFontSize(this SKBitmap original, SKFont font, String text)
        {
            float size = font.Size;
            int width = original.Width;
            SKFont f = font;

            //Maximum 10 loops
            for (int i = 0; i < 100; i++)
            {
                var measured = TextMeasurer.MeasureSize(text, new TextOptions(f));

                if (measured.Width <= width)
                {
                    return size;
                }

                size -= 1;
                f = new SKFont(font, size);
            }

            return size;

        }

    }
}

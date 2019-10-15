using System.IO;
using static System.Math;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

namespace BatchProcess {

    public static class mdlGlobals {

        public static string myAppMajor;
        public static string myAppMinor;
        public static string myAppBuild;
        public static string myAppVersion;
        public static string myAppPath;
        public static string myAppBuildDate;
        public static string myAppTitle;

        public static void InitGlobals()
        {
            string myExeName;

            System.Reflection.Assembly myAssy;
            System.Reflection.AssemblyName myAssyName;

            //Version number (defined in the AssemblyInfo.vb file)
            myAssy = System.Reflection.Assembly.GetExecutingAssembly();
            myAssyName = myAssy.GetName();
            myAppMajor = myAssyName.Version.Major.ToString();
            myAppMinor = myAssyName.Version.Minor.ToString();
            myAppBuild = myAssyName.Version.Build.ToString();
            myAppVersion = myAppMajor.ToString() + "." + myAppMinor.ToString() + "." + myAppBuild.ToString();
            myAppPath = myAssy.Location;
            myAppBuildDate = File.GetLastWriteTime(myAppPath).ToString();
            myExeName = Path.GetFileName(myAppPath);
            myAppTitle = Path.GetFileNameWithoutExtension(myAppPath);
            myAppPath = Path.GetFullPath((Left(myAppPath, (Len(myAppPath) - Len(myExeName)))));
        }

        public static int Len(string source)
        {
            if (source == null || source == "") return 0;
            return source.Length;
        }

        public static string Left(string source, int i)
        {
            if (source == null || source == "" || i <= 0) return "";
            return source.Substring(0, Min(i, source.Length));
        }

        public static byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            Bitmap b = new Bitmap(imageIn);
            Rectangle rect = new Rectangle(0, 0, b.Width, b.Height);
            BitmapData bmpData = b.LockBits(rect, ImageLockMode.ReadWrite, b.PixelFormat);

            int bytes = Abs(bmpData.Stride) * b.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);

            b.UnlockBits(bmpData);
            return rgbValues;
        }

        /// <summary>
        /// Converts a bitmap into an 8-bit grayscale bitmap
        /// </summary>
        public static Bitmap ColorToGrayscale(Bitmap bmp)
        {
            int w = bmp.Width,
                h = bmp.Height,
                r, ic, oc, bmpStride, outputStride, bytesPerPixel;
            PixelFormat pfIn = bmp.PixelFormat;
            ColorPalette palette;
            Bitmap output;
            BitmapData bmpData, outputData;

            //Create the new bitmap
            output = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

            //Build a grayscale color Palette
            palette = output.Palette;
            for (int i = 0; i < 256; i++) {
                System.Drawing.Color tmp = System.Drawing.Color.FromArgb(255, i, i, i);
                palette.Entries[i] = System.Drawing.Color.FromArgb(255, i, i, i);
            }
            output.Palette = palette;

            //No need to convert formats if already in 8 bit
            if (pfIn == PixelFormat.Format8bppIndexed) {
                output = (Bitmap)bmp.Clone();

                //Make sure the palette is a grayscale palette and not some other
                //8-bit indexed palette
                output.Palette = palette;

                return output;
            }

            //Get the number of bytes per pixel
            switch (pfIn) {
                case PixelFormat.Format24bppRgb: bytesPerPixel = 3; break;
                case PixelFormat.Format32bppArgb: bytesPerPixel = 4; break;
                case PixelFormat.Format32bppRgb: bytesPerPixel = 4; break;
                default: throw new InvalidOperationException("Image format not supported");
            }

            //Lock the images
            bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly,
                                   pfIn);
            outputData = output.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly,
                                         PixelFormat.Format8bppIndexed);
            bmpStride = bmpData.Stride;
            outputStride = outputData.Stride;

            //Traverse each pixel of the image
            unsafe {
                byte* bmpPtr = (byte*)bmpData.Scan0.ToPointer(),
                outputPtr = (byte*)outputData.Scan0.ToPointer();

                if (bytesPerPixel == 3) {
                    //Convert the pixel to it's luminance using the formula:
                    // L = .299*R + .587*G + .114*B
                    //Note that ic is the input column and oc is the output column
                    for (r = 0; r < h; r++)
                        for (ic = oc = 0; oc < w; ic += 3, ++oc)
                            outputPtr[r * outputStride + oc] = (byte)(int)
                                (0.299f * bmpPtr[r * bmpStride + ic] +
                                 0.587f * bmpPtr[r * bmpStride + ic + 1] +
                                 0.114f * bmpPtr[r * bmpStride + ic + 2]);
                }
                else //bytesPerPixel == 4
                {
                    //Convert the pixel to it's luminance using the formula:
                    // L = alpha * (.299*R + .587*G + .114*B)
                    //Note that ic is the input column and oc is the output column
                    for (r = 0; r < h; r++)
                        for (ic = oc = 0; oc < w; ic += 4, ++oc)
                            outputPtr[r * outputStride + oc] = (byte)(int)
                                ((bmpPtr[r * bmpStride + ic] / 255.0f) *
                                (0.299f * bmpPtr[r * bmpStride + ic + 1] +
                                 0.587f * bmpPtr[r * bmpStride + ic + 2] +
                                 0.114f * bmpPtr[r * bmpStride + ic + 3]));
                }
            }

            //Unlock the images
            bmp.UnlockBits(bmpData);
            output.UnlockBits(outputData);

            return output;
        }

        public static byte[] ImageToGrayscaleByteArray(System.Drawing.Image imageIn) {
            Bitmap b = new Bitmap(imageIn);
            Rectangle rect = new Rectangle(0, 0, b.Width, b.Height);
            BitmapData bmpData = b.LockBits(rect, ImageLockMode.ReadWrite, b.PixelFormat);
            var bmpStride = bmpData.Stride;

            int bytes = Abs(bmpData.Stride) * b.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);

            b.UnlockBits(bmpData);

            bytes = b.Width * b.Height;
            byte[] byteArray = new byte[bytes];

            PixelFormat pfIn = b.PixelFormat;
            int bytesPerPixel = 3;
            //Get the number of bytes per pixel
            switch (pfIn) {
                case PixelFormat.Format24bppRgb: bytesPerPixel = 3; break;
                case PixelFormat.Format32bppArgb: bytesPerPixel = 4; break;
                case PixelFormat.Format32bppRgb: bytesPerPixel = 4; break;
                default: throw new InvalidOperationException("Image format not supported");
            }

            //Convert the pixel to it's luminance using the formula:
            // L = .299*R + .587*G + .114*B
            //Note that ic is the input column and oc is the output column
            for (int r = 0; r < b.Height; r++) {
                int ic = 0;
                for (int c = 0; c < b.Width; c += 1) {
                    byteArray[r * b.Width + c] = (byte)(int)
                        (0.299f * rgbValues[r * bmpStride + ic] +
                         0.587f * rgbValues[r * bmpStride + ic + 1] +
                         0.114f * rgbValues[r * bmpStride + ic + 2]);
                    ic = ic + bytesPerPixel;
                }
            }
            return byteArray;
        }

        public static void SaveByteArrayAsBitmap(string fileName, int width, int height, byte[] imageData) {
            // Need to copy our 8 bit greyscale image into a 32bit layout.
            // Choosing 32bit rather than 24 bit as its easier to calculate stride etc.
            // This will be slow enough and isn't the most efficient method.
            var data = new byte[width * height * 4];

            int o = 0;

            for (var i = 0; i < width * height; i++) {
                var value = imageData[i];

                // Greyscale image so r, g, b, get the same
                // intensity value.
                data[o++] = value;
                data[o++] = value;
                data[o++] = value;
                data[o++] = 0;  // Alpha isn't actually used
            }

            unsafe {
                fixed (byte* ptr = data) {
                    // Craete a bitmap wit a raw pointer to the data
                    using (Bitmap image = new Bitmap(width, height, width * 4,
                                PixelFormat.Format32bppRgb, new IntPtr(ptr))) {
                        // And save it.
                        image.Save(Path.ChangeExtension(fileName, ".bmp"));
                    }
                }
            }
        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height,PixelFormat.Format8bppIndexed);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        public static int byteSwapInt(int from)
        {
            byte[] b1 = BitConverter.GetBytes(from);
            byte[] b2 = BitConverter.GetBytes(from);
            int i;

            for (i = 0; i < 4; i++) {
                b2[i] = b1[3 - i];
            }
            int to = BitConverter.ToInt32(b2, 0);
            return to;
        }

        public static double byteSwapDouble(double from)
        {
            byte[] b1 = BitConverter.GetBytes(from);
            byte[] b2 = BitConverter.GetBytes(from);
            int i;

            for (i = 0; i < 8; i++) {
                b2[i] = b1[7 - i];
            }
            double to = BitConverter.ToDouble(b2, 0);
            return to;
        }

    }
}

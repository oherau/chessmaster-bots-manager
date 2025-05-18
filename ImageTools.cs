using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessmasterBotsManager
{
    public class ImageTools
    {
        public static Bitmap ConvertTo24bpp(Image img)
        {
            var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
            using (var gr = Graphics.FromImage(bmp))
                gr.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
            return bmp;
        }

        internal static Image CropImage(Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        internal static Image ResizeImage(Image imgToResize, Size size)
        {
            int neww = imgToResize.Height * 76 / 97;
            int newh = imgToResize.Height;
            imgToResize = CropImage(imgToResize, new Rectangle((imgToResize.Width - neww) / 2, 0, neww, newh));

            //Get the image current width  
            int sourceWidth = imgToResize.Width;
            //Get the image current height  
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            //Calulate  width with new desired size  
            nPercentW = ((float)size.Width / (float)sourceWidth);
            //Calculate height with new desired size  
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            //New Width  
            int destWidth = 76;// (int)(sourceWidth * nPercent);
            //New Height  
            int destHeight = 97;// (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height  
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (System.Drawing.Image)b;
        }

        internal static Image? LoadAvatarImage(string fileName)
        {
            try
            {
                var memoryStream = new MemoryStream(File.ReadAllBytes(fileName));
                Image img = Image.FromStream(memoryStream);

                //var img = new Bitmap(fileName);
                var img2 = ImageTools.ConvertTo24bpp(img);
                // display image in picture box
                if (img2.Size.Width == 76 && img2.Size.Height == 97)
                {
                    return img2;
                }
                else
                {
                    return ImageTools.ResizeImage(img2, new Size(76, 97));
                }
            } catch
            {
                return null;
            }
        }

        static internal void SaveBmp(Image image, string filename)
        {
            if (image == null) return;

            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;


            myEncoder = System.Drawing.Imaging.Encoder.ColorDepth;
            myEncoderParameters = new EncoderParameters(2);

            // Save the image with a color depth of 24 bits per pixel.
            myEncoderParameter = new EncoderParameter(myEncoder, (long)PixelFormat.Format24bppRgb);
            myEncoderParameters.Param[0] = myEncoderParameter;

            var myEncoderParameterComp = new EncoderParameter(myEncoder, (long)EncoderValue.CompressionLZW);
            myEncoderParameters.Param[1] = myEncoderParameterComp;

            var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
            using (var gr = Graphics.FromImage(bmp))
                gr.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
            bmp.Save(filename, ImageFormat.Bmp);

        }
    }
}

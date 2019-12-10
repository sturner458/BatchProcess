using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatchProcess
{
    public partial class frmImage : Form
    {
        public frmImage() {
            InitializeComponent();
        }

        private void frmImage_Load(object sender, EventArgs e) {

        }

        private void frmImage_Shown(object sender, EventArgs e) {
            Bitmap originalImage = (Bitmap)Image.FromFile("C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\survey 1 05-12-19\\survey 1\\Corners-Survey2.png");
            Bitmap adjustedImage = new Bitmap(originalImage.Width, originalImage.Height);
            float brightness = 0.0f; // darker
            float contrast = 2.0f; // twice the contrast
            float gamma = 1.0f; // half the gamma

            // create matrix that will brighten and contrast the image
            float[][] ptsArray ={
                new float[] {contrast, 0, 0, 0, 0}, // scale red
                new float[] {0, contrast, 0, 0, 0}, // scale green
                new float[] {0, 0, contrast, 0, 0}, // scale blue
                new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
                new float[] { brightness, brightness, brightness, 0, 1}
            };

            ImageAttributes imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(adjustedImage);
            g.DrawImage(originalImage, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height)
                , 0, 0, originalImage.Width, originalImage.Height,
                GraphicsUnit.Pixel, imageAttributes);
            pictureBox1.Image = adjustedImage;

            adjustedImage.Save("C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\survey 1 05-12-19\\survey 1\\Corners-Survey2a.png", ImageFormat.Png);
        }
    }
}

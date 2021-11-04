using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using static System.Math;
using Emgu.CV;
using Emgu.CV.Structure;

namespace BatchProcess {
    public partial class Form1 : Form {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var myDlg = new FolderBrowserDialog();

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos";
            var ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            var myFolder = myDlg.SelectedPath;
            var myFiles = new List<string>();
            int myWidth = 0, myHeight = 0, nImages = 0, nCount = 0;

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("calibration") && myFile.ToLower().EndsWith(".png") && !myFile.ToLower().Contains("-adj.png")) {
                    if (Path.GetFileNameWithoutExtension(myFile).Contains("17")) continue;
                    nImages = nImages + 1;
                    myFiles.Add(myFile);
                    if (myWidth == 0) {
                        Image myImage = Image.FromFile(myFile);
                        myWidth = myImage.Width;
                        myHeight = myImage.Height;
                    }
                }
            }

            // ARToolKitFunctions.Instance.arwInitialiseAR();
            ARToolKitFunctions.Instance.arwInitChessboardCorners(17, 13, 20, 3264, 2448, nImages);

            float[] corners = new float[442];
            int cornerCount;
            //string cornerFile = "C:\\Temp\\CornersARToolkit.txt";
            //if (File.Exists(cornerFile)) {
            //    try {
            //        File.Delete(cornerFile);
            //    }
            //    catch (Exception ex) {
            //        string s = ex.ToString();
            //    }
            //}

            myFiles.Sort(new AlphaNumericCompare());

            //StreamWriter sw = new StreamWriter(cornerFile);
            foreach (string myFile in myFiles) {
                var image = new Image<Gray, byte>(myFile);
                var size = image.Width * image.Height;

                var cornerPoints = new Emgu.CV.Util.VectorOfPointF();
                var mBoardSize = new Size(13, 17);
                var res = CvInvoke.FindChessboardCorners(image, mBoardSize, cornerPoints);

                byte[] imageBytes = new Byte[size];
                System.Buffer.BlockCopy(image.Data, 0, imageBytes, 0, size);

                //byte[] imageBytes = ImageToGrayscaleByteArray((Bitmap)Image.FromFile(myFile));

                int result = ARToolKitFunctions.Instance.arwFindChessboardCorners(corners, out cornerCount, imageBytes);

                var imagePoints = new Emgu.CV.Util.VectorOfPointF();
                int l = 0;
                for (int i = 0; i < 17; i++) {
                    for (int j = 0; j < 13; j++) {
                        //sw.WriteLine(corners[l * 2].ToString() + '\t' + corners[l * 2 + 1].ToString());
                        imagePoints.Push(new PointF[] { new PointF(corners[l * 2], corners[l * 2 + 1]) });
                        l++;
                    }
                }

                if (result == 1) {

                    if (imagePoints.Size > 0) {
                        Mat imageCopy = Emgu.CV.CvInvoke.Imread(myFile, Emgu.CV.CvEnum.ImreadModes.Color);
                        if (imagePoints.Size > 0) mdlEmguDetection.DrawCornersOnImage(imageCopy, imagePoints, System.Drawing.Color.Green);
                        CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\Corners-" + Path.GetFileNameWithoutExtension(myFile) + ".png", imageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));
                    }

                    cornerCount = ARToolKitFunctions.Instance.arwCaptureChessboardCorners();
                    System.Diagnostics.Debug.Print("Processed image " + cornerCount.ToString());
                    if (cornerCount == nImages) {
                        nCount = nCount + 1;
                        float[] reprojectionErrors = new float[nImages];
                        float reprojectionError = ARToolKitFunctions.Instance.arwCalibChessboardCorners(nImages, "C:\\Temp\\Calib.dat", out reprojectionErrors);

                        System.Diagnostics.Debug.Print("Total reprojection error: " + reprojectionError.ToString());
                        for (int i = 0; i < reprojectionErrors.Length; i++) {
                            System.Diagnostics.Debug.Print("Reprojection error " + (i + 1).ToString() + ": " + reprojectionErrors[i].ToString());
                        }
                    }
                } else {
                    System.Diagnostics.Debug.Print("Failed to process image " + myFile);
                }

            }
            //sw.Close();
        }

        private void btnPhotos_Click(object sender, EventArgs e)
        {
            mdlDetectPhotos.ProcessPhotos(lblStatus);
        }

        private Image ImageFromRawRgbaArray(byte[] arr, int width, int height)
        {
            var output = new Bitmap(width, height);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = output.LockBits(rect, ImageLockMode.ReadWrite, output.PixelFormat);
            var ptr = bmpData.Scan0;
            Marshal.Copy(arr, 0, ptr, arr.Length);
            output.UnlockBits(bmpData);
            return output;
        }

        private void btnImportDiagnostics_Click(object sender, EventArgs e)
        {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile;
            int i, j, k, n;
            clsMarkerPoint2 myMarkerPoint, mySeenFromMarker;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\Photogrammetry\\BatchProcess\\Diagnostic Files";
            myDlg.Filter = "Diagnostic Files (*.txt)|*.txt";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            mdlDetectPhotos.LoadAutoSaveData(myFile);

            //Temp fix for levelled markers
            for (i = 0; i < mdlDetectPhotos.ConfirmedMarkers.Count; i++) {
                for (j = 0; j < mdlDetectPhotos.ConfirmedMarkers[i].History.Count; j++) {
                    if (mdlDetectPhotos.ConfirmedMarkers[i].History[j].SeenFromMarkerID == mdlDetectPhotos.myGFMarkerID) {
                        mdlDetectPhotos.RelevelMarkerFromGF(mdlDetectPhotos.ConfirmedMarkers[i].History[j], true);
                    }
                }
            }

            mdlDetectPhotos.ConfirmedMarkersCopy.Clear();
            for (k = 0; k < 2; k++) {
                mdlDetectPhotos.mySuspectedMarkers.Clear();

                for (i = 0; i < mdlDetectPhotos.ConfirmedMarkers.Count; i++) {
                    for (j = 0; j < mdlDetectPhotos.ConfirmedMarkers[i].History.Count; j++) {
                        mdlDetectPhotos.mySuspectedMarkers.Add(mdlDetectPhotos.ConfirmedMarkers[i].History[j].Copy());
                    }
                }
                mdlDetectPhotos.ConfirmedMarkers.Clear();

                n = -1;
                while (n < mdlDetectPhotos.ConfirmedMarkers.Count && mdlDetectPhotos.mySuspectedMarkers.Count > 0) {
                    n = mdlDetectPhotos.ConfirmedMarkers.Count;
                    mdlDetectPhotos.ProcessMarkers(true);
                }

                mdlDetectPhotos.ConfirmedMarkersCopy.Clear();
                for (i = 0; i < mdlDetectPhotos.ConfirmedMarkers.Count; i++) {
                    mdlDetectPhotos.ConfirmedMarkersCopy.Add(mdlDetectPhotos.ConfirmedMarkers[i].Copy());
                }
            }

            for (i = 0; i < mdlDetectPhotos.ConfirmedMarkers.Count; i++) {
                mdlDetectPhotos.RelevelMarkerFromGF(mdlDetectPhotos.ConfirmedMarkers[i]);
            }

            mdlDetectPhotos.SaveSurvey();
            MessageBox.Show("Finished");
        }

        private void button4_Click(object sender, EventArgs e)
        {

            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile;
            int i, j, k, n;
            clsMarkerPoint myMarkerPoint, mySeenFromMarker;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\Photogrammetry\\BatchProcess\\Diagnostic Files";
            myDlg.Filter = "Diagnostic Files (*.txt)|*.txt";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            mdlDetectPhotos.LoadAutoSaveData(myFile);

            mdlDetectPhotos.ConfirmedMarkersCopy.Clear();
            mdlDetectPhotos.mySuspectedMarkers.Clear();

            for (i = 0; i < mdlDetectPhotos.ConfirmedMarkers.Count; i++) {
                for (j = 0; j < mdlDetectPhotos.ConfirmedMarkers[i].History.Count; j++) {
                    mdlDetectPhotos.mySuspectedMarkers.Add(mdlDetectPhotos.ConfirmedMarkers[i].History[j].Copy());
                    if (mdlDetectPhotos.mySuspectedMarkers.Last().SeenFromMarkerID == mdlDetectPhotos.myGFMarkerID) {
                        mdlDetectPhotos.RelevelMarkerFromGF(mdlDetectPhotos.mySuspectedMarkers.Last(), true);
                    }
                }
            }
            mdlDetectPhotos.ConfirmedMarkers.Clear();

            n = -1;
            while (n < mdlDetectPhotos.ConfirmedMarkers.Count && mdlDetectPhotos.mySuspectedMarkers.Count > 0) {
                n = mdlDetectPhotos.ConfirmedMarkers.Count;
                mdlDetectPhotos.ProcessMarkers(true);
            }

            for (i = 0; i < mdlDetectPhotos.ConfirmedMarkers.Count; i++) {
                mdlDetectPhotos.RelevelMarkerFromGF(mdlDetectPhotos.ConfirmedMarkers[i]);
            }

            mdlDetectPhotos.SaveSurvey();
            MessageBox.Show("Finished");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile;

            myDlg.InitialDirectory = "C:\\Temp";
            myDlg.Filter = "Dat Files (*.dat)|*.dat";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            var param = mdlRecognise.ReadCameraCalibrationFile(myFile);

            System.Diagnostics.Debug.Print("Filename\t" + System.IO.Path.GetFileName(myFile));
            System.Diagnostics.Debug.Print("xSize\t" + param.xsize.ToString());
            System.Diagnostics.Debug.Print("ySize\t" + param.ysize.ToString());
            System.Diagnostics.Debug.Print("Mat[3][4]\t" + param.mat[0, 0].ToString() + "\t" + param.mat[0, 1].ToString() + "\t" + param.mat[0, 2].ToString() + "\t" + param.mat[0, 3].ToString());
            System.Diagnostics.Debug.Print("\t" + param.mat[1, 0].ToString() + "\t" + param.mat[1, 1].ToString() + "\t" + param.mat[1, 2].ToString() + "\t" + param.mat[1, 3].ToString());
            System.Diagnostics.Debug.Print("\t" + param.mat[2, 0].ToString() + "\t" + param.mat[2, 1].ToString() + "\t" + param.mat[2, 2].ToString() + "\t" + param.mat[2, 3].ToString());
            for (int i = 0; i < 17; i++) {
                System.Diagnostics.Debug.Print("dist_factor[" + i.ToString() + "]\t" + param.dist_factor[i].ToString());
            }
        }

        private void WriteCalibrationFile(ARParam param)
        {
            SaveFileDialog myDlg = new SaveFileDialog();
            DialogResult ret;
            string myFile;
            int i, j;

            myDlg.InitialDirectory = "C:\\Temp";
            myDlg.Filter = "Dat Files (*.dat)|*.dat";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            FileStream sw = File.Open(myFile, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(sw);

            bw.Write(byteSwapInt(param.xsize));
            bw.Write(byteSwapInt(param.ysize));
            
            for (i = 0; i < 3; i++) {
                for (j = 0; j < 4; j++) {
                    bw.Write(byteSwapDouble(param.mat[i, j]));
                }
            }
            for (i = 0; i < 9; i++) {
                bw.Write(byteSwapDouble(param.dist_factor[i]));
            }
            bw.Close();
            sw.Close();
        }

        private void btnOpenCV_Click(object sender, EventArgs e)
        {

            FolderBrowserDialog myDlg = new FolderBrowserDialog();
            DialogResult ret;
            string myFolder;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\Calibration14-11-19\\Calibration";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFolder = myDlg.SelectedPath;

            int myWidth = 0, myHeight = 0, nImages = 0;

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (!(myFile.ToLower().EndsWith(".png") || myFile.ToLower().EndsWith(".jpg"))) continue;
                nImages = nImages + 1;
                if (myWidth == 0) {
                    Image myImage = Image.FromFile(myFile);
                    myWidth = myImage.Width;
                    myHeight = myImage.Height;
                }
            }

            mdlEmguCalibration.InitCalibration("C:\\Temp\\Calib.dat", 17, 13, 20.0f, nImages);

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("calibration") && myFile.ToLower().EndsWith(".png")) {
                    mdlEmguCalibration.ProcessImage(myFile);
                }
            }

            mdlEmguCalibration.CalibrateCamera(true);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnUndistort_Click(object sender, EventArgs e)
        {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile, myImageFile;

            myDlg.InitialDirectory = "C:\\Temp";
            myDlg.Filter = "Dat Files (*.dat)|*.dat";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            //myDlg.InitialDirectory = "C:\\Customer\\Stannah\\PhotoGrammetry\\BatchProcess\\Photos\\iPad Chessboard From Camera App";
            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\PhotoGrammetry\\Photos";
            myDlg.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myImageFile = myDlg.FileName;

            mdlEmguCalibration.Undistort(myFile, myImageFile);
            // mdlEmguCalibration.UndistortSimple(myFile, myImageFile);
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            //mdlEmguDetection.DrawMarkers();

            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile, myImageFile;

            myDlg.InitialDirectory = "C:\\Temp";
            myDlg.Filter = "Dat Files (*.dat)|*.dat";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\PhotoGrammetry\\BatchProcess\\Photos\\iPad 1";
            myDlg.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myImageFile = myDlg.FileName;

            mdlEmguDetection.DoDetection(myFile, myImageFile);
        }

        private void btnDetectMarkers_Click(object sender, EventArgs e) {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myImageFile;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\Survey 2";
            myDlg.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myImageFile = myDlg.FileName;

            mdlEmguDetection.DetectMarkers_RevA(myImageFile);
            mdlEmguDetection.DetectMarkers_RevC1(myImageFile);
        }

        private void btnDetectDatums_Click(object sender, EventArgs e) {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myImageFile;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\DatumSurvey\\Survey";
            myDlg.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myImageFile = myDlg.FileName;

            mdlEmguDetection.DetectMarkers_RevC1(myImageFile);
            //mdlEmguDetection.DetectMarkers_RevC7(myImageFile);
        }

        private void btnBrightness_Click(object sender, EventArgs e) {
            var myDlg = new FolderBrowserDialog();
            string myFolder;
            List<string> myFiles = new List<string>();
            int nFiles = 0;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\survey 1 05-12-19\\survey 1\\";
            //myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\Survey 2";
            var ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFolder = myDlg.SelectedPath;

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if ((Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("survey") || Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("calibration")) && myFile.ToLower().EndsWith(".png") && !myFile.ToLower().Contains("-adj")) {
                    myFiles.Add(myFile);
                }
            }

            foreach (string myFile in myFiles) {
                nFiles++;
                BrightenImage(myFile);
                lblStatus.Text = nFiles.ToString() + "/" + myFiles.Count().ToString();
                Application.DoEvents();
            }

        }

        void BrightenImage(string myFile) {
            using (var originalImage = (Bitmap)Image.FromFile(myFile)) {
                using (var adjustedImage = new Bitmap(originalImage.Width, originalImage.Height)) {
                    float brightness = -1.0f; // darker
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
                    adjustedImage.Save(Path.Combine(Path.GetDirectoryName(myFile), Path.GetFileNameWithoutExtension(myFile) + "-adj.png"));
                }
            }
        }

        private void btnSimplePhotos_Click(object sender, EventArgs e) {
            mdlRecognise.ProcessPhotos(lblStatus);
        }

        private void btnImportDiagnostics3_Click(object sender, EventArgs e) {

            bool useDatums = false;
            int arToolkitMarkerType = -1;
            int circlesToUse = 0;

            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile, myCalibFile;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\Photogrammetry\\Model Files\\Testing";
            myDlg.Filter = "Diagnostic Files (*.txt)|*.txt";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            var datFiles = Directory.GetFiles(Path.GetDirectoryName(myFile), "*.dat");
            if (datFiles.Count() == 1) {
                myCalibFile = datFiles.First();
            } else {
                myDlg.InitialDirectory = Path.GetDirectoryName(myDlg.FileName);
                myDlg.FileName = "";
                myDlg.Filter = "Calibration Files (*.dat)|*.dat";
                ret = myDlg.ShowDialog();
                if (ret != DialogResult.OK) return;
                myCalibFile = myDlg.FileName;
            }

            mdlRecognise.LoadSavedDataFile(myFile);
            //mdlRecognise.TempFixPoints();
            mdlRecognise.BatchBundleAdjust(lblStatus, myFile, myCalibFile, useDatums, arToolkitMarkerType, circlesToUse);

            MessageBox.Show("Finished");
        }

        private void btnImportDiagnostics4_Click(object sender, EventArgs e) {
            var myDlg = new FolderBrowserDialog();
            DialogResult ret;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Model Files\\Rodion\\Rev C1 6 Circles\\SurveyPhotos";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            var myFolder = myDlg.SelectedPath;

            string myCameraFile = "data\\calib-rod.dat";
            mdlRecognise.BatchProcessPhotos(lblStatus, myFolder, myCameraFile);

            MessageBox.Show("Finished");
        }

        private void btnBatchImport_Click(object sender, EventArgs e) {

            bool useDatums = false;
            int arToolkitMarkerType = -1;
            int circlesToUse = 0;

            var folderDialog = new FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            folderDialog.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Model Files\\Testing"; // C:\Customer\Stannah\PhotoGrammetry\Model Files\Mezz staircase for reprocessingg
            var ret = folderDialog.ShowDialog();
            if (ret != DialogResult.OK) return;
            var folder = folderDialog.SelectedPath;

            foreach (var sf in Directory.GetDirectories(folder)) {
                var calibFile = "";
                foreach (var file in Directory.GetFiles(sf)) {
                    if (file.EndsWith(".dat")) calibFile = file;
                    if (file.StartsWith("ATT") && file.EndsWith(".txt")) {
                        try {
                            File.Delete(file);
                        } catch {

                        }
                    }
                }
                var parentFolderName = Path.GetFileName(sf);
                var diagFile = sf + "\\" + parentFolderName + ".txt";
                var diagFile2 = sf + "\\Diagnostics.txt";
                if (!File.Exists(sf + "\\" + parentFolderName + ".3dm") && File.Exists(calibFile) && File.Exists(diagFile)) {
                    mdlRecognise.LoadSavedDataFile(diagFile);
                    mdlRecognise.BatchBundleAdjust(lblStatus, diagFile, calibFile, useDatums, arToolkitMarkerType, circlesToUse);
                } else if (!File.Exists(sf + "\\" + parentFolderName + ".3dm") && File.Exists(calibFile) && File.Exists(diagFile2)) {
                    mdlRecognise.LoadSavedDataFile(diagFile2);
                    mdlRecognise.BatchBundleAdjust(lblStatus, diagFile, calibFile, useDatums, arToolkitMarkerType, circlesToUse);
                }
            }
        }
    }

    class AlphaNumericCompare : IComparer<string> {
        public int Compare(string lhs, string rhs)
        {
            var numExtract = new Regex("[0-9]+");
            int lhsNumber = int.Parse(numExtract.Match(Path.GetFileNameWithoutExtension(lhs)).Value);
            int rhsNumber = int.Parse(numExtract.Match(Path.GetFileNameWithoutExtension(rhs)).Value);
            return lhsNumber.CompareTo(rhsNumber);
        }
    }
    
}

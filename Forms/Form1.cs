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
using static BatchProcess.mdlDetectPhotos;
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
            FolderBrowserDialog myDlg = new FolderBrowserDialog();
            DialogResult ret;
            string myFolder;
            List<string> myFiles = new List<string>();
            int nFiles = 0;

            InitGlobals();

            bool USE_DATUMS = false;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\Survey 2";
            //myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\021219";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFolder = myDlg.SelectedPath;

            if (File.Exists(Path.Combine(myFolder, "Calib.dat")) && File.Exists(Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"))) {
                try {
                    File.Delete(Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"));
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            try {
                File.Copy(Path.Combine(myFolder, "Calib.dat"), Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"));
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                return;
            }

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (myFile.ToLower().EndsWith("-debug.png")) {
                    try {
                        File.Delete(myFile);
                    }
                    catch (Exception ex) {
                        string s = ex.ToString();
                    }
                }
                if (Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("survey") && !myFile.ToLower().EndsWith("-adj.png")) {
                    myFiles.Add(myFile);
                    if (myVideoHeight == 0) {
                        Image myImage = Image.FromFile(myFile);
                        myVideoWidth = myImage.Width;
                        myVideoHeight = myImage.Height;
                        myVideoPixelSize = 4;
                    }
                }
            }
            ARToolKitFunctions.Instance.arwInitialiseAR();
            StartTracking(myVideoWidth, myVideoHeight, USE_DATUMS);
            string myCameraFile = "data\\calib.dat";
            var arParams = mdlEmguCalibration.LoadCameraFromFile2(myCameraFile);

            myFiles.Sort(new AlphaNumericCompare());

            foreach (string myFile in myFiles) {
                nFiles = nFiles + 1;
                lblStatus.Text = nFiles.ToString() + "/" + myFiles.Count().ToString();
                try {
                    var image = new Image<Gray, byte>(myFile);
                    var size = image.Width * image.Height;
                    byte[] imageBytes = new Byte[size];
                    System.Buffer.BlockCopy(image.Data, 0, imageBytes, 0, size);
                    Application.DoEvents();
                    try {
                        RecogniseMarkers(imageBytes, myFile, arParams);
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
                Console.WriteLine("Processed " + nFiles + " out of " + myFiles.Count + " photos - " + Path.GetFileName(myFile));
            }

            var pts = new List<clsPGPoint>();
            for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
                DetectMapperMarkerVisible(myMarkerIDs[i], ref pts, USE_DATUMS);
            }

            DetectMapperMarkerVisible(myGFMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myStepMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myLeftBulkheadMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myRightBulkheadMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myDoorHingeRightMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myDoorFrameRightMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myDoorHingeLeftMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myDoorFrameLeftMarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myObstruct1MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myObstruct2MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myObstruct3MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myObstruct4MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myWall1MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myWall2MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myWall3MarkerID, ref pts, USE_DATUMS);
            DetectMapperMarkerVisible(myWall4MarkerID, ref pts, USE_DATUMS);

            pts.Sort((p1, p2) => p1.z.CompareTo(p2.z));

            var sw = new System.IO.StreamWriter("C:\\Temp\\points.txt");
            pts.ForEach(p => sw.WriteLine(p.x.ToString() + '\t'+ p.z.ToString() + '\t' + (-p.y).ToString() + '\t' + (p.ID + 1).ToString() + '\t' + p.ParentID));
            sw.Close();

            sw = new System.IO.StreamWriter("C:\\Temp\\points.3dm");
            pts.ForEach(p => sw.WriteLine(p.x.ToString() + '\t' + p.y.ToString() + '\t' + p.z.ToString() + '\t' + (p.ID + 1).ToString() + '\t' + p.ParentID));
            sw.Close();

            //SaveSurvey();
            MessageBox.Show("Finished");
        }

        private static void DetectMapperMarkerVisible(int myMarkerID, ref List<clsPGPoint> pts, bool useDatums) {

            double[] myMatrix = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myMarkerID, 0, myMatrix, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);

            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, barcodeID, mv)) {

                OpenTK.Matrix4d matrix = new OpenTK.Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
                var pt = new OpenTK.Vector4d(mv[12], mv[13], mv[14], 0);
                if (!useDatums) {
                    if (myMarkerID < 50) {
                        pt = new OpenTK.Vector4d(140.0f, -45.0f, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    } else if (myMarkerID < 100) {
                        pt = new OpenTK.Vector4d(140.0, 45.0, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    }
                } else {
                    if (myMarkerID - 2 >= 0 && myMarkerID - 2 < 50) {
                        pt = new OpenTK.Vector4d(160.0f, -45.0f, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    } else if (myMarkerID - 2 >= 50 && myMarkerID - 2 < 100) {
                        pt = new OpenTK.Vector4d(160.0, 45.0, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    }
                }

                pts.Add(new clsPGPoint(pt.X, pt.Y, pt.Z, myMarkerID, barcodeID));
            }
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

            LoadAutoSaveData(myFile);

            //Temp fix for levelled markers
            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                for (j = 0; j < ConfirmedMarkers[i].History.Count; j++) {
                    if (ConfirmedMarkers[i].History[j].SeenFromMarkerID == myGFMarkerID) {
                        RelevelMarkerFromGF(ConfirmedMarkers[i].History[j], true);
                    }
                }
            }

            ConfirmedMarkersCopy.Clear();
            for (k = 0; k < 2; k++) {
                mySuspectedMarkers.Clear();

                for (i = 0; i < ConfirmedMarkers.Count; i++) {
                    for (j = 0; j < ConfirmedMarkers[i].History.Count; j++) {
                        mySuspectedMarkers.Add(ConfirmedMarkers[i].History[j].Copy());
                    }
                }
                ConfirmedMarkers.Clear();

                n = -1;
                while (n < ConfirmedMarkers.Count && mySuspectedMarkers.Count > 0) {
                    n = ConfirmedMarkers.Count;
                    ProcessMarkers(true);
                }

                ConfirmedMarkersCopy.Clear();
                for (i = 0; i < ConfirmedMarkers.Count; i++) {
                    ConfirmedMarkersCopy.Add(ConfirmedMarkers[i].Copy());
                }
            }

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                RelevelMarkerFromGF(ConfirmedMarkers[i]);
            }

            SaveSurvey();
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

            LoadAutoSaveData(myFile);

            ConfirmedMarkersCopy.Clear();
            mySuspectedMarkers.Clear();

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                for (j = 0; j < ConfirmedMarkers[i].History.Count; j++) {
                    mySuspectedMarkers.Add(ConfirmedMarkers[i].History[j].Copy());
                    if (mySuspectedMarkers.Last().SeenFromMarkerID == myGFMarkerID) {
                        RelevelMarkerFromGF(mySuspectedMarkers.Last(), true);
                    }
                }
            }
            ConfirmedMarkers.Clear();

            n = -1;
            while (n < ConfirmedMarkers.Count && mySuspectedMarkers.Count > 0) {
                n = ConfirmedMarkers.Count;
                ProcessMarkers(true);
            }

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                RelevelMarkerFromGF(ConfirmedMarkers[i]);
            }

            SaveSurvey();
            MessageBox.Show("Finished");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile;
            int i, j;

            myDlg.InitialDirectory = "C:\\Temp";
            myDlg.Filter = "Dat Files (*.dat)|*.dat";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;


            FileStream sr = File.Open(myFile, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(sr);

            ARParam param = new ARParam();
            param.xsize = byteSwapInt(br.ReadInt32());
            param.ysize = byteSwapInt(br.ReadInt32());
            for (i = 0; i < 3; i++) {
                for (j = 0; j < 4; j++) {
                    param.mat[i, j] = byteSwapDouble(br.ReadDouble());
                }
            }
            for (i = 0; i < 17; i++) {
                if (br.PeekChar() != -1) param.dist_factor[i] = byteSwapDouble(br.ReadDouble());
            }
            br.Close();
            sr.Close();

            System.Diagnostics.Debug.Print("Filename\t" + System.IO.Path.GetFileName(myFile));
            System.Diagnostics.Debug.Print("xSize\t" + param.xsize.ToString());
            System.Diagnostics.Debug.Print("ySize\t" + param.ysize.ToString());
            System.Diagnostics.Debug.Print("Mat[3][4]\t" + param.mat[0, 0].ToString() + "\t" + param.mat[0, 1].ToString() + "\t" + param.mat[0, 2].ToString() + "\t" + param.mat[0, 3].ToString());
            System.Diagnostics.Debug.Print("\t" + param.mat[1, 0].ToString() + "\t" + param.mat[1, 1].ToString() + "\t" + param.mat[1, 2].ToString() + "\t" + param.mat[1, 3].ToString());
            System.Diagnostics.Debug.Print("\t" + param.mat[2, 0].ToString() + "\t" + param.mat[2, 1].ToString() + "\t" + param.mat[2, 2].ToString() + "\t" + param.mat[2, 3].ToString());
            for (i = 0; i < 17; i++) {
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

            mdlEmguDetection.DetectMarkers(myImageFile);
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

            mdlEmguDetection.DetectDatums(myImageFile);
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
            FolderBrowserDialog myDlg = new FolderBrowserDialog();
            DialogResult ret;
            string myFolder;
            List<string> myFiles = new List<string>();
            int nFiles = 0;

            InitGlobals();

            bool USE_DATUMS = false;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\Survey 2";
            //myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\021219";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFolder = myDlg.SelectedPath;

            if (File.Exists(Path.Combine(myFolder, "Calib.dat")) && File.Exists(Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"))) {
                try {
                    File.Delete(Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"));
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            try {
                File.Copy(Path.Combine(myFolder, "Calib.dat"), Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"));
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                return;
            }

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (myFile.ToLower().EndsWith("-debug.png")) {
                    try {
                        File.Delete(myFile);
                    } catch (Exception ex) {
                        string s = ex.ToString();
                    }
                }
                if (Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("survey") && !myFile.ToLower().EndsWith("-adj.png")) {
                    myFiles.Add(myFile);
                    if (myVideoHeight == 0) {
                        Image myImage = Image.FromFile(myFile);
                        myVideoWidth = myImage.Width;
                        myVideoHeight = myImage.Height;
                        myVideoPixelSize = 4;
                    }
                }
            }
            ARToolKitFunctions.Instance.arwInitialiseAR();
            mdlRecognise.StartTracking(myVideoWidth, myVideoHeight);
            string myCameraFile = "data\\calib.dat";
            var arParams = mdlEmguCalibration.LoadCameraFromFile2(myCameraFile);

            myFiles.Sort(new AlphaNumericCompare());

            foreach (string myFile in myFiles) {
                nFiles = nFiles + 1;
                lblStatus.Text = nFiles.ToString() + "/" + myFiles.Count().ToString();
                try {
                    var image = new Image<Gray, byte>(myFile);
                    var size = image.Width * image.Height;
                    byte[] imageBytes = new Byte[size];
                    System.Buffer.BlockCopy(image.Data, 0, imageBytes, 0, size);
                    Application.DoEvents();
                    try {
                        mdlRecognise.RecogniseMarkers(imageBytes);
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
                Console.WriteLine("Processed " + nFiles + " out of " + myFiles.Count + " photos - " + Path.GetFileName(myFile));
            }

            mdlRecognise.ConfirmedMarkers.Sort(new MarkerPointComparer());

            var res = mdlRecognise.SaveToString(false);

            var sw = new System.IO.StreamWriter("C:\\Temp\\Output.txt");
            sw.WriteLine(res);
            sw.Close();

            sw = new System.IO.StreamWriter("C:\\Temp\\points.3dm");
            mdlRecognise.ConfirmedMarkers.ForEach(p => sw.WriteLine(p.Point.x.ToString() + '\t' + p.Point.y.ToString() + '\t' + p.Point.z.ToString() + '\t' + (p.MarkerID + 1).ToString() + '\t' + p.SeenFromMarkerID));
            sw.Close();

            MessageBox.Show("Finished");
        }

        private void btnImportDiagnostics3_Click(object sender, EventArgs e) {
            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myFile, myCalibFile;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\Photogrammetry\\BatchProcess\\Diagnostic Files";
            myDlg.Filter = "Diagnostic Files (*.txt)|*.txt";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFile = myDlg.FileName;

            myDlg.Filter = "Calibration Files (*.dat)|*.dat";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myCalibFile = myDlg.FileName;

            mdlRecognise.LoadSavedDataFile(myFile);
            mdlRecognise.BatchBundleAdjust(myCalibFile);

            MessageBox.Show("Finished");
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

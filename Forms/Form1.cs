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
using static BatchProcess.mdlRecognise;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using static System.Math;

namespace BatchProcess {
    public partial class Form1 : Form {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog myDlg = new FolderBrowserDialog();
            DialogResult ret;
            string myFolder;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFolder = myDlg.SelectedPath;

            int myWidth = 0, myHeight = 0, nImages = 0, nCount = 0;

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (!(myFile.ToLower().EndsWith(".png") || myFile.ToLower().EndsWith(".jpg"))) continue;
                nImages = nImages + 1;
                if (myWidth == 0) {
                    Image myImage = Image.FromFile(myFile);
                    myWidth = myImage.Width;
                    myHeight = myImage.Height;
                }
            }

            // ARToolKitFunctions.Instance.arwInitialiseAR();
            ARToolKitFunctions.Instance.arwInitChessboardCorners(17, 13, 20, 3264, 2448, 20);

            float[] corners = new float[442];
            int cornerCount;
            string cornerFile = "C:\\Temp\\CornersARToolkit.txt";
            if (File.Exists(cornerFile)) {
                try {
                    File.Delete(cornerFile);
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                }
            }

            StreamWriter sw = new StreamWriter(cornerFile);
            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (!(myFile.ToLower().EndsWith(".png") || myFile.ToLower().EndsWith(".jpg"))) continue;
                byte[] imageBytes = ImageToGrayscaleByteArray((Bitmap)Image.FromFile(myFile));

                int result = ARToolKitFunctions.Instance.arwFindChessboardCorners(corners, out cornerCount, imageBytes);

                int l = 0;
                for (int i = 0; i < 17; i++) {
                    for (int j = 0; j < 13; j++) {
                        sw.WriteLine(corners[l * 2].ToString() + '\t' + corners[l * 2 + 1].ToString());
                        l++;
                    }
                }

                if (result == 1) {
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
            sw.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog myDlg = new FolderBrowserDialog();
            DialogResult ret;
            string myFolder;
            List<string> myFiles = new List<string>();
            int nFiles = 0;

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos\\Survey";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myFolder = myDlg.SelectedPath;

            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (myFile.ToLower().EndsWith("-debug.png")) {
                    try {
                        File.Delete(myFile);
                    }
                    catch (Exception ex) {
                        string s = ex.ToString();
                    }
                }
                if (myFile.ToLower().EndsWith(".png")) {
                    myFiles.Add(Path.GetFileName(myFile));
                    if (myVideoHeight == 0) {
                        Image myImage = Image.FromFile(myFile);
                        myVideoWidth = myImage.Width;
                        myVideoHeight = myImage.Height;
                        myVideoPixelSize = 4;
                    }
                }
            }
            ARToolKitFunctions.Instance.arwInitialiseAR();
            StartTracking(myVideoWidth, myVideoHeight);

            //TEMP:
            myVerticalVector.X = -0.00706971850143557;
            myVerticalVector.Y = -0.00875298481130118;
            myVerticalVector.Z = 0.999936700165167;
            myVerticalVector.X = 0.0;
            myVerticalVector.Y = 0.0;
            myVerticalVector.Z = 0.1;

            myFiles.Sort(new AlphaNumericCompare());
            
            foreach (string myFile in myFiles) {
                if (myFile.ToLower().EndsWith(".png")) {
                    byte[] imageBytes = ImageToGrayscaleByteArray((Bitmap)Image.FromFile(myFolder + "\\" + myFile));
                    nFiles = nFiles + 1;
                    lblStatus.Text = nFiles.ToString() + "/" + myFiles.Count().ToString();
                    Application.DoEvents();
                    RecogniseMarkers(imageBytes);
                    Console.WriteLine("Processed " + nFiles + " out of " + myFiles.Count + " photos - " + myFile);

                    //ARToolKitFunctions.Instance.arwSetVideoDebugMode(true);
                    //ARToolKitFunctions.Instance.arwUpdateARToolKit(imageBytes, 2);
                    //Color32[] myPixels = new Color32[myVideoWidth * myVideoHeight];
                    //byte[] myBytes = new byte[myVideoWidth * myVideoHeight * myVideoPixelSize];
                    //ARToolKitFunctions.Instance.arwUpdateTexture32(myPixels);
                    //for (int i = 0; i <= myPixels.GetUpperBound(0); i++) {
                    //    if (myPixels[i].g < 128) {
                    //        myBytes[i * myVideoPixelSize] = 0;
                    //        myBytes[i * myVideoPixelSize + 1] = 0;
                    //        myBytes[i * myVideoPixelSize + 2] = 0;
                    //        myBytes[i * myVideoPixelSize + 3] = 255;
                    //    }
                    //    else {
                    //        myBytes[i * myVideoPixelSize] = 255;
                    //        myBytes[i * myVideoPixelSize + 1] = 255;
                    //        myBytes[i * myVideoPixelSize + 2] = 255;
                    //        myBytes[i * myVideoPixelSize + 3] = 255;
                    //    }
                    //}

                    //Image newImage = ImageFromRawRgbaArray(myBytes, myVideoWidth, myVideoHeight);
                    //if (File.Exists(myFile.ToLower().Replace(".jpg", "-debug.png"))) {
                    //    try {
                    //        File.Delete(myFile.ToLower().Replace(".jpg", "-debug.png"));
                    //    }
                    //    catch (Exception ex) {
                    //        string s = ex.ToString();
                    //    }
                    //}
                    //newImage.Save(myFile.ToLower().Replace(".jpg", "-debug.png"), ImageFormat.Png);
                    //ARToolKitFunctions.Instance.arwSetVideoDebugMode(false);
                }
            }

            //if (mySuspectedMarkers.Count > 0) {
            //    ProcessMarkers(true);
            //}


            ////Now clear all data and re-process:
            //mySuspectedMarkers.Clear();
            //for (int i = 0; i < ConfirmedMarkers.Count; i++) {
            //    for (int j = 0; j < ConfirmedMarkers[i].History.Count; j++) {
            //        mySuspectedMarkers.Add(ConfirmedMarkers[i].History[j].Copy());
            //    }
            //}
            //ConfirmedMarkers.Clear();

            //int n = -1;
            //while (n < ConfirmedMarkers.Count && mySuspectedMarkers.Count > 0) {
            //    n = ConfirmedMarkers.Count;
            //    ProcessMarkers(true);
            //}

            //myVerticalVector.X = -0.00706971850143557;
            //myVerticalVector.Y = -0.00875298481130118;
            //myVerticalVector.Z = 0.999936700165167;

            //for (int i = 0; i < ConfirmedMarkers.Count; i++) {
            //    RelevelMarkerFromGF(ConfirmedMarkers[i]);
            //}

            var pts = new List<clsPGPoint>();
            for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
                DetectMapperMarkerVisible(myMarkerIDs[i], ref pts);
            }

            DetectMapperMarkerVisible(myGFMarkerID, ref pts);
            DetectMapperMarkerVisible(myStepMarkerID, ref pts);
            DetectMapperMarkerVisible(myLeftBulkheadMarkerID, ref pts);
            DetectMapperMarkerVisible(myRightBulkheadMarkerID, ref pts);
            DetectMapperMarkerVisible(myDoorHingeRightMarkerID, ref pts);
            DetectMapperMarkerVisible(myDoorFrameRightMarkerID, ref pts);
            DetectMapperMarkerVisible(myDoorHingeLeftMarkerID, ref pts);
            DetectMapperMarkerVisible(myDoorFrameLeftMarkerID, ref pts);
            DetectMapperMarkerVisible(myObstruct1MarkerID, ref pts);
            DetectMapperMarkerVisible(myObstruct2MarkerID, ref pts);
            DetectMapperMarkerVisible(myObstruct3MarkerID, ref pts);
            DetectMapperMarkerVisible(myObstruct4MarkerID, ref pts);
            DetectMapperMarkerVisible(myWall1MarkerID, ref pts);
            DetectMapperMarkerVisible(myWall2MarkerID, ref pts);
            DetectMapperMarkerVisible(myWall3MarkerID, ref pts);
            DetectMapperMarkerVisible(myWall4MarkerID, ref pts);


            var sw = new System.IO.StreamWriter("C:\\Temp\\points.txt");
            pts.ForEach(p => sw.WriteLine(p.x.ToString() + '\t'+ p.z.ToString() + '\t' + (-p.y).ToString() + '\t' + (p.ID + 1).ToString() + '\t' + p.ParentID));
            sw.Close();

            //SaveSurvey();
            MessageBox.Show("Finished");
        }

        private static void DetectMapperMarkerVisible(int myMarkerID, ref List<clsPGPoint> pts) {

            float[] myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myMarkerID, 0, myMatrix, out float width, out float height, out int imageSizeX, out int imageSizeY, out int barcodeID);

            float[] mv = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, barcodeID, mv)) {

                OpenTK.Matrix4 matrix = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
                var pt = new OpenTK.Vector4(mv[12], mv[13], mv[14], 0);
                if (myMarkerID < 50) {
                    pt = new OpenTK.Vector4(140.0f, -45.0f, 0.0f, 1);
                    pt = OpenTK.Vector4.Transform(pt, matrix);
                } else if (myMarkerID < 100) {
                    pt = new OpenTK.Vector4(140.0f, 45.0f, 0.0f, 1);
                    pt = OpenTK.Vector4.Transform(pt, matrix);
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

        private void button3_Click(object sender, EventArgs e)
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

            myDlg.SelectedPath = "C:\\Customer\\Stannah\\Photogrammetry\\Photos";
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
                if (!(myFile.ToLower().EndsWith(".png") || myFile.ToLower().EndsWith(".jpg"))) continue;
                mdlEmguCalibration.ProcessImage(myFile);
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

        private void btnDetectCircles_Click(object sender, EventArgs e) {
            //mdlEmguDetection.DrawMarkers();

            OpenFileDialog myDlg = new OpenFileDialog();
            DialogResult ret;
            string myImageFile;

            myDlg.InitialDirectory = "C:\\Customer\\Stannah\\PhotoGrammetry\\BatchProcess\\Photos\\iPad 1";
            myDlg.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png";
            ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            myImageFile = myDlg.FileName;

            mdlEmguDetection.DetectDatums(myImageFile);
        }
    }

    class AlphaNumericCompare : IComparer<string> {
        public int Compare(string lhs, string rhs)
        {
            var numExtract = new Regex("[0-9]+");
            int lhsNumber = int.Parse(numExtract.Match(lhs).Value);
            int rhsNumber = int.Parse(numExtract.Match(rhs).Value);
            return lhsNumber.CompareTo(rhsNumber);
        }
    }
    
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu;
using Emgu.CV;
using Emgu.Util;
using System.Drawing;
using static BatchProcess.mdlGlobals;
using System.Runtime.InteropServices;
using System.IO;
using static System.Math;
using Emgu.CV.Structure;
using Microsoft.Office.Interop;
using System.Reflection;

namespace BatchProcess {
    public static class mdlEmguCalibration {

        static Size mBoardSize;
        static float mSquareSize;
        static Size mImageSize;
        static string _outputPath = "";
        static List<PointF[]> allCornerPoints = new List<PointF[]>();
        static int capturedImageNum;
        static int totalImageNum;
        static List<string> myImageFiles = new List<string>();

        static int PD_LOOP = 3;
        static int PD_LOOP2 = 5;

        public static void InitCalibration(string outputPath, int numCornersX, int numCornersY, float squareSize, int numImages) {
            _outputPath = outputPath;
            mBoardSize = new Size(numCornersY, numCornersX);
            mSquareSize = squareSize;
            myImageFiles.Clear();
            allCornerPoints.Clear();
            capturedImageNum = 0;
            totalImageNum = numImages;
        }

        public static void ProcessImage(string myFile)
        {
            bool res;
            //byte[] byteArray = ImageToByteArray(myImage);

            var cornerPoints = new Emgu.CV.Util.VectorOfPointF();
            var image = new Image<Gray, byte>(myFile);
            mImageSize = image.Size;
            Mat imageCopy = Emgu.CV.CvInvoke.Imread(myFile, Emgu.CV.CvEnum.ImreadModes.Color);

            res = CvInvoke.FindChessboardCorners(image, mBoardSize, cornerPoints, Emgu.CV.CvEnum.CalibCbType.FastCheck | Emgu.CV.CvEnum.CalibCbType.AdaptiveThresh | Emgu.CV.CvEnum.CalibCbType.FilterQuads);

            if (res) {
                var cornersCopy = new List<clsPoint>();
                var cornersCopy2 = new List<clsPoint>();
                foreach (var p in cornerPoints.ToArray())  cornersCopy.Add(new clsPoint(p.X, p.Y));
                CvInvoke.CornerSubPix(image, cornerPoints, new Size(5, 5), new Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(100));

                foreach (var p in cornerPoints.ToArray())  cornersCopy2.Add(new clsPoint(p.X, p.Y));

                var cornersErr = new Emgu.CV.Util.VectorOfPointF();
                var cornersErr2 = new Emgu.CV.Util.VectorOfPointF();
                for (int i = 0; i < cornersCopy.Count; i++) {
                    if (cornersCopy[i].Dist(cornersCopy2[i]) > 4.0) {
                        cornersErr.Push(new PointF[] { new PointF((float)cornersCopy[i].x, (float)cornersCopy[i].y) });
                        cornersErr2.Push(new PointF[] { new PointF((float)cornersCopy2[i].x, (float)cornersCopy2[i].y) });
                    }
                }
                if (cornersErr.Size > 0) {
                    mdlEmguDetection.DrawCornersOnImage(imageCopy, cornersErr, System.Drawing.Color.Green);
                    mdlEmguDetection.DrawCornersOnImage(imageCopy, cornersErr2, System.Drawing.Color.Red);
                    CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\Corners-" + Path.GetFileNameWithoutExtension(myFile) + ".png", imageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));
                }

                allCornerPoints.Add(cornerPoints.ToArray());
                myImageFiles.Add(myFile);

                //foreach (PointF cornerPoint in cornerPoints.ToArray()) {
                //    System.Diagnostics.Debug.WriteLine(cornerPoint.X + ", " + cornerPoint.Y);
                //}

                capturedImageNum = capturedImageNum + 1;
                System.Diagnostics.Debug.WriteLine("Processed Image " + capturedImageNum);
            } else {
                capturedImageNum = capturedImageNum + 1;
                System.Diagnostics.Debug.WriteLine("Failed To Process Image " + capturedImageNum);
            }

        }

        private static Image<Gray, byte> RGBA2Grayscale(Image<Rgba, byte> imageIn) {
            Image<Gray, byte> grayImage = new Image<Gray, byte>(new Size(imageIn.Width, imageIn.Height));

            //Convert the pixel to it's luminance using the formula:
            // L = .299*R + .587*G + .114*B
            //Note that ic is the input column and oc is the output column
            for (int r = 0; r < imageIn.Height; r++) {
                for (int c = 0; c < imageIn.Width; c += 1) {
                    grayImage.Data[r, c, 0] = (byte)(int)
                        (0.299f * imageIn.Data[r, c, 2] +
                         0.587f * imageIn.Data[r, c, 1] +
                         0.114f * imageIn.Data[r, c, 0]);
                }
            }
            return grayImage;
        }

        public static void CalibrateCamera(bool saveResult = false)
        {
            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            Mat distortionCoeffs = new Mat(8, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            Mat[] rvecs, tvecs;
            int i, j, k;
            ARParam param = new ARParam();
            List<Emgu.CV.Structure.MCvPoint3D32f> objectPointList;
            List<Emgu.CV.Structure.MCvPoint3D32f[]> objectPoints = new List<Emgu.CV.Structure.MCvPoint3D32f[]>();

            for (k = 0; k < capturedImageNum; k++) {
                objectPointList = new List<Emgu.CV.Structure.MCvPoint3D32f>();
                for (i = 0; i < mBoardSize.Height; i++) {
                    for (j = 0; j < mBoardSize.Width; j++) {
                        objectPointList.Add(new Emgu.CV.Structure.MCvPoint3D32f(j * mSquareSize, i * mSquareSize, 0));
                    }
                }
                objectPoints.Add(objectPointList.ToArray());
            }

            // double error = CvInvoke.CalibrateCamera(objectPoints.ToArray(), allCornerPoints.ToArray(), mImageSize, cameraMatrix, distortionCoeffs,
            //    Emgu.CV.CvEnum.CalibType.FixK3, new Emgu.CV.Structure.MCvTermCriteria(30, 0.1), out rvecs, out tvecs);

            double error = CvInvoke.CalibrateCamera(objectPoints.ToArray(), allCornerPoints.ToArray(), mImageSize, cameraMatrix, distortionCoeffs,
                Emgu.CV.CvEnum.CalibType.RationalModel, new Emgu.CV.Structure.MCvTermCriteria(100), out rvecs, out tvecs);

            double[] cameraArray = new double[9];
            Marshal.Copy(cameraMatrix.DataPointer, cameraArray, 0, 9);
            double[] distCoeffArray = new double[8];
            Marshal.Copy(distortionCoeffs.DataPointer, distCoeffArray, 0, 8);

            double[][] rvecArray = new double[capturedImageNum][];
            double[][] tvecArray = new double[capturedImageNum][];
            for (i = 0; i < capturedImageNum; i ++) {
                rvecArray[i] = new double[3];
                Marshal.Copy(rvecs[i].DataPointer, rvecArray[i], 0, 3);
                tvecArray[i] = new double[3];
                Marshal.Copy(tvecs[i].DataPointer, tvecArray[i], 0, 3);
            }

            param.xsize = mImageSize.Width;
            param.ysize = mImageSize.Height;
            param.dist_function_version = 5;

            for (j = 0; j < 3; j++) {
                for (i = 0; i < 3; i++) {
                    param.mat[j, i] = cameraArray[j * 3 + i];
                }
                param.mat[j, 3] = 0.0;
            }

            param.dist_factor[0] = distCoeffArray[0];     /* k1  */
            param.dist_factor[1] = distCoeffArray[1];     /* k2  */
            param.dist_factor[2] = distCoeffArray[2];     /* p1  */
            param.dist_factor[3] = distCoeffArray[3];     /* p2  */
            param.dist_factor[4] = distCoeffArray[4];     /* k3  */
            param.dist_factor[5] = distCoeffArray[5];     /* k4  */
            param.dist_factor[6] = distCoeffArray[6];     /* k6  */
            param.dist_factor[7] = distCoeffArray[7];     /* k6  */
            param.dist_factor[8] = 0;                     /* s1  */
            param.dist_factor[9] = 0;                     /* s2  */
            param.dist_factor[10] = 0;                    /* s3  */
            param.dist_factor[11] = 0;                    /* s4  */
            param.dist_factor[12] = param.mat[0, 0];      /* fx  */
            param.dist_factor[13] = param.mat[1, 1];      /* fy  */
            param.dist_factor[14] = param.mat[0, 2];      /* x6  */
            param.dist_factor[15] = param.mat[1, 2];      /* cy  */
            param.dist_factor[16] = 1.0;                  /* s   */

            double s = getSizeFactor(param.dist_factor, param.xsize, param.ysize, param.dist_function_version);
            param.mat[0, 0] /= s;
            param.mat[0, 1] /= s;
            param.mat[1, 0] /= s;
            param.mat[1, 1] /= s;
            param.dist_factor[16] = s;

            if (saveResult) {
                saveParam(_outputPath, param);
            }
                
            double totErr = 0;
            double[] results = new double[capturedImageNum];

            for (k = 0; k < capturedImageNum; k++) {
                var objectPoints2 = new Emgu.CV.Util.VectorOfPoint3D32F(objectPoints[k]);
                var imagePoints2 = new Emgu.CV.Util.VectorOfPointF();
                Emgu.CV.CvInvoke.ProjectPoints(objectPoints2, rvecs[k], tvecs[k], cameraMatrix, distortionCoeffs, imagePoints2);
                var imagePoints = new Emgu.CV.Util.VectorOfPointF(allCornerPoints[k]);
                double err = Emgu.CV.CvInvoke.Norm(imagePoints, imagePoints2, Emgu.CV.CvEnum.NormType.L2);

                int n = allCornerPoints[k].Length;
                results[k] = (float)Sqrt(err * err / n);
                System.Diagnostics.Debug.Print(results[k].ToString());
                totErr += err * err;
            }
            totErr = Sqrt(totErr / (mBoardSize.Width * mBoardSize.Height * capturedImageNum));
            System.Diagnostics.Debug.Print(totErr.ToString());
        }

        public static void CalibrateCameraSimple(bool saveResult = false) {
            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            Mat distortionCoeffs = new Mat(8, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            Mat[] rvecs, tvecs;
            int i, j, k;
            ARParam param = new ARParam();
            List<Emgu.CV.Structure.MCvPoint3D32f> objectPointList;
            List<Emgu.CV.Structure.MCvPoint3D32f[]> objectPoints = new List<Emgu.CV.Structure.MCvPoint3D32f[]>();

            for (k = 0; k < capturedImageNum; k++) {
                objectPointList = new List<Emgu.CV.Structure.MCvPoint3D32f>();
                for (i = 0; i < mBoardSize.Height; i++) {
                    for (j = 0; j < mBoardSize.Width; j++) {
                        objectPointList.Add(new Emgu.CV.Structure.MCvPoint3D32f(j * mSquareSize, i * mSquareSize, 0));
                    }
                }
                objectPoints.Add(objectPointList.ToArray());
            }

            // double error = CvInvoke.CalibrateCamera(objectPoints.ToArray(), allCornerPoints.ToArray(), mImageSize, cameraMatrix, distortionCoeffs,
            //    Emgu.CV.CvEnum.CalibType.FixK3, new Emgu.CV.Structure.MCvTermCriteria(30, 0.1), out rvecs, out tvecs);

            double error = CvInvoke.CalibrateCamera(objectPoints.ToArray(), allCornerPoints.ToArray(), mImageSize, cameraMatrix, distortionCoeffs,
                Emgu.CV.CvEnum.CalibType.FixK3, new Emgu.CV.Structure.MCvTermCriteria(100, 0.0000001), out rvecs, out tvecs);

            double[] cameraArray = new double[9];
            Marshal.Copy(cameraMatrix.DataPointer, cameraArray, 0, 9);
            double[] distCoeffArray = new double[4];
            Marshal.Copy(distortionCoeffs.DataPointer, distCoeffArray, 0, 4);

            double[][] rvecArray = new double[capturedImageNum][];
            double[][] tvecArray = new double[capturedImageNum][];
            for (i = 0; i < capturedImageNum; i++) {
                rvecArray[i] = new double[3];
                Marshal.Copy(rvecs[i].DataPointer, rvecArray[i], 0, 3);
                tvecArray[i] = new double[3];
                Marshal.Copy(tvecs[i].DataPointer, tvecArray[i], 0, 3);
            }

            param.xsize = mImageSize.Width;
            param.ysize = mImageSize.Height;
            param.dist_function_version = 4;

            for (j = 0; j < 3; j++) {
                for (i = 0; i < 3; i++) {
                    param.mat[j, i] = cameraArray[j * 3 + i];
                }
                param.mat[j, 3] = 0.0;
            }

            param.dist_factor[0] = distCoeffArray[0];     /* k1  */
            param.dist_factor[1] = distCoeffArray[1];     /* k2  */
            param.dist_factor[2] = distCoeffArray[2];     /* p1  */
            param.dist_factor[3] = distCoeffArray[3];     /* p2  */
            param.dist_factor[4] = param.mat[0, 0];  /* fx  */
            param.dist_factor[5] = param.mat[1, 1];  /* fy  */
            param.dist_factor[6] = param.mat[0, 2];  /* x0  */
            param.dist_factor[7] = param.mat[1, 2];  /* y0  */
            param.dist_factor[8] = 1.0;         /* s   */

            double s = getSizeFactor(param.dist_factor, param.xsize, param.ysize, param.dist_function_version);
            param.mat[0, 0] /= s;
            param.mat[0, 1] /= s;
            param.mat[1, 0] /= s;
            param.mat[1, 1] /= s;
            param.dist_factor[8] = s;

            if (saveResult) {
                saveParamSimple(_outputPath, param);

                //    string cornerFile = "C:\\Temp\\CornersOpenCV.txt";
                //    if (File.Exists(cornerFile)) {
                //        try {
                //            File.Delete(cornerFile);
                //        } catch (Exception ex) {
                //            string str = ex.ToString();
                //        }
                //    }

                //    StreamWriter sw = new StreamWriter(cornerFile);
                //    int l = 0;
                //    for (k = 0; k < capturedImageNum; k++) {
                //        l = 0;
                //        for (i = 0; i < mBoardSize.Height; i++) {
                //            for (j = 0; j < mBoardSize.Width; j++) {
                //                sw.WriteLine(allCornerPoints2[k][l].X.ToString() + '\t' + allCornerPoints2[k][l].Y.ToString());
                //                l++;
                //            }
                //        }
                //    }
                //    sw.Close();
            }

            double totErr = 0;
            double[] results = new double[capturedImageNum];

            for (k = 0; k < capturedImageNum; k++) {
                var objectPoints2 = new Emgu.CV.Util.VectorOfPoint3D32F(objectPoints[k]);
                var imagePoints2 = new Emgu.CV.Util.VectorOfPointF();
                Emgu.CV.CvInvoke.ProjectPoints(objectPoints2, rvecs[k], tvecs[k], cameraMatrix, distortionCoeffs, imagePoints2);
                var imagePoints = new Emgu.CV.Util.VectorOfPointF(allCornerPoints[k]);
                double err = Emgu.CV.CvInvoke.Norm(imagePoints, imagePoints2, Emgu.CV.CvEnum.NormType.L2);

                int n = allCornerPoints[k].Length;
                results[k] = (float)Sqrt(err * err / n);
                System.Diagnostics.Debug.Print(results[k].ToString());
                totErr += err * err;
            }
            totErr = Sqrt(totErr / (mBoardSize.Width * mBoardSize.Height * capturedImageNum));
            System.Diagnostics.Debug.Print(totErr.ToString());
        }

        static void saveParam(string myFile, ARParam param)
        {
            if (File.Exists(myFile)) {
                try {
                    File.Delete(myFile);
                } catch (Exception ex) {
                    string s = ex.ToString();
                }
            }

            FileStream sw = File.Open(myFile, FileMode.CreateNew, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(sw);

            bw.Write(byteSwapInt(param.xsize));
            bw.Write(byteSwapInt(param.ysize));
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 4; j++) {
                    bw.Write(byteSwapDouble(param.mat[i, j]));
                }
            }
            for (int i = 0; i < 17; i++) {
                bw.Write(byteSwapDouble(param.dist_factor[i]));
            }
            bw.Close();
            sw.Close();

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

        static void saveParamSimple(string myFile, ARParam param) {
            if (File.Exists(myFile)) {
                try {
                    File.Delete(myFile);
                } catch (Exception ex) {
                    string s = ex.ToString();
                }
            }

            FileStream sw = File.Open(myFile, FileMode.CreateNew, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(sw);

            bw.Write(byteSwapInt(param.xsize));
            bw.Write(byteSwapInt(param.ysize));
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 4; j++) {
                    bw.Write(byteSwapDouble(param.mat[i, j]));
                }
            }
            for (int i = 0; i < 9; i++) {
                bw.Write(byteSwapDouble(param.dist_factor[i]));
            }
            bw.Close();
            sw.Close();

            System.Diagnostics.Debug.Print("Filename\t" + System.IO.Path.GetFileName(myFile));
            System.Diagnostics.Debug.Print("xSize\t" + param.xsize.ToString());
            System.Diagnostics.Debug.Print("ySize\t" + param.ysize.ToString());
            System.Diagnostics.Debug.Print("Mat[3][4]\t" + param.mat[0, 0].ToString() + "\t" + param.mat[0, 1].ToString() + "\t" + param.mat[0, 2].ToString() + "\t" + param.mat[0, 3].ToString());
            System.Diagnostics.Debug.Print("\t" + param.mat[1, 0].ToString() + "\t" + param.mat[1, 1].ToString() + "\t" + param.mat[1, 2].ToString() + "\t" + param.mat[1, 3].ToString());
            System.Diagnostics.Debug.Print("\t" + param.mat[2, 0].ToString() + "\t" + param.mat[2, 1].ToString() + "\t" + param.mat[2, 2].ToString() + "\t" + param.mat[2, 3].ToString());
            for (int i = 0; i < 9; i++) {
                System.Diagnostics.Debug.Print("dist_factor[" + i.ToString() + "]\t" + param.dist_factor[i].ToString());
            }

        }

        static double getSizeFactor( double[] dist_factor,  int xsize,  int ysize,  int dist_function_version) {
            double ox, oy, ix, iy;
            double olen, ilen;
            double sf, sf1;
            double cx, cy;

            if (dist_function_version == 5) {
                cx = dist_factor[14];
                cy = dist_factor[15];
            } else if (dist_function_version == 4) {
                cx = dist_factor[6];
                cy = dist_factor[7];
            } else {
                return 1;
            }

            sf = 100.0f;
            ox = 0.0f;
            oy = cy;
            olen = cx;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = cx - ix;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            ox = xsize;
            oy = cy;
            olen = xsize - cx;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = ix - cx;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            ox = cx;
            oy = 0.0;
            olen = cy;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = cy - iy;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            ox = cx;
            oy = ysize;
            olen = ysize - cy;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = iy - cy;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }


            ox = 0.0f;
            oy = 0.0f;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = cx - ix;
            olen = cx;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }
            ilen = cy - iy;
            olen = cy;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            ox = xsize;
            oy = 0.0f;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = ix - cx;
            olen = xsize - cx;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }
            ilen = cy - iy;
            olen = cy;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            ox = 0.0f;
            oy = ysize;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = cx - ix;
            olen = cx;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }
            ilen = iy - cy;
            olen = ysize - cy;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            ox = xsize;
            oy = ysize;
            arParamObserv2Ideal(dist_factor, ox, oy, out ix, out iy, dist_function_version);
            ilen = ix - cx;
            olen = xsize - cx;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }
            ilen = iy - cy;
            olen = ysize - cy;
            if (ilen > 0.0f) {
                sf1 = ilen / olen;
                if (sf1<sf) sf = sf1;
            }

            if (sf == 100.0f) sf = 1.0f;

            return sf;
        }

        //static int arParamObserv2Ideal(double[] dist_factor, double ox, double oy, ref double ix, ref double iy, int dist_function_version) {
        //    // OpenCV distortion model, with addition of a scale factor so that
        //    // entire image fits onscreen.
        //    double k1, k2, p1, p2, fx, fy, x0, y0, s;
        //    double px, py, x02, y02;
        //    int i;

        //    k1 = dist_factor[0];
        //    k2 = dist_factor[1];
        //    p1 = dist_factor[2];
        //    p2 = dist_factor[3];
        //    fx = dist_factor[4];
        //    fy = dist_factor[5];
        //    x0 = dist_factor[6];
        //    y0 = dist_factor[7];
        //    s  = dist_factor[8];

        //    px = (ox - x0)/fx;
        //    py = (oy - y0)/fy;

        //    x02 = px* px;
        //        y02 = py* py;

        //    for(i = 1; ; i++ ) {
        //        if (x02 != 0.0 || y02 != 0.0 ) {
        //            px = px - ((1.0 + k1* (x02+y02) + k2* (x02+y02)*(x02+y02))*px + 2.0*p1* px*py + p2* (x02 + y02 + 2.0*x02)-((ox - x0)/fx))/(1.0+k1* (3.0*x02+y02)+k2* (5.0*x02* x02+3.0*x02* y02+y02* y02)+2.0*p1* py+6.0*p2* px);
        //            py = py - ((1.0 + k1* (x02+y02) + k2* (x02+y02)*(x02+y02))*py + p1* (x02 + y02 + 2.0*y02) + 2.0*p2* px*py-((oy - y0)/fy))/(1.0+k1* (x02+3.0*y02)+k2* (x02* x02+3.0*x02* y02+5.0*y02* y02)+6.0*p1* py+2.0*p2* px);
        //        } else {
        //            px = 0.0;
        //            py = 0.0;
        //            break;
        //        }
        //        if(i == 4 ) break;

        //        x02 = px* px;
        //        y02 = py* py;
        //    }


        //    ix = px * fx / s + x0;
        //    iy = py * fy / s + y0;

        //    return 0;
        //}

        public static int arParamObserv2Ideal(double[] dist_factor, double ix, double iy, out double ox, out double oy, int dist_function_version)
        {
            // ----------------------------------------
            if (dist_function_version == 5) {

                // OpenCV 12-factor distortion model, with addition of a scale factor so that
                // entire image fits onscreen.
                double k1, k2, p1, p2, k3, k4, k5, k6, s1, s2, s3, s4, fx, fy, cx, cy, s;
                double x, y, x0, y0;
                int i;

                k1 = dist_factor[0];
                k2 = dist_factor[1];
                p1 = dist_factor[2];
                p2 = dist_factor[3];
                k3 = dist_factor[4];
                k4 = dist_factor[5];
                k5 = dist_factor[6];
                k6 = dist_factor[7];
                s1 = dist_factor[8];
                s2 = dist_factor[9];
                s3 = dist_factor[10];
                s4 = dist_factor[11];
                fx = dist_factor[12];
                fy = dist_factor[13];
                cx = dist_factor[14];
                cy = dist_factor[15];
                s  = dist_factor[16];
        
                x0 = x = (ix - cx)/fx;
                y0 = y = (iy - cy)/fy;
        
                for (i = 0; i<PD_LOOP2; i++) {
                    double r2 = x * x + y * y;
                    double icdist = (1.0 + ((k6 * r2 + k5) * r2 + k4) * r2) / (1.0 + ((k3 * r2 + k2) * r2 + k1) * r2);
                    double deltaX = 2.0 * p1 * x * y + p2 * (r2 + 2.0 * x * x) + s1 * r2 + s2 * r2 * r2;
                    double deltaY = p1 * (r2 + 2.0 * y * y) + 2.0 * p2 * x * y + s3 * r2 + s4 * r2 * r2;
                    x = (x0 - deltaX)*icdist;
                    y = (y0 - deltaY)*icdist;
                }

                ox = x * fx / s + cx;
                oy = y * fy / s + cy;
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 4) {

                // OpenCV distortion model, with addition of a scale factor so that
                // entire image fits onscreen.
                double k1, k2, p1, p2, fx, fy, cx, cy, s;
                double x, y, x0, y0;
                int i;

                k1 = dist_factor[0];
                k2 = dist_factor[1];
                p1 = dist_factor[2];
                p2 = dist_factor[3];
                fx = dist_factor[4];
                fy = dist_factor[5];
                cx = dist_factor[6];
                cy = dist_factor[7];
                s  = dist_factor[8];
        
                x0 = x = (ix - cx)/fx;
                y0 = y = (iy - cy)/fy;
        
                for (i = 0; i<PD_LOOP2; i++) {
                    if (x == 0.0 && y == 0.0) break;
                    double r2 = x * x + y * y;
                    double icdist = 1.0 / (1.0 + (k2 * r2 + k1) * r2);
                    double deltaX = 2.0 * p1 * x * y + p2 * (r2 + 2.0 * x * x);
                    double deltaY = p1 * (r2 + 2.0 * y * y) + 2.0 * p2 * x * y;
                    x = (x0 - deltaX)* icdist;
                    y = (y0 - deltaY)* icdist;
                }


                ox = x * fx / s + cx;
                oy = y * fy / s + cy;

                ox = x * fx / s + cx;
                oy = y * fy / s + cy;
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 3) {

                double z02, z0, p1, p2, q, z, px, py, ar;
                int i;

                ar = dist_factor[3];
                px = (ix - dist_factor[0]) / ar;
                py =  iy - dist_factor[1];
                p1 = dist_factor[4]/100000000.0;
                p2 = dist_factor[5]/100000000.0/100000.0;
                z02 = px * px + py * py;
                q = z0 = Math.Sqrt(px * px + py * py);
        
                for (i = 1; ; i++) {
                    if (z0 != 0.0) {
                        z = z0 - ((1.0 - p1* z02 - p2* z02* z02)* z0 - q) / (1.0 - 3.0* p1* z02 - 5.0* p2* z02* z02);
                        px = px* z / z0;
                        py = py* z / z0;
                    } else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == PD_LOOP) break;
            
                    z02 = px* px+ py* py;
                    z0 = Math.Sqrt(px * px + py * py);
                }


                ox = px / dist_factor[2] + dist_factor[0];
                oy = py / dist_factor[2] + dist_factor[1];
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 2) {

                double z02, z0, p1, p2, q, z, px, py;
                int i;

                px = ix - dist_factor[0];
                py = iy - dist_factor[1];
                p1 = dist_factor[3]/100000000.0;
                p2 = dist_factor[4]/100000000.0/100000.0;
                z02 = px* px+ py* py;
                q = z0 = Math.Sqrt(px * px + py * py);
        
                for (i = 1; ; i++) {
                    if (z0 != 0.0) {
                        z = z0 - ((1.0 - p1* z02 - p2* z02* z02)* z0 - q) / (1.0 - 3.0* p1* z02 - 5.0* p2* z02* z02);
                        px = px* z / z0;
                        py = py* z / z0;
                    } else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == PD_LOOP) break;
            
                    z02 = px * px + py * py;
                    z0 = Math.Sqrt(px * px + py * py);
                }


                ox = px / dist_factor[2] + dist_factor[0];
                oy = py / dist_factor[2] + dist_factor[1];
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 1) {

                double z02, z0, p, q, z, px, py;
                int i;

                px = ix - dist_factor[0];
                py = iy - dist_factor[1];
                p = dist_factor[3]/100000000.0;
                z02 = px * px + py * py;
                q = z0 = Math.Sqrt(px * px + py* py);
        
                for (i = 1; ; i++) {
                    if (z0 != 0.0) {
                        z = z0 - ((1.0 - p* z02)* z0 - q) / (1.0 - 3.0* p* z02);
                        px = px * z / z0;
                        py = py * z / z0;
                    } else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == PD_LOOP) break;
            
                    z02 = px * px + py * py;
                    z0 = Math.Sqrt(px * px+ py * py);
                }


                ox = px / dist_factor[2] + dist_factor[0];
                oy = py / dist_factor[2] + dist_factor[1];
        
                return 0;
        
                // ----------------------------------------
            } else {

                ox = 0;
                oy = 0;
                return -1;
        
            }
        }

        //static int arParamIdeal2Observ(double[] dist_factor, double ix, double iy, ref double ox, ref double oy, int dist_function_version) {
        //    double k1, k2, p1, p2, fx, fy, x0, y0, s;
        //    double l, x, y;

        //    k1 = dist_factor[0];
        //    k2 = dist_factor[1];
        //    p1 = dist_factor[2];
        //    p2 = dist_factor[3];
        //    fx = dist_factor[4];
        //    fy = dist_factor[5];
        //    x0 = dist_factor[6];
        //    y0 = dist_factor[7];
        //    s = dist_factor[8];

        //    x = (ix - x0) * s / fx;
        //    y = (iy - y0) * s / fy;
        //    l = x * x + y * y;
        //    ox = (x * (1.0 + k1 * l + k2 * l * l) + 2.0 * p1 * x * y + p2 * (l + 2.0 * x * x)) * fx + x0;
        //    oy = (y * (1.0 + k1 * l + k2 * l * l) + p1 * (l + 2.0 * y * y) + 2.0 * p2 * x * y) * fy + y0;

        //    return 0;
        //}


        public static int arParamIdeal2Observ(double[] dist_factor, double ix, double iy, out double ox, out double oy, int dist_function_version)
        {
            // ----------------------------------------
            if (dist_function_version == 5) {

                double k1, k2, p1, p2, k3, k4, k5, k6, s1, s2, s3, s4, fx, fy, cx, cy, s;
                double l, x, y;

                k1 = dist_factor[0];
                k2 = dist_factor[1];
                p1 = dist_factor[2];
                p2 = dist_factor[3];
                k3 = dist_factor[4];
                k4 = dist_factor[5];
                k5 = dist_factor[6];
                k6 = dist_factor[7];
                s1 = dist_factor[8];
                s2 = dist_factor[9];
                s3 = dist_factor[10];
                s4 = dist_factor[11];
                fx = dist_factor[12];
                fy = dist_factor[13];
                cx = dist_factor[14];
                cy = dist_factor[15];
                s  = dist_factor[16];
        
                x = (ix - cx)*s/fx;
                y = (iy - cy)*s/fy;
                l = x* x + y* y;
                ox = (x*(1.0 + k1* l + k2* l*l + k3* l*l* l)/(1.0 + k4* l + k5* l*l + k6* l*l* l) + 2.0*p1* x*y + p2* (l + 2.0*x* x) + s1* l + s2* l*l)*fx + cx;
                oy = (y*(1.0 + k1* l + k2* l*l + k3* l*l* l)/(1.0 + k4* l + k5* l*l + k6* l*l* l) + p1* (l + 2.0*y* y) + 2.0*p2* x*y + s3* l + s4* l*l)*fy + cy;
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 4) {

                double k1, k2, p1, p2, fx, fy, cx, cy, s;
                double l, x, y;

                k1 = dist_factor[0];
                k2 = dist_factor[1];
                p1 = dist_factor[2];
                p2 = dist_factor[3];
                fx = dist_factor[4];
                fy = dist_factor[5];
                cx = dist_factor[6];
                cy = dist_factor[7];
                s  = dist_factor[8];
        
                x = (ix - cx)* s/fx;
                y = (iy - cy)* s/fy;
                l = x* x + y* y;
                ox = (x * (1.0 + k1 * l + k2 * l * l) + 2.0 * p1 * x * y + p2 * (l + 2.0 * x * x)) * fx + cx;
                oy = (y * (1.0 + k1 * l + k2 * l * l) + p1 * (l + 2.0 * y * y) + 2.0 * p2 * x * y) * fy + cy;
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 3) {

                double x, y, l, d, ar;

                ar = dist_factor[3];
                x = (ix - dist_factor[0]) * dist_factor[2];
                y = (iy - dist_factor[1]) * dist_factor[2];
                if (x == 0.0 && y == 0.0) {
                    ox = dist_factor[0];
                    oy = dist_factor[1];
                } else {
                    l = x* x + y* y;
                    d = 1.0 - dist_factor[4]/100000000.0 * l - dist_factor[5]/100000000.0/100000.0 * l * l;
                    ox = x * d * ar + dist_factor[0];
                    oy = y * d + dist_factor[1];
                }
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 2) {

                double x, y, l, d;

                x = (ix - dist_factor[0]) * dist_factor[2];
                y = (iy - dist_factor[1]) * dist_factor[2];
                if (x == 0.0 && y == 0.0) {
                    ox = dist_factor[0];
                    oy = dist_factor[1];
                } else {
                    l = x* x + y* y;
                    d = 1.0 - dist_factor[3]/100000000.0 * l - dist_factor[4]/100000000.0/100000.0 * l * l;
                    ox = x * d + dist_factor[0];
                    oy = y * d + dist_factor[1];
                }
        
                return 0;
        
                // ----------------------------------------
            } else if (dist_function_version == 1) {

                double x, y, d;

                x = (ix - dist_factor[0]) * dist_factor[2];
                y = (iy - dist_factor[1]) * dist_factor[2];
                if (x == 0.0 && y == 0.0) {
                    ox = dist_factor[0];
                    oy = dist_factor[1];
                } else {
                    d = 1.0 - dist_factor[3]/100000000.0 * (x* x+y* y);
                    ox = x * d + dist_factor[0];
                    oy = y * d + dist_factor[1];
                }
        
                return 0;
                // ----------------------------------------
            } else {
                ox = 0;
                oy = 0;
                return -1;
            }
        }


        public static void Undistort(string myFile, string myImageFile)
        {
            int i, j;
            var grayImage = new Image<Gray, byte>(myImageFile);
            Mat myImage = Emgu.CV.CvInvoke.Imread(myImageFile, Emgu.CV.CvEnum.ImreadModes.Color);

            DrawCornersOnImage(myImage, grayImage, Path.GetDirectoryName(myImageFile) + "\\" + System.IO.Path.GetFileNameWithoutExtension(myImageFile) + "-Distorted.png", out Emgu.CV.Util.VectorOfPointF cornerPoints);

            ARParam param = LoadCameraFromFile(myFile);

            //Emgu.CV.Util.VectorOfPointF newCornerPoints = new Emgu.CV.Util.VectorOfPointF();
            //for (i = 0; i < cornerPoints.Size; i++) {
            //    arParamObserv2Ideal(param.dist_factor, cornerPoints[i].X, cornerPoints[i].Y, out double ix, out double iy, 5);
            //    newCornerPoints.Push(new PointF[] { new PointF((float)ix, (float)iy) });
            //    //newCornerPoints.Push(new PointF[] { cornerPoints[i] });
            //}

            Mat outImage = myImage.Clone();
            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            int nFactors = 8;
            Mat distortionCoeffs = new Mat(nFactors, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            double[] cameraArray = new double[9];
            for (j = 0; j < 3; j++) {
                for (i = 0; i < 3; i++) {
                    cameraArray[j * 3 + i] = param.mat[j, i];
                }
            }
            double[] distCoeffArray = new double[nFactors];
            for (i = 0; i < nFactors; i++) {
                distCoeffArray[i] = param.dist_factor[i];
            }
            Marshal.Copy(cameraArray, 0, cameraMatrix.DataPointer, 9);
            Marshal.Copy(distCoeffArray, 0, distortionCoeffs.DataPointer, nFactors);
            CalculateProjectionErrorsForImage(myImage, cameraMatrix, distortionCoeffs, param);
            CvInvoke.Undistort(myImage, outImage, cameraMatrix, distortionCoeffs);
            var image = outImage.ToImage<Emgu.CV.Structure.Rgba, byte>();
            var newFileName = Path.GetDirectoryName(myImageFile) + "\\" + System.IO.Path.GetFileNameWithoutExtension(myImageFile) + "-Undistorted.png";
            CvInvoke.Imwrite(newFileName, image, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 5));
            grayImage = new Image<Gray, byte>(newFileName);
            // grayImage = RGBA2Grayscale(image);

            DrawCornersOnImage(outImage, grayImage, newFileName, out cornerPoints);
        }

        public static ARParam LoadCameraFromFile(string myFile) {
            FileStream sr = File.Open(myFile, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(sr);

            ARParam param = new ARParam();
            param.xsize = byteSwapInt(br.ReadInt32());
            param.ysize = byteSwapInt(br.ReadInt32());
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 4; j++) {
                    param.mat[i, j] = byteSwapDouble(br.ReadDouble());
                }
            }
            for (int i = 0; i < 17; i++) {
                param.dist_factor[i] = byteSwapDouble(br.ReadDouble());
            }
            br.Close();
            sr.Close();

            double s = param.dist_factor[16];
            //param.mat[0, 0] *= s;
            //param.mat[0, 1] *= s;
            //param.mat[1, 0] *= s;
            //param.mat[1, 1] *= s;
            //param.dist_factor[12] /= s;
            //param.dist_factor[13] /= s;
            //param.dist_factor[14] /= s;
            //param.dist_factor[15] /= s;
            //param.dist_factor[16] = 1.0;
            param.dist_function_version = 5;

            return param;
        }

        public static void UndistortSimple(string myFile, string myImageFile) {
            int i, j;
            var grayImage = new Image<Gray, byte>(myImageFile);
            Mat myImage = Emgu.CV.CvInvoke.Imread(myImageFile, Emgu.CV.CvEnum.ImreadModes.Color);

            DrawCornersOnImage(myImage, grayImage, Path.GetDirectoryName(myImageFile) + "\\" + System.IO.Path.GetFileNameWithoutExtension(myImageFile) + "-Distorted.jpg", out Emgu.CV.Util.VectorOfPointF cornerPoints);

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
            for (i = 0; i < 9; i++) {
                param.dist_factor[i] = byteSwapDouble(br.ReadDouble());
            }
            br.Close();
            sr.Close();

            double s = param.dist_factor[8];
            param.mat[0, 0] *= s;
            param.mat[0, 1] *= s;
            param.mat[1, 0] *= s;
            param.mat[1, 1] *= s;
            param.dist_factor[4] /= s;
            param.dist_factor[5] /= s;
            param.dist_factor[6] /= s;
            param.dist_factor[7] /= s;
            param.dist_factor[8] = 1.0;
            param.dist_function_version = 4;

            //Emgu.CV.Util.VectorOfPointF newCornerPoints = new Emgu.CV.Util.VectorOfPointF();
            //for (i = 0; i < cornerPoints.Size; i++) {
            //    arParamObserv2Ideal(param.dist_factor, cornerPoints[i].X, cornerPoints[i].Y, out double ix, out double iy, 5);
            //    newCornerPoints.Push(new PointF[] { new PointF((float)ix, (float)iy) });
            //    //newCornerPoints.Push(new PointF[] { cornerPoints[i] });
            //}

            Mat outImage = myImage.Clone();
            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            int nFactors = 4;
            Mat distortionCoeffs = new Mat(nFactors, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            double[] cameraArray = new double[9];
            for (j = 0; j < 3; j++) {
                for (i = 0; i < 3; i++) {
                    cameraArray[j * 3 + i] = param.mat[j, i];
                }
            }
            double[] distCoeffArray = new double[nFactors];
            for (i = 0; i < nFactors; i++) {
                distCoeffArray[i] = param.dist_factor[i];
            }
            Marshal.Copy(cameraArray, 0, cameraMatrix.DataPointer, 9);
            Marshal.Copy(distCoeffArray, 0, distortionCoeffs.DataPointer, nFactors);
            CalculateProjectionErrorsForImage(myImage, cameraMatrix, distortionCoeffs, param);
            CvInvoke.Undistort(myImage, outImage, cameraMatrix, distortionCoeffs);
            var image = outImage.ToImage<Emgu.CV.Structure.Rgba, byte>();
            grayImage = RGBA2Grayscale(image);

            DrawCornersOnImage(outImage, grayImage, Path.GetDirectoryName(myImageFile) + "\\" + System.IO.Path.GetFileNameWithoutExtension(myImageFile) + "-Undistorted.jpg", out cornerPoints);
        }

        static void DrawCornersOnImage(Mat image, Image<Gray, byte> grayImage, string outFileName, out Emgu.CV.Util.VectorOfPointF cornerPoints, Emgu.CV.Util.VectorOfPointF cornersToDraw = null) {
            bool res;

            Mat imageCopy = image.Clone();
            cornerPoints = new Emgu.CV.Util.VectorOfPointF();
            mBoardSize = new Size(13, 17);
            res = CvInvoke.FindChessboardCorners(grayImage, mBoardSize, cornerPoints);

            if (res) {
                CvInvoke.CornerSubPix(grayImage, cornerPoints, new Size(5, 5), new Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(100, 0.1));
                for (int i = 0; i < 17; i++) {
                    CvInvoke.Line(imageCopy, new Point((int)cornerPoints[i * 13].X, (int)cornerPoints[i * 13].Y), new Point((int)cornerPoints[i * 13 + 12].X, (int)cornerPoints[i * 13 + 12].Y), new Bgr(System.Drawing.Color.Red).MCvScalar, 1);
                }
                for (int i = 0; i < 13; i++) {
                    CvInvoke.Line(imageCopy, new Point((int)cornerPoints[i].X, (int)cornerPoints[i].Y), new Point((int)cornerPoints[i + 208].X, (int)cornerPoints[i + 208].Y), new Bgr(System.Drawing.Color.Red).MCvScalar, 1);
                }
            }

            if (cornersToDraw != null) {
                for (int i = 0; i < cornersToDraw.Size; i++) {
                    CvInvoke.Line(imageCopy, new Point((int)cornersToDraw[i].X, (int)cornersToDraw[i].Y), new Point((int)cornersToDraw[i].X, (int)cornersToDraw[i].Y), new Bgr(System.Drawing.Color.Green).MCvScalar, 4);
                }
            }

            CvInvoke.Imwrite(outFileName, imageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 5));
        }

        static double CalculateProjectionErrorsForImage(Mat image ,Mat cameraMatrix, Mat distortionCoeffs, ARParam param) {

            Mat rvec = new Mat();
            Mat tvec = new Mat();
            Mat rotationMatrix = new Mat();

            Mat imageCopy = image.Clone();
            Emgu.CV.Util.VectorOfPointF cornerPoints = new Emgu.CV.Util.VectorOfPointF();

            mBoardSize = new Size(13, 17);
            mSquareSize = 20;
            var greyImage = imageCopy.ToImage<Emgu.CV.Structure.Gray, byte>();
            bool res = CvInvoke.FindChessboardCorners(greyImage, mBoardSize, cornerPoints);

            List<MCvPoint3D32f> objectPointList;
            MCvPoint3D32f[] objectPoints;

            objectPointList = new List<Emgu.CV.Structure.MCvPoint3D32f>();
            for (int i = 0; i < mBoardSize.Height; i++) {
                for (int j = 0; j < mBoardSize.Width; j++) {
                    objectPointList.Add(new MCvPoint3D32f(j * mSquareSize, i * mSquareSize, 0));
                }
            }
            objectPoints = objectPointList.ToArray();

            Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbooks xlWbs = xlApp.Workbooks;
            Microsoft.Office.Interop.Excel.Workbook xlWb = xlWbs.Add();
            Microsoft.Office.Interop.Excel.Worksheet xlSheet = (Microsoft.Office.Interop.Excel.Worksheet)xlWb.ActiveSheet;
            Microsoft.Office.Interop.Excel.Range xlRange;
            Microsoft.Office.Interop.Excel.Interior xlInterior = null;

            double totErr = 0;
            if (res) {
                CvInvoke.CornerSubPix(greyImage, cornerPoints, new Size(5, 5), new Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(100, 0.1));
                CvInvoke.SolvePnP(objectPoints, cornerPoints.ToArray(), cameraMatrix, distortionCoeffs, rvec, tvec);
                CvInvoke.Rodrigues(rvec, rotationMatrix);

                double[,] trans = new double[3, 4];
                double[] rotationMatrixArray = new double[12];
                Marshal.Copy(rotationMatrix.DataPointer, rotationMatrixArray, 0, 12);
                double[] translationMatrixArray = new double[3];
                Marshal.Copy(tvec.DataPointer, translationMatrixArray, 0, 3);
                for (int j = 0; j < 3; j++) {
                    for (int i = 0; i < 3; i++) {
                        trans[j, i] = rotationMatrixArray[3 * j + i];
                    }
                    trans[j, 3] = (float)translationMatrixArray[j];
                }

                int nRow = 2;

                xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[1, 1];
                xlRange.Value = "delta X";
                xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[1, 2];
                xlRange.Value = "delta Y";
                xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[1, 3];
                xlRange.Value = "X";
                xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[1, 4];
                xlRange.Value = "Y";
                xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                for (int i = 1; i <= 17; i++) {
                    xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[1, i + 8];
                    xlRange.Value = i.ToString();
                    xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                }
                for (int i = 1; i <= 13; i++) {
                    xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[i + 1, 8];
                    xlRange.Value = i.ToString();
                    xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignRight;
                }

                for (int i = 0; i < mBoardSize.Height; i++) {
                    for (int j = 0; j < mBoardSize.Width; j++) {
                        float x = objectPoints[i * mBoardSize.Width + j].X;
                        float y = objectPoints[i * mBoardSize.Width + j].Y;
                        double cx = trans[0, 0] * x + trans[0, 1] * y + trans[0, 3];
                        double cy = trans[1, 0] * x + trans[1, 1] * y + trans[1, 3];
                        double cz = trans[2, 0] * x + trans[2, 1] * y + trans[2, 3];
                        double hx = param.mat[0, 0] * cx + param.mat[0, 1] * cy + param.mat[0, 2] * cz + param.mat[0, 3];
                        double hy = param.mat[1, 0] * cx + param.mat[1, 1] * cy + param.mat[1, 2] * cz + param.mat[1, 3];
                        double h = param.mat[2, 0] * cx + param.mat[2, 1] * cy + param.mat[2, 2] * cz + param.mat[2, 3];
                        if (h == 0.0) continue;
                        double sx = hx / h;
                        double sy = hy / h;
                        arParamIdeal2Observ(param.dist_factor, sx, sy, out double ox, out double oy, param.dist_function_version);
                        sx = cornerPoints[i * mBoardSize.Width + j].X;
                        sy = cornerPoints[i * mBoardSize.Width + j].Y;
                        System.Diagnostics.Debug.Print((ox - sx).ToString("0.000") + "\t" + (oy - sy).ToString("0.000"));
                        double err = (ox - sx) * (ox - sx) + (oy - sy) * (oy - sy);
                        totErr += err;
                        err = Math.Sqrt(err);
                        xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[j + 2, i + 9];
                        xlRange.Value = err;
                        xlInterior = xlRange.Interior;
                        xlInterior.Pattern = Microsoft.Office.Interop.Excel.XlPattern.xlPatternSolid;
                        xlInterior.PatternColorIndex = Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic;
                        xlInterior.Color = System.Drawing.ColorTranslator.ToOle(ConvertIntToColor((int)(100.0 * err / 3.0)));
                        xlInterior.TintAndShade = 0;
                        xlInterior.PatternTintAndShade = 0;

                        xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[nRow, 1];
                        xlRange.Value = (ox - sx);
                        xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[nRow, 2];
                        xlRange.Value = (oy - sy);
                        xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[nRow, 3];
                        xlRange.Value = sx;
                        xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[nRow, 4];
                        xlRange.Value = sx;
                        nRow = nRow + 1;
                    }
                }
            }
            totErr = Math.Sqrt(totErr / (mBoardSize.Width * mBoardSize.Height));
            System.Diagnostics.Debug.Print("Total Projection Error: " + totErr.ToString("0.000"));
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[1, 6];
            xlRange.Value = "Projection Error:";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[2, 6];
            xlRange.Value = totErr;

            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[16, 9];
            xlRange.Value = "Interior Red:";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[17, 9];
            xlRange.Formula = "=COUNTIF($J$3:$X$13,\">2\")";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[16, 11];
            xlRange.Value = "Red:";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[17, 11];
            xlRange.Formula = "=COUNTIF($I$2:$Y$14,\">2\")";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[16, 13];
            xlRange.Value = "Orange:";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[17, 13];
            xlRange.Formula = "=COUNTIFS($I$2:$Y$14, \">1\", $I$2:$Y$14, \"<2\")";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[16, 15];
            xlRange.Value = "Blue(ish):";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[17, 15];
            xlRange.Formula = "=COUNTIFS($I$2:$Y$14, \">0.5\", $I$2:$Y$14, \"<1\")";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[16, 17];
            xlRange.Value = "Blue:";
            xlRange = (Microsoft.Office.Interop.Excel.Range)xlSheet.Cells[17, 17];
            xlRange.Formula = "=COUNTIF($I$2:$Y$14,\"<0.5\")";

            //Insert chart

            xlRange = xlSheet.Range["F20"];
            xlRange.Select();
            xlRange = xlSheet.Range["A2:B222"];
            Microsoft.Office.Interop.Excel.Shapes xlShapes = xlSheet.Shapes;
            Microsoft.Office.Interop.Excel.Shape xlShape = xlShapes.AddChart();
            xlShape.Select();
            Microsoft.Office.Interop.Excel.Chart xlChart = xlApp.ActiveChart;
            xlChart.HasTitle = true;
            Microsoft.Office.Interop.Excel.ChartTitle xlTitle = xlChart.ChartTitle;
            xlTitle.Text = "Residual Calibration Error per Corner";
            xlChart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xlXYScatter;
            xlChart.ChartStyle = 244;
            Microsoft.Office.Interop.Excel.Legend xlLegend = xlChart.Legend;
            xlLegend.Clear();
            xlChart.SetSourceData(xlRange);
            xlRange = xlSheet.Range["F20"];
            xlShape.Top = (float)((double)xlRange.Top);
            xlShape.Left = (float)((double)xlRange.Left);
            xlRange = xlSheet.Range["F3"];
            xlRange.Select();

            xlApp.Visible = true;

            // Cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (xlInterior != null) Marshal.FinalReleaseComObject(xlInterior);
            Marshal.FinalReleaseComObject(xlTitle);
            Marshal.FinalReleaseComObject(xlLegend);
            Marshal.FinalReleaseComObject(xlChart);
            Marshal.FinalReleaseComObject(xlShape);
            Marshal.FinalReleaseComObject(xlShapes);
            Marshal.FinalReleaseComObject(xlRange);
            Marshal.FinalReleaseComObject(xlSheet);
            Marshal.FinalReleaseComObject(xlWb);
            Marshal.FinalReleaseComObject(xlWbs);
            Marshal.FinalReleaseComObject(xlApp);

            return totErr;
        }

        private static System.Drawing.Color ConvertIntToColor(int t) { // 0 <= t <= 100
            Bitmap temperature = (Bitmap)Properties.Resources.Temperature;
            System.Drawing.Color c = System.Drawing.Color.Black;
            if (t < 0) t = 0;
            if (t > temperature.Width - 1) t = temperature.Width - 1;
            return temperature.GetPixel(t, 10);
        }

    }
}

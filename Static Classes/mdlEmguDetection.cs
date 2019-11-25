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
using static BatchProcess.mdlRecognise;
using System.Drawing.Imaging;
using OpenTK;
using static BatchProcess.mdlEmguCalibration;
using static ARToolKitFunctions;

namespace BatchProcess {
    public static class mdlEmguDetection {

        static Logger myLogger;

        public static void DrawMarkers()
        {
            int i;

            Emgu.CV.Aruco.Dictionary myDict = new Emgu.CV.Aruco.Dictionary(Emgu.CV.Aruco.Dictionary.PredefinedDictionaryName.Dict4X4_1000);

            for (i = 0; i < 1000; i++) {
                Mat img = new Mat(6, 6, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
                Emgu.CV.Aruco.ArucoInvoke.DrawMarker(myDict, i, 64, img);
                img.Save("C:\\Temp\\Aruco Markers HiRes\\Marker" + (i + 1).ToString("0000") + ".png");
            }
        }
        
        public static void DoDetection(string myCalibFile, string myImageFile)
        {
            int i, j;
            Mat myImage = Emgu.CV.CvInvoke.Imread(myImageFile, Emgu.CV.CvEnum.ImreadModes.Color);

            FileStream sr = File.Open(myCalibFile, FileMode.Open, FileAccess.Read);
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
            
            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            Mat distortionCoeffs = new Mat(4, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            double[] cameraArray = new double[9];
            for (j = 0; j < 3; j++) {
                for (i = 0; i < 3; i++) {
                    cameraArray[j * 3 + i] = param.mat[j, i];
                }
            }
            double[] distCoeffArray = new double[4];
            for (i = 0; i < 4; i++) {
                distCoeffArray[i] = param.dist_factor[i];
            }
            Marshal.Copy(cameraArray, 0, cameraMatrix.DataPointer, 9);
            Marshal.Copy(distCoeffArray, 0, distortionCoeffs.DataPointer, 4);

            Emgu.CV.Aruco.Dictionary myDict = new Emgu.CV.Aruco.Dictionary(Emgu.CV.Aruco.Dictionary.PredefinedDictionaryName.Dict4X4_1000);
            Emgu.CV.Aruco.GridBoard myBoard = new Emgu.CV.Aruco.GridBoard(2, 1, 0.08f, 0.005f, myDict, 33);


            Mat mappedImage = myImage.Clone();
            CvInvoke.Undistort(myImage, mappedImage, cameraMatrix, distortionCoeffs);

            //Mat rvecs = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv32F, 1);
            //Mat tvecs = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv32F, 1);
            
            Emgu.CV.Aruco.DetectorParameters myParams = Emgu.CV.Aruco.DetectorParameters.GetDefault();
            myParams.CornerRefinementMethod = Emgu.CV.Aruco.DetectorParameters.RefinementMethod.Subpix;
            //myParams.AdaptiveThreshWinSizeStep = 10;
            //myParams.AdaptiveThreshWinSizeMax = 23;
            //myParams.AdaptiveThreshWinSizeMin = 3;
            //myParams.MaxMarkerPerimeterRate = 4.0;
            //myParams.MinMarkerPerimeterRate = 0.03;
            //myParams.AdaptiveThreshConstant = 7;
            //myParams.PolygonalApproxAccuracyRate = 0.1;
            //myParams.MinCornerDistanceRate = 0.05;
            //myParams.MinDistanceToBorder = 3;
            //myParams.MinMarkerDistanceRate = 0.05;
            //myParams.CornerRefinementMinAccuracy = 0.1;
            //myParams.CornerRefinementWinSize = 5;
            //myParams.CornerRefinementMaxIterations = 30;
            myParams.MarkerBorderBits = 2;
            //myParams.PerspectiveRemoveIgnoredMarginPerCell = 0.13;
            //myParams.PerspectiveRemovePixelPerCell = 8;
            //myParams.MaxErroneousBitsInBorderRate = 0.35;
            //myParams.MinOtsuStdDev = 5.0;
            //myParams.ErrorCorrectionRate = 0.6;

            using (Emgu.CV.Util.VectorOfInt ids = new Emgu.CV.Util.VectorOfInt())
            using (Emgu.CV.Util.VectorOfVectorOfPointF corners = new Emgu.CV.Util.VectorOfVectorOfPointF())
            using (Emgu.CV.Util.VectorOfVectorOfPointF rejected = new Emgu.CV.Util.VectorOfVectorOfPointF()) {
                Emgu.CV.Aruco.ArucoInvoke.DetectMarkers(mappedImage, myDict, corners, ids, myParams, rejected);

                if (ids.Size > 0) {
                    //Emgu.CV.Aruco.ArucoInvoke.RefineDetectedMarkers(mappedImage, myBoard, corners, ids, rejected, null, null, 10, 3, true, null, myParams);
                    using (Mat rvecs = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv32F, 1))
                    using (Mat tvecs = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv32F, 1)) {
                        Emgu.CV.Aruco.ArucoInvoke.EstimatePoseSingleMarkers(corners, 0.08f, cameraMatrix, distortionCoeffs, rvecs, tvecs);
                        for (i = 0; i < rvecs.Rows; i++) {
                            Emgu.CV.Aruco.ArucoInvoke.DrawAxis(mappedImage, cameraMatrix, distortionCoeffs, rvecs.Row(i), tvecs.Row(i), 0.05f);
                        }
                    }

                    Emgu.CV.Aruco.ArucoInvoke.DrawDetectedMarkers(mappedImage, corners, ids, new Emgu.CV.Structure.MCvScalar(0.0, 200.0, 50.0));
                    mappedImage.Save("C:\\Temp\\ArucoDetect.png");
                } else if (rejected.Size > 0) {
                    PointF[][] myPts = rejected.ToArrayOfArray();
                    for (i = 0; i < myPts.GetUpperBound(0); i++) {
                        CvInvoke.Line(mappedImage, new Point((int)myPts[i][0].X, (int)myPts[i][0].Y), new Point((int)myPts[i][1].X, (int)myPts[i][1].Y), new Emgu.CV.Structure.MCvScalar(0.0, 0.5, 200.0), 2);
                        CvInvoke.Line(mappedImage, new Point((int)myPts[i][1].X, (int)myPts[i][1].Y), new Point((int)myPts[i][2].X, (int)myPts[i][2].Y), new Emgu.CV.Structure.MCvScalar(0.0, 0.5, 200.0), 2);
                        CvInvoke.Line(mappedImage, new Point((int)myPts[i][2].X, (int)myPts[i][2].Y), new Point((int)myPts[i][3].X, (int)myPts[i][3].Y), new Emgu.CV.Structure.MCvScalar(0.0, 0.5, 200.0), 2);
                        CvInvoke.Line(mappedImage, new Point((int)myPts[i][3].X, (int)myPts[i][3].Y), new Point((int)myPts[i][0].X, (int)myPts[i][0].Y), new Emgu.CV.Structure.MCvScalar(0.0, 0.5, 200.0), 2);
                    }
                    mappedImage.Save("C:\\Temp\\ArucoReject.png");
                }
            }
        }

        public static Emgu.CV.Util.VectorOfPointF DetectEllipses(Image<Gray, byte> grayImage, Mat imageCopy) {
            var centerPoints = new Emgu.CV.Util.VectorOfPointF();
            var contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Mat heirarchy = null;
            var grayImageCopy = grayImage.Clone();
            var res = CvInvoke.Threshold(grayImage, grayImageCopy, 170, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
            //CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\" + Path.GetFileNameWithoutExtension(myFile) + "-threshold" + Path.GetExtension(myFile), grayImageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.JpegQuality, 95));
            grayImageCopy._Not();

            CvInvoke.FindContours(grayImageCopy, contours, heirarchy, Emgu.CV.CvEnum.RetrType.Ccomp, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            // var circles = CvInvoke.HoughCircles(grayImage, Emgu.CV.CvEnum.HoughType.Gradient, 1, grayImage.Rows / 16);
            if (contours.Size > 0) {

                double largestArea = 0;
                for (int i = 0; i < contours.Size; i++) {
                    var contour = contours[i];
                    if (contour.Size > 4) {
                        var rect = CvInvoke.FitEllipse(contour);
                        var area = rect.Size.Width * rect.Size.Height;
                        var width = rect.Size.Width > rect.Size.Height ? rect.Size.Width : rect.Size.Height;
                        var height = rect.Size.Width > rect.Size.Height ? rect.Size.Height : rect.Size.Width;
                        if (area > 1000 && width / height < 3) {
                            var averageDist = AverageDistanceToEllipse(contour, rect);
                            var furthestDist = FurthestDistanceToEllipse(contour, rect);
                            if (averageDist < 1.5 && furthestDist < 4) {
                                if (area > largestArea) largestArea = area;
                                centerPoints.Push(new PointF[] { rect.Center });
                                DrawContoursOnImage(imageCopy, contours[i]);
                                // CvInvoke.Ellipse(imageCopy, rect, new Bgr(System.Drawing.Color.Red).MCvScalar);
                                CvInvoke.Line(imageCopy, new Point((int)rect.GetVertices()[0].X, (int)rect.GetVertices()[0].Y), new Point((int)rect.GetVertices()[1].X, (int)rect.GetVertices()[1].Y), new Bgr(System.Drawing.Color.Red).MCvScalar);
                                CvInvoke.Line(imageCopy, new Point((int)rect.GetVertices()[1].X, (int)rect.GetVertices()[1].Y), new Point((int)rect.GetVertices()[2].X, (int)rect.GetVertices()[2].Y), new Bgr(System.Drawing.Color.Red).MCvScalar);
                                CvInvoke.Line(imageCopy, new Point((int)rect.GetVertices()[2].X, (int)rect.GetVertices()[2].Y), new Point((int)rect.GetVertices()[3].X, (int)rect.GetVertices()[3].Y), new Bgr(System.Drawing.Color.Red).MCvScalar);
                                CvInvoke.Line(imageCopy, new Point((int)rect.GetVertices()[3].X, (int)rect.GetVertices()[3].Y), new Point((int)rect.GetVertices()[0].X, (int)rect.GetVertices()[0].Y), new Bgr(System.Drawing.Color.Red).MCvScalar);
                            }
                        }
                    }
                }
            }

            return centerPoints;
        }

        public static void DetectDatums(string myFile) {
            var grayImage = new Image<Gray, byte>(myFile);

            Mat imageCopy = Emgu.CV.CvInvoke.Imread(myFile, Emgu.CV.CvEnum.ImreadModes.Color);
            byte[] grayImageBytes = new byte[grayImage.Data.Length];
            Buffer.BlockCopy(grayImage.Data, 0, grayImageBytes, 0, grayImage.Data.Length);
            myVideoWidth = grayImage.Width;
            myVideoHeight = grayImage.Height;

            //var thresh = grayImage.Clone();
            //double otsuThreshold = CvInvoke.Threshold(grayImage, thresh, 128.0, 255.0, Emgu.CV.CvEnum.ThresholdType.Otsu);
            //CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\" + Path.GetFileNameWithoutExtension(myFile) + "-threshold" + Path.GetExtension(myFile), thresh, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));

            //Detect the AR Marker first

            // Initialise AR
            string myCameraFile = "data\\calib.dat";
            var arParams = LoadCameraFromFile(myCameraFile);
            // string myVConf = "-module=Image -preset=photo -format=BGRA";
            string myVConf = "-module=Image -width=" + myVideoWidth + " -height=" + myVideoHeight + " -format=MONO";
            ARToolKitFunctions.Instance.arwInitialiseAR();
            ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile);
            string artkVersion = ARToolKitFunctions.Instance.arwGetARToolKitVersion();
            string pixelFormat = string.Empty;
            ARToolKitFunctions.Instance.arwSetLogLevel(0);
            myLogger = new Logger();

            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            int nFactors = 8;
            Mat distortionCoeffs = new Mat(nFactors, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            double[] cameraArray = new double[9];
            for (int j = 0; j < 3; j++) {
                for (int i = 0; i < 3; i++) {
                    cameraArray[j * 3 + i] = arParams.mat[j, i];
                }
            }
            double[] distCoeffArray = new double[nFactors];
            for (int i = 0; i < nFactors; i++) {
                distCoeffArray[i] = arParams.dist_factor[i];
            }
            Marshal.Copy(cameraArray, 0, cameraMatrix.DataPointer, 9);
            Marshal.Copy(distCoeffArray, 0, distortionCoeffs.DataPointer, nFactors);

            mdlRecognise.AddDatumMarkersToARToolKit();

            var cornersErr = new Emgu.CV.Util.VectorOfPointF();
            var cornersErr2 = new Emgu.CV.Util.VectorOfPointF();

            var retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(grayImageBytes, true);

            for (int markerID = 0; markerID < 102; markerID++) {

                double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                retB = ARToolKitFunctions.Instance.arwQueryMarkerTransformation(markerID, mv, corners, out int numCorners);
                if (!retB) continue;

                var trans = OpenGL2Trans(mv);

                var pts2d = new List<clsPoint>();
                var cornerPoints = new Emgu.CV.Util.VectorOfPointF();
                pts2d.Add(new clsPoint(-40, -40));
                pts2d.Add(new clsPoint(40, -40));
                pts2d.Add(new clsPoint(40, 40));
                pts2d.Add(new clsPoint(-40, 40));
                if (markerID == myGFMarkerID) {
                    pts2d.Add(new clsPoint(-128.5, -85));
                    pts2d.Add(new clsPoint(128.5, -85));
                    pts2d.Add(new clsPoint(128.5, 85));
                    pts2d.Add(new clsPoint(-128.5, 85));
                } else {
                    pts2d.Add(new clsPoint(-55, -30));
                    pts2d.Add(new clsPoint(55, -30));
                    pts2d.Add(new clsPoint(55, 30));
                    pts2d.Add(new clsPoint(-55, 30));
                }

                //var objectPoints = new Emgu.CV.Util.VectorOfPoint3D32F(pts2d.Select(p => new MCvPoint3D32f((float)p.x, (float)p.y, 0)).ToArray());

                //var reprojectPoints = new Emgu.CV.Util.VectorOfPointF();
                //Mat rvec = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
                //double[] matrixArray = new double[9];
                //for (int j = 0; j < 3; j++) {
                //    for (int k = 0; k < 3; k++) {
                //        matrixArray[j * 3 + k] = trans[j, k];
                //    }
                //}
                //Marshal.Copy(matrixArray, 0, rvec.DataPointer, 9);

                //Mat tvec = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
                //double[] vectorArray = new double[3];
                //for (int j = 0; j < 3; j++) {
                //    vectorArray[j] = trans[j, 3];
                //}
                //Marshal.Copy(vectorArray, 0, tvec.DataPointer, 3);
                //CvInvoke.ProjectPoints(objectPoints, rvec, tvec, cameraMatrix, distortionCoeffs, reprojectPoints);
                //cornerPoints.Push(reprojectPoints.ToArray());
                //cornersErr.Push(reprojectPoints.ToArray());

                //for (int i = 0; i < pts2d.Count; i++) {
                //    var pt = ModelToImageSpace(arParams, trans, pts2d[i]);
                //    cornerPoints.Push(new PointF[] { new PointF((float)pt.X, (float)pt.Y) });
                //}
                //cornersErr.Push(cornerPoints.ToArray());

                for (int i = 0; i < numCorners; i++) {
                    cornerPoints.Push(new PointF[] { new PointF((float)corners[i * 2], (float)corners[i * 2 + 1]) });
                }
                cornersErr.Push(cornerPoints.ToArray());

                if (cornerPoints.Size == pts2d.Count) {
                    //cornersErr.Push(cornerPoints.ToArray());
                    //var cornersCopy = new List<clsPoint>();
                    //var cornersCopy2 = new List<clsPoint>();
                    //foreach (var p in cornerPoints.ToArray()) cornersCopy.Add(new clsPoint(p.X, p.Y));
                    //CvInvoke.CornerSubPix(grayImage, cornerPoints, new Size(5, 5), new Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(100));

                    //foreach (var p in cornerPoints.ToArray()) cornersCopy2.Add(new clsPoint(p.X, p.Y));

                    //for (int i = 0; i < cornersCopy.Count; i++) {
                    //    cornersErr.Push(new PointF[] { new PointF((float)cornersCopy[i].x, (float)cornersCopy[i].y) });
                    //    if (cornersCopy[i].Dist(cornersCopy2[i]) > 4.0) {
                    //        cornersErr2.Push(new PointF[] { new PointF((float)cornersCopy2[i].x, (float)cornersCopy2[i].y) });
                    //    }
                    //}

                    //Emgu.CV.Util.VectorOfPointF imagePoints = new Emgu.CV.Util.VectorOfPointF();
                    //for (int i = 0; i < centerPoints.Size; i++) {
                    //    arParamObserv2Ideal(arParams.dist_factor, centerPoints[i].X, centerPoints[i].Y, out double ox, out double oy, arParams.dist_function_version);
                    //    imagePoints.Push(new PointF[] { new PointF((float)ox, (float)oy) });
                    //}

                    //Mat rvec = new Mat();
                    //Mat tvec = new Mat();
                    //CvInvoke.SolvePnP(objectPoints, imagePoints, cameraMatrix, distortionCoeffs, rvec, tvec, false, Emgu.CV.CvEnum.SolvePnpMethod.IPPE);
                    //Mat rotationMatrix = new Mat();
                    //CvInvoke.Rodrigues(rvec, rotationMatrix);

                    //trans = new double[3, 4];
                    //double[] rotationMatrixArray = new double[12];
                    //Marshal.Copy(rotationMatrix.DataPointer, rotationMatrixArray, 0, 12);
                    //double[] translationMatrixArray = new double[3];
                    //Marshal.Copy(tvec.DataPointer, translationMatrixArray, 0, 3);
                    //for (int j = 0; j < 3; j++) {
                    //    for (int i = 0; i < 3; i++) {
                    //        trans[j, i] = rotationMatrixArray[3 * j + i];
                    //    }
                    //    trans[j, 3] = translationMatrixArray[j];
                    //}

                    //mv = Trans2OpenGL(trans);
                }

            }

            DrawCornersOnImage(imageCopy, cornersErr, System.Drawing.Color.Red);
            //if (cornersErr2.Size > 0)  DrawCornersOnImage(imageCopy, cornersErr2, System.Drawing.Color.Red);
            CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\Corners-" + Path.GetFileNameWithoutExtension(myFile) + ".png", imageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));
        }

        public static void DetectMarkers(string myFile) {
            var grayImage = new Image<Gray, byte>(myFile);
            //CheckConnectedComponents(grayImage.Mat);

            Mat imageCopy = Emgu.CV.CvInvoke.Imread(myFile, Emgu.CV.CvEnum.ImreadModes.Color);
            byte[] grayImageBytes = new byte[grayImage.Data.Length];
            Buffer.BlockCopy(grayImage.Data, 0, grayImageBytes, 0, grayImage.Data.Length);
            myVideoWidth = grayImage.Width;
            myVideoHeight = grayImage.Height;

            //var thresh = grayImage.Clone();
            //double otsuThreshold = CvInvoke.Threshold(grayImage, thresh, 128.0, 255.0, Emgu.CV.CvEnum.ThresholdType.Otsu);
            //CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\" + Path.GetFileNameWithoutExtension(myFile) + "-threshold" + Path.GetExtension(myFile), thresh, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));

            //Detect the AR Marker first

            // Initialise AR
            string myCameraFile = "data\\calib.dat";
            var arParams = LoadCameraFromFile(myCameraFile);
            // string myVConf = "-module=Image -preset=photo -format=BGRA";
            string myVConf = "-module=Image -width=" + myVideoWidth + " -height=" + myVideoHeight + " -format=MONO";
            ARToolKitFunctions.Instance.arwInitialiseAR();
            ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile);
            string artkVersion = ARToolKitFunctions.Instance.arwGetARToolKitVersion();
            string pixelFormat = string.Empty;
            ARToolKitFunctions.Instance.arwSetLogLevel(0);
            myLogger = new Logger();

            Mat cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            int nFactors = 8;
            Mat distortionCoeffs = new Mat(nFactors, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            double[] cameraArray = new double[9];
            for (int j = 0; j < 3; j++) {
                for (int i = 0; i < 3; i++) {
                    cameraArray[j * 3 + i] = arParams.mat[j, i];
                }
            }
            double[] distCoeffArray = new double[nFactors];
            for (int i = 0; i < nFactors; i++) {
                distCoeffArray[i] = arParams.dist_factor[i];
            }
            Marshal.Copy(cameraArray, 0, cameraMatrix.DataPointer, 9);
            Marshal.Copy(distCoeffArray, 0, distortionCoeffs.DataPointer, nFactors);

            mdlRecognise.AddMarkersToARToolKit();

            var cornersErr = new Emgu.CV.Util.VectorOfPointF();
            var cornersErr2 = new Emgu.CV.Util.VectorOfPointF();

            var retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(grayImageBytes, false);

            for (int markerID = 0; markerID < 102; markerID++) {

                double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                retB = ARToolKitFunctions.Instance.arwQueryMarkerTransformation(markerID, mv, corners, out int numCorners);
                if (!retB) continue;

                var trans = OpenGL2Trans(mv);

                var pts2d = new List<clsPoint>();
                var cornerPoints = new Emgu.CV.Util.VectorOfPointF();
                pts2d.Add(new clsPoint(-40, -40));
                pts2d.Add(new clsPoint(40, -40));
                pts2d.Add(new clsPoint(40, 40));
                pts2d.Add(new clsPoint(-40, 40));
                if (markerID == myGFMarkerID) {
                    pts2d.Add(new clsPoint(110 - 40, -40));
                    pts2d.Add(new clsPoint(110 + 40, -40));
                    pts2d.Add(new clsPoint(110 + 40, 40));
                    pts2d.Add(new clsPoint(110 - 40, 40));
                    pts2d.Add(new clsPoint(110 - 40, -40 - 190));
                    pts2d.Add(new clsPoint(110 + 40, -40 - 190));
                    pts2d.Add(new clsPoint(110 + 40, 40 - 190));
                    pts2d.Add(new clsPoint(110 - 40, 40 - 190));
                    pts2d.Add(new clsPoint(-40, -40 - 190));
                    pts2d.Add(new clsPoint(40, -40 - 190));
                    pts2d.Add(new clsPoint(40, 40 - 190));
                    pts2d.Add(new clsPoint(-40, 40 - 190));
                } else {
                    pts2d.Add(new clsPoint(-40 - 85, -40));
                    pts2d.Add(new clsPoint(40 - 85, -40));
                    pts2d.Add(new clsPoint(40 - 85, 40));
                    pts2d.Add(new clsPoint(-40 - 85, 40));
                }

                //var objectPoints = new Emgu.CV.Util.VectorOfPoint3D32F(pts2d.Select(p => new MCvPoint3D32f((float)p.x, (float)p.y, 0)).ToArray());

                //var reprojectPoints = new Emgu.CV.Util.VectorOfPointF();
                //Mat rvec = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
                //double[] matrixArray = new double[9];
                //for (int j = 0; j < 3; j++) {
                //    for (int k = 0; k < 3; k++) {
                //        matrixArray[j * 3 + k] = trans[j, k];
                //    }
                //}
                //Marshal.Copy(matrixArray, 0, rvec.DataPointer, 9);

                //Mat tvec = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
                //double[] vectorArray = new double[3];
                //for (int j = 0; j < 3; j++) {
                //    vectorArray[j] = trans[j, 3];
                //}
                //Marshal.Copy(vectorArray, 0, tvec.DataPointer, 3);
                //CvInvoke.ProjectPoints(objectPoints, rvec, tvec, cameraMatrix, distortionCoeffs, reprojectPoints);
                //cornerPoints.Push(reprojectPoints.ToArray());
                //cornersErr.Push(reprojectPoints.ToArray());

                for (int i = 0; i < pts2d.Count; i++) {
                    var pt = ModelToImageSpace(arParams, trans, pts2d[i]);
                    cornerPoints.Push(new PointF[] { new PointF((float)pt.X, (float)pt.Y) });
                }
                cornersErr.Push(cornerPoints.ToArray());

                //for (int i = 0; i < numCorners; i++) {
                //    //pts2d.Add(new clsPoint(0, 0));
                //    cornerPoints.Push(new PointF[] { new PointF((float)corners[i * 2], (float)corners[i * 2 + 1]) });
                //    //arParamIdeal2Observ(arParams.dist_factor, corners[i * 2], corners[i * 2 + 1], out double ox, out double oy, arParams.dist_function_version);
                //    //cornerPoints.Push(new PointF[] { new PointF((float)ox, (float)oy) });
                //}

                if (cornerPoints.Size == pts2d.Count) {
                    //cornersErr.Push(cornerPoints.ToArray());
                    //var cornersCopy = new List<clsPoint>();
                    //var cornersCopy2 = new List<clsPoint>();
                    //foreach (var p in cornerPoints.ToArray()) cornersCopy.Add(new clsPoint(p.X, p.Y));
                    //CvInvoke.CornerSubPix(grayImage, cornerPoints, new Size(5, 5), new Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(100));

                    //foreach (var p in cornerPoints.ToArray()) cornersCopy2.Add(new clsPoint(p.X, p.Y));

                    //for (int i = 0; i < cornersCopy.Count; i++) {
                    //    cornersErr.Push(new PointF[] { new PointF((float)cornersCopy[i].x, (float)cornersCopy[i].y) });
                    //    if (cornersCopy[i].Dist(cornersCopy2[i]) > 4.0) {
                    //        cornersErr2.Push(new PointF[] { new PointF((float)cornersCopy2[i].x, (float)cornersCopy2[i].y) });
                    //    }
                    //}

                    //Emgu.CV.Util.VectorOfPointF imagePoints = new Emgu.CV.Util.VectorOfPointF();
                    //for (int i = 0; i < centerPoints.Size; i++) {
                    //    arParamObserv2Ideal(arParams.dist_factor, centerPoints[i].X, centerPoints[i].Y, out double ox, out double oy, arParams.dist_function_version);
                    //    imagePoints.Push(new PointF[] { new PointF((float)ox, (float)oy) });
                    //}

                    //Mat rvec = new Mat();
                    //Mat tvec = new Mat();
                    //CvInvoke.SolvePnP(objectPoints, imagePoints, cameraMatrix, distortionCoeffs, rvec, tvec, false, Emgu.CV.CvEnum.SolvePnpMethod.IPPE);
                    //Mat rotationMatrix = new Mat();
                    //CvInvoke.Rodrigues(rvec, rotationMatrix);

                    //trans = new double[3, 4];
                    //double[] rotationMatrixArray = new double[12];
                    //Marshal.Copy(rotationMatrix.DataPointer, rotationMatrixArray, 0, 12);
                    //double[] translationMatrixArray = new double[3];
                    //Marshal.Copy(tvec.DataPointer, translationMatrixArray, 0, 3);
                    //for (int j = 0; j < 3; j++) {
                    //    for (int i = 0; i < 3; i++) {
                    //        trans[j, i] = rotationMatrixArray[3 * j + i];
                    //    }
                    //    trans[j, 3] = translationMatrixArray[j];
                    //}

                    //mv = Trans2OpenGL(trans);
                }

            }

            DrawCornersOnImage(imageCopy, cornersErr, System.Drawing.Color.Green);
            //if (cornersErr2.Size > 0)  DrawCornersOnImage(imageCopy, cornersErr2, System.Drawing.Color.Red);
            CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\Corners-" + Path.GetFileNameWithoutExtension(myFile) + ".png", imageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));
        }

        public static double[,] OpenGL2Trans(double[] mv) {
            double[,] trans = new double[3, 4];
            trans[0, 0] = mv[0 + 0 * 4]; // R1C1
            trans[0, 1] = mv[0 + 1 * 4]; // R1C2
            trans[0, 2] = mv[0 + 2 * 4];
            trans[0, 3] = mv[0 + 3 * 4];
            trans[1, 0] = -mv[1 + 0 * 4]; // R2
            trans[1, 1] = -mv[1 + 1 * 4];
            trans[1, 2] = -mv[1 + 2 * 4];
            trans[1, 3] = -mv[1 + 3 * 4];
            trans[2, 0] = -mv[2 + 0 * 4]; // R3
            trans[2, 1] = -mv[2 + 1 * 4];
            trans[2, 2] = -mv[2 + 2 * 4];
            trans[2, 3] = -mv[2 + 3 * 4];
            return trans;
        }

        private static double[] Trans2OpenGL(double[,] trans) {
            double[] mv = new double[16];
            mv[0 + 0 * 4] = trans[0, 0]; // R1C1
            mv[0 + 1 * 4] = trans[0, 1]; // R1C2
            mv[0 + 2 * 4] = trans[0, 2];
            mv[0 + 3 * 4] = trans[0, 3];
            mv[1 + 0 * 4] = -trans[1, 0]; // R2
            mv[1 + 1 * 4] = -trans[1, 1];
            mv[1 + 2 * 4] = -trans[1, 2];
            mv[1 + 3 * 4] = -trans[1, 3];
            mv[2 + 0 * 4] = -trans[2, 0]; // R3
            mv[2 + 1 * 4] = -trans[2, 1];
            mv[2 + 2 * 4] = -trans[2, 2];
            mv[2 + 3 * 4] = -trans[2, 3];
            mv[3 + 0 * 4] = 0.0f;
            mv[3 + 1 * 4] = 0.0f;
            mv[3 + 2 * 4] = 0.0f;
            mv[3 + 3 * 4] = 1.0f;
            return mv;
        }

        private static void GetCenterPointForDatum(clsPoint pt, double[,] model, ARParam arParams, int[] vp, Image<Gray, byte> grayImage, ref Emgu.CV.Util.VectorOfPointF centerPoints) {
            var cpt = ModelToImageSpace(arParams, model, pt);
            var halfSquare = GetSquareForDatum(arParams, model, pt);
            if (halfSquare < 8) return;
            if (cpt.x - halfSquare < 0 || cpt.x + halfSquare > vp[2] || cpt.y - halfSquare < 0 || cpt.y + halfSquare > vp[3]) return;

            var rect = new Rectangle((int)cpt.x - halfSquare, (int)cpt.y - halfSquare, 2 * halfSquare, 2 * halfSquare);
            var region = new Mat(grayImage.Mat, rect);
            var binaryRegion = region.Clone();
            double otsuThreshold = CvInvoke.Threshold(region, binaryRegion, 0.0, 255.0, Emgu.CV.CvEnum.ThresholdType.Otsu);
            int nonzero = CvInvoke.CountNonZero(binaryRegion);
            var square = 4 * halfSquare * halfSquare;
            if (nonzero > square * 0.2f && nonzero < square * 0.8f) {
                centerPoints.Push(new PointF[] { new PointF((float)cpt.X, (float)cpt.Y) } );
            }
        }

        public static clsPoint ModelToImageSpace(ARParam param, double[,] trans, clsPoint p1) {
            double cx, cy, cz, hx, hy, h, sx, sy;

            cx = trans[0, 0] * p1.x + trans[0, 1] * p1.y + trans[0, 3];
            cy = trans[1, 0] * p1.x + trans[1, 1] * p1.y + trans[1, 3];
            cz = trans[2, 0] * p1.x + trans[2, 1] * p1.y + trans[2, 3];
            hx = param.mat[0, 0] * cx + param.mat[0, 1] * cy + param.mat[0, 2] * cz + param.mat[0, 3];
            hy = param.mat[1, 0] * cx + param.mat[1, 1] * cy + param.mat[1, 2] * cz + param.mat[1, 3];
            h = param.mat[2, 0] * cx + param.mat[2, 1] * cy + param.mat[2, 2] * cz + param.mat[2, 3];
            if (h == 0.0) return new clsPoint(0, 0);
            sx = hx / h;
            sy = hy / h;
            arParamIdeal2Observ(param.dist_factor, sx, sy, out double ox, out double oy, param.dist_function_version);
            return new clsPoint(ox, oy);
        }

        private static clsPoint ModelToImageSpace2(Matrix4 projection, Matrix4 modelview, int[] vp, ARParam arParams, clsPoint3d p1) {
            Vector4 vec;

            vec.X = (float)p1.X;
            vec.Y = (float)p1.Y;
            vec.Z = (float)p1.Z;
            vec.W = 1.0f;

            Vector4.Transform(ref vec, ref modelview, out vec);
            Vector4.Transform(ref vec, ref projection, out vec);

            if (vec.W > float.Epsilon || vec.W < -float.Epsilon) {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            var p3d = new clsPoint3d(vp[0] + (1.0f + vec.X) * vp[2] / 2.0f, vp[3] -  (vp[1] + (1.0f + vec.Y) * vp[3] / 2.0f), (1.0f + vec.Z) / 2.0f);
            arParamIdeal2Observ(arParams.dist_factor, p3d.X, p3d.Y, out double ox, out double oy, arParams.dist_function_version);
            return new clsPoint(ox, oy);
        }

        static int GetSquareForDatum(ARParam arParams, double[,] model, clsPoint pt) {
            var cpt = ModelToImageSpace(arParams, model, pt);
            var pt1 = ModelToImageSpace(arParams, model, new clsPoint(pt.x - 8, pt.y - 8));
            var pt2 = ModelToImageSpace(arParams, model, new clsPoint(pt.x + 8, pt.y - 8));
            var pt3 = ModelToImageSpace(arParams, model, new clsPoint(pt.x + 8, pt.y + 8));
            var pt4 = ModelToImageSpace(arParams, model, new clsPoint(pt.x - 8, pt.y + 8));
            var l1 = new clsLine(pt1, pt2);
            var l2 = new clsLine(pt2, pt3);
            var l3 = new clsLine(pt3, pt4);
            var l4 = new clsLine(pt4, pt1);

            double d = 100;
            var l = new clsLine(cpt, new clsPoint(cpt.x - 1, cpt.y - 1));
            var p1 = l.Intersect(l1);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);
            p1 = l.Intersect(l2);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);
            p1 = l.Intersect(l3);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);
            p1 = l.Intersect(l4);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);

            l = new clsLine(cpt, new clsPoint(cpt.x + 1, cpt.y - 1));
            p1 = l.Intersect(l1);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);
            p1 = l.Intersect(l2);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);
            p1 = l.Intersect(l3);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);
            p1 = l.Intersect(l4);
            if (p1 != null && p1.Dist(cpt) < d) d = p1.Dist(cpt);

            return (int)(d / Sqrt(2.0) + 0.5);
        }

        public static void DrawCornersOnImage(Mat image, Emgu.CV.Util.VectorOfPointF cornerPoints, System.Drawing.Color col) {
            for (int i = 0; i < cornerPoints.Size; i++) {
                CvInvoke.Line(image, new Point((int)cornerPoints[i].X, (int)cornerPoints[i].Y), new Point((int)cornerPoints[i].X, (int)cornerPoints[i].Y), new Bgr(col).MCvScalar, 1);
            }
        }

        private static double AverageDistanceToEllipse(Emgu.CV.Util.VectorOfPoint contour, RotatedRect rect) {
            var n = contour.Size;
            var dist = 0d;
            for (int i = 0; i < n; i++) {
                dist = dist + DistanceToEllipse(new clsPoint(contour[i].X, contour[i].Y), rect);
            }
            return dist / n;
        }

        private static double FurthestDistanceToEllipse(Emgu.CV.Util.VectorOfPoint contour, RotatedRect rect) {
            var n = contour.Size;
            var dist = 0d;
            for (int i = 0; i < n; i++) {
                var d = DistanceToEllipse(new clsPoint(contour[i].X, contour[i].Y), rect);
                if (d > dist) dist = d;
            }
            return dist;
        }

        private static double DistanceToEllipse(clsPoint pt, RotatedRect rect) {
            var c = rect.Center;
            var a = rect.Angle * PI / 180f;
            pt.Move(-c.X, -c.Y);
            pt.Rotate(-a);
            var pt2 = NearestPointOnEllipse(pt, rect.Size.Width / 2, rect.Size.Height / 2);
            return pt.Dist(pt2);
        }

        public static void DetectBlobEllipses(string myFile) {
            var grayImage = new Image<Gray, byte>(myFile);
            Mat imageCopy = Emgu.CV.CvInvoke.Imread(myFile, Emgu.CV.CvEnum.ImreadModes.Color);
            //grayImage._Not();

            var grayImageCopy = grayImage.Clone();
            var res = CvInvoke.Threshold(grayImage, grayImageCopy, 170, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
            CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\" + Path.GetFileNameWithoutExtension(myFile) + "-threshold" + Path.GetExtension(myFile), grayImageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.JpegQuality, 95));
            //grayImageCopy._Not();

            // Set our filtering parameters 
            // Initialize parameter settiing using cv2.SimpleBlobDetector 
            var paramsx = new Emgu.CV.Features2D.SimpleBlobDetectorParams();

            // Set Area filtering parameters 
            paramsx.FilterByArea = true;
            paramsx.MinArea = 1000;

            // Set Circularity filtering parameters 
            paramsx.FilterByCircularity = false;
            paramsx.MinCircularity = 0.9f;

            // Set Convexity filtering parameters 
            paramsx.FilterByConvexity = false;
            paramsx.MinConvexity = 0.2f;

            // Set inertia filtering parameters 
            paramsx.FilterByInertia = false;
            paramsx.MinInertiaRatio = 0.01f;

            // Create a detector with the parameters 
            var detector = new Emgu.CV.Features2D.SimpleBlobDetector(paramsx);

            // Detect blobs 
            var keypoints = detector.Detect(grayImageCopy);
            var keyPoints = new Emgu.CV.Util.VectorOfKeyPoint(keypoints);

            // Draw blobs on our image as red circles 
            var blank = imageCopy.Clone();
            Emgu.CV.Features2D.Features2DToolbox.DrawKeypoints(imageCopy, keyPoints, blank, new Bgr(System.Drawing.Color.Red), Emgu.CV.Features2D.Features2DToolbox.KeypointDrawType.DrawRichKeypoints);

            var number_of_blobs = keyPoints.Size;
            CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\" + Path.GetFileNameWithoutExtension(myFile) + "-copy" + Path.GetExtension(myFile), blank, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.JpegQuality, 95));
        }

        static void DrawContoursOnImage(Mat imageCopy, Emgu.CV.Util.VectorOfPoint cornerPoints) {
            for (int i = 0; i < cornerPoints.Size - 1; i++) {
                CvInvoke.Line(imageCopy, new Point(cornerPoints[i].X, cornerPoints[i].Y), new Point(cornerPoints[i + 1].X, cornerPoints[i + 1].Y), new Bgr(System.Drawing.Color.Red).MCvScalar, 1);
            }
            CvInvoke.Line(imageCopy, new Point(cornerPoints[cornerPoints.Size - 1].X, cornerPoints[cornerPoints.Size - 1].Y), new Point(cornerPoints[0].X, cornerPoints[0].Y), new Bgr(System.Drawing.Color.Red).MCvScalar, 1);
        }

        public static clsPoint NearestPointOnEllipse(clsPoint point, double semiMajor, double semiMinor) {
            double px = Math.Abs(point.x);
            double py = Math.Abs(point.y);

            double a = semiMajor;
            double b = semiMinor;

            double tx = 0.70710678118;
            double ty = 0.70710678118;

            double x, y, ex, ey, rx, ry, qx, qy, r, q, t = 0;

            for (int i = 0; i < 3; ++i) {
                x = a * tx;
                y = b * ty;

                ex = (a * a - b * b) * (tx * tx * tx) / a;
                ey = (b * b - a * a) * (ty * ty * ty) / b;

                rx = x - ex;
                ry = y - ey;

                qx = px - ex;
                qy = py - ey;

                r = Math.Sqrt(rx * rx + ry * ry);
                q = Math.Sqrt(qy * qy + qx * qx);

                tx = Math.Min(1, Math.Max(0, (qx * r / q + ex) / a));
                ty = Math.Min(1, Math.Max(0, (qy * r / q + ey) / b));

                t = Math.Sqrt(tx * tx + ty * ty);

                tx /= t;
                ty /= t;
            }

            return new clsPoint {
                x = (float)(a * (point.x < 0 ? -tx : tx)),
                y = (float)(b * (point.y < 0 ? -ty : ty))
            };
        }

        static bool CheckConnectedComponents(Mat grayImage) {
            // Threshold using Otsu bi-modal (black&white) assumption
            Mat binaryImage = grayImage.Clone();
            double otsuThreshold = CvInvoke.Threshold(grayImage, binaryImage, 0.0, 255.0, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);

            // dilate to connect two squares
            Mat kernel = new Mat();
            CvInvoke.Dilate(binaryImage, binaryImage, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);

            CvInvoke.Imwrite("C:\\Temp\\Dilate.png", binaryImage, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));

            // compute number of labels (should be 2: 0 for background, 1 for white)
            Mat labelRegion = new Mat(new System.Drawing.Size(binaryImage.Width, binaryImage.Height), Emgu.CV.CvEnum.DepthType.Cv32S, 1);
            Mat statistics = new Mat();
            Mat centroids = new Mat();
            var numberOfLabels = CvInvoke.ConnectedComponentsWithStats(binaryImage, labelRegion, statistics, centroids, Emgu.CV.CvEnum.LineType.EightConnected, Emgu.CV.CvEnum.DepthType.Cv32S);

            Console.WriteLine(" - Number of labels: %d\n", numberOfLabels);

            if (numberOfLabels != 2) return false;

            // compute centers of background and foreground (should also be close to image center)
            Emgu.CV.Util.VectorOfPoint imageCentre = new Emgu.CV.Util.VectorOfPoint( new Point [] { new Point((int)(grayImage.Cols / 2.0f), (int)(grayImage.Rows / 2.0f)) });
            Emgu.CV.Util.VectorOfPointF blackCenter = new Emgu.CV.Util.VectorOfPointF(new PointF[] { new PointF((float)centroids.GetDoubleValue(0, 0), (float)centroids.GetDoubleValue(0, 1)) } );
            Emgu.CV.Util.VectorOfPointF whiteCenter = new Emgu.CV.Util.VectorOfPointF(new PointF[] { new PointF((float)centroids.GetDoubleValue(1, 0), (float)centroids.GetDoubleValue(1, 1)) });

            var blackCentroidDistance = CvInvoke.Norm(blackCenter, imageCentre, Emgu.CV.CvEnum.NormType.L2);
            var whiteCentroidDistance = CvInvoke.Norm(whiteCenter, imageCentre, Emgu.CV.CvEnum.NormType.L2);

            for (var label = 0; label < numberOfLabels; label++) {
                Console.WriteLine(" - [%d] centroid at (%.1lf,%.1lf)\n", label, (float)centroids.GetDoubleValue(label, 0), (float)centroids.GetDoubleValue(label, 1));
            }

            return numberOfLabels == 2 && blackCentroidDistance < 10.0 && whiteCentroidDistance < 10.0;
        }

        static double GetDoubleValue(this Mat mat, int row, int col) {
            var value = new double[1];
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

    }

}


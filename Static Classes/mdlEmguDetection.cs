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

namespace BatchProcess {
    public static class mdlEmguDetection {

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
        
    }
}

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
    public static class mdlEmguBundleAdjustment {

        public static void initBundleAdjust(string myCalibFile, int xsize, int ysize, int npoints, float[] points, int nMeasurements, float[] matrices)
        {
            // camera related parameters
            Mat cameraMat;
            Mat distCoeffs;
            LoadCameraFromFile(myCalibFile, out cameraMat, out distCoeffs);

            Size cameraRes = new Size(xsize, ysize);
            
            List<Emgu.CV.Structure.MCvPoint3D32f> objectPoints = new List<Emgu.CV.Structure.MCvPoint3D32f>();
            objectPoints.Add(new Emgu.CV.Structure.MCvPoint3D32f(points[0], points[1], points[2]));
            var points3d = new Emgu.CV.Util.VectorOfPoint3D32F(objectPoints.ToArray());
            var points2d = new Emgu.CV.Util.VectorOfVectorOfPointF();
            var visibility = new Emgu.CV.Util.VectorOfVectorOfInt();
            var cameraMatrix = new Emgu.CV.Util.VectorOfMat();
            var R_true = new Emgu.CV.Util.VectorOfMat();
            var T_true = new Emgu.CV.Util.VectorOfMat();
            var distortionCoeffs = new Emgu.CV.Util.VectorOfMat();
            
            Emgu.CV.Structure.MCvTermCriteria criteria = new Emgu.CV.Structure.MCvTermCriteria(70, 1e-10);
            
            // define cameras
            for (int i = 0; i < nMeasurements; i++) {
                cameraMatrix.Push(cameraMat);
                distortionCoeffs.Push(distCoeffs);

                Mat _R_true = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
                double[] matrixArray = new double[9];
                for (int j = 0; j < 3; j++) {
                    for (int k = 0; k < 3; k++) {
                        matrixArray[j * 3 + i] = matrices[i * 12 + j * 3 + k];
                    }
                }
                Marshal.Copy(matrixArray, 0, _R_true.DataPointer, 9);
                R_true.Push(_R_true);

                Mat _T_true = new Mat(3, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
                double[] vectorArray = new double[3];
                for (int j = 0; j < 3; j++) {
                    vectorArray[j] = matrices[i * 12 + 9 + j];
                }
                Marshal.Copy(vectorArray, 0, _T_true.DataPointer, 3);
                T_true.Push(_T_true);
            }

            // project points to image coordinates
            for (int i = 0; i < nMeasurements; i++) {
                // project
                var imagePoints2 = new Emgu.CV.Util.VectorOfPointF();
                Emgu.CV.CvInvoke.ProjectPoints(points3d, R_true[i], T_true[i], cameraMatrix[i], distortionCoeffs[i], imagePoints2);

                // check if the point is in camera shot
                Emgu.CV.Util.VectorOfInt vis = new Emgu.CV.Util.VectorOfInt(1);
                // if the image point is within camera resolution then the point is visible
                if ((0 <= imagePoints2[0].X) && (imagePoints2[0].X <= cameraRes.Width) &&
                    (0 <= imagePoints2[0].Y) && (imagePoints2[0].Y <= cameraRes.Height)) {
                    vis.Push(new int[] { 1 });
                }
                // else, the point is not visible 
                else {
                    vis.Push(new int[] { 0 });
                }
                points2d.Push(imagePoints2);
                visibility.Push(vis);
        }
            //Emgu.CV.
        //	cv::LevMarqSparse lv;
        //lv.bundleAdjust(points_true, imagePoints, visibility, cameraMatrix, R_true, T_true, distCoeffs, criteria);

    }

        public static void LoadCameraFromFile(string myCalibFile, out Mat cameraMatrix, out Mat distortionCoeffs)
        {
            int i, j;

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

            cameraMatrix = new Mat(3, 3, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            distortionCoeffs = new Mat(4, 1, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
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
        }
    }
}

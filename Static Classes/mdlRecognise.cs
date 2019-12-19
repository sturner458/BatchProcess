﻿using System;
using static System.Math;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using static ARToolKitFunctions;
using OpenTK;

namespace BatchProcess
{
    public static class mdlRecognise
    {
        public static bool UseDatumMarkers = false;

        public static int myVideoWidth;
        public static int myVideoHeight;
        public static int myVideoPixelSize;

        public static int numImagesProcessed = 0;

        private static List<clsMarkerPoint> _confirmedMarkerPoints = new List<clsMarkerPoint>();
        public static List<clsMarkerPoint> ConfirmedMarkers {
            get { return _confirmedMarkerPoints; }
            set {
                _confirmedMarkerPoints = value;
            }
        }
        public delegate void BlankEventHandler();
        public static event BlankEventHandler ConfirmedMarkersUpdated;
        public static event BlankEventHandler ConfirmedMarkersVisibleChanged;

        public static List<clsMeasurement> myMeasurements = new List<clsMeasurement>();
        public static List<clsMarkerPoint> mySuspectedMarkers = new List<clsMarkerPoint>();
        public static List<clsMarkerPoint> myBulkheadMarkers = new List<clsMarkerPoint>();
        public static List<clsMarkerPoint> myDoorMarkers = new List<clsMarkerPoint>();
        public static List<clsMarkerPoint> myObstructMarkers = new List<clsMarkerPoint>();
        public static List<clsMarkerPoint> myWallMarkers = new List<clsMarkerPoint>();

        public static int myGFMarkerID = 100;
        public static int myStepMarkerID = 101;
        public static int myLeftBulkheadMarkerID = 102;
        public static int myRightBulkheadMarkerID = 103;
        public static int myDoorHingeRightMarkerID = 104;
        public static int myDoorFrameRightMarkerID = 105;
        public static int myDoorHingeLeftMarkerID = 106;
        public static int myDoorFrameLeftMarkerID = 107;
        public static int myObstruct1MarkerID = 108;
        public static int myObstruct2MarkerID = 109;
        public static int myObstruct3MarkerID = 110;
        public static int myObstruct4MarkerID = 111;
        public static int myWall1MarkerID = 112;
        public static int myWall2MarkerID = 113;
        public static int myWall3MarkerID = 114;
        public static int myWall4MarkerID = 115;
        //public static int myMapperMarkerID = 116;
        public static int myMaximumMarkerID = 116; //Please keep this up to date
        static List<int> myBulkheadMarkerIDs = new List<int>();
        static List<int> myDoorMarkerIDs = new List<int>();
        static List<int> myObstructMarkerIDs = new List<int>();
        static List<int> myWallMarkerIDs = new List<int>();
        static List<int> myAllFeatureMarkerIDs = new List<int>();

        public static List<int> myMarkerIDs = new List<int>();
        public static clsMarkerPoint myGFMarker = new clsMarkerPoint();

        private static clsMarkerPoint _stepMarkerPoint = new clsMarkerPoint();
        public static clsMarkerPoint StepMarker {
            get { return _stepMarkerPoint; }
            set {
                _stepMarkerPoint = value;
                StepMarkerChanged?.Invoke();
            }
        }
        public static Action StepMarkerChanged;
        public static RecognisedMarkers Data = new RecognisedMarkers();

        private static clsPoint3d myUncorrectedVerticalVector = null;
        private static clsPoint3d myVerticalVector = null;
        private static clsPoint3d myCorrectionVector = null;

        public static bool StartTracking(int hiResX, int hiResY) {

            //Only initialize ARToolkit the first time this is run
            if (myMarkerIDs.Count > 0) {

                //Clear bulkheads and door markers
                for (int i = 0; i < myBulkheadMarkers.Count; i++) {
                    myBulkheadMarkers[i].Confirmed = false;
                }
                for (int i = 0; i < myDoorMarkers.Count; i++) {
                    myDoorMarkers[i].Confirmed = false;
                }
                for (int i = 0; i < myObstructMarkers.Count; i++) {
                    myObstructMarkers[i].Confirmed = false;
                }
                for (int i = 0; i < myWallMarkers.Count; i++) {
                    myWallMarkers[i].Confirmed = false;
                }

                myVideoWidth = hiResX;
                myVideoHeight = hiResY;
                return true;
            }

            //Clear out images
            string myDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/";

            myVideoWidth = hiResX;
            myVideoHeight = hiResY;
            //ARToolKitXLogger.InitARToolKitXLogger();
            ARToolKitFunctions.Instance.arwSetLogLevel(0);
            InitialiseARToolKit(hiResX, hiResY);
            string artkVersion = ARToolKitFunctions.Instance.arwGetARToolKitVersion();
            System.Diagnostics.Debug.Print(artkVersion);

            AddMarkersToARToolKit();

            //mySuspectedMarkers.Clear()
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint(myStepMarkerID, -1);
            }

            return true;
        }

        private static void AddMarkersToARToolKit() {
            if (!UseDatumMarkers) {
                AddOldStyleMarkersToARToolKit();
            } else {
                AddDatumMarkersToARToolKit();
            }
        }

        private static void AddOldStyleMarkersToARToolKit() {
            //!!!IMPORTANT NOTE:
            //In arConfig.h:
            //#define   AR_LABELING_32_BIT                  1     // 0 = 16 bits per label, 1 = 32 bits per label.
            //#  define AR_LABELING_WORK_SIZE      1024*32*64

            ARToolKitFunctions.Instance.arwSetPatternDetectionMode(AR_MATRIX_CODE_DETECTION);
            ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_4x4);
            //ARToolKitFunctions.Instance.arwSetMarkerExtractionMode(AR_USE_TRACKING_HISTORY_V2); //This doesn't work in ARToolKitX
            ARToolKitFunctions.Instance.arwSetVideoThreshold(50);
            //ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL);
            ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL);
            ARToolKitFunctions.Instance.arwSetCornerRefinementMode(true);

            myMarkerIDs.Clear();

            for (int i = 1; i <= 100; i++) {
                myMarkerIDs.Add(ARToolKitFunctions.Instance.arwAddMarker("multi;data/MarkerLarge" + i.ToString("00") + ".dat"));
                //Path to markers is local
                if (myMarkerIDs[myMarkerIDs.Count - 1] > -1) {
                    ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 2);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 1.0f);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);
                }
            }

            myGFMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/GFMarker.dat");
            if (myGFMarkerID > -1) {
                ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myGFMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 4);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myGFMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 1.0f);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myGFMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myGFMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);
            }

            myStepMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/StepMarker.dat");
            if (myStepMarkerID > -1) {
                ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myStepMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 4);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myStepMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 1.0f);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myStepMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myStepMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);
            }

            myLeftBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;249;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myLeftBulkheadMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRightBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;250;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRightBulkheadMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myDoorHingeRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;251;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorHingeRightMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorFrameRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;252;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorFrameRightMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorHingeLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;253;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorHingeLeftMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorFrameLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;254;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorFrameLeftMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myObstruct1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;255;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct1MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;256;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct2MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;257;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct3MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;258;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct4MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myWall1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;259;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall1MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;260;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall2MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;261;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall3MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;262;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall4MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            //string sConfig = "multi_auto;121;80;";
            //myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            //ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);

            myMaximumMarkerID = myWall4MarkerID + 1; //Please keep this up to date
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
        }

        private static void AddDatumMarkersToARToolKit() {
            int markerID;

            //!!!IMPORTANT NOTE:
            //In arConfig.h:
            //#define   AR_LABELING_32_BIT                  1     // 0 = 16 bits per label, 1 = 32 bits per label.
            //#  define AR_LABELING_WORK_SIZE      1024*32*64

            ARToolKitFunctions.Instance.arwSetPatternDetectionMode(AR_MATRIX_CODE_DETECTION);
            //ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_4x4);
            ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_5x5_BCH_22_12_5);
            //ARToolKitFunctions.Instance.arwSetMarkerExtractionMode(AR_USE_TRACKING_HISTORY_V2); //This doesn't work in ARToolKitX
            ARToolKitFunctions.Instance.arwSetVideoThreshold(50);
            //ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL);
            ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_AUTO_ADAPTIVE);
            ARToolKitFunctions.Instance.arwSetCornerRefinementMode(true);

            myMarkerIDs.Clear();

            myGFMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;0;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myGFMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myStepMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;1;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myStepMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            for (int i = 1; i <= 100; i++) {
                markerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;" + (i + 1) + ";80");
                myMarkerIDs.Add(markerID);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(markerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            }

            myLeftBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;102;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myLeftBulkheadMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRightBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;103;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRightBulkheadMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myDoorHingeRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;104;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorHingeRightMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorFrameRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;105;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorFrameRightMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorHingeLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;106;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorHingeLeftMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorFrameLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;107;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorFrameLeftMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myObstruct1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;108;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct1MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;109;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct2MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;110;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct3MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;111;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct4MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myWall1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;112;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall1MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;113;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall2MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;114;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall3MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;115;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall4MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            //string sConfig = "multi_auto;0;80;";
            //myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            //ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);

            myMaximumMarkerID = myWall4MarkerID + 1; //Please keep this up to date
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
        }

        private static void InitialiseARToolKit(int hiResX, int hiResY) {
            string myCameraFile = "data\\calib.dat";
            //string myVConf = "-preset=720p -format=BGRA";
            //string myVConf = "-flipV";
            string myVConf = "-module=Image -width=" + hiResX.ToString() + " -height=" + hiResY.ToString() + " -format=MONO";
            // ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile, myVConf, myCameraFile, myNear, myFar, hiResX, hiResY, hiResX, hiResY);
            ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile);
        }

        public static void RecogniseMarkers(byte[] grayscaleBytes) {
            Data.Clear();

            var retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(grayscaleBytes, false);

            for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
                DetectMarkerVisible(myMarkerIDs[i]);
            }
            DetectMarkerVisible(myStepMarkerID);
            DetectMarkerVisible(myGFMarkerID);
            DetectMarkerVisible(myLeftBulkheadMarkerID);
            DetectMarkerVisible(myRightBulkheadMarkerID);
            DetectMarkerVisible(myDoorHingeRightMarkerID);
            DetectMarkerVisible(myDoorFrameRightMarkerID);
            DetectMarkerVisible(myDoorHingeLeftMarkerID);
            DetectMarkerVisible(myDoorFrameLeftMarkerID);
            DetectMarkerVisible(myObstruct1MarkerID);
            DetectMarkerVisible(myObstruct2MarkerID);
            DetectMarkerVisible(myObstruct3MarkerID);
            DetectMarkerVisible(myObstruct4MarkerID);
            DetectMarkerVisible(myWall1MarkerID);
            DetectMarkerVisible(myWall2MarkerID);
            DetectMarkerVisible(myWall3MarkerID);
            DetectMarkerVisible(myWall4MarkerID);

            if (Data.MarkersSeenID.Any()) { // Record a measurement
                var measurement = new clsMeasurement();
                measurement.MeasurementNumber = myMeasurements.Count;
                for (int i = 0; i < Data.MarkersSeenID.Count; i++) {
                    var markerID = Data.MarkersSeenID[i];
                    if (!UseDatumMarkers && markerID <= myStepMarkerID) {
                        var matrix = MatrixFromArray(Data.ModelViewMatrix[i]);
                        var n = ARToolKitFunctions.Instance.arwGetTrackablePatternCount(markerID);
                        int k = -1;
                        for (int j = 0; j < n; j++) {
                            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                            ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(markerID, j, mv, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);

                            measurement.MarkerUIDs.Add(barcodeID);
                            var configMatrix = MatrixFromArray(mv);
                            var totalMatrix = OpenTK.Matrix4d.Mult(configMatrix, matrix);
                            mv = ArrayFromMatrix(totalMatrix);
                            measurement.Matrixes.Add(mv);
                            var corners = new List<clsPoint>();
                            for (int k1 = 0; k1 < 4; k1++) {
                                k = k + 1;
                                corners.Add(Data.Corners[i][k]);
                            }
                            measurement.Corners.Add(corners);
                        }
                    } else {
                        measurement.Matrixes.Add(Data.ModelViewMatrix[i]);
                        var corners = new List<clsPoint>();
                        for (int k = 0; k < 4; k++) {
                            corners.Add(Data.Corners[i][k]);
                        }
                        measurement.Corners.Add(corners);
                        measurement.MarkerUIDs.Add(Data.MarkersSeenID[i]);
                    }
                }
                myMeasurements.Add(measurement);
            }

            AddNewSuspectedMarkers();
            ConvertSuspectedToConfirmed();
            numImagesProcessed = numImagesProcessed + 1;

            Data.GetMarkersCopy();
        }

        private static void DetectMarkerVisible(int myMarkerID) {

            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMarkerID, mv, corners, out int numCorners)) {
                clsPoint3d pt = new clsPoint3d(mv[12], mv[13], mv[14]);
                if (pt.Length < 2000) {

                    clsPoint3d myCameraPoint = PointFromInvMatrix(mv);

                    if (IsSameDbl(myCameraPoint.Length, 0) == false) {
                        myCameraPoint.Normalise();
                        double a1 = Abs(myCameraPoint.AngleToHorizontal) * 180 / PI;
                        if ((a1 > 22 && a1 < 80) || myWallMarkerIDs.Contains(myMarkerID)) {
                            Data.MarkersSeenID.Add(myMarkerID);
                            Data.ModelViewMatrix.Add(mv);
                            var c = new List<clsPoint>();
                            for (int i = 0; i < numCorners; i++) {
                                c.Add(new clsPoint(corners[i * 2], corners[i * 2 + 1]));
                            }
                            Data.Corners.Add(c);
                        }
                    }

                }
            }
        }

        private static void AddNewSuspectedMarkers() {
            int myMarkerID;
            List<int> mySuspectConfirmedID = new List<int>();

            //Take a measurement of each of the markers
            for (int i = 0; i <= Data.MarkersSeenID.Count - 1; i++) {
                myMarkerID = Data.MarkersSeenID[i];

                if (myMarkerID == myGFMarkerID) continue; //Ignore the GF marker
                if (ConfirmedMarkerIDs().Contains(myMarkerID)) continue; //Ignore confirmed markers
                if (myMarkerID == myStepMarkerID && StepMarker.Confirmed) continue; //Ignore the step marker

                CheckSuspectedMarker(myMarkerID, i);
            }

            //Now repeat, but from the suspected marker point of view
            for (int i = 0; i <= Data.MarkersSeenID.Count - 1; i++) {
                myMarkerID = Data.MarkersSeenID[i];

                if (myMarkerID == myGFMarkerID) continue; //Ignore the GF marker
                if (ConfirmedMarkerIDs().Contains(myMarkerID)) continue; //Ignore confirmed markers
                if (myMarkerID == myStepMarkerID && StepMarker.Confirmed) continue; //Ignore the step marker

                CheckSuspectedMarkerFromSuspectedMarkers(myMarkerID, i);
            }

            mySuspectedMarkers.Sort(new SuspectedMarkerPointComparer());
        }

        private static void ConvertSuspectedToConfirmed() {
            clsMarkerPoint myConfirmedMarker;
            int myMarkerID, mySeenFromMarkerID;
            bool myMarkerConfirmed = false;
            string myErrorString = "";
            double a1 = 0, a2 = 0;
            clsPoint3d v1 = null, v2 = null, v3 = null, v4 = null;
            bool b1 = false, b2 = false;

            //Convert the "confirmed" suspects to Confirmed
            int n = 0;
            while (n < mySuspectedMarkers.Count) {
                myMarkerID = mySuspectedMarkers[n].MarkerID;

                var pts1 = new List<clsPoint3d>();
                var pts2 = new List<clsPoint3d>();
                var pts3 = new List<clsPoint3d>();

                var imagesSeenIn = new List<int>();

                int n1 = -1;
                for (int i = 0; i < mySuspectedMarkers.Count; i++) {
                    if (mySuspectedMarkers[i].MarkerID != myMarkerID) continue;

                    mySeenFromMarkerID = mySuspectedMarkers[i].SeenFromMarkerID;

                    if (mySeenFromMarkerID == myGFMarkerID) {

                        if (mySuspectedMarkers[i].Confirmed == false) {
                            if (mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {
                                mySuspectedMarkers[i].Confirmed = true;
                            }
                        }

                        for (int j = 0; j < mySuspectedMarkers[i].PhotoNumbers.Count; j++) {
                            if (!imagesSeenIn.Contains(mySuspectedMarkers[i].PhotoNumbers[j])) imagesSeenIn.Add(mySuspectedMarkers[i].PhotoNumbers[j]);
                        }

                        for (int j = 0; j < mySuspectedMarkers[i].Matrixes.Count; j++) {
                            var matrix1 = MatrixFromArray(mySuspectedMarkers[i].Matrixes[j]);

                            var vec = new OpenTK.Vector4d(0, 0, 0, 1.0);
                            vec = OpenTK.Vector4d.Transform(vec, matrix1);
                            pts1.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                            vec = new OpenTK.Vector4d(100.0, 0, 0, 1.0);
                            vec = OpenTK.Vector4d.Transform(vec, matrix1);
                            pts2.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                            vec = new OpenTK.Vector4d(0, 100.0, 0, 1.0);
                            vec = OpenTK.Vector4d.Transform(vec, matrix1);
                            pts3.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));
                        }

                        if (n1 == -1) n1 = i;
                    } else if (ConfirmedMarkers.Select(m => m.MarkerID).Contains(mySeenFromMarkerID)) {
                        int k = ConfirmedMarkers.FindIndex(m => m.MarkerID == mySeenFromMarkerID);
                        if (!myAllFeatureMarkerIDs.Contains(ConfirmedMarkers[k].ActualMarkerID) && !(ConfirmedMarkers[k].MarkerID == myStepMarkerID && StepMarker.Levelled == false)) {

                            if (mySuspectedMarkers[i].Confirmed == false) {
                                if (mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {
                                    mySuspectedMarkers[i].Confirmed = true;
                                }
                            }

                            for (int j = 0; j < mySuspectedMarkers[i].PhotoNumbers.Count; j++) {
                                if (!imagesSeenIn.Contains(mySuspectedMarkers[i].PhotoNumbers[j])) imagesSeenIn.Add(mySuspectedMarkers[i].PhotoNumbers[j]);
                            }

                            for (int j = 0; j < mySuspectedMarkers[i].Matrixes.Count; j++) {
                                var matrix1 = MatrixFromArray(mySuspectedMarkers[i].Matrixes[j]);
                                var matrix2 = MatrixFromArray(ConfirmedMarkers[k].ModelViewMatrix);

                                var vec = new OpenTK.Vector4d(0, 0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts1.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                                vec = new OpenTK.Vector4d(100.0, 0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts2.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                                vec = new OpenTK.Vector4d(0, 100.0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts3.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));
                            }

                            if (n1 == -1) n1 = i;
                        }
                    }

                    //For Door markers, allow them to be seen from other Door markers
                    if (myDoorMarkerIDs.Contains(myMarkerID) && myDoorMarkers.Select(m => m.MarkerID).Contains(mySeenFromMarkerID)) {
                        int k = myDoorMarkers.FindIndex(m => m.MarkerID == mySeenFromMarkerID);
                        if (myDoorMarkers[k].MarkerID != myMarkerID && myDoorMarkers[k].Confirmed) {

                            if (mySuspectedMarkers[i].Confirmed == false) {
                                if (mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {
                                    mySuspectedMarkers[i].Confirmed = true;
                                }
                            }

                            for (int j = 0; j < mySuspectedMarkers[i].PhotoNumbers.Count; j++) {
                                if (!imagesSeenIn.Contains(mySuspectedMarkers[i].PhotoNumbers[j])) imagesSeenIn.Add(mySuspectedMarkers[i].PhotoNumbers[j]);
                            }

                            for (int j = 0; j < mySuspectedMarkers[i].Matrixes.Count; j++) {
                                var matrix1 = MatrixFromArray(mySuspectedMarkers[i].Matrixes[j]);
                                var matrix2 = MatrixFromArray(myDoorMarkers[k].ModelViewMatrix);

                                var vec = new OpenTK.Vector4d(0, 0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts1.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                                vec = new OpenTK.Vector4d(100.0, 0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts2.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                                vec = new OpenTK.Vector4d(0, 100.0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts3.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));
                            }

                            if (n1 == -1) n1 = i;
                        }
                    }

                    //For Obstruction markers, allow them to be seen from other confirmed Obstruction markers
                    if (myObstructMarkerIDs.Contains(myMarkerID) && myObstructMarkers.Select(m => m.MarkerID).Contains(mySeenFromMarkerID)) {
                        int k = myObstructMarkers.FindIndex(m => m.MarkerID == mySeenFromMarkerID);
                        if (myObstructMarkers[k].MarkerID != myMarkerID && myObstructMarkers[k].Confirmed) {

                            if (mySuspectedMarkers[i].Confirmed == false) {
                                if (mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {
                                    mySuspectedMarkers[i].Confirmed = true;
                                }
                            }

                            for (int j = 0; j < mySuspectedMarkers[i].PhotoNumbers.Count; j++) {
                                if (!imagesSeenIn.Contains(mySuspectedMarkers[i].PhotoNumbers[j])) imagesSeenIn.Add(mySuspectedMarkers[i].PhotoNumbers[j]);
                            }

                            for (int j = 0; j < mySuspectedMarkers[i].Matrixes.Count; j++) {
                                var matrix1 = MatrixFromArray(mySuspectedMarkers[i].Matrixes[j]);
                                var matrix2 = MatrixFromArray(myObstructMarkers[k].ModelViewMatrix);

                                var vec = new OpenTK.Vector4d(0, 0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts1.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                                vec = new OpenTK.Vector4d(100.0, 0.0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts2.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));

                                vec = new OpenTK.Vector4d(0, 100.0, 0, 1.0);
                                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                                vec = OpenTK.Vector4d.Transform(vec, matrix2);
                                pts3.Add(new clsPoint3d(vec.X, vec.Y, vec.Z));
                            }

                            if (n1 == -1) n1 = i;
                        }
                    }
                }

                if (n1 == -1 || !mySuspectedMarkers[n].Confirmed || imagesSeenIn.Count < 10) {
                    n = n + 1;
                    continue;
                }

                mySeenFromMarkerID = mySuspectedMarkers[n1].SeenFromMarkerID;

                var pt1 = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                var vx = AverageAxisSimple(pts1, pts2);
                if (IsSameDbl(vx.Length, 0)) continue;
                var vy = AverageAxisSimple(pts1, pts3);
                if (IsSameDbl(vy.Length, 0)) continue;
                vx.Normalise();
                vy.Normalise();
                var vz = vx.Cross(vy);
                if (IsSameDbl(vz.Length, 0)) continue;
                vz.Normalise();
                vy = vz.Cross(vx);
                if (IsSameDbl(vy.Length, 0)) continue;
                vy.Normalise();

                //Now we can convert our suspected marker to a Confirmed marker
                if (myMarkerID == myStepMarkerID) {
                    myMarkerConfirmed = true; //So we can auto-save
                    StepMarker.Confirmed = true;
                    StepMarker.Levelled = false;
                    StepMarker.Stitched = false;

                    //Set the Stepmarker coordinates so we can use it to recognise other markers
                    StepMarker.Origin = pt1.Copy();
                    StepMarker.Vx = vx.Copy();
                    StepMarker.Vy = vy.Copy();
                    StepMarker.Vz = vz.Copy();
                    StepMarker.SetEndPointBasedOnZVectors();

                    myConfirmedMarker = StepMarker.Copy();
                    UpdateStepMarkerIDs();
                    myConfirmedMarker.MarkerID = myStepMarkerID;
                    myConfirmedMarker.SeenFromMarkerID = mySeenFromMarkerID;
                    myConfirmedMarker.ActualMarkerID = myStepMarkerID;
                    myConfirmedMarker.VerticalVect = StepMarker.VerticalVect?.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = myMeasurements.Count - 1;
                    ConfirmedMarkers.Add(myConfirmedMarker);

                } else if (myBulkheadMarkerIDs.Contains(myMarkerID)) {
                    myMarkerConfirmed = true; //So we can auto-save
                    myConfirmedMarker = new clsMarkerPoint(myMarkerID, mySeenFromMarkerID);
                    myConfirmedMarker.Confirmed = true;
                    myConfirmedMarker.Origin = pt1.Copy();
                    myConfirmedMarker.Vx = vx.Copy();
                    myConfirmedMarker.Vy = vy.Copy();
                    myConfirmedMarker.Vz = vz.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = myMeasurements.Count - 1;
                    myConfirmedMarker.SetEndPointBasedOnZVectors();
                    myBulkheadMarkers.Add(myConfirmedMarker);

                    string myBulkheadName = (myMarkerID == myLeftBulkheadMarkerID) ? "Left Bulkhead" : "Right Bulkhead";
                    //(mySurveyForm.ViewController).ShowHeightInput((HeightZ) => { double z; if (double.TryParse(HeightZ, out z) && z > myTol) myBulkheadMarkers.Last().BulkheadHeight = ConvertLengthUnitsToMM(z); }, 0.0, "Bulkhead", "Enter Height of " + myBulkheadName + " above riser");

                } else if (myDoorMarkerIDs.Contains(myMarkerID)) {
                    myMarkerConfirmed = true; //So we can auto-save
                    myConfirmedMarker = new clsMarkerPoint(myMarkerID, mySeenFromMarkerID);
                    myConfirmedMarker.Confirmed = true;
                    myConfirmedMarker.Origin = pt1.Copy();
                    myConfirmedMarker.Vx = vx.Copy();
                    myConfirmedMarker.Vy = vy.Copy();
                    myConfirmedMarker.Vz = vz.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = myMeasurements.Count - 1;
                    myConfirmedMarker.SetEndPointBasedOnZVectors();
                    myDoorMarkers.Add(myConfirmedMarker);

                } else if (myObstructMarkerIDs.Contains(myMarkerID)) {
                    myMarkerConfirmed = true; //So we can auto-save
                    myConfirmedMarker = new clsMarkerPoint(myMarkerID, mySeenFromMarkerID);
                    myConfirmedMarker.Confirmed = true;
                    myConfirmedMarker.Origin = pt1.Copy();
                    myConfirmedMarker.Vx = vx.Copy();
                    myConfirmedMarker.Vy = vy.Copy();
                    myConfirmedMarker.Vz = vz.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = myMeasurements.Count - 1;
                    myConfirmedMarker.SetEndPointBasedOnZVectors();
                    myObstructMarkers.Add(myConfirmedMarker);

                    string myMarkerName = "Obstruction";
                    if (myMarkerID == myObstruct1MarkerID) {
                        myMarkerName = "Obstruction 1";
                    } else if (myMarkerID == myObstruct2MarkerID) {
                        myMarkerName = "Obstruction 2";
                    } else if (myMarkerID == myObstruct3MarkerID) {
                        myMarkerName = "Obstruction 3";
                    } else if (myMarkerID == myObstruct4MarkerID) {
                        myMarkerName = "Obstruction 4";
                    }
                    //(mySurveyForm.ViewController).ShowHeightInput((HeightZ) => { double z; if (double.TryParse(HeightZ, out z) && z > myTol) myObstructMarkers.Last().BulkheadHeight = ConvertLengthUnitsToMM(z); }, 0.0, "Obstruction", "Enter Height of " + myMarkerName + " above riser");

                } else if (myWallMarkerIDs.Contains(myMarkerID)) {
                    myMarkerConfirmed = true; //So we can auto-save
                    myConfirmedMarker = new clsMarkerPoint(myMarkerID, mySeenFromMarkerID);
                    myConfirmedMarker.Confirmed = true;
                    myConfirmedMarker.Origin = pt1.Copy();
                    myConfirmedMarker.Vx = vx.Copy();
                    myConfirmedMarker.Vy = vy.Copy();
                    myConfirmedMarker.Vz = vz.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = myMeasurements.Count - 1;
                    myConfirmedMarker.SetEndPointBasedOnZVectors();
                    myWallMarkers.Add(myConfirmedMarker);

                } else {
                    myMarkerConfirmed = true; //So we can auto-save
                    myConfirmedMarker = new clsMarkerPoint(myMarkerID, mySeenFromMarkerID);
                    myConfirmedMarker.Confirmed = true;
                    myConfirmedMarker.Origin = pt1.Copy();
                    myConfirmedMarker.Vx = vx.Copy();
                    myConfirmedMarker.Vy = vy.Copy();
                    myConfirmedMarker.Vz = vz.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = myMeasurements.Count - 1;
                    myConfirmedMarker.SetEndPointBasedOnZVectors();
                    ConfirmedMarkers.Add(myConfirmedMarker);
                }

                int j1 = 0;
                while (j1 <= mySuspectedMarkers.Count - 1) {
                    if (mySuspectedMarkers[j1].MarkerID == myMarkerID) {
                        mySuspectedMarkers.RemoveAt(j1);
                    } else {
                        j1 = j1 + 1;
                    }
                }
            }

        }

        private static bool SuspectedMarkerIsConfirmedObstruction(int myMarkerID) {
            int i;

            for (i = 0; i < myBulkheadMarkers.Count(); i++) {
                if (myBulkheadMarkers[i].MarkerID == myMarkerID && myBulkheadMarkers[i].Confirmed) return true;
            }

            for (i = 0; i < myDoorMarkers.Count(); i++) {
                if (myDoorMarkers[i].MarkerID == myMarkerID && myDoorMarkers[i].Confirmed) return true;
            }

            for (i = 0; i < myWallMarkers.Count(); i++) {
                if (myWallMarkers[i].MarkerID == myMarkerID && myWallMarkers[i].Confirmed) return true;
            }

            for (i = 0; i < myObstructMarkers.Count(); i++) {
                if (myObstructMarkers[i].MarkerID == myMarkerID && myObstructMarkers[i].Confirmed) return true;
            }

            return false;
        }

        private static List<int> ConfirmedMarkerIDs() {
            List<int> myMarkerList = new List<int>();
            int i;
            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                myMarkerList.Add(ConfirmedMarkers[i].MarkerID);
            }
            return myMarkerList;
        }

        public static void UpdateStepMarkerIDs() {
            int i;
            int maxConfirmedID = myMaximumMarkerID - 1;

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID > maxConfirmedID) maxConfirmedID = ConfirmedMarkers[i].MarkerID;
            }
            maxConfirmedID = maxConfirmedID + 1;

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID == myStepMarkerID) {
                    ConfirmedMarkers[i].MarkerID = maxConfirmedID;
                    break;
                }
            }
        }

        public static string SaveToString(bool includeSuspectedMarkers = true) {
            if (myCorrectionVector == null) myCorrectionVector = new clsPoint3d(0, 0, 0);
            if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
            if (myUncorrectedVerticalVector == null) myUncorrectedVerticalVector = myVerticalVector.Copy();

            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms) { AutoFlush = true }) {
                    var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    var engageAppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    if (engageAppVersion == null) engageAppVersion = appVersion;
                    sw.WriteLine("#VERSION," + appVersion.ToString());
                    sw.WriteLine("#ENGAGEVERSION," + engageAppVersion.ToString());
                    sw.WriteLine("SETTINGS");
                    sw.WriteLine("AppVersion," + myAppVersion);
                    sw.WriteLine("CalibrationFile,evice");
                    sw.WriteLine("CalibrationScore,0.5");
                    sw.WriteLine("AutoFocus,false");
                    sw.WriteLine("FocalDistance,0.9");
                    sw.WriteLine("ThresholdMode,Manual");
                    sw.WriteLine("UncorrectedVerticalVectorX," + myUncorrectedVerticalVector.X);
                    sw.WriteLine("UncorrectedVerticalVectorY," + myUncorrectedVerticalVector.Y);
                    sw.WriteLine("UncorrectedVerticalVectorZ," + myUncorrectedVerticalVector.Z);
                    sw.WriteLine("CorrectionVectorX," + myCorrectionVector.X);
                    sw.WriteLine("CorrectionVectorY," + myCorrectionVector.Y);
                    sw.WriteLine("CorrectionVectorZ," + myCorrectionVector.Z);
                    sw.WriteLine("VerticalVectorX," + myVerticalVector.X);
                    sw.WriteLine("VerticalVectorY," + myVerticalVector.Y);
                    sw.WriteLine("VerticalVectorZ," + myVerticalVector.Z);
                    //if (AppPreferences.UseNewStyleMarkers) sw.WriteLine("UseNewStyleMarkers,1");
                    sw.WriteLine("UseNewStyleMarkers,1");
                    sw.WriteLine("GFMarkerID," + myGFMarkerID);
                    sw.WriteLine("StepMarkerID," + myStepMarkerID);
                    sw.WriteLine("END_SETTINGS");

                    sw.WriteLine(myMeasurements.Count);
                    for (var i = 0; i <= myMeasurements.Count - 1; i++) {
                        myMeasurements[i].Save(sw);
                    }
                    sw.WriteLine(ConfirmedMarkers.Count);
                    for (var i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                        ConfirmedMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myBulkheadMarkers.Count);
                    for (var i = 0; i <= myBulkheadMarkers.Count - 1; i++) {
                        myBulkheadMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myDoorMarkers.Count);
                    for (var i = 0; i <= myDoorMarkers.Count - 1; i++) {
                        myDoorMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myObstructMarkers.Count);
                    for (var i = 0; i <= myObstructMarkers.Count - 1; i++) {
                        myObstructMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myWallMarkers.Count);
                    for (var i = 0; i <= myWallMarkers.Count - 1; i++) {
                        myWallMarkers[i].Save(sw);
                    }
                    if (includeSuspectedMarkers) {
                        sw.WriteLine(mySuspectedMarkers.Count);
                        for (var i = 0; i <= mySuspectedMarkers.Count - 1; i++) {
                            mySuspectedMarkers[i].Save(sw);
                        }
                    }

                    ms.Position = 0;
                    using (var sr = new StreamReader(ms)) {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public static void LoadFromStreamReader(StreamReader sr) {
            ResetMeasurements();
            string myPGLoadedVersion = "1.1";

            var myLine = sr.ReadLine();
            var mySplit = myLine.Split(',');
            if (mySplit.GetUpperBound(0) >= 1) {
                myPGLoadedVersion = mySplit[1];
            } else {
                myPGLoadedVersion = "1.1";
            }
            myLine = sr.ReadLine();
            if (myLine.Contains(",")) {
                mySplit = myLine.Split(',');
                if (mySplit.GetUpperBound(0) >= 1) myPGLoadedVersion = mySplit[1];
                myLine = sr.ReadLine();
            }
            if (myLine == "SETTINGS") {
                myLine = sr.ReadLine();
                while (myLine != "END_SETTINGS") {
                    mySplit = myLine.Split(',');
                    if (mySplit.GetUpperBound(0) == 1) {
                        if (mySplit[0] == "VerticalVectorX") {
                            if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
                            myVerticalVector.X = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorY") {
                            if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
                            myVerticalVector.Y = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorZ") {
                            if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
                            myVerticalVector.Z = Convert.ToDouble(mySplit[1]);
                        }
                    }
                    myLine = sr.ReadLine();
                }
                myLine = sr.ReadLine();
            }

            myMeasurements.Clear();
            ConfirmedMarkers.Clear();
            myBulkheadMarkers.Clear();
            myDoorMarkers.Clear();
            myObstructMarkers.Clear();
            myWallMarkers.Clear();
            mySuspectedMarkers.Clear();

            if (sr.Peek() == -1) return;
            var n = Convert.ToInt32(myLine);
            clsMeasurement myMeasurement;
            for (var i = 1; i <= n; i++) {
                myMeasurement = new clsMeasurement();
                myMeasurement.Load(sr);
                myMeasurements.Add(myMeasurement);
            }

            if (sr.Peek() == -1) return;
            n = Convert.ToInt32(myLine);
            clsMarkerPoint myMarkerPoint;
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                ConfirmedMarkers.Add(myMarkerPoint);
            }

            if (sr.Peek() == -1) return;
            myLine = sr.ReadLine();
            n = Convert.ToInt32(myLine);
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                myBulkheadMarkers.Add(myMarkerPoint);
            }

            if (sr.Peek() == -1) return;
            myLine = sr.ReadLine();
            n = Convert.ToInt32(myLine);
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                myDoorMarkers.Add(myMarkerPoint);
            }

            if (sr.Peek() == -1) return;
            myLine = sr.ReadLine();
            n = Convert.ToInt32(myLine);
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                myObstructMarkers.Add(myMarkerPoint);
            }

            if (sr.Peek() == -1) return;
            myLine = sr.ReadLine();
            n = Convert.ToInt32(myLine);
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                myWallMarkers.Add(myMarkerPoint);
            }

            if (sr.Peek() == -1) return;
            myLine = sr.ReadLine();
            n = Convert.ToInt32(myLine);
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                mySuspectedMarkers.Add(myMarkerPoint);
            }

        }

        public static bool LoadSavedDataFile(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("filePath was null or whitespace");
            if (!File.Exists(filePath)) return false;
            using (var sr = new StreamReader(filePath)) {
                LoadFromStreamReader(sr);
            }
            return true;
        }

        public static clsPoint3d PointFromInvMatrix(int n, bool lowRes = false) {
            double[] mv = new double[16];

            for (int i = 0; i <= 15; i++) {
                mv[i] = Data.ModelViewMatrix[n][i];
            }
            return PointFromInvMatrix(mv);
        }

        public static clsPoint3d PointFromInvMatrix(double[] mv) {
            OpenTK.Matrix4d myModel = MatrixFromArray(mv);
            OpenTK.Matrix4d modelViewInv = OpenTK.Matrix4d.Invert(myModel);
            return new clsPoint3d(modelViewInv.M41, modelViewInv.M42, modelViewInv.M43);
        }

        public static void RelevelMarkerFromGF(clsMarkerPoint myMarker, bool goBack = false) {
            int j;
            clsPoint3d p1 = myVerticalVector.Copy();
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            double a = p1.AngleToHorizontal;

            if (IsSameAngle(a, PI / 2) == false) {
                double b = -(PI / 2 - a);
                if (goBack) b = -b;
                clsPoint3d p2 = new clsPoint3d(p1.X, p1.Y, 0);
                p2.Normalise();
                clsPoint3d p3 = p1.Cross(p2);
                p3.Normalise();

                myMarker.Origin.RotateAboutLine(p3.Line(), b);
                myMarker.EndXAxis.RotateAboutLine(p3.Line(), b);
                myMarker.EndYAxis.RotateAboutLine(p3.Line(), b);
                myMarker.Vx.RotateAboutLine(p3.Line(), b);
                myMarker.Vy.RotateAboutLine(p3.Line(), b);
                myMarker.Vz.RotateAboutLine(p3.Line(), b);
                myMarker.Point.RotateAboutLine(p3.Line(), b);
            }
        }

        public static void RelevelPointFromGF(clsPoint3d myPt, bool goBack = false) {
            int j;
            clsPoint3d p1 = myVerticalVector.Copy();
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            if (p1.Length < 0.9) return;
            double a = p1.AngleToHorizontal;

            if (IsSameAngle(a, PI / 2) == false) {
                double b = -(PI / 2 - a);
                if (goBack) b = -b;
                clsPoint3d p2 = new clsPoint3d(p1.X, p1.Y, 0);
                p2.Normalise();
                clsPoint3d p3 = p1.Cross(p2);
                p3.Normalise();

                myPt.RotateAboutLine(p3.Line(), b);
            }
        }

        public static clsPoint3d RelevelVerticalVector(clsPoint3d pt, clsPoint3d p1) {
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            if (p1.Length < 0.9) return pt;
            double a = p1.AngleToHorizontal;
            pt = pt.Copy();

            if (IsSameDbl(a, PI / 2) == false) {
                double b = -(PI / 2 - a);
                clsPoint3d p2 = new clsPoint3d(p1.X, p1.Y, 0);
                p2.Normalise();
                clsPoint3d p3 = p1.Cross(p2);
                p3.Normalise();

                pt.RotateAboutLine(p3.Line(), b);
            }
            return pt;
        }

        private static bool CheckSuspectedMarker(int myMarkerID, int myMarkerIndex) {
            bool suspectedMarkerAdded = false;

            var cameraPoint = PointFromInvMatrix(Data.ModelViewMatrix[myMarkerIndex]);

            if (Data.GetMarkerVisible(myGFMarkerID)) {
                var i1 = Data.GetMarkerIndex(myGFMarkerID);
                var matrix1 = OpenTK.Matrix4d.Invert(MatrixFromArray(Data.ModelViewMatrix[i1]));
                var matrix2 = MatrixFromArray(Data.ModelViewMatrix[myMarkerIndex]);
                var matrix3 = OpenTK.Matrix4d.Mult(matrix2, matrix1);
                var ret = AddSuspectedMarker(myMarkerID, myGFMarkerID, ArrayFromMatrix(matrix3), cameraPoint);
                suspectedMarkerAdded = suspectedMarkerAdded | ret;
            }

            for (int k = 0; k <= ConfirmedMarkers.Count - 1; k++) {
                if (myAllFeatureMarkerIDs.Contains(ConfirmedMarkers[k].ActualMarkerID)) continue; //Not from obstruction markers

                if (ConfirmedMarkers[k].MarkerID == myStepMarkerID && StepMarker.Levelled == false) continue;

                if (Data.GetMarkerVisible(ConfirmedMarkers[k].MarkerID)) {
                    var i1 = Data.GetMarkerIndex(ConfirmedMarkers[k].MarkerID);
                    var matrix1 = OpenTK.Matrix4d.Invert(MatrixFromArray(Data.ModelViewMatrix[i1]));
                    var matrix2 = MatrixFromArray(Data.ModelViewMatrix[myMarkerIndex]);
                    var matrix3 = OpenTK.Matrix4d.Mult(matrix2, matrix1);
                    var ret = AddSuspectedMarker(myMarkerID, ConfirmedMarkers[k].MarkerID, ArrayFromMatrix(matrix3), cameraPoint);
                    suspectedMarkerAdded = suspectedMarkerAdded | ret;
                }
            }

            //For Door markers, allow them to be seen from other Door markers
            if (myDoorMarkerIDs.Contains(myMarkerID)) {
                for (int k = 0; k <= myDoorMarkers.Count - 1; k++) {
                    if (myDoorMarkers[k].MarkerID == myMarkerID) continue;
                    if (!myDoorMarkers[k].Confirmed) continue;

                    if (Data.GetMarkerVisible(myDoorMarkers[k].MarkerID)) {
                        var i1 = Data.GetMarkerIndex(myDoorMarkers[k].MarkerID);
                        var matrix1 = OpenTK.Matrix4d.Invert(MatrixFromArray(Data.ModelViewMatrix[i1]));
                        var matrix2 = MatrixFromArray(Data.ModelViewMatrix[myMarkerIndex]);
                        var matrix3 = OpenTK.Matrix4d.Mult(matrix2, matrix1);
                        var ret = AddSuspectedMarker(myMarkerID, myDoorMarkers[k].MarkerID, ArrayFromMatrix(matrix3), cameraPoint);
                        suspectedMarkerAdded = suspectedMarkerAdded | ret;
                    }
                }
            }

            //For Obstruction markers, allow them to be seen from other Obstruction markers
            if (myObstructMarkerIDs.Contains(myMarkerID)) {
                for (int k = 0; k <= myObstructMarkers.Count - 1; k++) {
                    if (myObstructMarkers[k].MarkerID == myMarkerID) continue;
                    if (!myObstructMarkers[k].Confirmed) continue;

                    if (Data.GetMarkerVisible(myObstructMarkers[k].MarkerID)) {
                        var i1 = Data.GetMarkerIndex(myObstructMarkers[k].MarkerID);
                        var matrix1 = OpenTK.Matrix4d.Invert(MatrixFromArray(Data.ModelViewMatrix[i1]));
                        var matrix2 = MatrixFromArray(Data.ModelViewMatrix[myMarkerIndex]);
                        var matrix3 = OpenTK.Matrix4d.Mult(matrix2, matrix1);
                        var ret = AddSuspectedMarker(myMarkerID, myObstructMarkers[k].MarkerID, ArrayFromMatrix(matrix3), cameraPoint);
                        suspectedMarkerAdded = suspectedMarkerAdded | ret;
                    }
                }
            }

            return suspectedMarkerAdded;
        }

        private static bool CheckSuspectedMarkerFromSuspectedMarkers(int myMarkerID, int myMarkerIndex) {
            bool suspectedMarkerAdded = false;
            var suspectedMarkerIDs = new List<int>();

            var cameraPoint = PointFromInvMatrix(Data.ModelViewMatrix[myMarkerIndex]);
            for (int k = 0; k <= mySuspectedMarkers.Count - 1; k++) {
                if (suspectedMarkerIDs.Contains(mySuspectedMarkers[k].MarkerID)) continue;
                suspectedMarkerIDs.Add(mySuspectedMarkers[k].MarkerID);
                if (mySuspectedMarkers[k].MarkerID == myMarkerID) continue;
                if (myAllFeatureMarkerIDs.Contains(mySuspectedMarkers[k].ActualMarkerID)) continue; //Not from obstruction markers

                if (mySuspectedMarkers[k].MarkerID == myStepMarkerID && StepMarker.Levelled == false) continue;

                if (Data.GetMarkerVisible(mySuspectedMarkers[k].MarkerID)) {
                    var i1 = Data.GetMarkerIndex(mySuspectedMarkers[k].MarkerID);
                    var matrix1 = OpenTK.Matrix4d.Invert(MatrixFromArray(Data.ModelViewMatrix[i1]));
                    var matrix2 = MatrixFromArray(Data.ModelViewMatrix[myMarkerIndex]);
                    var matrix3 = OpenTK.Matrix4d.Mult(matrix2, matrix1);
                    var ret = AddSuspectedMarker(myMarkerID, mySuspectedMarkers[k].MarkerID, ArrayFromMatrix(matrix3), cameraPoint);
                    suspectedMarkerAdded = suspectedMarkerAdded & ret;
                }
            }

            return suspectedMarkerAdded;
        }

        private static bool AddSuspectedMarker(int myMarkerID, int mySeenFromMarkerID, double[] mv, clsPoint3d CameraPoint) {
            string myStr;
            string myErrorString = "";
            int i;
            clsPoint3d v1 = null, v2 = null, v3 = null, v4 = null;
            bool b1 = false, b2 = false;
            double a1 = 0, a2 = 0;

            //Don't add new suspected markers for bulkheads or doors if they have just been measured
            for (i = 0; i < myBulkheadMarkers.Count; i++) {
                if (myBulkheadMarkers[i].Confirmed && myBulkheadMarkers[i].MarkerID == myMarkerID) return false;
            }
            for (i = 0; i < myDoorMarkers.Count; i++) {
                if (myDoorMarkers[i].Confirmed && myDoorMarkers[i].MarkerID == myMarkerID) return false;
            }
            for (i = 0; i < myObstructMarkers.Count; i++) {
                if (myObstructMarkers[i].Confirmed && myObstructMarkers[i].MarkerID == myMarkerID) return false;
            }
            for (i = 0; i < myWallMarkers.Count; i++) {
                if (myWallMarkers[i].Confirmed && myWallMarkers[i].MarkerID == myMarkerID) return false;
            }

            int k = mySuspectedMarkers.FindIndex(p => p.MarkerID == myMarkerID && p.SeenFromMarkerID == mySeenFromMarkerID);
            if (k == -1) {
                mySuspectedMarkers.Add(new clsMarkerPoint(myMarkerID, mySeenFromMarkerID));
                k = mySuspectedMarkers.Count - 1;
            } else {
                mySuspectedMarkers[k].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2);
                myStr = myMarkerIDs.IndexOf(myMarkerID) + 1 + " / " + mySuspectedMarkers[k].Matrixes.Count + " - MaxD=" + Round(mySuspectedMarkers[k].MaxDistance(ref v1, ref v2)) + " - MaxA2=" + Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1);
                if (myStr != mySuspectedMarkers[k].Label) {
                    mySuspectedMarkers[k].Label = myStr;
                    mySuspectedMarkers[k].MarkerName = Convert.ToString(myMarkerIDs.IndexOf(myMarkerID) + 1);
                    mySuspectedMarkers[k].MaximumAngleA = Convert.ToString(Round(mySuspectedMarkers[k].MaxDistance(ref v1, ref v2)));
                    mySuspectedMarkers[k].MaximumAngleXY = Convert.ToString(Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1));
                }
            }

            mySuspectedMarkers[k].PhotoNumbers.Add(numImagesProcessed + 1);
            mySuspectedMarkers[k].Matrixes.Add(mv);
            mySuspectedMarkers[k].CameraPoints.Add(CameraPoint);
            mySuspectedMarkers[k].Velocity.Add(0);
            mySuspectedMarkers[k].AngularVelocity.Add(0);

            return true;
        }

        private static clsPoint3d AverageAxisSimple(List<clsPoint3d> axisStartPoints, List<clsPoint3d> axisEndPoints) {
            if (axisStartPoints == null || axisEndPoints == null) throw new ArgumentNullException("Axis start or end point lists are null.");
            if (axisStartPoints.Count != axisEndPoints.Count) throw new ArgumentException("Axis start and end point lists are not matched.");

            var points = new List<clsPoint3d>();
            for (var i = 0; i <= axisStartPoints.Count - 1; i++) {
                points.Add(axisEndPoints[i] - axisStartPoints[i]);
            }

            // Return the point (if we have more than 7 points within the tolerance)
            return new clsPoint3d(points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z));
        }

        public static void ResetMeasurements() {
            myMeasurements.Clear();
            mySuspectedMarkers.Clear();
            ConfirmedMarkers.Clear();
            myBulkheadMarkers.Clear();
            myDoorMarkers.Clear();
            myObstructMarkers.Clear();
            myWallMarkers.Clear();
            myVerticalVector = null;
            StepMarker.Confirmed = false;
            StepMarker.Levelled = false;
            StepMarker.Stitched = false;
            StepMarker.VerticalVect = null;
            numImagesProcessed = 0;
        }

        public static void StopTracking() {
            mySuspectedMarkers.Clear();
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint(myStepMarkerID, -1);
            }
            //ARToolKitFunctions.Instance.arwRegisterLogCallback(null);
            ARToolKitFunctions.Instance.arwShutdownAR();
            myMarkerIDs.Clear();
        }

        public static OpenTK.Matrix4d MatrixFromArray(double[] mv) {
            return new OpenTK.Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
        }

        public static OpenTK.Matrix4d MatrixFromArray(float[] mv) {
            return new OpenTK.Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
        }

        public static double[] ArrayFromMatrix(OpenTK.Matrix4d m) {
            return new double[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 };
        }

        public static float[] ArrayFromMatrix(OpenTK.Matrix4 m) {
            return new float[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 };
        }

        public static double[] GetMatrixFromVectorsAndPoint(clsPoint3d p1, clsPoint3d vx, clsPoint3d vy, clsPoint3d vz) {
            return new double[] { vx.x, vx.y, vx.z, 0, vy.x, vy.y, vy.z, 0, vz.x, vz.y, vz.z, 0, p1.x, p1.y, p1.z, 1.0 };
        }

        public static double[] GetModelViewMatrixFromPoints(clsPoint3d p1, clsPoint3d p2, clsPoint3d p3) {
            clsPoint3d vx;
            clsPoint3d vy;
            clsPoint3d vz;
            clsPoint pt;
            double a1, a2, a3;

            vx = p2 - p1;
            vx.Normalise();
            vy = p3 - p1;
            vy.Normalise();
            vz = vx.Cross(vy);
            vz.Normalise();
            vy = vz.Cross(vx);
            vy.Normalise();

            Matrix4d myRot;
            Matrix4d myTran;

            pt = vx.Point2D();
            if (IsSameDbl(pt.Length, 0) == false) {
                pt.Normalise();
                a1 = pt.Angle(true);
                a2 = vx.AngleToHorizontal;
                a3 = vy.AngleToHorizontal;

                myTran = Matrix4d.CreateTranslation(p1.X, p1.Y, p1.Z);
                if (vz.Z > 0) {
                    myRot = Matrix4d.CreateRotationX(a3);
                } else {
                    myRot = Matrix4d.CreateRotationX(-a3);
                }
                myTran = Matrix4d.Mult(myRot, myTran);
                myRot = Matrix4d.CreateRotationY(-a2);
                myTran = Matrix4d.Mult(myRot, myTran);
                myRot = Matrix4d.CreateRotationZ(a1);
                myTran = Matrix4d.Mult(myRot, myTran);
            } else {
                pt = vy.Point2D();
                pt.Normalise();
                a1 = pt.Angle(true);
                a2 = vy.AngleToHorizontal;
                a3 = vz.AngleToHorizontal;

                myTran = Matrix4d.CreateTranslation(p1.X, p1.Y, p1.Z);
                if (vx.X > 0) {
                    myRot = Matrix4d.CreateRotationY(a3);
                } else {
                    myRot = Matrix4d.CreateRotationY(-a3);
                }
                myTran = Matrix4d.Mult(myRot, myTran);
                myRot = Matrix4d.CreateRotationZ(-a2);
                myTran = Matrix4d.Mult(myRot, myTran);
                myRot = Matrix4d.CreateRotationX(a1);
                myTran = Matrix4d.Mult(myRot, myTran);
            }

            double[] mv = ArrayFromMatrix(myTran);
            return mv;
        }

        public static void BatchBundleAdjust(string myCalibFile) {
            myVideoWidth = 3264;
            myVideoHeight = 2448;
            ARToolKitFunctions.Instance.arwInitialiseAR();
            StartTracking(myVideoWidth, myVideoHeight);
            var arParams = mdlEmguCalibration.LoadCameraFromFile2(myCalibFile);

            ARToolKitFunctions.Instance.arwSetPatternDetectionMode(AR_MATRIX_CODE_DETECTION);
            ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_4x4);
            ARToolKitFunctions.Instance.arwSetVideoThreshold(50);
            ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL);
            ARToolKitFunctions.Instance.arwSetCornerRefinementMode(true);

            myGFMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/GFMarker.dat");

            string sConfig = "multi_auto;121;80;";
            var myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);

            foreach (var measurement in myMeasurements) {
                ARToolKitFunctions.Instance.arwAddMappedMarkers(myMapperMarkerID, myGFMarkerID, measurement.MarkerUIDs.Count, measurement.Trans(), measurement.MarkerUIDs.ToArray(), measurement.Corners.SelectMany(c => c).SelectMany(p => new double[] { p.x, p.y }).ToArray());
            }

            var pts = new List<clsPGPoint>();
            for (int i = 2; i <= 100; i = i + 2) {
                DetectMapperMarkerVisible(myMapperMarkerID, i, ref pts, false);
            }
            for (int i = 130; i <= 228; i = i + 2) {
                DetectMapperMarkerVisible(myMapperMarkerID, i, ref pts, false);
            }

            // DetectMapperMarkerVisible(myMapperMarkerID, 121, ref pts, false);
            DetectMapperMarkerVisible(myMapperMarkerID, 125, ref pts, false);

            pts.Sort((p1, p2) => p1.z.CompareTo(p2.z));

            var sw = new System.IO.StreamWriter("C:\\Temp\\points.txt");
            pts.ForEach(p => sw.WriteLine(p.x.ToString() + '\t' + p.z.ToString() + '\t' + (-p.y).ToString() + '\t' + p.ID.ToString() + '\t' + p.ParentID));
            sw.Close();

            sw = new System.IO.StreamWriter("C:\\Temp\\points.3dm");
            pts.ForEach(p => sw.WriteLine(p.x.ToString() + '\t' + p.y.ToString() + '\t' + p.z.ToString() + '\t' + p.ID.ToString() + '\t' + p.ParentID));
            sw.Close();

        }

        private static void DetectMapperMarkerVisible(int myMapperMarkerID, int myBarcodeID, ref List<clsPGPoint> pts, bool useDatums) {
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, myBarcodeID, mv)) {

                OpenTK.Matrix4d matrix = new OpenTK.Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
                var pt = new OpenTK.Vector4d(mv[12], mv[13], mv[14], 0);
                if (!useDatums) {
                    int markerID = myBarcodeID;
                    if (myBarcodeID == 125) {

                    } else if (markerID <= 100) {
                        pt = new OpenTK.Vector4d(140.0f, -45.0f, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    } else {
                        pt = new OpenTK.Vector4d(140.0, 45.0, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    }
                } else {
                    int markerID = myBarcodeID;
                    if (markerID - 2 >= 0 && markerID - 2 < 50) {
                        pt = new OpenTK.Vector4d(160.0f, -45.0f, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    } else if (markerID - 2 >= 50 && markerID - 2 < 100) {
                        pt = new OpenTK.Vector4d(160.0, 45.0, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    }
                }

                pts.Add(new clsPGPoint(pt.X, pt.Y, pt.Z, myBarcodeID));
            }
        }

    }

}

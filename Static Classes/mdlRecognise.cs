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
using System.Windows.Forms;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV;

namespace BatchProcess
{
    public static class mdlRecognise
    {
        public static bool UseDatumMarkers = false;
        public static int arToolkitMarkerType = 0;
        public static int circlesToUse = 0;

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
        public static int measurementNumber;
        public static List<clsMarkerPoint> mySuspectedMarkers = new List<clsMarkerPoint>();

        public static int myGFMarkerID = 100;
        public static int myStepMarkerID = 101;
        public static int myLastDatumId = 100;
        public static int myLeftBulkheadMarker1ID = 102;
        public static int myLeftBulkheadMarker2ID = 103;
        public static int myRightBulkheadMarker1ID = 104;
        public static int myRightBulkheadMarker2ID = 105;
        public static int myDoorHingeRightMarkerID = 106;
        public static int myDoorFrameRightMarkerID = 107;
        public static int myDoorHingeLeftMarkerID = 108;
        public static int myDoorFrameLeftMarkerID = 109;
        public static int myObstruct1MarkerID = 110;
        public static int myObstruct2MarkerID = 111;
        public static int myObstruct3MarkerID = 112;
        public static int myObstruct4MarkerID = 113;
        public static int myWall1MarkerID = 114;
        public static int myWall2MarkerID = 115;
        public static int myWall3MarkerID = 116;
        public static int myWall4MarkerID = 117;
        public static int myRailStartMarkerID = 118;
        public static int myRailEndMarkerID = 119;
        public static int myMapperMarkerID = -1;
        public static int myMaximumMarkerID = 121; //Please keep this up to date
        public static List<int> myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myLeftBulkheadMarker2ID, myRightBulkheadMarker1ID, myRightBulkheadMarker2ID };
        public static List<int> myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
        public static List<int> myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
        public static List<int> myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
        public static List<int> myRailEndMarkerIDs = new List<int> { myRailStartMarkerID, myRailEndMarkerID };
        public static List<int> myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myLeftBulkheadMarker2ID, myRightBulkheadMarker1ID, myRightBulkheadMarker2ID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID, myRailStartMarkerID, myRailEndMarkerID };
        public static List<int> stitchingMeasurements = new List<int>();
        public static List<clsPoint3d> stitchingVectors = new List<clsPoint3d>();
        private static int stitchingVectorIndex = 0;

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
        public static bool SaveHiResSurveyPhoto { get; set; } = false;

        private static clsPoint3d myUncorrectedVerticalVector = null;
        private static clsPoint3d myVerticalVector = null;
        private static clsPoint3d myCorrectionVector = null;
        private static List<string> DebugStringList = new List<string>();
        public static int GTSAMMinimumPhotos = 10;
        public static double GTSAMAngleTolerance1 = 40d;
        public static double GTSAMAngleTolerance2 = 1d;
        public static double GTSAMTolerance = 0.25d;

        public static bool StartTracking(int hiResX, int hiResY, bool avoidAddingMarkers, bool useDatums, int arToolkitMarkerType, int circlesToUse) {

            //Only initialize ARToolkit the first time this is run
            if (myMarkerIDs.Count > 0) {

                myVideoWidth = hiResX;
                myVideoHeight = hiResY;
                return true;
            }

            myVideoWidth = hiResX;
            myVideoHeight = hiResY;
            //ARToolKitXLogger.InitARToolKitXLogger();
            ARToolKitFunctions.Instance.arwSetLogLevel(0);
            InitialiseARToolKit(hiResX, hiResY);
            string artkVersion = ARToolKitFunctions.Instance.arwGetARToolKitVersion();
            System.Diagnostics.Debug.Print(artkVersion);

            if (!avoidAddingMarkers) AddMarkersToARToolKit(arToolkitMarkerType);

            //mySuspectedMarkers.Clear()
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint(myStepMarkerID);
            }

            return true;
        }

        private static void AddMarkersToARToolKit(int arToolkitMarkerType) {
            if (arToolkitMarkerType == -1) {
                AddOldStyleMarkersToARToolKit();
            } else if (arToolkitMarkerType == 0) {
                AddMarkersToARToolKit_RevC1();
            } else {
                throw new Exception($"Marker type {arToolkitMarkerType} is not supported!");
            }
        }

        public static void AddMarkersToARToolKit_RevC1() {

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
            DebugStringList.Clear();

            for (int i = 1; i <= 100; i++) {
                myMarkerIDs.Add(ARToolKitFunctions.Instance.arwAddMarker("multi;data/MarkerLargeRevC1_" + i.ToString("00") + ".dat"));
                //Path to markers is local
                if (myMarkerIDs[myMarkerIDs.Count - 1] > -1) {
                    ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 2);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 1.0f);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);
                }
            }

            myGFMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/GFMarkerRevC1.dat");
            if (myGFMarkerID > -1) {
                ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myGFMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 4);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myGFMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 1.0f);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myGFMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myGFMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);
            }

            myStepMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/StepMarkerRevC1.dat");
            if (myStepMarkerID > -1) {
                ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myStepMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 4);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myStepMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 1.0f);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myStepMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myStepMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);
            }

            myLeftBulkheadMarker1ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;249;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myLeftBulkheadMarker1ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRightBulkheadMarker1ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;250;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRightBulkheadMarker1ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myDoorHingeRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;251;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorHingeRightMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorFrameRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;252;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorFrameRightMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorHingeLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;253;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorHingeLeftMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myDoorFrameLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;254;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myDoorFrameLeftMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myObstruct1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;255;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct1MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;256;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct2MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;257;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct3MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myObstruct4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;258;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myObstruct4MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            myWall1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;259;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall1MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;260;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall2MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;261;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall3MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myWall4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;262;65;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myWall4MarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            //float[] myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //bool b = ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myGFMarkerID, 0, myMatrix, out float width, out float height, out int imageSizeX, out int imageSizeY, out int barcodeID);
            //string sConfig = "multi_auto;" + barcodeID + ";" + ((int)width) + ";";
            //string sConfig = "multi_auto;" + myGFMarkerID + ";80;";
            string sConfig = "multi_auto;121;65;";
            myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);

            myMaximumMarkerID = myMapperMarkerID + 1; //Please keep this up to date
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myRightBulkheadMarker1ID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myRightBulkheadMarker1ID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
        }

        private static void AddOldStyleMarkersToARToolKit() {
            //!!!IMPORTANT NOTE:
            //In arConfig.h:
            //#define   AR_LABELING_32_BIT                  1     // 0 = 16 bits per label, 1 = 32 bits per label.
            //#  define AR_LABELING_WORK_SIZE      1024*32*64

            ARToolKitFunctions.Instance.arwSetPatternDetectionMode(AR_MATRIX_CODE_DETECTION);
            ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_4x4);
            ARToolKitFunctions.Instance.arwSetVideoThreshold(50);
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
            myLastDatumId = myGFMarkerID;

            myLeftBulkheadMarker1ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;249;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myLeftBulkheadMarker1ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myLeftBulkheadMarker2ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;263;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myLeftBulkheadMarker2ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRightBulkheadMarker1ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;250;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRightBulkheadMarker1ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRightBulkheadMarker2ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;264;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRightBulkheadMarker2ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

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

            myRailStartMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;265;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRailStartMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRailEndMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;266;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRailEndMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

            string sConfig = "multi_auto;121;80;";
            myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);

            myMaximumMarkerID = myMapperMarkerID + 1; //Please keep this up to date
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myRightBulkheadMarker1ID, myLeftBulkheadMarker2ID, myRightBulkheadMarker2ID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myRightBulkheadMarker1ID, myLeftBulkheadMarker2ID, myRightBulkheadMarker2ID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID,
                myRailStartMarkerID, myRailEndMarkerID
            };
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

            myLeftBulkheadMarker1ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;102;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myLeftBulkheadMarker1ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
            myRightBulkheadMarker1ID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;103;80;");
            ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myRightBulkheadMarker1ID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);

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
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myRightBulkheadMarker1ID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarker1ID, myRightBulkheadMarker1ID,
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

        public static void ProcessPhotos(Label lblStatus) {

            InitGlobals();

            bool USE_DATUMS = false;

            FolderBrowserDialog myDlg = new FolderBrowserDialog();
            myDlg.SelectedPath = "C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\070220\\Survey 0.25\\Flight 1";
            var ret = myDlg.ShowDialog();
            if (ret != DialogResult.OK) return;
            var myFolder = myDlg.SelectedPath;

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

            var myFiles = new List<string>();
            int nFiles = 0;
            foreach (string myFile in Directory.GetFiles(myFolder)) {
                if (myFile.ToLower().EndsWith("-debug.png")) {
                    try {
                        File.Delete(myFile);
                    } catch (Exception ex) {
                        string s = ex.ToString();
                    }
                }
                if (myFile.ToLower().EndsWith(".png") && Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("survey") && !myFile.ToLower().EndsWith("-adj.png")) {
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
            StartTracking(myVideoWidth, myVideoHeight, false, false, -1, 0); // set up for RevA only

            int lastConfirmedMarker = 0;
            clsMarkerPoint lastStepMarker = null;

            var pts = new List<clsPGPoint>();
            var pts2 = new List<clsPGPoint>();
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            while (ret == DialogResult.OK) {
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
                            RecogniseMarkers(imageBytes);
                        } catch (Exception ex) {
                            Console.WriteLine(ex.ToString());
                        }

                        //for (int i = 0; i < mySuspectedMarkers.Count; i++) {
                        //    if (mySuspectedMarkers[i].MarkerID == 1) {
                        //        Console.WriteLine(mySuspectedMarkers[i].Origin.x.ToString() + ", " + mySuspectedMarkers[i].Origin.y.ToString() + ", " + mySuspectedMarkers[i].Origin.z.ToString());
                        //    }
                        //}

                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                    //Console.WriteLine("Processed " + nFiles + " out of " + myFiles.Count + " photos - " + Path.GetFileName(myFile));
                }

                for (int i = 0; i < mySuspectedMarkers.Count; i++) {
                    mySuspectedMarkers[0].OKToConfirm(out _, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);
                }

                if (lastConfirmedMarker > 0 && lastStepMarker != null) {
                    for (int i = lastConfirmedMarker; i < ConfirmedMarkers.Count; i++) {
                        var p1 = ConfirmedMarkers[i].Point;
                        ConfirmedMarkers[i].Point = lastStepMarker.Point + lastStepMarker.Vx * p1.X + lastStepMarker.Vy * p1.Y + lastStepMarker.Vz * p1.Z;
                    }
                }
                lastStepMarker = StepMarker.Copy();
                lastConfirmedMarker = ConfirmedMarkers.Count;

                myDlg.SelectedPath = "C:\\Customer\\Stannah\\PhotoGrammetry\\Photos\\070220\\Survey 0.25\\Flight 2";
                ret = myDlg.ShowDialog();
                if (ret == DialogResult.OK) {
                    myFolder = myDlg.SelectedPath;
                    myFiles.Clear();
                    nFiles = 0;
                    foreach (string myFile in Directory.GetFiles(myFolder)) {
                        if (myFile.ToLower().EndsWith(".png") && Path.GetFileNameWithoutExtension(myFile).ToLower().StartsWith("survey") && !myFile.ToLower().EndsWith("-adj.png")) {
                            myFiles.Add(myFile);
                        }
                    }
                    stitchingMeasurements.Add(myMeasurements.Count - 1);
                    StartStitching(-1); // -1 for RevA, RevC1 not supported yet.
                }
            }

            ConfirmedMarkers.Sort(new MarkerPointComparer());
            var sw = new System.IO.StreamWriter("C:\\Temp\\points.3dm");
            ConfirmedMarkers.ForEach(p => sw.WriteLine(p.Point.x.ToString() + '\t' + p.Point.y.ToString() + '\t' + p.Point.z.ToString() + '\t' + (p.ActualMarkerID + 1).ToString()));
            sw.Close();

            MessageBox.Show("Finished");
        }

        private static bool MapperMarkerVisible() {
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMapperMarkerID, mv, corners, out int numCorners));
        }

        public static async void RecogniseMarkers(byte[] grayscaleBytes) {

            var retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(grayscaleBytes, -1);

            Data.Clear();

            for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
                DetectMarkerVisible(myMarkerIDs[i]);
            }
            DetectMarkerVisible(myStepMarkerID);
            DetectMarkerVisible(myGFMarkerID);
            DetectMarkerVisible(myLeftBulkheadMarker1ID);
            DetectMarkerVisible(myRightBulkheadMarker1ID);
            DetectMarkerVisible(myLeftBulkheadMarker2ID);
            DetectMarkerVisible(myRightBulkheadMarker2ID);
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
            DetectMarkerVisible(myRailStartMarkerID);
            DetectMarkerVisible(myRailEndMarkerID);

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

            //Update positions of confirmed markers by bundle adjustment
            double[] modelMatrix = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] cornerCoords = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            SaveHiResSurveyPhoto = false;
            if (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMapperMarkerID, modelMatrix, cornerCoords, out int numCorners)) {
                SaveHiResSurveyPhoto = true;
                AddNewSuspectedMarkers(usingRevAMarkerType: true);
                ConvertSuspectedToConfirmed();
                UpdateConfirmedMarkersWithBundleAdjustment(usingRevAMarkerType: true);
            }

            if (SaveHiResSurveyPhoto) {
                numImagesProcessed = numImagesProcessed + 1;
            }

            Data.GetMarkersCopy();

        }

        private static void UpdateConfirmedMarkersWithBundleAdjustment(bool usingRevAMarkerType) {
            for (int i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID == ConfirmedMarkers[i].ActualMarkerID) {
                    double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(ConfirmedMarkers[i].MarkerID, 0, mv, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);

                    if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, barcodeID, mv)) {
                        ConfirmedMarkers[i].ModelViewMatrix = mv;
                        var pt = new OpenTK.Vector4d(mv[12], mv[13], mv[14], 0);
                        ConfirmedMarkers[i].Origin = new clsPoint3d(pt.X, pt.Y, pt.Z);
                        OpenTK.Matrix4d matrix = MatrixFromArray(mv);
                        if (barcodeID == 125) {

                        } else if (barcodeID <= 100) {
                            pt = usingRevAMarkerType ? new OpenTK.Vector4d(140.0f, -45.0f, 0.0f, 1) : new OpenTK.Vector4d(142.5f, -45.0f, 0.0f, 1);
                            pt = OpenTK.Vector4d.Transform(pt, matrix);
                        } else {
                            pt = usingRevAMarkerType ? new OpenTK.Vector4d(140.0f, 45.0f, 0.0f, 1) : new OpenTK.Vector4d(142.5f, 45.0f, 0.0f, 1);
                            pt = OpenTK.Vector4d.Transform(pt, matrix);
                        }
                        ConfirmedMarkers[i].Point = new clsPoint3d(pt.X, pt.Y, pt.Z);
                    }
                }
            }
        }

        private static void DetectMarkerVisible(int myMarkerID) {
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMarkerID, mv, corners, out int numCorners)) {
                var pt = new clsPoint3d(mv[12], mv[13], mv[14]);
                if (pt.Length < 2000) {
                    var myCameraPoint = PointFromInvMatrix(mv);
                    if (IsSameDbl(myCameraPoint.Length, 0) == false) {
                        myCameraPoint.Normalise();
                        double a1 = Abs(myCameraPoint.AngleToHorizontal) * 180 / PI;
                        if ((a1 > 22 && a1 < 90) || myWallMarkerIDs.Contains(myMarkerID)) {
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

        private static void AddNewSuspectedMarkers(bool usingRevAMarkerType) {
            string myErrorString = "";

            //Take a measurement of each of the markers
            for (int index = 0; index <= Data.MarkersSeenID.Count - 1; index++) {
                var myMarkerID = Data.MarkersSeenID[index];

                if (myMarkerID == myLastDatumId) continue; //Ignore the GF marker
                if (ConfirmedMarkers.Select(m => m.MarkerID).Contains(myMarkerID)) continue; //Ignore confirmed markers

                double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myMarkerID, 0, mv, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);

                if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, barcodeID, mv)) {
                    var cameraPoint = PointFromInvMatrix(Data.ModelViewMatrix[index]);
                    var pt = new OpenTK.Vector4d(mv[12], mv[13], mv[14], 0);

                    int k = mySuspectedMarkers.FindIndex(p => p.MarkerID == myMarkerID);
                    if (k == -1) {
                        mySuspectedMarkers.Add(new clsMarkerPoint(myMarkerID));
                        k = mySuspectedMarkers.Count - 1;
                    } else {
                        //mySuspectedMarkers[k].OKToConfirm(out myErrorString, out clsPoint3d v1, out clsPoint3d v2, out clsPoint3d v3, out clsPoint3d v4,
                        //    out bool b1, out bool b2, out bool b3, out bool b4, out double a1, out double a2);
                        //var myStr = myMarkerIDs.IndexOf(myMarkerID) + 1 + " / " + mySuspectedMarkers[k].GTSAMMatrixes.Count + " - MaxD=" + Round(mySuspectedMarkers[k].MaxDistance(out v1, out v2)) + " - MaxA2=" + Round(mySuspectedMarkers[k].MaxAnglePerpendicular(out v1, out v2) * 180 / PI, 1);
                        //if (myStr != mySuspectedMarkers[k].Label) {
                        //    mySuspectedMarkers[k].Label = myStr;
                        //    mySuspectedMarkers[k].MarkerName = Convert.ToString(myMarkerIDs.IndexOf(myMarkerID) + 1);
                        //    mySuspectedMarkers[k].MaximumAngleA = Convert.ToString(Round(mySuspectedMarkers[k].MaxDistance(out v1, out v2)));
                        //    mySuspectedMarkers[k].MaximumAngleXY = Convert.ToString(Round(mySuspectedMarkers[k].MaxAnglePerpendicular(out v1, out v2) * 180 / PI, 1));
                        //}
                    }

                    mySuspectedMarkers[k].PhotoNumbers.Add(numImagesProcessed + 1);
                    mySuspectedMarkers[k].Matrixes.Add(Data.ModelViewMatrix[index]);
                    mySuspectedMarkers[k].GTSAMMatrixes.Add(mv);
                    mySuspectedMarkers[k].CameraPoints.Add(cameraPoint);
                    mySuspectedMarkers[k].Velocity.Add(0);
                    mySuspectedMarkers[k].AngularVelocity.Add(0);

                    mySuspectedMarkers[k].Origin = new clsPoint3d(pt.X, pt.Y, pt.Z);
                    OpenTK.Matrix4d matrix = MatrixFromArray(mv);
                    if (barcodeID == 125) {

                    } else if (barcodeID <= 100) {
                        pt = usingRevAMarkerType ? new OpenTK.Vector4d(140.0f, -45.0f, 0.0f, 1) : new OpenTK.Vector4d(142.5f, -45.0f, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    } else {
                        pt = usingRevAMarkerType ? new OpenTK.Vector4d(140.0f, 45.0f, 0.0f, 1) : new OpenTK.Vector4d(142.5f, 45.0f, 0.0f, 1);
                        pt = OpenTK.Vector4d.Transform(pt, matrix);
                    }
                    mySuspectedMarkers[k].Point = new clsPoint3d(pt.X, pt.Y, pt.Z);
                    mySuspectedMarkers[k].ModelViewMatrix = mv;

                    string mySeenMarker = "";
                    if (myMarkerID == myGFMarkerID) {
                        mySeenMarker = "GF";
                    } else if (myMarkerID == myStepMarkerID) {
                        mySeenMarker = "Step";
                    } else {
                        mySeenMarker = mySuspectedMarkers[k].NewMarkerID().ToString();
                    }

                    if (DebugStringList.Count == 0 || !DebugStringList[DebugStringList.Count - 1].StartsWith(mySeenMarker + " Measured")) {
                        DebugStringList.Add(mySeenMarker + " Measured " + mySuspectedMarkers[k].GTSAMMatrixes.Count + " Times");
                    } else {
                        DebugStringList[DebugStringList.Count - 1] = mySeenMarker + " Measured " + mySuspectedMarkers[k].GTSAMMatrixes.Count + " Times";
                    }

                }
            }

            mySuspectedMarkers.Sort(new SuspectedMarkerPointComparer());
        }

        private static void ConvertSuspectedToConfirmed(bool forceOK = false) {
            clsMarkerPoint myConfirmedMarker;
            int myMarkerID;
            string myErrorString = "";
            bool myMarkerConfirmed = false;

            //Convert the "confirmed" suspects to Confirmed
            int n = 0;
            while (n < mySuspectedMarkers.Count) {
                myMarkerID = mySuspectedMarkers[n].MarkerID;

                if (mySuspectedMarkers[n].OKToConfirm(out myErrorString, out clsPoint3d v1, out clsPoint3d v2, out clsPoint3d v3, out clsPoint3d v4,
                    out bool b1, out bool b2, out bool b3, out bool b4, out double a1, out double a2, forceOK)) {
                    mySuspectedMarkers[n].Confirmed = true;
                } else {
                    n = n + 1;
                    continue;
                }

                var matrix1 = MatrixFromArray(mySuspectedMarkers[n].GTSAMMatrixes.Last());

                var vec = new OpenTK.Vector4d(0, 0, 0, 1.0);
                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                var pt1 = new clsPoint3d(vec.X, vec.Y, vec.Z);

                vec = new OpenTK.Vector4d(100.0, 0, 0, 1.0);
                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                var pt2 = new clsPoint3d(vec.X, vec.Y, vec.Z);

                vec = new OpenTK.Vector4d(0, 100.0, 0, 1.0);
                vec = OpenTK.Vector4d.Transform(vec, matrix1);
                var pt3 = new clsPoint3d(vec.X, vec.Y, vec.Z);

                var vx = pt2 - pt1;
                if (IsSameDbl(vx.Length, 0)) continue;
                vx.Normalise();
                var vy = pt3 - pt1;
                if (IsSameDbl(vy.Length, 0)) continue;
                vy.Normalise();
                var vz = vx.Cross(vy);
                if (IsSameDbl(vz.Length, 0)) continue;
                vz.Normalise();
                vy = vz.Cross(vx);
                if (IsSameDbl(vy.Length, 0)) continue;
                vy.Normalise();

                //Now we can convert our suspected marker to a Confirmed marker
                if ((myLastDatumId == myGFMarkerID && myMarkerID == myStepMarkerID) || (myLastDatumId == myStepMarkerID && myMarkerID == myGFMarkerID)) {
                    myMarkerConfirmed = true; //So we can auto-save
                    StepMarker.Confirmed = true;
                    StepMarker.Levelled = false;
                    StepMarker.Stitched = false;
                    //ConfirmedMarkers.ForEach(m => m.Stitched = false);

                    //Set the Stepmarker coordinates so we can use it to recognise other markers
                    StepMarker.Origin = pt1.Copy();
                    StepMarker.Vx = vx.Copy();
                    StepMarker.Vy = vy.Copy();
                    StepMarker.Vz = vz.Copy();
                    StepMarker.SetEndPointBasedOnZVectors();

                    myConfirmedMarker = StepMarker.Copy();
                    UpdateStepMarkerIDs();
                    myConfirmedMarker.MarkerID = myLastDatumId == myGFMarkerID ? myStepMarkerID : myGFMarkerID;
                    myConfirmedMarker.ActualMarkerID = myConfirmedMarker.MarkerID;
                    myConfirmedMarker.VerticalVect = null;
                    myConfirmedMarker.ConfirmedImageNumber = measurementNumber;
                    ConfirmedMarkers.Add(myConfirmedMarker);

                    if (stitchingVectorIndex < stitchingVectors.Count) {
                        myConfirmedMarker.VerticalVect = stitchingVectors[stitchingVectorIndex];
                        myConfirmedMarker.Levelled = true;
                        stitchingVectorIndex++;
                    }

                } else {
                    myMarkerConfirmed = true; //So we can auto-save
                    myConfirmedMarker = new clsMarkerPoint(myMarkerID);
                    myConfirmedMarker.Confirmed = true;
                    myConfirmedMarker.Origin = pt1.Copy();
                    myConfirmedMarker.Vx = vx.Copy();
                    myConfirmedMarker.Vy = vy.Copy();
                    myConfirmedMarker.Vz = vz.Copy();
                    myConfirmedMarker.ConfirmedImageNumber = measurementNumber;
                    myConfirmedMarker.SetEndPointBasedOnZVectors();
                    ConfirmedMarkers.Add(myConfirmedMarker);
                }

                myConfirmedMarker.GTSAMMatrixes.AddRange(mySuspectedMarkers[n].GTSAMMatrixes);
                mySuspectedMarkers.RemoveAt(n);
            }

            if (myMarkerConfirmed) ConfirmedMarkersUpdated?.Invoke();
        }

        public static void UpdateStepMarkerIDs() {
            int i;
            int maxConfirmedID = myMaximumMarkerID - 1;

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID > maxConfirmedID) maxConfirmedID = ConfirmedMarkers[i].MarkerID;
            }
            maxConfirmedID = maxConfirmedID + 1;

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID == myStepMarkerID || ConfirmedMarkers[i].MarkerID == myGFMarkerID) {
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
                // Set decimal places to use dot instead of comma:
                var currentCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                using (var sw = new StreamWriter(ms) { AutoFlush = true }) {
                    var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    var engageAppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    if (engageAppVersion == null) engageAppVersion = appVersion;
                    sw.WriteLine("#VERSION," + appVersion.ToString());
                    sw.WriteLine("#ENGAGEVERSION," + engageAppVersion.ToString());
                    sw.WriteLine("SETTINGS");
                    sw.WriteLine("AppVersion," + myAppVersion);
                    sw.WriteLine("CalibrationFile,Calib.dat");
                    sw.WriteLine("CalibrationScore," + 0.5);
                    sw.WriteLine("AutoFocus,false");
                    sw.WriteLine("FocalDistance,0.9");
                    sw.WriteLine("ThresholdMode,Manual");
                    sw.WriteLine("MinimumNumberOfImages," + GTSAMMinimumPhotos);
                    sw.WriteLine("MinimumAngle1," + GTSAMAngleTolerance1);
                    sw.WriteLine("MinimumAngle2," + GTSAMAngleTolerance2);
                    sw.WriteLine("GTSAMTolerance," + GTSAMTolerance);
                    sw.WriteLine("UncorrectedVerticalVectorX," + myUncorrectedVerticalVector.X);
                    sw.WriteLine("UncorrectedVerticalVectorY," + myUncorrectedVerticalVector.Y);
                    sw.WriteLine("UncorrectedVerticalVectorZ," + myUncorrectedVerticalVector.Z);
                    sw.WriteLine("CorrectionVectorX," + myCorrectionVector.X);
                    sw.WriteLine("CorrectionVectorY," + myCorrectionVector.Y);
                    sw.WriteLine("CorrectionVectorZ," + myCorrectionVector.Z);
                    sw.WriteLine("VerticalVectorX," + myVerticalVector.X);
                    sw.WriteLine("VerticalVectorY," + myVerticalVector.Y);
                    sw.WriteLine("VerticalVectorZ," + myVerticalVector.Z);
                    sw.WriteLine("UseDatumMarkers,0");
                    sw.WriteLine("MarkerType," + arToolkitMarkerType);
                    sw.WriteLine("NumCircles," + circlesToUse);
                    sw.WriteLine("GFMarkerID," + myGFMarkerID);
                    sw.WriteLine("StepMarkerID," + myStepMarkerID);
                    sw.WriteLine("LastDatumID," + myLastDatumId);
                    sw.WriteLine("StepMarkerLevelled," + (StepMarker.Levelled ? "1" : "0"));
                    sw.WriteLine("StepMarkerStitched," + (StepMarker.Stitched ? "1" : "0"));
                    sw.WriteLine("StepMarkerConfirmed," + (StepMarker.Confirmed ? "1" : "0"));
                    foreach (var id in stitchingMeasurements) sw.WriteLine("StitchingMeasurement," + id);
                    sw.WriteLine("NumImagesProcessed," + numImagesProcessed);
                    for (var i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                        sw.WriteLine("Marker " + ConfirmedMarkers[i].MarkerID + "," + ConfirmedMarkers[i].GTSAMMatrixes.Count);
                    }
                    sw.WriteLine("END_SETTINGS");

                    sw.WriteLine(myMeasurements.Count);
                    for (var i = 0; i <= myMeasurements.Count - 1; i++) {
                        myMeasurements[i].Save(sw);
                    }
                    sw.WriteLine(ConfirmedMarkers.Count);
                    for (var i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                        ConfirmedMarkers[i].Save(sw);
                    }

                    if (includeSuspectedMarkers) {
                        sw.WriteLine(mySuspectedMarkers.Count);
                        for (var i = 0; i <= mySuspectedMarkers.Count - 1; i++) {
                            mySuspectedMarkers[i].Save(sw);
                        }
                    }

                    ms.Position = 0;
                    System.Threading.Thread.CurrentThread.CurrentCulture = currentCultureInfo;
                    using (var sr = new StreamReader(ms)) {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public static void LoadFromStreamReader(StreamReader sr) {
            ResetMeasurements();
            stitchingMeasurements.Clear();
            stitchingVectors.Clear();
            stitchingVectorIndex = 0;
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

            clsPoint3d _correctionVector = new clsPoint3d(0, 0, 0);

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
                        if (mySplit[0] == "CorrectionVectorX") _correctionVector.X = Convert.ToDouble(mySplit[1]);
                        if (mySplit[0] == "CorrectionVectorY") _correctionVector.Y = Convert.ToDouble(mySplit[1]);
                        if (mySplit[0] == "CorrectionVectorZ") _correctionVector.Z = Convert.ToDouble(mySplit[1]);
                        if (mySplit[0] == "StitchingMeasurement") {
                            stitchingMeasurements.Add(Convert.ToInt32(mySplit[1]));
                        }
                        if (mySplit[0] == "MinimumNumberOfImages") {
                            GTSAMMinimumPhotos = Convert.ToInt32(mySplit[1]);
                        }
                        if (mySplit[0] == "MinimumAngle1") {
                            GTSAMAngleTolerance1 = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "MinimumAngle2") {
                            GTSAMAngleTolerance2 = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "GTSAMTolerance") {
                            GTSAMTolerance = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "UseDatumMarkers") {
                            UseDatumMarkers = Convert.ToInt32(mySplit[1]) == 0 ? false : true;
                        }
                        if (mySplit[0] == "MarkerType") {
                            arToolkitMarkerType = Convert.ToInt32(mySplit[1]);
                        }
                        if (mySplit[0] == "NumCircles") {
                            circlesToUse = Convert.ToInt32(mySplit[1]);
                        }
                    }
                    myLine = sr.ReadLine();
                }
                myLine = sr.ReadLine();
            }

            myMeasurements.Clear();
            measurementNumber = 0;
            ConfirmedMarkers.Clear();
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
            myLine = sr.ReadLine();
            n = Convert.ToInt32(myLine);
            clsMarkerPoint myMarkerPoint;
            for (var i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
                myMarkerPoint.Load(sr);
                ConfirmedMarkers.Add(myMarkerPoint);
            }
            ConfirmedMarkers.Sort((c1, c2) => c1.ConfirmedImageNumber.CompareTo(c2.ConfirmedImageNumber));
            ConfirmedMarkers.ForEach(c => {
                if (c.VerticalVect != null && (c.ActualMarkerID == myGFMarkerID || c.ActualMarkerID == myStepMarkerID)) {
                    stitchingVectors.Add(c.VerticalVect);
                }
            });

            if (sr.Peek() == -1) return;
            myLine = sr.ReadLine();
            if (!int.TryParse(myLine, out n)) return;
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

        public static void TempFixPoints() {

            var lastStepMarker = ConfirmedMarkers.Where(m => m.ActualMarkerID == myGFMarkerID || m.ActualMarkerID == myStepMarkerID).LastOrDefault();
            var lastConfirmedMarker = ConfirmedMarkers.IndexOf(lastStepMarker) + 1;
            if (lastConfirmedMarker > 0 && lastStepMarker != null) {
                for (int i = lastConfirmedMarker; i < ConfirmedMarkers.Count; i++) {
                    var p1 = ConfirmedMarkers[i].Point;
                    ConfirmedMarkers[i].Point = lastStepMarker.Point + lastStepMarker.Vx * p1.X + lastStepMarker.Vy * p1.Y + lastStepMarker.Vz * p1.Z;
                }
            }

            var sw = new System.IO.StreamWriter("C:\\Temp\\points.3dm");
            ConfirmedMarkers.ForEach(p => sw.WriteLine(p.Point.x.ToString() + '\t' + p.Point.y.ToString() + '\t' + p.Point.z.ToString() + '\t' + p.MarkerID.ToString()));
            sw.Close();
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

        public static void RelevelVerticalAboutOrigin(clsPoint3d verticalVector, ref clsMarkerPoint pt) {
            var p1 = verticalVector.Copy();
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            if (p1.Length < 0.9) return;
            double a = p1.AngleToHorizontal;

            double b = -(PI / 2 - a);

            var p2 = new clsPoint3d(p1.X, p1.Y, 0);
            p2.Normalise();
            var p3 = p1.Cross(p2);
            p3.Normalise();

            var vz = new clsPoint3d(0, 0, 1);
            vz.RotateAboutLine(p3.Line(), b);
            if (vz.Dot(p2) > 0) b = -b;

            //b = -b; // Levelling Debug
            //b = -0; // Levelling Debug

            pt.Origin.RotateAboutLine(p3.Line(), b);
            pt.Point.RotateAboutLine(p3.Line(), b);
            pt.Vx.RotateAboutLine(p3.Line(), b);
            pt.Vy.RotateAboutLine(p3.Line(), b);
            pt.Vz.RotateAboutLine(p3.Line(), b);
            return;
        }

        // Make the step marker flat and then tilt it to match the accelerometer reading
        public static void RelevelStepMarker(ref clsMarkerPoint stepMarker) {
            stepMarker.Vz = new clsPoint3d(0, 0, 1);
            stepMarker.Vy = stepMarker.Vz.Cross(stepMarker.Vx);
            stepMarker.Vy.Normalise();
            stepMarker.Vx = stepMarker.Vy.Cross(stepMarker.Vz);
            stepMarker.Vx.Normalise();

            var p1 = stepMarker.VerticalVect.Copy();
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            if (p1.Length < 0.9) return;
            double a = p1.AngleToHorizontal;
            double b = -(PI / 2 - a);

            var p2 = new clsPoint3d(p1.X, p1.Y, 0);
            p2.Normalise();
            var p3 = p1.Cross(p2);
            p3.Normalise();

            var vz = new clsPoint3d(0, 0, 1);
            vz.RotateAboutLine(p3.Line(), b);
            if (vz.Dot(p2) > 0) b = -b;

            //b = -b; // Levelling Debug
            //b = -0; // Levelling Debug

            stepMarker.Vx.RotateAboutLine(p3.Line(), b);
            stepMarker.Vy.RotateAboutLine(p3.Line(), b);
            stepMarker.Vz.RotateAboutLine(p3.Line(), b);
            return;
        }

        // Make the step marker flat
        public static void FlattenStepMarker(ref clsMarkerPoint stepMarker) {
            stepMarker.Vz = new clsPoint3d(0, 0, 1);
            stepMarker.Vy = stepMarker.Vz.Cross(stepMarker.Vx);
            stepMarker.Vy.Normalise();
            stepMarker.Vx = stepMarker.Vy.Cross(stepMarker.Vz);
            stepMarker.Vx.Normalise();
        }

        // We have measured that the landing board is tilted at the wrong angle
        // Assuming that the previous flight was correct at the start, and wrong by that angle at the end
        // On average, it is out by half that angle

        // How this works:
        // We believe that (0, 0, 1) is in the direction of gravity
        // However, after taking a measurement with the accelerometer, we know it is in the direction of p1
        // So we simply rotate the landing board by the angle between (0, 0, 1) and p1
        public static double RelevelMarkersOnPreviousFlightByHalfAngle(clsPoint3d originPt, clsMarkerPoint lastStepMarker, ref clsMarkerPoint marker) {
            var p1 = lastStepMarker.Vx * lastStepMarker.VerticalVect.x + lastStepMarker.Vy * lastStepMarker.VerticalVect.y + lastStepMarker.Vz * lastStepMarker.VerticalVect.z;
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            if (p1.Length < 0.9) return 0;
            double a = p1.AngleToHorizontal;

            if (IsSameDbl(a, PI / 2) == false) {
                double b = -(PI / 2 - a);

                var p2 = new clsPoint3d(p1.X, p1.Y, 0);
                p2.Normalise();
                var p3 = p1.Cross(p2);
                p3.Normalise();

                var vz = new clsPoint3d(0, 0, 1);
                vz.RotateAboutLine(p3.Line(), b);
                if (vz.Dot(p2) > 0) b = -b;

                //b = -b; // Levelling Debug
                //b = -0; // Levelling Debug

                var vect = new clsLine3d(originPt, originPt + p3);
                marker.Origin.RotateAboutLine(vect, b / 2);
                marker.Point.RotateAboutLine(vect, b / 2);
                return b * 180 / PI;
            }
            return 0;
        }

        public static void ResetMeasurements() {
            myMeasurements.Clear();
            measurementNumber = 0;
            mySuspectedMarkers.Clear();
            ConfirmedMarkers.Clear();
            myVerticalVector = null;
            StepMarker.Confirmed = false;
            StepMarker.Levelled = false;
            StepMarker.Stitched = false;
            StepMarker.VerticalVect = null;
            numImagesProcessed = 0;
        }

        public static void ResetMeasurements2() {
            measurementNumber = 0;
            mySuspectedMarkers.Clear();
            ConfirmedMarkers.Clear();
            StepMarker.Confirmed = false;
            StepMarker.Levelled = false;
            StepMarker.Stitched = false;
            StepMarker.VerticalVect = null;
            numImagesProcessed = 0;
        }

        public static void StopTracking() {
            mySuspectedMarkers.Clear();
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint(myStepMarkerID);
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

        public static void BatchBundleAdjust(Label lblStatus, string myFile, string cameraCalibFile) {
            InitGlobals();

            ResetMeasurements2();

            // myMeasurements[0].SaveCorners();

            if (!File.Exists(cameraCalibFile)) {
                MessageBox.Show("Cannot find Calib.dat file");
                return;
            }

            if (File.Exists(Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"))) {
                try {
                    File.Delete(Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"));
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            try {
                File.Copy(cameraCalibFile, Path.Combine(Path.Combine(myAppPath, "data"), "Calib.dat"));
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                return;
            }

            var param = ReadCameraCalibrationFile(cameraCalibFile);

            myVideoWidth = param.xsize;
            myVideoHeight = param.ysize;
            //myVideoWidth = 3264;
            //myVideoHeight = 2448;
            //myVideoWidth = 4032;
            //myVideoHeight = 3024;



            ARToolKitFunctions.Instance.arwInitialiseAR();
            StartTracking(myVideoWidth, myVideoHeight, false, UseDatumMarkers, arToolkitMarkerType, circlesToUse);

            //DEBUG
            //stitchingMeasurements.Add(139);
            //stitchingMeasurements.Add(269);
            //stitchingMeasurements.Add(493);

            measurementNumber = 0;
            foreach (var measurement in myMeasurements) {

                int numCircles = 0;
                if (UseDatumMarkers && arToolkitMarkerType == 0) numCircles = circlesToUse;

                ARToolKitFunctions.Instance.arwSetMappedMarkersVisible(measurement.MarkerUIDs.Count, measurement.Trans(), measurement.MarkerUIDs.ToArray(), measurement.Corners.SelectMany(c => c).SelectMany(p => new double[] { p.x, p.y }).ToArray());

                RecogniseMarkersFromMeasurements(measurement, UseDatumMarkers, arToolkitMarkerType, circlesToUse);

                //for (int i = 0; i < mySuspectedMarkers.Count; i++) {
                //    if (mySuspectedMarkers[i].MarkerID == 1) {
                //        Console.WriteLine(mySuspectedMarkers[i].Origin.x.ToString() + ", " + mySuspectedMarkers[i].Origin.y.ToString() + ", " + mySuspectedMarkers[i].Origin.z.ToString());
                //    }
                //}

                //if (!SaveHiResSurveyPhoto) {
                //    var n = myMeasurements.IndexOf(measurement);
                //    Console.WriteLine(n.ToString());
                //}

                lblStatus.Text = (myMeasurements.IndexOf(measurement) + 1).ToString() + "/" + myMeasurements.Count().ToString();
                Application.DoEvents();

                if (stitchingMeasurements.Contains(myMeasurements.IndexOf(measurement))) {
                    StartStitching(arToolkitMarkerType);
                }

                measurementNumber++;
            }

            //ConvertSuspectedToConfirmed(true);
            StopTracking();

            var result = SaveToString(false);
            using (var sw = new System.IO.StreamWriter(myFile.Replace(".txt", "-process.txt"))) {
                sw.Write(result);
            }

            // If we have a step marker, do something with it:
            var lastStepMarkerIndex = ConfirmedMarkers.FindLastIndex(m => m.Levelled && (m.ActualMarkerID == myGFMarkerID || m.ActualMarkerID == myStepMarkerID));
            if (lastStepMarkerIndex != -1) {
                var lastStepMarker = ConfirmedMarkers[lastStepMarkerIndex];
                var lastLastStepMarkerIndex = ConfirmedMarkers.FindLastIndex(m => m.MarkerID != lastStepMarker.MarkerID && (m.ActualMarkerID == myStepMarkerID || m.ActualMarkerID == myGFMarkerID));
                if (!lastStepMarker.Stitched) {
                    if (lastLastStepMarkerIndex == -1) {
                        RelevelFromGFMarker();
                    } else {
                        RelevelFromVerticalVector(lastLastStepMarkerIndex + 1, ConfirmedMarkers[lastLastStepMarkerIndex].VerticalVect);
                    }
                    if (lastStepMarker.Levelled) ModifyPreviousFlightCoordinates(lastLastStepMarkerIndex + 1, lastStepMarkerIndex);
                    if (lastLastStepMarkerIndex != -1 && ConfirmedMarkers[lastLastStepMarkerIndex].Stitched) AddMarkersOntoLastStepMarker(lastLastStepMarkerIndex);
                } else {
                    AddMarkersOntoLastStepMarker(lastStepMarkerIndex);
                }
            } else {
                RelevelFromGFMarker();
            }

            var sortedMarkers = ConfirmedMarkers.Select(m => m.Copy()).ToList();
            sortedMarkers.Sort(new MarkerPointComparer()); //Order by Z value and then by ID

            using (var sw = new System.IO.StreamWriter(myFile.Replace(".txt", ".3dm"))) {
                sortedMarkers.ForEach(pt => {
                    if (myBulkheadMarkerIDs.Contains(pt.ActualMarkerID)) {
                        sw.WriteLine(pt.Origin.X.ToString() + '\t' + pt.Origin.Y.ToString() + '\t' + pt.Origin.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString() + '\t' + pt.BulkheadHeight.ToString());
                    } else if (myDoorMarkerIDs.Contains(pt.ActualMarkerID)) {
                        var a = (pt.Point.Point2D() - pt.Origin.Point2D()).Angle();
                        sw.WriteLine(pt.Point.X.ToString() + '\t' + pt.Point.Y.ToString() + '\t' + pt.Point.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString() + '\t' + a.ToString());
                    } else if (myObstructMarkerIDs.Contains(pt.ActualMarkerID)) {
                        sw.WriteLine(pt.Point.X.ToString() + '\t' + pt.Point.Y.ToString() + '\t' + pt.Point.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString() + '\t' + pt.BulkheadHeight.ToString());
                    } else if (myWallMarkerIDs.Contains(pt.ActualMarkerID)) {
                        var a = (pt.Vz.Point2D().Angle());
                        sw.WriteLine(pt.Point.X.ToString() + '\t' + pt.Point.Y.ToString() + '\t' + pt.Point.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString() + '\t' + a.ToString());
                    } else if (pt.ActualMarkerID == -1) { // Camera Captured Obstructions
                        sw.WriteLine(pt.Point.X.ToString() + '\t' + pt.Point.Y.ToString() + '\t' + pt.Point.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString());
                    } else {
                        if ((pt.ActualMarkerID != myStepMarkerID && pt.ActualMarkerID != myGFMarkerID) || pt.VerticalVect == null) {
                            sw.WriteLine(pt.Point.X.ToString() + '\t' + pt.Point.Y.ToString() + '\t' + pt.Point.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString());
                        } else {
                            sw.WriteLine(pt.Point.X.ToString() + '\t' + pt.Point.Y.ToString() + '\t' + pt.Point.Z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString() + '\t' + pt.CorrectionAngle.ToString() + '\t' + pt.ConfirmedImageNumber.ToString());
                        }
                    }

                    //sw.WriteLine(pt.Point.x.ToString() + '\t' + pt.Point.y.ToString() + '\t' + pt.Point.z.ToString() + '\t' + (pt.ActualMarkerID + 1).ToString() + ((pt.ActualMarkerID == myGFMarkerID || p.ActualMarkerID == myStepMarkerID) ? '\t' + pt.CorrectionAngle.ToString() + '\t' + pt.ConfirmedImageNumber.ToString() : string.Empty));
                });
            }

        }

        public static void BatchProcessPhotos(Label lblStatus, string myFolder, string cameraCalibFile) {
            InitGlobals();

            ResetMeasurements2();

            var pngFiles = Directory.GetFiles(myFolder, "*.png").ToList();
            pngFiles.Sort((s1, s2) => {
                var n1 = Convert.ToInt32(Path.GetFileNameWithoutExtension(s1).Replace("Survey", ""));
                var n2 = Convert.ToInt32(Path.GetFileNameWithoutExtension(s2).Replace("Survey", ""));
                return n1.CompareTo(n2);
            });

            var param = ReadCameraCalibrationFile(cameraCalibFile);

            myVideoWidth = param.xsize;
            myVideoHeight = param.ysize;

            // Initialise AR
            // string myVConf = "-module=Image -preset=photo -format=BGRA";
            string myVConf = "-module=Image -width=" + myVideoWidth + " -height=" + myVideoHeight + " -format=MONO";
            ARToolKitFunctions.Instance.arwInitialiseAR();
            ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, cameraCalibFile);
            string artkVersion = ARToolKitFunctions.Instance.arwGetARToolKitVersion();
            string pixelFormat = string.Empty;
            ARToolKitFunctions.Instance.arwSetLogLevel(0);
            var myLogger = new Logger();

            AddMarkersToARToolKit_RevC1();

            var cornersErr = new Emgu.CV.Util.VectorOfPointF();
            var datumsErr = new Emgu.CV.Util.VectorOfPointF();
            var cornersErr2 = new Emgu.CV.Util.VectorOfPointF();

            var arToolkitMarkerType = (int)TargetTypeE.x4Double;

            //DEBUG
            //stitchingMeasurements.Add(139);
            //stitchingMeasurements.Add(269);
            //stitchingMeasurements.Add(493);

            measurementNumber = 0;
            foreach (var pngFile in pngFiles) {
                var grayImage = new Image<Gray, byte>(pngFile);
                Mat imageCopy = Emgu.CV.CvInvoke.Imread(pngFile, Emgu.CV.CvEnum.ImreadModes.Color);
                byte[] grayImageBytes = new byte[grayImage.Data.Length];
                Buffer.BlockCopy(grayImage.Data, 0, grayImageBytes, 0, grayImage.Data.Length);

                var retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(grayImageBytes, arToolkitMarkerType);

                RecogniseMarkersFromImage(arToolkitMarkerType);

                //for (int i = 0; i < mySuspectedMarkers.Count; i++) {
                //    if (mySuspectedMarkers[i].MarkerID == 1) {
                //        Console.WriteLine(mySuspectedMarkers[i].Origin.x.ToString() + ", " + mySuspectedMarkers[i].Origin.y.ToString() + ", " + mySuspectedMarkers[i].Origin.z.ToString());
                //    }
                //}
            }

            //ConvertSuspectedToConfirmed(true);
            StopTracking();

            // If we have a step marker, do something with it:
            var lastStepMarkerIndex = ConfirmedMarkers.FindLastIndex(m => m.ActualMarkerID == myGFMarkerID || m.ActualMarkerID == myStepMarkerID);
            if (lastStepMarkerIndex != -1) {
                var lastStepMarker = ConfirmedMarkers[lastStepMarkerIndex];
                var lastLastStepMarkerIndex = ConfirmedMarkers.FindLastIndex(m => m.MarkerID != lastStepMarker.MarkerID && (m.ActualMarkerID == myStepMarkerID || m.ActualMarkerID == myGFMarkerID));
                if (!lastStepMarker.Stitched) {
                    if (lastLastStepMarkerIndex == -1) {
                        RelevelFromGFMarker();
                    } else {
                        RelevelFromVerticalVector(lastLastStepMarkerIndex + 1, ConfirmedMarkers[lastStepMarkerIndex].VerticalVect);
                    }
                    if (lastStepMarker.Levelled) ModifyPreviousFlightCoordinates(lastLastStepMarkerIndex + 1, lastStepMarkerIndex);
                    if (lastLastStepMarkerIndex != -1 && ConfirmedMarkers[lastLastStepMarkerIndex].Stitched) AddMarkersOntoLastStepMarker(lastLastStepMarkerIndex);
                } else {
                    AddMarkersOntoLastStepMarker(lastStepMarkerIndex);
                }
            } else {
                RelevelFromGFMarker();
            }

            var sortedMarkers = ConfirmedMarkers.Select(m => m.Copy()).ToList();
            sortedMarkers.Sort(new MarkerPointComparer()); //Order by Z value and then by ID

            var sw = new System.IO.StreamWriter(myFolder + "\\Output.3dm");
            sortedMarkers.ForEach(p => sw.WriteLine(p.Point.x.ToString() + '\t' + p.Point.y.ToString() + '\t' + p.Point.z.ToString() + '\t' + (p.ActualMarkerID + 1).ToString() + ((p.ActualMarkerID == myGFMarkerID || p.ActualMarkerID == myStepMarkerID) ? '\t' + p.CorrectionAngle.ToString() + '\t' + p.ConfirmedImageNumber.ToString() : string.Empty)));
            sw.Close();

        }

        private static void StartStitching(int arToolkitMarkerType) {
            int maxConfirmedID = myMaximumMarkerID - 1;

            var lastStepMarkerIndex = ConfirmedMarkers.FindLastIndex(m => m.ActualMarkerID == myStepMarkerID || m.ActualMarkerID == myGFMarkerID);
            if (lastStepMarkerIndex == -1) return;

            for (int i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID > maxConfirmedID) maxConfirmedID = ConfirmedMarkers[i].MarkerID;
            }
            maxConfirmedID = maxConfirmedID + 1;

            for (int i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].MarkerID == ConfirmedMarkers[i].ActualMarkerID && ConfirmedMarkers[i].MarkerID < myMaximumMarkerID) {
                    ConfirmedMarkers[i].MarkerID = maxConfirmedID;
                    maxConfirmedID = maxConfirmedID + 1;
                }
            }

            StepMarker.Stitched = true;
            StepMarker.Confirmed = false;
            StepMarker.Levelled = false;
            ConfirmedMarkers[lastStepMarkerIndex].Stitched = true;
            myLastDatumId = ConfirmedMarkers[lastStepMarkerIndex].ActualMarkerID;



            var sConfig = "";
            if (arToolkitMarkerType == 0) {
                sConfig = "multi_auto;125;65;";
            } else {
                sConfig = "multi_auto;125;80;";
            }
            if (myLastDatumId == myGFMarkerID) {
                if (arToolkitMarkerType == 0) {
                    sConfig = "multi_auto;121;65;";
                } else {
                    sConfig = "multi_auto;121;80;";
                }
            };            

            myMapperMarkerID = ARToolKitFunctions.Instance.arwResetMapperTrackable(myMapperMarkerID, sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARToolKitFunctions.ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);

            // Move the landing marker to the end of the list:
            if (lastStepMarkerIndex != ConfirmedMarkers.Count - 1) {
                ConfirmedMarkers.Add(ConfirmedMarkers[lastStepMarkerIndex]);
                ConfirmedMarkers.RemoveAt(lastStepMarkerIndex);
                lastStepMarkerIndex = ConfirmedMarkers.Count - 1;
            }
            var lastLastStepMarker = ConfirmedMarkers.FindLastIndex(m => m.MarkerID != ConfirmedMarkers.Last().MarkerID && (m.ActualMarkerID == myStepMarkerID || m.ActualMarkerID == myGFMarkerID));

            if (lastLastStepMarker == -1) {
                RelevelFromGFMarker();
            } else {
                RelevelFromVerticalVector(lastLastStepMarker + 1, ConfirmedMarkers[lastLastStepMarker].VerticalVect);
            }
            ModifyPreviousFlightCoordinates(lastLastStepMarker + 1, lastStepMarkerIndex);
            if (lastLastStepMarker != -1) AddMarkersOntoLastStepMarker(lastLastStepMarker);

            mySuspectedMarkers.Clear();
        }

        public static void ModifyPreviousFlightCoordinates(int lastConfirmedMarker, int stepMarkerIndex) {
            double correctionAngle = 0;

            // Correct the previous flight by half the angle error of the step marker
            var stepMarker = ConfirmedMarkers[stepMarkerIndex].Copy();
            for (int i = lastConfirmedMarker; i < ConfirmedMarkers.Count; i++) {
                clsPoint3d originPt = new clsPoint3d(0, 0, 0);
                //if (lastLastConfirmedMarker >= 0) originPt = ConfirmedMarkers[lastLastConfirmedMarker].Origin;
                var marker = ConfirmedMarkers[i];
                correctionAngle = RelevelMarkersOnPreviousFlightByHalfAngle(originPt, stepMarker, ref marker);
            }

            // Flatten this step marker and then take into account the accelerometer reading
            // Keep the X axis constant (in plan view)
            stepMarker = ConfirmedMarkers[stepMarkerIndex];
            stepMarker.CorrectionAngle = correctionAngle;
            //RelevelStepMarker(ref stepMarker);
            FlattenStepMarker(ref stepMarker);
        }

        public static void RelevelFromGFMarker() {
            for (int i = 0; i < ConfirmedMarkers.Count; i++) {
                var pt = ConfirmedMarkers[i];
                RelevelVerticalAboutOrigin(myVerticalVector, ref pt);
            }
        }

        public static void RelevelFromVerticalVector(int startMarkerIndex, clsPoint3d verticalVector) {
            for (int i = startMarkerIndex; i < ConfirmedMarkers.Count; i++) {
                var pt = ConfirmedMarkers[i];
                RelevelVerticalAboutOrigin(verticalVector, ref pt);
            }
        }

        // Add the coordinates of the previously confirmed step marker onto the coordinates of all subsequent markers
        public static void AddMarkersOntoLastStepMarker(int lastStepMarker) {
            if (lastStepMarker > -1) {
                var stepMarker = ConfirmedMarkers[lastStepMarker];

                var pt = new clsPoint3d(-0.0083333753266945, 0.00480327880875209, 0.99995374061421);
                var pt2 = stepMarker.Vx * pt.X + stepMarker.Vy * pt.Y + stepMarker.Vz * pt.Z;

                for (int i = lastStepMarker + 1; i < ConfirmedMarkers.Count; i++) {
                    var p1 = ConfirmedMarkers[i].Origin;
                    ConfirmedMarkers[i].Origin = stepMarker.Origin + stepMarker.Vx * p1.X + stepMarker.Vy * p1.Y + stepMarker.Vz * p1.Z;
                    p1 = ConfirmedMarkers[i].Point;
                    ConfirmedMarkers[i].Point = stepMarker.Origin + stepMarker.Vx * p1.X + stepMarker.Vy * p1.Y + stepMarker.Vz * p1.Z;
                    p1 = ConfirmedMarkers[i].Vx;
                    ConfirmedMarkers[i].Vx = stepMarker.Vx * p1.X + stepMarker.Vy * p1.Y + stepMarker.Vz * p1.Z;
                    p1 = ConfirmedMarkers[i].Vy;
                    ConfirmedMarkers[i].Vy = stepMarker.Vx * p1.X + stepMarker.Vy * p1.Y + stepMarker.Vz * p1.Z;
                    p1 = ConfirmedMarkers[i].Vz;
                    ConfirmedMarkers[i].Vz = stepMarker.Vx * p1.X + stepMarker.Vy * p1.Y + stepMarker.Vz * p1.Z;
                }
            }
        }

        //public static void StartStitching() {
        //    int i;
        //    int maxConfirmedID = myMaximumMarkerID - 1;

        //    for (i = 0; i < ConfirmedMarkers.Count; i++) {
        //        if (ConfirmedMarkers[i].MarkerID > maxConfirmedID) maxConfirmedID = ConfirmedMarkers[i].MarkerID;
        //    }
        //    maxConfirmedID = maxConfirmedID + 1;

        //    for (i = 0; i < ConfirmedMarkers.Count; i++) {
        //        if (ConfirmedMarkers[i].MarkerID < myMaximumMarkerID) {
        //            ConfirmedMarkers[i].MarkerID = maxConfirmedID;
        //            maxConfirmedID = maxConfirmedID + 1;
        //        }
        //    }

        //    StepMarker.Stitched = true;
        //    var lastStepMarkerIndex = ConfirmedMarkers.FindLastIndex(m => m.ActualMarkerID == myGFMarkerID || m.ActualMarkerID == myStepMarkerID);
        //    if (lastStepMarkerIndex > -1) {
        //        ConfirmedMarkers[lastStepMarkerIndex].Stitched = true;
        //        myLastDatumId = ConfirmedMarkers[lastStepMarkerIndex].ActualMarkerID;
        //    }

        //    mySuspectedMarkers.Clear();

        //    var sConfig = "multi_auto;125;80;";
        //    if (myLastDatumId == myGFMarkerID) sConfig = "multi_auto;121;80;";
        //    myMapperMarkerID = ARToolKitFunctions.Instance.arwResetMapperTrackable(myMapperMarkerID, sConfig);
        //    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARToolKitFunctions.ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);

        //    stitchingsPeformed++;
        //}

        public static void RecogniseMarkersFromMeasurements(clsMeasurement measurement, bool useDatums, int arToolkitMarkerType, int circlesToUse) {

            Data.Clear();

            for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
                DetectMarkerVisible(myMarkerIDs[i]);
            }
            DetectMarkerVisible(myStepMarkerID);
            DetectMarkerVisible(myGFMarkerID);
            DetectMarkerVisible(myLeftBulkheadMarker1ID);
            DetectMarkerVisible(myRightBulkheadMarker1ID);
            DetectMarkerVisible(myLeftBulkheadMarker2ID);
            DetectMarkerVisible(myRightBulkheadMarker2ID);
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
            DetectMarkerVisible(myRailStartMarkerID);
            DetectMarkerVisible(myRailEndMarkerID);

            Console.WriteLine("Markers Seen: " + string.Join(",", Data.MarkersSeenID.Select(i => (i + 1).ToString()).ToArray()));

            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            int numCircles = 0;
            if (useDatums && arToolkitMarkerType == 0) numCircles = circlesToUse;
            bool usingRevAMarkerType = arToolkitMarkerType == -1;

            //Start passing the marker info across to the multi-marker
            //Do this in 2 phases so that we can check if a marker has been accidentally moved
            //The first phase sets the visibility of the multi-marker
            var initialiseMultiMarker = ARToolKitFunctions.Instance.arwAddMappedMarkers(myMapperMarkerID, myLastDatumId, measurement.MarkerUIDs.Count, measurement.Trans(), measurement.MarkerUIDs.ToArray(), measurement.Corners.SelectMany(c => c).SelectMany(p => new double[] { p.X, p.Y }).ToArray());

            //Is the multi-marker visible?
            //If not, we don't need to do any checks, and we don't need to update it
            if (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMapperMarkerID, mv, corners, out int numCorners)) {

                OpenTK.Matrix4d myModel = MatrixFromArray(mv);
                myModel = OpenTK.Matrix4d.Invert(myModel);

                //Check for markers having been moved
                var checksOK = true;
                var markersToIgnore = new List<int>();
                for (int i = 0; i < Data.MarkersSeenID.Count; i++) {
                    var markerOK = true;
                    var markerId = Data.MarkersSeenID[i];

                    mv = Data.ModelViewMatrix[i];
                    OpenTK.Matrix4d myModel2 = MatrixFromArray(mv);
                    //myModel2 = OpenTK.Matrix4d.Invert(myModel2);
                    myModel2 = myModel2 * myModel; // This multiplication might need to be reversed
                    mv = ArrayFromMatrix(myModel2);
                    var pt = new clsPoint3d(mv[12], mv[13], mv[14]); //This is the coordinates of the marker in the multi-marker space
                    //var pt = new clsPoint3d(myModel2.M41, myModel2.M42, myModel2.M43); //This is the coordinates of the marker in the multi-marker space

                    var getTrackablePatternConfigResult = ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(markerId, 0, mv, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);
                    if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, barcodeID, mv)) {
                        var pt2 = new clsPoint3d(mv[12], mv[13], mv[14]); //This is where we last saw this marker
                        var d = pt.Dist(pt2);
                        if (d > 50) { //50mm is a guess. If this is coming out completely wrong, then the matrix multiplication above needs to be reversed
                            markerOK = false;
                            //checksOK = false;
                        }
                    }
                }

                //Now update the multi-marker
                var res = ARToolKitFunctions.Instance.arwUpdateMultiMarker(myMapperMarkerID, myLastDatumId, measurement.MarkerUIDs.Count, measurement.Trans(), measurement.MarkerUIDs.ToArray(), measurement.Corners.SelectMany(c => c).SelectMany(p => new double[] { p.X, p.Y }).ToArray(), initialiseMultiMarker);

                if (res == -1 || !checksOK) { //res = -1 indicates that gtsam has thrown an exception
                                              // Show an alert
                    var markerId = -1;
                    if (markersToIgnore.Any()) markerId = markersToIgnore.First();
                    //(mySurveyForm.ViewController as NSObject).InvokeOnMainThread(
                    //    () => mySurveyForm.ViewController.ShowMarkerWarning((n) => IgnoreMarkerId(n), () => RestartFlight(), markerId)
                    //    );
                } else {

                    //Update positions of confirmed markers by bundle adjustment
                    SaveHiResSurveyPhoto = false;
                    AddNewSuspectedMarkers(usingRevAMarkerType);
                    ConvertSuspectedToConfirmed();
                    UpdateConfirmedMarkersWithBundleAdjustment(usingRevAMarkerType);
                }
            }

            if (SaveHiResSurveyPhoto) {
                numImagesProcessed = numImagesProcessed + 1;
            }

            Data.GetMarkersCopy();

        }

        public static void RecogniseMarkersFromImage(int arToolkitMarkerType) {

            Data.Clear();

            for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
                DetectMarkerVisible(myMarkerIDs[i]);
            }
            DetectMarkerVisible(myStepMarkerID);
            DetectMarkerVisible(myGFMarkerID);
            DetectMarkerVisible(myLeftBulkheadMarker1ID);
            DetectMarkerVisible(myRightBulkheadMarker1ID);
            DetectMarkerVisible(myLeftBulkheadMarker2ID);
            DetectMarkerVisible(myRightBulkheadMarker2ID);
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
            DetectMarkerVisible(myRailStartMarkerID);
            DetectMarkerVisible(myRailEndMarkerID);

            var measurement = new clsMeasurement();
            measurement.MeasurementNumber = myMeasurements.Count;
            for (int i = 0; i < Data.MarkersSeenID.Count; i++) {
                var markerID = Data.MarkersSeenID[i];

                // RevC1 == Rev A  = type -1 || 0
                // RevC7 - single bar code riser markers  1

                if ((arToolkitMarkerType != 1 && markerID <= myStepMarkerID) || markerID == myGFMarkerID || markerID == myStepMarkerID) {
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

            //Update the GTSAM mapper
            int myOriginMarkerID = myGFMarkerID;
            if (stitchingMeasurements.Any()) myOriginMarkerID = myStepMarkerID;
            if (myLastDatumId == myGFMarkerID) myOriginMarkerID = myGFMarkerID;

            // Just use RevA and 0 circles.
            int numCircles = 0;
            ARToolKitFunctions.Instance.arwAddMappedMarkers(myMapperMarkerID, myOriginMarkerID, measurement.MarkerUIDs.Count, measurement.Trans(), measurement.MarkerUIDs.ToArray(), measurement.Corners.SelectMany(c => c).SelectMany(p => new double[] { p.X, p.Y }).ToArray());

            //Update positions of confirmed markers by bundle adjustment
            double[] modelMatrix = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] cornerCoords = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            SaveHiResSurveyPhoto = false;
            if (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMapperMarkerID, modelMatrix, cornerCoords, out int numCorners)) {
                SaveHiResSurveyPhoto = true;
                AddNewSuspectedMarkers(usingRevAMarkerType: true);
                ConvertSuspectedToConfirmed();
                UpdateConfirmedMarkersWithBundleAdjustment(usingRevAMarkerType: true);
            }

            if (SaveHiResSurveyPhoto) {
                numImagesProcessed = numImagesProcessed + 1;
            }

            Data.GetMarkersCopy();

        }

        private static double[] ARTransFromMatrix(double[] matrix) {
            var trans = new double[12];
            trans[0] = matrix[0];
            trans[1] = matrix[4];
            trans[2] = matrix[8];
            trans[3] = matrix[12];
            trans[4] = matrix[1];
            trans[5] = matrix[5];
            trans[6] = matrix[9];
            trans[7] = matrix[13];
            trans[8] = matrix[2];
            trans[9] = matrix[6];
            trans[10] = matrix[10];
            trans[11] = matrix[14];
            return trans;
        }

        private static void DetectMapperMarkerVisible(int myMapperMarkerID, int myBarcodeID, ref List<clsPGPoint> pts, bool useDatums) {
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, myBarcodeID, mv)) {

                OpenTK.Matrix4d matrix = MatrixFromArray(mv);
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

        private static void DetectMapperMarkerVisible(int myMapperMarkerID, int myBarcodeID, ref List<clsPGPoint> pts, Matrix4d extraTransform) {
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, myBarcodeID, mv)) {

                OpenTK.Matrix4d matrix = MatrixFromArray(mv);
                var pt = new OpenTK.Vector4d(mv[12], mv[13], mv[14], 0);
                int markerID = myBarcodeID;
                if (myBarcodeID == 125) {

                } else if (markerID <= 100) {
                    pt = new OpenTK.Vector4d(140.0f, -45.0f, 0.0f, 1);
                    pt = OpenTK.Vector4d.Transform(pt, matrix);
                } else {
                    pt = new OpenTK.Vector4d(140.0, 45.0, 0.0f, 1);
                    pt = OpenTK.Vector4d.Transform(pt, matrix);
                }
                pt.W = 1;
                pt = OpenTK.Vector4d.Transform(pt, extraTransform);
                pts.Add(new clsPGPoint(pt.X, pt.Y, pt.Z, myBarcodeID));
            }
        }

        private static clsPGPoint DetectMapperMarkerVisible(int myMapperMarkerID, int myBarcodeID) {
            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, myBarcodeID, mv)) {
                return new clsPGPoint(mv[12], mv[13], mv[14], myBarcodeID);
            }
            return null;
        }

        public static ARParam ReadCameraCalibrationFile(string myFile) {
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
                if (br.PeekChar() != -1) param.dist_factor[i] = byteSwapDouble(br.ReadDouble());
            }
            br.Close();
            sr.Close();
            return param;
        }

    }

}

using System;
using static System.Math;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using System.IO;
using OpenTK;
using static ARToolKitFunctions;
using System.Drawing;
using Emgu.CV;

namespace BatchProcess {
    public static class mdlDetectPhotos {
        private static bool _isTrackingEnabled = false;
        public static bool isTrackingEnabled {
            get { return _isTrackingEnabled; }
            set {
                _isTrackingEnabled = value;
            }
        }

        private static bool UseDatums;

        static Logger myLogger;

        public static double myPGAngleTol = 15;
        public static double myPGPointTol = 50;
        public static double myPGAxisTol = 0.75;
        public static int myPGMinMarkers = 2;
        public static double myPGMatrixThreshold = 0.75;
        public static bool myPGUsePoseEstimation = false;

        public static clsPoint3d myVerticalVector = new clsPoint3d(0, 0, 1.0f);
        public static clsPoint3d myStepVerticalVector = new clsPoint3d(0, 0, 1.0f);
        public static int myVideoWidth;
        public static int myVideoHeight;
        public static int myVideoWidthHiRes;
        public static int myVideoHeightHiRes;
        public static int myVideoPixelSize;

        private static List<clsMarkerPoint2> _confirmedMarkerPoints = new List<clsMarkerPoint2>();
        public static bool UseNewStyleMarkers = true;

        public static RecognisedMarkers Data = new RecognisedMarkers();

        public static List<clsMarkerPoint2> ConfirmedMarkers {
            get { return _confirmedMarkerPoints; }
            set {
                _confirmedMarkerPoints = value;
                ConfirmedMarkersUpdated?.Invoke();
            }
        }
        public static List<clsMarkerPoint2> mySuspectedMarkers = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myBulkheadMarkers = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myDoorMarkers = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myObstructMarkers = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myWallMarkers = new List<clsMarkerPoint2>();

        //Bundle adjusted markers
        public static List<clsMarkerPoint2> ConfirmedMarkersAdj = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myBulkheadMarkersAdj = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myDoorMarkersAdj = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myObstructMarkersAdj = new List<clsMarkerPoint2>();
        public static List<clsMarkerPoint2> myWallMarkersAdj = new List<clsMarkerPoint2>();

        public delegate void BlankEventHandler();
        public static event BlankEventHandler ConfirmedMarkersUpdated;

        public static List<clsMarkerPoint2> ConfirmedMarkersCopy = new List<clsMarkerPoint2>();

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
        public static int myMapperMarkerID = 116;
        public static int myMaximumMarkerID = 117; //Please keep this up to date
        static List<int> myBulkheadMarkerIDs = new List<int>();
        static List<int> myDoorMarkerIDs = new List<int>();
        static List<int> myObstructMarkerIDs = new List<int>();
        static List<int> myWallMarkerIDs = new List<int>();
        static List<int> myAllFeatureMarkerIDs = new List<int>();

        public static List<int> myMarkerIDs = new List<int>();
        public static clsMarkerPoint2 myGFMarker = new clsMarkerPoint2();

        private static clsMarkerPoint2 _stepMarkerPoint = new clsMarkerPoint2();
        public static clsMarkerPoint2 StepMarker {
            get { return _stepMarkerPoint; }
            set {
                _stepMarkerPoint = value;
                StepMarkerChanged?.Invoke();
            }
        }
        public static Action StepMarkerChanged;

        //static Logger myLogger;
        public static bool mySaveSnapshot = false;
        public static bool myThresholdView = false;

        private static bool _inSettingsMode = false;

        public static bool inSettingsMode {
            get { return _inSettingsMode; }
            set {
                _inSettingsMode = value;
            }
        }


        public static ObservableCollection<string> DebugStringList { get; } = new ObservableCollection<string>();
        //Dim myLastBitmap As Bitmap

        public static float myNear;
        public static float myFar;

        public static bool StartTracking(int hiResX, int hiResY, bool useDatums)
        {
            UseDatums = useDatums;

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

                isTrackingEnabled = true;
                return true;
            }
            
            myNear = 10;
            myFar = 3000;
            myVideoWidthHiRes = hiResX;
            myVideoHeightHiRes = hiResY;
            InitialiseARToolKit(hiResX, hiResY);
            //ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile, myVConfLowRes, "data/" + myCameraFileLowRes, myNear, myFar, hiResX, hiResYXY);

            ARToolKitFunctions.Instance.arwSetLogLevel(0);
            myLogger = new Logger();

            if (!useDatums) {
                AddMarkersToARToolKit();
            } else {
                AddDatumMarkersToARToolKit();
            }

            //mySuspectedMarkers.Clear()
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint2(myStepMarkerID, myStepMarkerID);
            }

            isTrackingEnabled = true;
            return true;
        }

        public static void AddMarkersToARToolKit() {

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

            //float[] myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //bool b = ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myGFMarkerID, 0, myMatrix, out float width, out float height, out int imageSizeX, out int imageSizeY, out int barcodeID);
            //string sConfig = "multi_auto;" + barcodeID + ";" + ((int)width) + ";";
            //string sConfig = "multi_auto;" + myGFMarkerID + ";80;";
            string sConfig = "multi_auto;121;80;";
            myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);

            myMaximumMarkerID = myMapperMarkerID + 1; //Please keep this up to date
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
        }

        public static void AddDatumMarkersToARToolKit() {
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
            DebugStringList.Clear();

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

            //float[] myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //bool b = ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myGFMultiMarkerID, 0, myMatrix, out float width, out float height, out int imageSizeX, out int imageSizeY, out int barcodeID);
            //string sConfig = "multi_auto;" + barcodeID + ";" + ((int)width) + ";";
            string sConfig = "multi_auto;0;80;";
            myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);

            myMaximumMarkerID = myMapperMarkerID + 1; //Please keep this up to date
            myBulkheadMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID };
            myDoorMarkerIDs = new List<int> { myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID };
            myObstructMarkerIDs = new List<int> { myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID };
            myWallMarkerIDs = new List<int> { myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
            myAllFeatureMarkerIDs = new List<int> { myLeftBulkheadMarkerID, myRightBulkheadMarkerID,
                myDoorHingeRightMarkerID, myDoorFrameRightMarkerID, myDoorHingeLeftMarkerID, myDoorFrameLeftMarkerID,
                myObstruct1MarkerID, myObstruct2MarkerID, myObstruct3MarkerID, myObstruct4MarkerID,
                myWall1MarkerID, myWall2MarkerID, myWall3MarkerID, myWall4MarkerID };
        }

        private static void InitialiseARToolKit(int hiResX, int hiResY)
        {
            string myCameraFile = "data\\calib.dat";
            //string myVConf = "-preset=720p -format=BGRA";
            //string myVConf = "-flipV";
            string myVConf = "-module=Image -width=" + hiResX.ToString() + " -height=" + hiResY.ToString() + " -format=MONO";
            // ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile, myVConf, myCameraFile, myNear, myFar, hiResX, hiResY, hiResX, hiResY);
            ARToolKitFunctions.Instance.arwInitARToolKit(myVConf, myCameraFile);
        }

        public static void RecogniseMarkers(byte[] imageBytes, string myFile, ARParam arParams)
        {
            //Data.Clear();

            var retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(imageBytes, UseDatums);

            double[] mv = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            retB = ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMapperMarkerID, mv, corners, out int numCorners);
            if (!retB) return;
            var mapperMatrix = MatrixFromArray(mv);
            var imagePoints = new Emgu.CV.Util.VectorOfPointF();
            var cornerPoints = new Emgu.CV.Util.VectorOfPointF();

            for (int markerID = 0; markerID < 101; markerID++) {

                double[] mv2 = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                retB = ARToolKitFunctions.Instance.arwQueryMarkerTransformation(markerID, mv2, corners, out numCorners);
                if (!retB) continue;

                for (int i = 0; i < numCorners; i++) {
                    double ox, oy;
                    if (!UseDatums) {
                        mdlEmguCalibration.arParamIdeal2Observ(arParams.dist_factor, corners[i * 2], corners[i * 2 + 1], out ox, out oy, arParams.dist_function_version);
                    } else { //For datums, imagePoints are already in Observ coordinates
                        ox = corners[i * 2];
                        oy = corners[i * 2 + 1];
                    }
                    imagePoints.Push(new PointF[] { new PointF((float)ox, (float)oy) });
                }

                var numBarcodes = ARToolKitFunctions.Instance.arwGetTrackablePatternCount(markerID);
                for (int i = 0; i < numBarcodes; i++) {
                    ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(markerID, i, mv, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);

                    if (ARToolKitFunctions.Instance.arwQueryTrackableMapperTransformation(myMapperMarkerID, barcodeID, mv)) {
                        var barcodeMatrix = MatrixFromArray(mv);

                        barcodeMatrix = Matrix4d.Mult(barcodeMatrix, mapperMatrix);
                        mv = ArrayFromMatrix(barcodeMatrix);
                        var trans = mdlEmguDetection.OpenGL2Trans(mv);

                        var pts2d = new List<clsPoint>();
                        var cornerPoints2 = new Emgu.CV.Util.VectorOfPointF();
                        if (!UseDatums) {
                            pts2d.Add(new clsPoint(-40, -40));
                            pts2d.Add(new clsPoint(40, -40));
                            pts2d.Add(new clsPoint(40, 40));
                            pts2d.Add(new clsPoint(-40, 40));
                        } else {
                            if (barcodeID == 0 || barcodeID == 1) {
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
                        }

                        for (int j = 0; j < pts2d.Count; j++) {
                            var pt = mdlEmguDetection.ModelToImageSpace(arParams, trans, pts2d[j]);
                            cornerPoints2.Push(new PointF[] { new PointF((float)pt.X, (float)pt.Y) });
                        }
                        cornerPoints.Push(cornerPoints2.ToArray());
                    }
                }
            }

            if (imagePoints.Size > 0 || cornerPoints.Size > 0) {
                Mat imageCopy = Emgu.CV.CvInvoke.Imread(myFile, Emgu.CV.CvEnum.ImreadModes.Color);
                if (imagePoints.Size > 0) mdlEmguDetection.DrawCornersOnImage(imageCopy, imagePoints, System.Drawing.Color.Green);
                if (cornerPoints.Size > 0) mdlEmguDetection.DrawCornersOnImage(imageCopy, cornerPoints, System.Drawing.Color.Red);
                CvInvoke.Imwrite(Path.GetDirectoryName(myFile) + "\\Corners-" + Path.GetFileNameWithoutExtension(myFile) + ".png", imageCopy, new KeyValuePair<Emgu.CV.CvEnum.ImwriteFlags, int>(Emgu.CV.CvEnum.ImwriteFlags.PngCompression, 3));
            }

            //ARToolKitFunctions.Instance.arwGetProjectionMatrix(myNear, myFar, Data.ProjMatrix);
            //ARToolKitFunctions.Instance.arwListTrackables(myMapperMarkerID);

            //for (int i = 0; i <= myMarkerIDs.Count - 1; i++) {
            //    DetectMarkerVisible(myMarkerIDs[i]);
            //}

            //DetectMarkerVisible(myStepMarkerID);
            //DetectMarkerVisible(myGFMarkerID);
            //DetectMarkerVisible(myLeftBulkheadMarkerID);
            //DetectMarkerVisible(myRightBulkheadMarkerID);
            //DetectMarkerVisible(myDoorHingeRightMarkerID);
            //DetectMarkerVisible(myDoorFrameRightMarkerID);
            //DetectMarkerVisible(myDoorHingeLeftMarkerID);
            //DetectMarkerVisible(myDoorFrameLeftMarkerID);
            //DetectMarkerVisible(myObstruct1MarkerID);
            //DetectMarkerVisible(myObstruct2MarkerID);
            //DetectMarkerVisible(myObstruct3MarkerID);
            //DetectMarkerVisible(myObstruct4MarkerID);
            //DetectMarkerVisible(myWall1MarkerID);
            //DetectMarkerVisible(myWall2MarkerID);
            //DetectMarkerVisible(myWall3MarkerID);
            //DetectMarkerVisible(myWall4MarkerID);

            //ProcessMarkers();
            //ProcessMarkers( true); //Running this twice, because we also want to store information about newly seen markers in relation to other newly seen markers.

            //Data.GetMarkersCopy();

        }

        public static Matrix4d MatrixFromArray(double[] mv) {
            return new Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], 1);
        }

        public static double[] ArrayFromMatrix(Matrix4d m) {
            return new double[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 };
        }

        private static void DetectMarkerVisible(int myMarkerID) {

            double[] myMatrix = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] corners = new double[32] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMarkerID, myMatrix, corners, out int numCorners)) {
                clsPoint3d pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
                if (pt.Length < 2000) {

                    clsPoint3d myCameraPoint = PointFromInvMatrix(myMatrix);

                    if (IsSameDbl(myCameraPoint.Length, 0) == false) {
                        myCameraPoint.Normalise();
                        double a1 = Abs(myCameraPoint.AngleToHorizontal) * 180 / PI;
                        if ((a1 > 10 && a1 < 80) || myWallMarkerIDs.Contains(myMarkerID)) {
                        // if ((a1 > 22 && a1 < 68) || myWallMarkerIDs.Contains(myMarkerID)) {
                            Data.MarkersSeenID.Add(myMarkerID);
                            Data.ModelViewMatrix.Add(myMatrix);
                        }
                    }

                }
            }
        }

        public static void ProcessMarkers(bool includeSuspectedMarkers = false) {
            int i, j, k;
            int i1;
            clsMarkerPoint2 pt;
            int myMarkerID, mySeenFromMarkerID;
            List<int> mySuspectConfirmedID = new List<int>();
            bool myGFConfirmed;
            bool myStepConfirmed;
            int myNumConfirmed;
            clsMarkerPoint2 myConfirmedMarker;
            List<clsPoint3d> myOriginPts = new List<clsPoint3d>();
            List<clsPoint3d> myXAxisPts = new List<clsPoint3d>();
            List<clsPoint3d> myYAxisPts = new List<clsPoint3d>();
            List<clsMarkerPoint2> myHistoricMarkers = new List<clsMarkerPoint2>();
            List<int> mySeenFromMarkerIDs = new List<int>();
            string myErrorString = "";
            double a1 = 0, a2 = 0;
            clsPoint3d v1 = null, v2 = null, v3 = null, v4 = null;
            bool myMarkerConfirmed = false, b1 = false, b2 = false;
            bool mySuspectedMarkerAdded = false;
            bool myHistoricMarkerUsed = false;

            if (!includeSuspectedMarkers) {
                if (Data.MarkersSeenID.Count == 0 && Data.GetMarkerVisible(myStepMarkerID) == false &&
                    Data.GetMarkerVisible(myLeftBulkheadMarkerID) == false && Data.GetMarkerVisible(myRightBulkheadMarkerID) == false &&
                    Data.GetMarkerVisible(myDoorHingeRightMarkerID) == false && Data.GetMarkerVisible(myDoorFrameRightMarkerID) == false &&
                    Data.GetMarkerVisible(myDoorHingeLeftMarkerID) == false && Data.GetMarkerVisible(myDoorFrameLeftMarkerID) == false &&
                    Data.GetMarkerVisible(myObstruct1MarkerID) == false && Data.GetMarkerVisible(myObstruct2MarkerID) == false &&
                    Data.GetMarkerVisible(myObstruct3MarkerID) == false && Data.GetMarkerVisible(myObstruct4MarkerID) == false &&
                    Data.GetMarkerVisible(myWall1MarkerID) == false && Data.GetMarkerVisible(myWall2MarkerID) == false &&
                    Data.GetMarkerVisible(myWall3MarkerID) == false && Data.GetMarkerVisible(myWall4MarkerID) == false)
                    return;
            }

            mySuspectedMarkers.Sort(new SuspectedMarkerPoint2Comparer());

            //if (AppPreferences.GTSAMBundleAdjustment) ARToolKitFunctions.Instance.arwListTrackables(myMapperMarkerID);

            //Take a measurement of the step marker
            if (includeSuspectedMarkers == false && StepMarker.Confirmed == false && Data.GetMarkerVisible(myStepMarkerID)) {
                j = Data.GetMarkerIndex(myStepMarkerID);

                if (Data.GetMarkerVisible(myGFMarkerID)) {
                    if (AddSuspectedMarker(myStepMarkerID, j, myGFMarkerID, Data.GetMarkerIndex(myGFMarkerID))) mySuspectedMarkerAdded = true;
                }

                for (i = 0; i < ConfirmedMarkers.Count; i++) {
                    if (Data.GetMarkerVisible(ConfirmedMarkers[i].MarkerID)) {
                        i1 = Data.GetMarkerIndex(ConfirmedMarkers[i].MarkerID);
                        if (AddSuspectedMarker(myStepMarkerID, j, ConfirmedMarkers[i].MarkerID, i1)) mySuspectedMarkerAdded = true;
                    }
                }
            }


            //Take a measurement of each of the other markers
            for (j = 0; j <= Data.MarkersSeenID.Count - 1; j++) {
                myMarkerID = Data.MarkersSeenID[j];

                if (myMarkerID == myGFMarkerID) continue; //Ignore the GF marker
                if (ConfirmedMarkerIDs().Contains(myMarkerID)) continue; //Ignore confirmed markers
                if (myMarkerID == myStepMarkerID) continue; //Ignore the step marker

                if (CheckSuspectedMarker(myMarkerID, j, includeSuspectedMarkers)) mySuspectedMarkerAdded = true;
            }

            //Check if we can convert a suspected marker to a confirmed one
            for (i = 0; i <= mySuspectedMarkers.Count - 1; i++) {

                //Avoid measuring obstructions when a confirmed one exists. Wait until the user clears them
                if (SuspectedMarkerIsConfirmedObstruction(mySuspectedMarkers[i].MarkerID)) continue;

                if (mySuspectedMarkers[i].Confirmed == false) {
                    if (mySuspectedMarkers[i].OriginPoints.Count > 50) {

                        if (mySuspectedMarkers[i].MarkerID == myStepMarkerID) {
                            DebugStringList.Add("Step Marker Reset.");
                        } else if (myBulkheadMarkerIDs.Contains(mySuspectedMarkers[i].MarkerID)) {
                            DebugStringList.Add("Bulkhead Marker Reset.");
                        } else if (myDoorMarkerIDs.Contains(mySuspectedMarkers[i].MarkerID)) {
                            DebugStringList.Add("Door Marker Reset.");
                        } else {
                            DebugStringList.Add("Marker " + mySuspectedMarkers[i].NewMarkerID() + " Reset.");
                        }
                        mySuspectedMarkers[i].Clear();
                    } else if (mySuspectedMarkers[i].OriginPoints.Count > 1 && mySuspectedMarkers[i].Origin.Length > myTol &&
                          mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {


                        //DEBUG:
                        if (mySuspectedMarkers[i].OriginPoints.Count > 1 && mySuspectedMarkers[i].Origin.Length > myTol &&
                          mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {
                            mySuspectedMarkers[i].SetEndPointBasedOnZVectors();
                        }

                        mySuspectedMarkers[i].Confirmed = true;
                        v1 = myVerticalVector.Copy();
                        v1.Normalise();
                        double a = Abs(Asin(mySuspectedMarkers[i].VZ.Cross(v1).Length)) * 180 / Math.PI;

                        if (mySuspectedMarkers[i].MarkerID == myStepMarkerID) {
                            DebugStringList.Add(string.Format("Step Marker Confirmed. {0:0.0}° From Vertical.", a));
                        } else if (myBulkheadMarkerIDs.Contains(mySuspectedMarkers[i].MarkerID)) {
                            DebugStringList.Add(string.Format("Bulkhead Marker Confirmed."));
                        } else if (myDoorMarkerIDs.Contains(mySuspectedMarkers[i].MarkerID)) {
                            DebugStringList.Add(string.Format("Door Marker Confirmed."));
                        } else {
                            DebugStringList.Add(string.Format("Marker {0:0} Confirmed. {1:0.0}° From Vertical.", mySuspectedMarkers[i].NewMarkerID(), a));
                        }
                    }
                }
            }

            //Convert the "confirmed" suspects to Confirmed
            i = 0;
            while (i <= mySuspectedMarkers.Count - 1) {
                if (mySuspectedMarkers[i].Confirmed == false) {
                    i = i + 1;
                    continue;
                }

                myMarkerID = mySuspectedMarkers[i].MarkerID;
                mySeenFromMarkerID = mySuspectedMarkers[i].SeenFromMarkerID;
                myGFConfirmed = false;
                myStepConfirmed = false;
                myNumConfirmed = 0;

                //Takes the average measurement from the other confirmed markers:
                myOriginPts.Clear();
                myXAxisPts.Clear();
                myYAxisPts.Clear();
                myHistoricMarkers.Clear();
                mySeenFromMarkerIDs.Clear();

                for (j = 0; j <= mySuspectedMarkers.Count - 1; j++) {
                    if (mySuspectedMarkers[j].MarkerID == myMarkerID && mySuspectedMarkers[j].Origin.Length > myTol) {
                        myHistoricMarkerUsed = false;
                        if (mySuspectedMarkers[j].SeenFromMarkerID == myGFMarkerID) {
                            clsPoint3d p1 = mySuspectedMarkers[j].Origin.Copy();
                            RelevelPointFromGF(p1);
                            myOriginPts.Add(p1);
                            p1 = mySuspectedMarkers[j].XAxisPoint.Copy();
                            RelevelPointFromGF(p1);
                            myXAxisPts.Add(p1);
                            p1 = mySuspectedMarkers[j].YAxisPoint.Copy();
                            RelevelPointFromGF(p1);
                            myYAxisPts.Add(p1);
                            if (mySuspectedMarkers[j].Confirmed) myGFConfirmed = true;
                            myHistoricMarkerUsed = true;
                        } else if (myDoorMarkerIDs.Contains(mySuspectedMarkers[j].SeenFromMarkerID)) {
                            for (k = myDoorMarkers.Count - 1; k >= 0; k--) {
                                if (myDoorMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                    pt = myDoorMarkers[k];
                                    myOriginPts.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                                    myXAxisPts.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                                    myYAxisPts.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                                    if (mySuspectedMarkers[j].Confirmed) myNumConfirmed = myNumConfirmed + 1;
                                    myHistoricMarkerUsed = true;
                                    break;
                                }
                            }
                        } else if (myObstructMarkerIDs.Contains(mySuspectedMarkers[j].SeenFromMarkerID)) {
                            for (k = myObstructMarkers.Count - 1; k >= 0; k--) {
                                if (myObstructMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                    pt = myObstructMarkers[k];
                                    myOriginPts.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                                    myXAxisPts.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                                    myYAxisPts.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                                    if (mySuspectedMarkers[j].Confirmed) myNumConfirmed = myNumConfirmed + 1;
                                    myHistoricMarkerUsed = true;
                                    break;
                                }
                            }
                        } else {
                            for (k = 0; k <= ConfirmedMarkers.Count - 1; k++) {
                                if (ConfirmedMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                    if (ConfirmedMarkers[k].MarkerID == myStepMarkerID && ConfirmedMarkers[k].Levelled == false) continue;
                                    pt = ConfirmedMarkers[k];
                                    myOriginPts.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                                    myXAxisPts.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                                    myYAxisPts.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                                    if (mySuspectedMarkers[j].Confirmed) {
                                        if (ConfirmedMarkers[k].MarkerID == myStepMarkerID) {
                                            myStepConfirmed = true;
                                        } else {
                                            myNumConfirmed = myNumConfirmed + 1;
                                        }
                                    }
                                    myHistoricMarkerUsed = true;
                                }
                            }
                        }

                        if (myHistoricMarkerUsed) {
                            myHistoricMarkers.Add(mySuspectedMarkers[j].Copy());
                            mySeenFromMarkerIDs.Add(mySuspectedMarkers[j].SeenFromMarkerID);
                        }
                    }
                }

                //Now we can convert our suspected marker to a Confirmed marker
                if ((myGFConfirmed && (mySuspectedMarkers[i].Origin.Z < 50 || myNumConfirmed >= 1)) || myStepConfirmed || (myNumConfirmed >= 2 || (myNumConfirmed >= 1 && (myMarkerID == myStepMarkerID || myAllFeatureMarkerIDs.Contains(myMarkerID))))) {
                    if (myMarkerID == myStepMarkerID) {
                        myMarkerConfirmed = true; //So we can auto-save
                        StepMarker.Confirmed = true;
                        StepMarker.Levelled = false;
                        StepMarker.Stitched = false;
                        myConfirmedMarker = StepMarker.Copy();

                        PopulateMarkerPoint(ref myConfirmedMarker, mySeenFromMarkerID, myOriginPts, myXAxisPts, myYAxisPts, mySeenFromMarkerIDs, myHistoricMarkers);

                        //Set the Stepmarker coordinates so we can use it to recognise other markers
                        StepMarker.Origin = myConfirmedMarker.Origin.Copy();
                        StepMarker.XAxisPoint = myConfirmedMarker.XAxisPoint.Copy();
                        StepMarker.YAxisPoint = myConfirmedMarker.YAxisPoint.Copy();
                        StepMarker.VX = myConfirmedMarker.VX.Copy();
                        StepMarker.VY = myConfirmedMarker.VY.Copy();
                        StepMarker.VZ = myConfirmedMarker.VZ.Copy();

                        UpdateStepMarkerIDs();
                        myConfirmedMarker.MarkerID = myStepMarkerID;
                        myConfirmedMarker.ActualMarkerID = myStepMarkerID;
                        myConfirmedMarker.History[0].MarkerID = myConfirmedMarker.MarkerID;
                        myConfirmedMarker.History[0].ActualMarkerID = myConfirmedMarker.ActualMarkerID;
                        myConfirmedMarker.History[0].VerticalVect = StepMarker.VerticalVect?.Copy();
                        ConfirmedMarkers.Add(myConfirmedMarker);

                    } else if (myBulkheadMarkerIDs.Contains(myMarkerID)) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint2(myMarkerID, -1);

                        PopulateMarkerPoint(ref myConfirmedMarker, mySeenFromMarkerID, myOriginPts, myXAxisPts, myYAxisPts, mySeenFromMarkerIDs, myHistoricMarkers);

                        myBulkheadMarkers.Add(myConfirmedMarker);

                    } else if (myDoorMarkerIDs.Contains(myMarkerID)) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint2(myMarkerID, -1);

                        PopulateMarkerPoint(ref myConfirmedMarker, mySeenFromMarkerID, myOriginPts, myXAxisPts, myYAxisPts, mySeenFromMarkerIDs, myHistoricMarkers);

                        myDoorMarkers.Add(myConfirmedMarker);

                    } else if (myObstructMarkerIDs.Contains(myMarkerID)) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint2(myMarkerID, -1);

                        PopulateMarkerPoint(ref myConfirmedMarker, mySeenFromMarkerID, myOriginPts, myXAxisPts, myYAxisPts, mySeenFromMarkerIDs, myHistoricMarkers);

                        myObstructMarkers.Add(myConfirmedMarker);

                    } else if (myWallMarkerIDs.Contains(myMarkerID)) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint2(myMarkerID, -1);

                        PopulateMarkerPoint(ref myConfirmedMarker, mySeenFromMarkerID, myOriginPts, myXAxisPts, myYAxisPts, mySeenFromMarkerIDs, myHistoricMarkers);

                        myWallMarkers.Add(myConfirmedMarker);

                    } else {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint2(myMarkerID, -1);

                        PopulateMarkerPoint(ref myConfirmedMarker, mySeenFromMarkerID, myOriginPts, myXAxisPts, myYAxisPts, mySeenFromMarkerIDs, myHistoricMarkers);

                        ConfirmedMarkers.Add(myConfirmedMarker);
                    }
                    j = 0;
                    while (j <= mySuspectedMarkers.Count - 1) {
                        if (mySuspectedMarkers[j].MarkerID == myMarkerID) {
                            mySuspectedMarkers.RemoveAt(j);
                        } else {
                            j = j + 1;
                        }
                    }

                    continue;
                }

                i = i + 1;
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
                if (ConfirmedMarkers[i].MarkerID == myStepMarkerID) {
                    ConfirmedMarkers[i].MarkerID = maxConfirmedID;
                    break;
                }
            }

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (ConfirmedMarkers[i].SeenFromMarkerID == myStepMarkerID) {
                    ConfirmedMarkers[i].SeenFromMarkerID = maxConfirmedID;
                }
                for (int j = 0; j < ConfirmedMarkers[i].SeenFromMarkerIDs.Count; j++) {
                    if (ConfirmedMarkers[i].SeenFromMarkerIDs[j] == myStepMarkerID) {
                        ConfirmedMarkers[i].SeenFromMarkerIDs[j] = maxConfirmedID;
                    }
                }
            }

            for (i = 0; i < mySuspectedMarkers.Count; i++) {
                if (mySuspectedMarkers[i].SeenFromMarkerID == myStepMarkerID) {
                    mySuspectedMarkers[i].SeenFromMarkerID = maxConfirmedID;
                }
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

        private static void PopulateMarkerPoint(ref clsMarkerPoint2 myMarkerPoint, int mySeenFromMarkerID, List<clsPoint3d> pts1, List<clsPoint3d> pts2,
            List<clsPoint3d> pts3, List<int> mySeenFromMarkerIDs, List<clsMarkerPoint2> myHistoricMarkers) {
            myMarkerPoint.ActualMarkerID = myMarkerPoint.MarkerID;
            myMarkerPoint.SeenFromMarkerID = mySeenFromMarkerID;
            myMarkerPoint.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
            myMarkerPoint.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
            myMarkerPoint.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
            myMarkerPoint.SetZNormal();
            myMarkerPoint.SetEndPoint();
            myMarkerPoint.ModelViewMatrix = GetModelViewMatrixFromPoints(myMarkerPoint.Origin, myMarkerPoint.XAxisPoint, myMarkerPoint.YAxisPoint);
            myMarkerPoint.SeenFromMarkerIDs.Clear();
            myMarkerPoint.SeenFromMarkerIDs.AddRange(mySeenFromMarkerIDs);
            myMarkerPoint.History.AddRange(myHistoricMarkers.ToArray());
            myMarkerPoint.Confirmed = true;

            //If seen from Step Marker, reset the SeenFromMarkerID
            if (mySeenFromMarkerID == myStepMarkerID) {
                int j = myStepMarkerID;
                for (int k = 0; k < ConfirmedMarkers.Count; k++) {
                    if (ConfirmedMarkers[k].ActualMarkerID == myStepMarkerID) j = ConfirmedMarkers[k].MarkerID;
                }
                myMarkerPoint.SeenFromMarkerID = j;
            }

        }

        private static bool AddSuspectedMarker(int myMarkerID, int myMarkerIndex, int mySeenFromMarkerID, int mySeenFromMarkerIndex, bool onlySuspectedMarkers = false) {
            clsPoint3d myCameraPoint = PointFromInvMatrix(myMarkerIndex);
            clsPoint3d mySeenFromCameraPoint = PointFromInvMatrix(mySeenFromMarkerIndex);
            clsPoint3d pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, mySeenFromMarkerIndex);
            clsPoint3d pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, mySeenFromMarkerIndex);
            clsPoint3d pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, mySeenFromMarkerIndex);
            return AddSuspectedMarker(myMarkerID, mySeenFromMarkerID, mySeenFromCameraPoint, pt1, pt2, pt3, myCameraPoint, onlySuspectedMarkers);
        }


        private static bool AddSuspectedMarker(int myMarkerID, int mySeenFromMarkerID, clsPoint3d mySeenFromCameraPoint, clsPoint3d pt1, clsPoint3d pt2, clsPoint3d pt3, clsPoint3d myCameraPoint, bool onlySuspectedMarkers = false) {
            int k;
            clsPoint3d v1 = null, v2 = null, v3 = null, v4 = null;
            TimeSpan t;
            double a, a1 = 0, a2 = 0;
            System.DateTime t1;
            System.DateTime t2;
            bool myHasTime;
            string myStr;
            string myMarkerStr;
            String myErrorString = "";
            int i;
            bool b1 = false, b2 = false;

            //Don't add new suspeccted markers for bulkheads or doors if they have just been measured
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

            v1 = myCameraPoint.Copy();
            v1.Normalise();

            t1 = DateTime.Now;
            t2 = t1;

            k = -1;
            for (i = 0; i <= mySuspectedMarkers.Count - 1; i++) {
                if (mySuspectedMarkers[i].MarkerID == myMarkerID && mySuspectedMarkers[i].SeenFromMarkerID == mySeenFromMarkerID) {
                    k = i;
                    break; // TODO: might not be correct. Was : Exit For
                }
            }

            if (onlySuspectedMarkers && k > -1) return false;

            if (k == -1) {
                mySuspectedMarkers.Add(new clsMarkerPoint2(myMarkerID, mySeenFromMarkerID));
                k = mySuspectedMarkers.Count - 1;
                mySuspectedMarkers[k].SeenFromMarkerID = mySeenFromMarkerID;
                mySuspectedMarkers[k].LastTime = t1;
                mySuspectedMarkers[k].LastVector = v1;
            } else {
                myHasTime = (mySuspectedMarkers[k].LastTime != null);
                if (myHasTime) {
                    t2 = (DateTime)mySuspectedMarkers[k].LastTime;
                    v2 = mySuspectedMarkers[k].LastVector;
                }
                mySuspectedMarkers[k].LastTime = t1;
                mySuspectedMarkers[k].LastVector = v1;
                if (myHasTime) {
                    a = Acos(v1.Dot(v2)) * 180 / PI;
                    //DebugStringList = string.Format("Angle {0:0.00}", a);
                    t = t1 - t2;
                    //myMain.Label4.Text = Round(a / t.TotalSeconds, 2) & " - " & mySuspectedMarkers[k].Points1a.Count & " - A=" & Round(mySuspectedMarkers[k].MaxAngle * 180 / PI, 1) & " - XY=" & Round(mySuspectedMarkers[k].MaxAngleXY * 180 / PI, 1) & " - Z=" & Round(mySuspectedMarkers[k].MaxAngleZ * 180 / PI, 1) & " - " & mySuspectedMarkers[k].NumPointsCalulated1 & "/" & mySuspectedMarkers[k].NumPointsCalulated2 & "/" & mySuspectedMarkers[k].NumPointsCalulated3 & "/" & mySuspectedMarkers[k].NumPointsCalulated5 & " - " & Round(mySuspectedMarkers[k].Origin.Length, 2)
                    if (a / t.TotalSeconds > 0.5) {
                        return false;
                    }

                    if (mySeenFromMarkerID == myGFMarkerID) {
                        myMarkerStr = "GF";
                    } else if (mySeenFromMarkerID == myStepMarkerID) {
                        myMarkerStr = "ST";
                    } else {
                        myMarkerStr = Convert.ToString(myMarkerIDs.IndexOf(mySeenFromMarkerID) + 1);
                    }

                    mySuspectedMarkers[k].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2);

                    //-DEVELOPMENT CHANGE
                    //myStr = myMarkerIDs.IndexOf(myMarkerID) + 1 + " / " + myMarkerStr + " " + mySuspectedMarkers[k].OriginPoints.Count + " - MaxA=" + Round(mySuspectedMarkers[k].MaxAngle(ref v1, ref v2) * 180 / PI, 1) + " - MaxA2=" + Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1);
                    myStr = myMarkerIDs.IndexOf(myMarkerID) + 1 + " / " + myMarkerStr + " " + mySuspectedMarkers[k].OriginPoints.Count + " - MaxD=" + Round(mySuspectedMarkers[k].MaxDistance(ref v1, ref v2)) + " - MaxA2=" + Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1);

                    if (myStr != mySuspectedMarkers[k].Label) {
                        mySuspectedMarkers[k].Label = myStr;
                        mySuspectedMarkers[k].MarkerName = Convert.ToString(myMarkerIDs.IndexOf(myMarkerID) + 1);
                        mySuspectedMarkers[k].SeenFromMarkerName = myMarkerStr;
                        mySuspectedMarkers[k].NumPoints = Convert.ToString(mySuspectedMarkers[k].OriginPoints.Count);

                        //-DEVELOPMENT CHANGE
                        //mySuspectedMarkers[k].MaximumAngleA = Convert.ToString(Round(mySuspectedMarkers[k].MaxAngle(ref v1, ref v2) * 180 / PI, 1));
                        mySuspectedMarkers[k].MaximumAngleA = Convert.ToString(Round(mySuspectedMarkers[k].MaxDistance(ref v1, ref v2)));

                        mySuspectedMarkers[k].MaximumAngleXY = Convert.ToString(Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1));
                    }
                }
            }

            mySuspectedMarkers[k].SeenFromCameraPoints.Add(mySeenFromCameraPoint);
            mySuspectedMarkers[k].OriginPoints.Add(pt1);
            mySuspectedMarkers[k].EndXAxisPoints.Add(pt2);
            mySuspectedMarkers[k].EndYAxisPoints.Add(pt3);
            mySuspectedMarkers[k].CameraPoints.Add(myCameraPoint);

            if (mySuspectedMarkers[k].FirstPointRemoved == false & mySuspectedMarkers[k].OriginPoints.Count == 2) {
                mySuspectedMarkers[k].SeenFromCameraPoints.RemoveAt(0);
                mySuspectedMarkers[k].OriginPoints.RemoveAt(0);
                mySuspectedMarkers[k].EndXAxisPoints.RemoveAt(0);
                mySuspectedMarkers[k].EndYAxisPoints.RemoveAt(0);

                if (mySuspectedMarkers[k].GyroData.Count > 0) mySuspectedMarkers[k].GyroData.RemoveAt(0);
                if (mySuspectedMarkers[k].LastGyroData.Count > 0) mySuspectedMarkers[k].LastGyroData.RemoveAt(0);
                if (mySuspectedMarkers[k].AccelData.Count > 0) mySuspectedMarkers[k].AccelData.RemoveAt(0);
                if (mySuspectedMarkers[k].LastAccelData.Count > 0) mySuspectedMarkers[k].LastAccelData.RemoveAt(0);

                mySuspectedMarkers[k].FirstPointRemoved = true;
            }

            mySuspectedMarkers[k].SetEndPointBasedOnZVectors();
            //mySuspectedMarkers[k].SetPointsToAverage()

            if (mySuspectedMarkers[k].MarkerID == myStepMarkerID) {
                if (DebugStringList.Count == 0 || !(DebugStringList[DebugStringList.Count - 1].StartsWith("Step Marker Measured"))) {
                    DebugStringList.Add("Step Marker Measured (" + mySuspectedMarkers[k].OriginPoints.Count + " Times)");
                } else {
                    DebugStringList[DebugStringList.Count - 1] = "Step Marker Measured (" + mySuspectedMarkers[k].OriginPoints.Count + " Times)";
                }
            } else {
                if (DebugStringList.Count == 0 || !(DebugStringList[DebugStringList.Count - 1].StartsWith("Marker " + mySuspectedMarkers[k].NewMarkerID() + " Measured"))) {
                    DebugStringList.Add("Marker " + mySuspectedMarkers[k].NewMarkerID() + " Measured (" + mySuspectedMarkers[k].OriginPoints.Count + " Times)");
                } else {
                    DebugStringList[DebugStringList.Count - 1] = "Marker " + mySuspectedMarkers[k].NewMarkerID() + " Measured (" + mySuspectedMarkers[k].OriginPoints.Count + " Times)";
                }
            }
            return true;
        }

        private static void FilterLists(double myDist, ref List<clsPoint3d> pts1, ref List<clsPoint3d> pts2, ref List<clsPoint3d> pts3, ref List<clsPoint3d> pts4, ref List<clsPoint3d> pts5)
        {
            int i;
            double d;
            List<double> myDists = new List<double>();
            bool myRemovedPoint = true;
            clsPoint3d pt;

            while (pts1.Count > 1 & myRemovedPoint) {
                myRemovedPoint = false;
                pt = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                myDists.Clear();
                for (i = 0; i < pts1.Count; i++) {
                    myDists.Add(pts1[i].Dist(pt));
                }
                d = myDists.Max();
                if (d > myDist) {
                    i = myDists.IndexOf(d);
                    if (i > -1) {
                        pts1.RemoveAt(i);
                        pts2.RemoveAt(i);
                        pts3.RemoveAt(i);
                        pts4.RemoveAt(i);
                        pts5.RemoveAt(i);
                        myRemovedPoint = true;
                    }
                }
            }
        }

        public static float[] GetModelViewMatrixFromPoints(clsPoint3d p1, clsPoint3d p2, clsPoint3d p3)
        {
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

            Matrix4 myRot;
            Matrix4 myTran;

            pt = vx.Point2D();
            if (IsSameDbl(pt.Length, 0) == false) {
                pt.Normalise();
                a1 = pt.Angle(true);
                a2 = vx.AngleToHorizontal;
                a3 = vy.AngleToHorizontal;

                myTran = Matrix4.CreateTranslation((float)p1.X, (float)p1.Y, (float)p1.Z);
                if (vz.Z > 0) {
                    myRot = Matrix4.CreateRotationX((float)a3);
                } else {
                    myRot = Matrix4.CreateRotationX(-(float)a3);
                }
                myTran = Matrix4.Mult(myRot, myTran);
                myRot = Matrix4.CreateRotationY(-(float)a2);
                myTran = Matrix4.Mult(myRot, myTran);
                myRot = Matrix4.CreateRotationZ((float)a1);
                myTran = Matrix4.Mult(myRot, myTran);
            } else {
                pt = vy.Point2D();
                pt.Normalise();
                a1 = pt.Angle(true);
                a2 = vy.AngleToHorizontal;
                a3 = vz.AngleToHorizontal;

                myTran = Matrix4.CreateTranslation((float)p1.X, (float)p1.Y, (float)p1.Z);
                if (vx.X > 0) {
                    myRot = Matrix4.CreateRotationY((float)a3);
                } else {
                    myRot = Matrix4.CreateRotationY(-(float)a3);
                }
                myTran = Matrix4.Mult(myRot, myTran);
                myRot = Matrix4.CreateRotationZ(-(float)a2);
                myTran = Matrix4.Mult(myRot, myTran);
                myRot = Matrix4.CreateRotationX((float)a1);
                myTran = Matrix4.Mult(myRot, myTran);
            }

            float[] mv = { myTran.M11, myTran.M12, myTran.M13, myTran.M14, myTran.M21, myTran.M22, myTran.M23, myTran.M24, myTran.M31, myTran.M32, myTran.M33, myTran.M34, myTran.M41, myTran.M42, myTran.M43, myTran.M44 };
            return mv;
        }

        public static void AutoSaveSurveyData()
        {
            int i;
            double a;

            if (Directory.Exists(myAppPath + "projects") == false) {
                try {
                    Directory.CreateDirectory(myAppPath + "projects");
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                    return;
                }
            }

            string myFileName = myAppPath + "projects/Survey_Data.txt";
            if (File.Exists(myFileName)) {
                try {
                    File.Delete(myFileName);
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                    return;
                }
            }

            if (File.Exists(myFileName)) {
                try {
                    File.Delete(myFileName);
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                    return;
                }
            }

            StreamWriter sw = new StreamWriter(myFileName);
            sw.WriteLine("#VERSION,1.3.99999");
            sw.WriteLine("SETTINGS");
            sw.WriteLine("VerticalVectorX," + myVerticalVector.X);
            sw.WriteLine("VerticalVectorY," + myVerticalVector.Y);
            sw.WriteLine("VerticalVectorZ," + myVerticalVector.Z);
            sw.WriteLine("UseNewStyleMarkers,1");
            sw.WriteLine("GFMarkerID," + myGFMarkerID);
            sw.WriteLine("StepMarkerID," + myStepMarkerID);
            sw.WriteLine("END_SETTINGS");
            sw.WriteLine(ConfirmedMarkers.Count);
            for (i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                ConfirmedMarkers[i].Save(sw);
            }
            sw.WriteLine(myBulkheadMarkers.Count);
            for (i = 0; i <= myBulkheadMarkers.Count - 1; i++) {
                myBulkheadMarkers[i].Save(sw);
            }
            sw.WriteLine(myDoorMarkers.Count);
            for (i = 0; i <= myDoorMarkers.Count - 1; i++) {
                myDoorMarkers[i].Save(sw);
            }
            sw.WriteLine(myObstructMarkers.Count);
            for (i = 0; i <= myObstructMarkers.Count - 1; i++) {
                myObstructMarkers[i].Save(sw);
            }
            sw.WriteLine(myWallMarkers.Count);
            for (i = 0; i <= myWallMarkers.Count - 1; i++) {
                myWallMarkers[i].Save(sw);
            }
            sw.Close();

        }

        public static void SaveSurvey()
        {
            int i;
            string myFileName;
            string myDiagFileName;
            double a;
            List<clsPGPoint> myExportPts = new List<clsPGPoint>();

            StopTracking();

            if (Directory.Exists(myAppPath + "projects") == false) {
                try {
                    Directory.CreateDirectory(myAppPath + "projects");
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                    return;
                }
            }
            myFileName = myAppPath + "projects/ipad_project.3dm";
            if (File.Exists(myFileName)) {
                try {
                    File.Delete(myFileName);
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                    return;
                }
            }

            myDiagFileName = myFileName.Replace(".3dm", "_Diag.txt");

            if (File.Exists(myDiagFileName)) {
                try {
                    File.Delete(myDiagFileName);
                }
                catch (Exception ex) {
                    string s = ex.ToString();
                    return;
                }
            }

            StreamWriter sw = new StreamWriter(myDiagFileName);
            sw.WriteLine("#VERSION," + myAppVersion);
            sw.WriteLine("SETTINGS");
            sw.WriteLine("VerticalVectorX," + myVerticalVector.X);
            sw.WriteLine("VerticalVectorY," + myVerticalVector.Y);
            sw.WriteLine("VerticalVectorZ," + myVerticalVector.Z);
            if (UseNewStyleMarkers) sw.WriteLine("UseNewStyleMarkers,1");
            sw.WriteLine("GFMarkerID," + myGFMarkerID);
            sw.WriteLine("StepMarkerID," + myStepMarkerID);
            sw.WriteLine("END_SETTINGS");
            sw.WriteLine(ConfirmedMarkers.Count);
            for (i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                ConfirmedMarkers[i].Save(sw);
            }
            sw.WriteLine(myBulkheadMarkers.Count);
            for (i = 0; i <= myBulkheadMarkers.Count - 1; i++) {
                myBulkheadMarkers[i].Save(sw);
            }
            sw.WriteLine(myDoorMarkers.Count);
            for (i = 0; i <= myDoorMarkers.Count - 1; i++) {
                myDoorMarkers[i].Save(sw);
            }
            sw.WriteLine(myObstructMarkers.Count);
            for (i = 0; i <= myObstructMarkers.Count - 1; i++) {
                myObstructMarkers[i].Save(sw);
            }
            sw.WriteLine(myWallMarkers.Count);
            for (i = 0; i <= myWallMarkers.Count - 1; i++) {
                myWallMarkers[i].Save(sw);
            }
            sw.Close();

            //Order by Z value and then by ID
            ConfirmedMarkers.Sort(new MarkerPoint2Comparer());

            sw = new StreamWriter(myFileName);
            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                sw.WriteLine(ConfirmedMarkers[i].Point.X.ToString() + '\t' + ConfirmedMarkers[i].Point.Y.ToString() + '\t' + ConfirmedMarkers[i].Point.Z.ToString() + '\t' + (ConfirmedMarkers[i].ActualMarkerID + 1).ToString() + '\t' + (ConfirmedMarkers[i].SeenFromMarkerID + 1).ToString());
                myExportPts.Add(new clsPGPoint(ConfirmedMarkers[i].Point.X, ConfirmedMarkers[i].Point.Y, ConfirmedMarkers[i].Point.Z, (ConfirmedMarkers[i].ActualMarkerID + 1), (ConfirmedMarkers[i].SeenFromMarkerID + 1)));
            }
            for (i = 0; i < myBulkheadMarkers.Count; i++) {
                sw.WriteLine(myBulkheadMarkers[i].Origin.X.ToString() + '\t' + myBulkheadMarkers[i].Origin.Y.ToString() + '\t' + myBulkheadMarkers[i].Origin.Z.ToString() + '\t' + (myBulkheadMarkers[i].MarkerID + 1).ToString() + '\t' + (myBulkheadMarkers[i].SeenFromMarkerID + 1).ToString() + '\t' + myBulkheadMarkers[i].BulkheadHeight.ToString());
                myExportPts.Add(new clsPGPoint(myBulkheadMarkers[i].Origin.X, myBulkheadMarkers[i].Origin.Y, myBulkheadMarkers[i].Origin.Z, myBulkheadMarkers[i].MarkerID + 1, myBulkheadMarkers[i].SeenFromMarkerID + 1, myBulkheadMarkers[i].BulkheadHeight));
            }
            for (i = 0; i < myDoorMarkers.Count; i++) {
                a = (myDoorMarkers[i].Point.Point2D() - myDoorMarkers[i].Origin.Point2D()).Angle();
                sw.WriteLine(myDoorMarkers[i].Point.X.ToString() + '\t' + myDoorMarkers[i].Point.Y.ToString() + '\t' + myDoorMarkers[i].Point.Z.ToString() + '\t' + (myDoorMarkers[i].MarkerID + 1).ToString() + '\t' + (myDoorMarkers[i].SeenFromMarkerID + 1).ToString() + '\t' + a.ToString());
                myExportPts.Add(new clsPGPoint(myDoorMarkers[i].Point.X, myDoorMarkers[i].Point.Y, myDoorMarkers[i].Point.Z, myDoorMarkers[i].MarkerID + 1, myDoorMarkers[i].SeenFromMarkerID + 1, a));
            }
            for (i = 0; i < myObstructMarkers.Count; i++) {
                sw.WriteLine(myObstructMarkers[i].Point.X.ToString() + '\t' + myObstructMarkers[i].Point.Y.ToString() + '\t' + myObstructMarkers[i].Point.Z.ToString() + '\t' + (myObstructMarkers[i].MarkerID + 1).ToString() + '\t' + (myObstructMarkers[i].SeenFromMarkerID + 1).ToString() + '\t' + '0');
                myExportPts.Add(new clsPGPoint(myObstructMarkers[i].Point.X, myObstructMarkers[i].Point.Y, myObstructMarkers[i].Point.Z, myObstructMarkers[i].MarkerID + 1, myObstructMarkers[i].SeenFromMarkerID + 1, 0));
            }
            for (i = 0; i < myWallMarkers.Count; i++) {
                a = (myWallMarkers[i].VZ.Point2D().Angle());
                sw.WriteLine(myWallMarkers[i].Point.X.ToString() + '\t' + myWallMarkers[i].Point.Y.ToString() + '\t' + myWallMarkers[i].Point.Z.ToString() + '\t' + (myWallMarkers[i].MarkerID + 1).ToString() + '\t' + (myWallMarkers[i].SeenFromMarkerID + 1).ToString() + '\t' + a.ToString());
                myExportPts.Add(new clsPGPoint(myWallMarkers[i].Point.X, myWallMarkers[i].Point.Y, myWallMarkers[i].Point.Z, myWallMarkers[i].MarkerID + 1, myWallMarkers[i].SeenFromMarkerID + 1, a));
            }
            sw.Close();
            
        }

        public static void LoadAutoSaveData(string myFileName)
        {
            int i;
            
            if (File.Exists(myFileName) == false) return;

            ResetMeasurements();

            StreamReader sr = new StreamReader(myFileName);
            string[] mySplit;
            string myLine = sr.ReadLine();
            mySplit = myLine.Split(',');
            myLine = sr.ReadLine();
            if (myLine == "SETTINGS") {
                myLine = sr.ReadLine();
                while (myLine != "END_SETTINGS") {
                    mySplit = myLine.Split(',');
                    if (mySplit.GetUpperBound(0) == 1) {
                        if (mySplit[0] == "VerticalVectorX") {
                            if (myVerticalVector == null)
                                myVerticalVector = new clsPoint3d(0, 0, 0);
                            myVerticalVector.X = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorY") {
                            if (myVerticalVector == null)
                                myVerticalVector = new clsPoint3d(0, 0, 0);
                            myVerticalVector.Y = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorZ") {
                            if (myVerticalVector == null)
                                myVerticalVector = new clsPoint3d(0, 0, 0);
                            myVerticalVector.Z = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "UseNewStyleMarkers") {
                            if (Convert.ToInt32(mySplit[1]) == 1)
                                UseNewStyleMarkers = true;
                        }
                        if (mySplit[0] == "GFMarkerID")
                            myGFMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "StepMarkerID")
                            myStepMarkerID = Convert.ToInt32(mySplit[1]);
                    }
                    myLine = sr.ReadLine();
                }
                myLine = sr.ReadLine();
            }

            int n = Convert.ToInt32(myLine);
            clsMarkerPoint2 myMarkerPoint;
            for (i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint2();
                myMarkerPoint.Load(sr);
                int myID = myMarkerPoint.ActualMarkerID;
                if (myID == myLeftBulkheadMarkerID || myID == myRightBulkheadMarkerID) {
                    myBulkheadMarkers.Add(myMarkerPoint);
                } else if (myID >= myDoorHingeRightMarkerID && myID <= myDoorFrameLeftMarkerID) {
                    myDoorMarkers.Add(myMarkerPoint);
                } else if (myID >= myObstruct1MarkerID && myID <= myObstruct4MarkerID) {
                    myObstructMarkers.Add(myMarkerPoint);
                } else if (myID >= myWall1MarkerID && myID <= myWall4MarkerID) {
                    myWallMarkers.Add(myMarkerPoint);
                } else {
                    ConfirmedMarkers.Add(myMarkerPoint);
                }
            }
            sr.Close();
        }

        public static clsPoint3d PointFromInvMatrix(int n, bool lowRes = false) {
            double[] mv = new double[16];

            for (int i = 0; i <= 15; i++) {
                mv[i] = Data.ModelViewMatrix[n][i];
            }
            return PointFromInvMatrix(mv);
        }

        public static clsPoint3d PointFromInvMatrix(double[] mv) {
            Matrix4d myModel = new Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            Matrix4d modelViewInv = Matrix4d.Invert(myModel);
            return new clsPoint3d(modelViewInv.M41, modelViewInv.M42, modelViewInv.M43);
        }

        public static void RelevelMarkerFromGF(clsMarkerPoint2 myMarker, bool goBack = false) {
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
                myMarker.XAxisPoint.RotateAboutLine(p3.Line(), b);
                myMarker.YAxisPoint.RotateAboutLine(p3.Line(), b);
                myMarker.VX.RotateAboutLine(p3.Line(), b);
                myMarker.VY.RotateAboutLine(p3.Line(), b);
                myMarker.VZ.RotateAboutLine(p3.Line(), b);
                myMarker.Point.RotateAboutLine(p3.Line(), b);
            }
        }

        private static void RelevelMarkerFromGF(ref clsPoint3d pt1a, ref clsPoint3d pt1b, ref clsPoint3d pt1, ref clsPoint3d pt2, ref clsPoint3d pt3)
        {
            int j;
            clsPoint3d p1 = myVerticalVector.Copy();
            if (p1.Z < 0) p1.Scale(-1);
            p1.Normalise();
            double a = p1.AngleToHorizontal;

            if (IsSameAngle(a, PI / 2) == false) {
                double b = -(PI / 2 - a);
                clsPoint3d p2 = new clsPoint3d(p1.X, p1.Y, 0);
                p2.Normalise();
                clsPoint3d p3 = p1.Cross(p2);
                p3.Normalise();

                pt1a.RotateAboutLine(p3.Line(), b);
                pt1b.RotateAboutLine(p3.Line(), b);
                pt1.RotateAboutLine(p3.Line(), b);
                pt2.RotateAboutLine(p3.Line(), b);
                pt3.RotateAboutLine(p3.Line(), b);
            }
        }


        private static bool CheckSuspectedMarker(int myMarkerID, int myMarkerIndex, bool onlySuspectedMarkers = false) {
            int i1, k;
            bool mySuspectedMarkerAdded = false;
            List<int> SeenFromMarkersAdded = new List<int>();

            if (onlySuspectedMarkers) goto onlySuspectedMarkersStart;

            //Get the coordinates from as many of the GF marker, the step marker and the other confirmed markers as are visible
            //Start with the Confirmed markers (this includes the Step Marker)
            for (k = 0; k <= ConfirmedMarkers.Count - 1; k++) {
                if (myAllFeatureMarkerIDs.Contains(ConfirmedMarkers[k].ActualMarkerID)) continue; //Not from obstruction markers

                int mySeenFromMarkerID = ConfirmedMarkers[k].MarkerID;
                if (mySeenFromMarkerID == myStepMarkerID && StepMarker.Levelled == false) continue;

                if (Data.GetMarkerVisible(mySeenFromMarkerID)) {
                    i1 = Data.GetMarkerIndex(mySeenFromMarkerID);
                    if (AddSuspectedMarker(myMarkerID, myMarkerIndex, mySeenFromMarkerID, i1, onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
                }
            }

            //Now from the GF marker
            if (Data.GetMarkerVisible(myGFMarkerID)) {
                if (AddSuspectedMarker(myMarkerID, myMarkerIndex, myGFMarkerID, Data.GetMarkerIndex(myGFMarkerID), onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
            }

        onlySuspectedMarkersStart:

            //New: now from other suspected markers
            if (!(myDoorMarkerIDs.Contains(myMarkerID) || myObstructMarkerIDs.Contains(myMarkerID))) {
                for (k = 0; k <= mySuspectedMarkers.Count - 1; k++) {
                    if (myAllFeatureMarkerIDs.Contains(mySuspectedMarkers[k].ActualMarkerID)) continue; //Not from obstruction markers

                    int mySeenFromMarkerID = mySuspectedMarkers[k].MarkerID;
                    if (mySeenFromMarkerID == myMarkerID) continue;
                    if (SeenFromMarkersAdded.Contains(mySeenFromMarkerID)) continue;
                    SeenFromMarkersAdded.Add(mySeenFromMarkerID);

                    if (Data.GetMarkerVisible(mySeenFromMarkerID)) {
                        i1 = Data.GetMarkerIndex(mySeenFromMarkerID);
                        if (i1 == myMarkerIndex) continue;

                        if (AddSuspectedMarker(myMarkerID, myMarkerIndex, mySeenFromMarkerID, i1, onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
                    }
                }
            }

            if (onlySuspectedMarkers) return mySuspectedMarkerAdded;

            //For Door markers, allow them to be seen from other Door markers
            if (myDoorMarkerIDs.Contains(myMarkerID)) {
                for (k = 0; k <= myDoorMarkers.Count - 1; k++) {
                    int mySeenFromMarkerID = myDoorMarkers[k].MarkerID;
                    if (mySeenFromMarkerID == myMarkerID) continue;
                    if (!myDoorMarkers[k].Confirmed) continue;

                    if (Data.GetMarkerVisible(mySeenFromMarkerID)) {
                        i1 = Data.GetMarkerIndex(mySeenFromMarkerID);
                        if (AddSuspectedMarker(myMarkerID, myMarkerIndex, myDoorMarkers[k].MarkerID, i1, onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
                        break;
                    }
                }
            }

            //For Obstruction markers, allow them to be seen from other Obstruction markers
            if (myObstructMarkerIDs.Contains(myMarkerID)) {
                for (k = 0; k <= myObstructMarkers.Count - 1; k++) {
                    int mySeenFromMarkerID = myObstructMarkers[k].MarkerID;
                    if (mySeenFromMarkerID == myMarkerID) continue;
                    if (!myObstructMarkers[k].Confirmed) continue;

                    if (Data.GetMarkerVisible(mySeenFromMarkerID)) {
                        i1 = Data.GetMarkerIndex(mySeenFromMarkerID);
                        if (AddSuspectedMarker(myMarkerID, myMarkerIndex, myObstructMarkers[k].MarkerID, i1, onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
                        break;
                    }
                }
            }
            return mySuspectedMarkerAdded;
        }

        public static void ResetMeasurements()
        {
            mySuspectedMarkers.Clear();
            ConfirmedMarkers.Clear();
            myBulkheadMarkers.Clear();
            myDoorMarkers.Clear();
            myObstructMarkers.Clear();
            myWallMarkers.Clear();
            myVerticalVector = null;
            myStepVerticalVector = null;
            StepMarker.Confirmed = false;
            StepMarker.Levelled = false;
            StepMarker.Stitched = false;
            StepMarker.VerticalVect = null;
            DebugStringList.Clear();
        }

        public static void StopTracking()
        {
            isTrackingEnabled = false;
            mySuspectedMarkers.Clear();
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint2(myStepMarkerID, myStepMarkerID);
            }
        }

        public static clsPoint3d UnProjectProject(clsPoint3d p1, int n1, int n2, bool lowRes = false) {
            int i;
            double[] mv = new double[16];

            for (i = 0; i <= 15; i++) {
                mv[i] = Data.ModelViewMatrix[n1][i];
            }

            Matrix4d myModel = new Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            Vector4d vec;
            vec.X = p1.X;
            vec.Y = p1.Y;
            vec.Z = p1.Z;
            vec.W = 1.0;
            Vector4d.Transform(ref vec, ref myModel, out vec);

            for (i = 0; i <= 15; i++) {
                mv[i] = Data.ModelViewMatrix[n2][i];
            }

            myModel = new Matrix4d(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            Matrix4d modelviewInv = Matrix4d.Invert(myModel);
            Vector4d.Transform(ref vec, ref modelviewInv, out vec);

            return new clsPoint3d(vec.X, vec.Y, vec.Z);
        }

        public static clsPoint3d gluProject(Matrix4 projection, Matrix4 modelview, int[] vp, clsPoint3d p1)
        {
            Vector4 vec;

            vec.X = (float)p1.X;
            vec.Y = (float)p1.Y;
            vec.Z = (float)p1.Z;
            vec.W = 1.0f;

            Vector4.Transform(ref vec, ref modelview, out vec);
            Vector4.Transform(ref vec, ref projection, out vec);

            if (vec.W > float.Epsilon || vec.W < float.Epsilon) {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return new clsPoint3d(vp[0] + (1.0f + vec.X) * vp[2] / 2.0f, vp[1] + (1.0f + vec.Y) * vp[3] / 2.0f, (1.0f + vec.Z) / 2.0f);
        }

        public static clsPoint3d gluUnProject(Matrix4 projection, Matrix4 modelview, int[] vp, clsPoint3d p1)
        {
            Vector4 vec;

            vec.X = 2.0f * (float)(p1.X - vp[0]) / (float)(vp[2] - vp[0]) - 1.0f;
            vec.Y = 2.0f * (float)(p1.Y - vp[1]) / (float)(vp[3] - vp[1]) - 1.0f;
            vec.Z = 2.0f * (float)p1.Z - 1.0f;
            vec.W = 1.0f;

            Matrix4 projInv = Matrix4.Invert(projection);
            Matrix4 modelviewInv = Matrix4.Invert(modelview);

            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref modelviewInv, out vec);

            if (vec.W > float.Epsilon || vec.W < float.Epsilon) {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return new clsPoint3d(vec.X, vec.Y, vec.Z);
        }

        private static int[] GetViewport()
        {
            int[] vp = new int[4];
            vp[0] = 0;
            vp[1] = 0;
            vp[2] = myVideoWidth;
            vp[3] = myVideoHeight;
            return vp;
        }

    }

    public class Logger {

        public void CallBackLog(string s)
        {
            System.Diagnostics.Debug.Print(s);
        }


        ARToolKitFunctions.LogCallback myCB;
        public Logger()
        {
            myCB = new ARToolKitFunctions.LogCallback(CallBackLog);
            ARToolKitFunctions.Instance.arwRegisterLogCallback(myCB);
        }
    }
}

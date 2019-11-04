using System;
using static System.Math;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using System.IO;
using OpenTK;

namespace BatchProcess {
    public static class mdlRecognise {
        private static bool _isTrackingEnabled = false;
        public static bool isTrackingEnabled {
            get { return _isTrackingEnabled; }
            set {
                _isTrackingEnabled = value;
            }
        }

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
        public static List<float[]> myModelViewMatrixHiRes = new List<float[]>();
        public static float[] myProjMatrixHiRes = new float[16];
        public static byte[] myLastHiResImage;

        //Processed markers
        public static bool myGFVisibleHiRes = false;
        public static bool myStepVisibleHiRes;
        public static List<int> myMarkersSeenIDHiRes = new List<int>();
        public static int myGFMultiMarkerIndexHiRes;
        public static int myStepMultiMarkerIndexHiRes;

        public static bool myLeftBulkheadVisibleHiRes = false;
        public static int myLeftBulkheadMarkerIndexHiRes;
        public static bool myRightBulkheadVisibleHiRes = false;
        public static int myRightBulkheadMarkerIndexHiRes;

        public static bool myDoorHingeLeftVisibleHiRes = false;
        public static int myDoorHingeLeftIndexHiRes;
        public static bool myDoorHingeRightVisibleHiRes = false;
        public static int myDoorHingeRightIndexHiRes;

        public static bool myDoorFrameLeftVisibleHiRes = false;
        public static int myDoorFrameLeftIndexHiRes;
        public static bool myDoorFrameRightVisibleHiRes = false;
        public static int myDoorFrameRightIndexHiRes;

        public static bool myObstruct1VisibleHiRes = false;
        public static int myObstruct1IndexHiRes;
        public static bool myObstruct2VisibleHiRes = false;
        public static int myObstruct2IndexHiRes;
        public static bool myObstruct3VisibleHiRes = false;
        public static int myObstruct3IndexHiRes;
        public static bool myObstruct4VisibleHiRes = false;
        public static int myObstruct4IndexHiRes;

        public static bool myWall1VisibleHiRes = false;
        public static int myWall1IndexHiRes;
        public static bool myWall2VisibleHiRes = false;
        public static int myWall2IndexHiRes;
        public static bool myWall3VisibleHiRes = false;
        public static int myWall3IndexHiRes;
        public static bool myWall4VisibleHiRes = false;
        public static int myWall4IndexHiRes;

        static List<int> myBulkheadMarkerIDs = new List<int>();
        static List<int> myDoorMarkerIDs = new List<int>();
        static List<int> myObstructMarkerIDs = new List<int>();
        static List<int> myWallMarkerIDs = new List<int>();
        static List<int> myAllFeatureMarkerIDs = new List<int>();

        private static List<clsMarkerPoint> _confirmedMarkerPoints = new List<clsMarkerPoint>();
        public static bool UseNewStyleMarkers = true;

        public static List<clsMarkerPoint> ConfirmedMarkers {
            get { return _confirmedMarkerPoints; }
            set {
                _confirmedMarkerPoints = value;
                ConfirmedMarkersUpdated?.Invoke();
            }
        }
        public delegate void BlankEventHandler();
        public static event BlankEventHandler ConfirmedMarkersUpdated;

        public static List<clsMarkerPoint> ConfirmedMarkersCopy = new List<clsMarkerPoint>();

        public static List<int> myConfirmedMarkersSeenIDHiRes = new List<int>();
        public static List<int> myConfirmedMarkersSeenIndexHiRes = new List<int>();
        public static List<clsMarkerPoint> mySuspectedMarkers = new List<clsMarkerPoint>();
        public static List<int> mySuspectedMarkersSeenIDHiRes = new List<int>();
        public static List<int> mySuspectedMarkersSeenIndexHiRes = new List<int>();
        public static List<clsMarkerPoint> myBulkheadMarkers = new List<clsMarkerPoint>();
        public static List<clsMarkerPoint> myDoorMarkers = new List<clsMarkerPoint>();
        public static List<int> myDoorMarkersSeenIDHiRes = new List<int>();
        public static List<int> myDoorMarkersSeenIndexHiRes = new List<int>();
        public static List<clsMarkerPoint> myObstructMarkers = new List<clsMarkerPoint>();
        public static List<int> myObstructMarkersSeenIDHiRes = new List<int>();
        public static List<int> myObstructMarkersSeenIndexHiRes = new List<int>();
        public static List<clsMarkerPoint> myWallMarkers = new List<clsMarkerPoint>();
        public static List<int> myWallMarkersSeenIDHiRes = new List<int>();
        public static List<int> myWallMarkersSeenIndexHiRes = new List<int>();
        public static int myGFMultiMarkerID = 80;
        public static int myStepMultiMarkerID = 81;
        public static int myLeftBulkheadMarkerID = 82;
        public static int myRightBulkheadMarkerID = 83;
        public static int myDoorHingeRightMarkerID = 84;
        public static int myDoorFrameRightMarkerID = 85;
        public static int myDoorHingeLeftMarkerID = 86;
        public static int myDoorFrameLeftMarkerID = 87;
        public static int myObstruct1MarkerID = 88;
        public static int myObstruct2MarkerID = 89;
        public static int myObstruct3MarkerID = 90;
        public static int myObstruct4MarkerID = 91;
        public static int myWall1MarkerID = 92;
        public static int myWall2MarkerID = 93;
        public static int myWall3MarkerID = 94;
        public static int myWall4MarkerID = 95;
        public static int myMapperMarkerID = 96;
        public static int myMaximumMarkerID = 97; //Please keep this up to date

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

        #region Enums and Constants

        public enum AR_LABELING_THRESH_MODE
        {
            AR_LABELING_THRESH_MODE_MANUAL = 0,     ///< Manual threshold selection via arSetLabelingThresh.
            AR_LABELING_THRESH_MODE_AUTO_MEDIAN,    ///< Automatic threshold selection via full-image histogram median.
            AR_LABELING_THRESH_MODE_AUTO_OTSU,      ///< Automatic threshold selection via Otsu's method for foreground/background selection.
            AR_LABELING_THRESH_MODE_AUTO_ADAPTIVE,  ///< Adaptive thresholding.
            AR_LABELING_THRESH_MODE_AUTO_BRACKETING ///< Automatic threshold selection via heuristic-based exposure bracketing.
        };

        public enum AR_MARKER_INFO_CUTOFF_PHASE
        {
            AR_MARKER_INFO_CUTOFF_PHASE_NONE,                   ///< Marker OK.
            AR_MARKER_INFO_CUTOFF_PHASE_PATTERN_EXTRACTION,     ///< Failure during pattern extraction.
            AR_MARKER_INFO_CUTOFF_PHASE_MATCH_GENERIC,          ///< Generic error during matching phase.
            AR_MARKER_INFO_CUTOFF_PHASE_MATCH_CONTRAST,         ///< Insufficient contrast during matching.
            AR_MARKER_INFO_CUTOFF_PHASE_MATCH_BARCODE_NOT_FOUND,///< Barcode matching could not find correct barcode locator pattern.
            AR_MARKER_INFO_CUTOFF_PHASE_MATCH_BARCODE_EDC_FAIL, ///< Barcode matching error detection/correction found unrecoverable error.
            AR_MARKER_INFO_CUTOFF_PHASE_MATCH_CONFIDENCE,       ///< Matching confidence cutoff value not reached.
            AR_MARKER_INFO_CUTOFF_PHASE_POSE_ERROR,             ///< Maximum allowable pose error exceeded.
            AR_MARKER_INFO_CUTOFF_PHASE_POSE_ERROR_MULTI,       ///< Multi-marker pose error value exceeded.
            AR_MARKER_INFO_CUTOFF_PHASE_HEURISTIC_TROUBLESOME_MATRIX_CODES ///< Heuristic-based rejection of troublesome matrix code which is often generated in error.
        };

        public const int AR_MATRIX_CODE_TYPE_SIZE_MASK = 0x000000ff;  ///< Mask value, bitwise-OR with matrix code type to find matrix code size.
        public const int AR_MATRIX_CODE_TYPE_ECC_NONE = 0x00000000;   ///< No error detection or correction.
        public const int AR_MATRIX_CODE_TYPE_ECC_PARITY = 0x00000100; ///< Single-bit parity.
        public const int AR_MATRIX_CODE_TYPE_ECC_HAMMING = 0x00000200; ///< Hamming code with Hamming distance of 3.
        public const int AR_MATRIX_CODE_TYPE_ECC_BCH___3 = 0x00000300; ///< BCH code with Hamming distance of 3.
        public const int AR_MATRIX_CODE_TYPE_ECC_BCH___5 = 0x00000400; ///< BCH code with Hamming distance of 5.
        public const int AR_MATRIX_CODE_TYPE_ECC_BCH___7 = 0x00000500; ///< BCH code with Hamming distance of 7.
        public const int AR_MATRIX_CODE_TYPE_ECC_BCH___9 = 0x00000600; ///< BCH code with Hamming distance of 9.
        public const int AR_MATRIX_CODE_TYPE_ECC_BCH___11 = 0x00000700; ///< BCH code with Hamming distance of 11.
        public const int AR_MATRIX_CODE_TYPE_ECC_BCH___19 = 0x00000b00; ///< BCH code with Hamming distance of 19.

        public enum AR_MATRIX_CODE_TYPE
        {
            AR_MATRIX_CODE_3x3 = 0x03,                                                  ///< Matrix code in range 0-63.
            AR_MATRIX_CODE_3x3_PARITY65 = 0x03 | AR_MATRIX_CODE_TYPE_ECC_PARITY,        ///< Matrix code in range 0-31.
            AR_MATRIX_CODE_3x3_HAMMING63 = 0x03 | AR_MATRIX_CODE_TYPE_ECC_HAMMING,      ///< Matrix code in range 0-7.
            AR_MATRIX_CODE_4x4 = 0x04,                                                  ///< Matrix code in range 0-8191.
            AR_MATRIX_CODE_4x4_BCH_13_9_3 = 0x04 | AR_MATRIX_CODE_TYPE_ECC_BCH___3,     ///< Matrix code in range 0-511.
            AR_MATRIX_CODE_4x4_BCH_13_5_5 = 0x04 | AR_MATRIX_CODE_TYPE_ECC_BCH___5,     ///< Matrix code in range 0-31.
            AR_MATRIX_CODE_5x5_BCH_22_12_5 = 0x05 | AR_MATRIX_CODE_TYPE_ECC_BCH___5,    ///< Matrix code in range 0-4095.
            AR_MATRIX_CODE_5x5_BCH_22_7_7 = 0x05 | AR_MATRIX_CODE_TYPE_ECC_BCH___7,     ///< Matrix code in range 0-127.
            AR_MATRIX_CODE_5x5 = 0x05,                                                  ///< Matrix code in range 0-4194303.
            AR_MATRIX_CODE_6x6 = 0x06,                                                  ///< Matrix code in range 0-8589934591.
            AR_MATRIX_CODE_GLOBAL_ID = 0x0e | AR_MATRIX_CODE_TYPE_ECC_BCH___19
        };

        public const int ARW_TRACKER_OPTION_NFT_MULTIMODE = 0;                          ///< int.
        public const int ARW_TRACKER_OPTION_SQUARE_THRESHOLD = 1;                       ///< Threshold value used for image binarization. int in range [0-255].
        public const int ARW_TRACKER_OPTION_SQUARE_THRESHOLD_MODE = 2;                  ///< Threshold mode used for image binarization. int.
        public const int ARW_TRACKER_OPTION_SQUARE_LABELING_MODE = 3;                   ///< int.
        public const int ARW_TRACKER_OPTION_SQUARE_PATTERN_DETECTION_MODE = 4;          ///< int.
        public const int ARW_TRACKER_OPTION_SQUARE_BORDER_SIZE = 5;                     ///< float in range (0-0.5).
        public const int ARW_TRACKER_OPTION_SQUARE_MATRIX_CODE_TYPE = 6;                ///< int.
        public const int ARW_TRACKER_OPTION_SQUARE_IMAGE_PROC_MODE = 7;                 ///< int.
        public const int ARW_TRACKER_OPTION_SQUARE_DEBUG_MODE = 8;                      ///< Enables or disable state of debug mode in the tracker. When enabled, a black and white debug image is generated during marker detection. The debug image is useful for visualising the binarization process and choosing a threshold value. bool.
        public const int ARW_TRACKER_OPTION_SQUARE_PATTERN_SIZE = 9;                    ///< Number of rows and columns in square template (pattern) markers. Defaults to AR_PATT_SIZE1, which is 16 in all versions of ARToolKit prior to 5.3. int.
        public const int ARW_TRACKER_OPTION_SQUARE_PATTERN_COUNT_MAX = 10;              ///< Maximum number of square template (pattern) markers that may be loaded at once. Defaults to AR_PATT_NUM_MAX, which is at least 25 in all versions of ARToolKit prior to 5.3. int.
        public const int ARW_TRACKER_OPTION_2D_TRACKER_FEATURE_TYPE = 11;               ///< Feature detector type used in the 2d Tracker - 0 AKAZE, 1 ORB, 2 BRISK, 3 KAZE
        public const int ARW_TRACKER_OPTION_2D_CORNER_REFINEMENT = 12;                  ///< Enables or disables corner refinement

        /**
         * Constants for use with trackable option setters/getters.
         */
        public const int ARW_TRACKABLE_OPTION_FILTERED = 1;                         ///< bool, true for filtering enabled.
        public const int ARW_TRACKABLE_OPTION_FILTER_SAMPLE_RATE = 2;               ///< float, sample rate for filter calculations.
        public const int ARW_TRACKABLE_OPTION_FILTER_CUTOFF_FREQ = 3;               ///< float, cutoff frequency of filter.
        public const int ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION = 4;  ///< bool, true to use continuous pose estimate.
        public const int ARW_TRACKABLE_OPTION_SQUARE_CONFIDENCE = 5;                ///< float, confidence value of most recent marker match
        public const int ARW_TRACKABLE_OPTION_SQUARE_CONFIDENCE_CUTOFF = 6;         ///< float, minimum allowable confidence value used in marker matching.
        public const int ARW_TRACKABLE_OPTION_NFT_SCALE = 7;                        ///< float, scale factor applied to NFT marker size.
        public const int ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS = 8;             ///< int, minimum number of submarkers for tracking to be valid.
        public const int ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX = 9;            ///< float, minimum confidence value for submarker matrix tracking to be valid.
        public const int ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_PATTERN = 10;          ///< float, minimum confidence value for submarker pattern tracking to be valid.
        public const int ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB = 11;           ///< float, minimum inlier probability value for robust multimarker pose estimation (range 1.0 - 0.0).

        /* for arDebug */
        public const int AR_DEBUG_DISABLE = 0;
        public const int AR_DEBUG_ENABLE = 1;
        public const int AR_DEFAULT_DEBUG_MODE = AR_DEBUG_DISABLE;

        /* for arLabelingMode */
        public const int AR_LABELING_WHITE_REGION = 0;
        public const int AR_LABELING_BLACK_REGION = 1;
        public const int AR_DEFAULT_LABELING_MODE = AR_LABELING_BLACK_REGION;

        /* for arlabelingThresh */
        public const int AR_DEFAULT_LABELING_THRESH = 100;

        /* for arImageProcMode */
        public const int AR_IMAGE_PROC_FRAME_IMAGE = 0;
        public const int AR_IMAGE_PROC_FIELD_IMAGE = 1;
        public const int AR_DEFAULT_IMAGE_PROC_MODE = AR_IMAGE_PROC_FRAME_IMAGE;

        /* for arPatternDetectionMode */
        public const int AR_TEMPLATE_MATCHING_COLOR = 0;
        public const int AR_TEMPLATE_MATCHING_MONO = 1;
        public const int AR_MATRIX_CODE_DETECTION = 2;
        public const int AR_TEMPLATE_MATCHING_COLOR_AND_MATRIX = 3;
        public const int AR_TEMPLATE_MATCHING_MONO_AND_MATRIX = 4;
        public const int AR_DEFAULT_PATTERN_DETECTION_MODE = AR_TEMPLATE_MATCHING_COLOR;

        /* for arMarkerExtractionMode */
        public const int AR_USE_TRACKING_HISTORY = 0;
        public const int AR_NOUSE_TRACKING_HISTORY = 1;
        public const int AR_USE_TRACKING_HISTORY_V2 = 2;
        public const int AR_DEFAULT_MARKER_EXTRACTION_MODE = AR_USE_TRACKING_HISTORY_V2;

        /* for arCornerRefinementMode */
        public const int AR_CORNER_REFINEMENT_DISABLE = 0;
        public const int AR_CORNER_REFINEMENT_ENABLE = 1;
        public const int AR_DEFAULT_CORNER_REFINEMENT_MODE = AR_CORNER_REFINEMENT_DISABLE;

        /* for arGetTransMat */
        public const int AR_MAX_LOOP_COUNT = 5;
        public const float AR_LOOP_BREAK_THRESH = 0.5f;

        /* for arPatt**      */
        public const int AR_PATT_NUM_MAX = 50;
        public const int AR_PATT_SIZE1 = 16;        // Default number of rows and columns in pattern when pattern detection mode is not AR_MATRIX_CODE_DETECTION. Must be 16 in order to be compatible with ARToolKit versions 1.0 to 5.1.6.
        public const int AR_PATT_SIZE1_MAX = 64;     // Maximum number of rows and columns allowed in pattern when pattern detection mode is not AR_MATRIX_CODE_DETECTION.
        public const int AR_PATT_SIZE2_MAX = 32;     // Maximum number of rows and columns allowed in pattern when pattern detection mode is AR_MATRIX_CODE_DETECTION.
        public const int AR_PATT_SAMPLE_FACTOR1 = 4;     // Maximum number of samples per pattern pixel row / column when pattern detection mode is not AR_MATRIX_CODE_DETECTION.
        public const int AR_PATT_SAMPLE_FACTOR2 = 3;     // Maximum number of samples per pattern pixel row / column when detection mode is AR_MATRIX_CODE_DETECTION.
        public const float AR_PATT_CONTRAST_THRESH1 = 15.0f;   // Required contrast over pattern space when pattern detection mode is AR_TEMPLATE_MATCHING_MONO or AR_TEMPLATE_MATCHING_COLOR.
        public const float AR_PATT_CONTRAST_THRESH2 = 30.0f;   // Required contrast between black and white barcode segments when pattern detection mode is AR_MATRIX_CODE_DETECTION.
        public const float AR_PATT_RATIO = 0.5f;   // Default value for percentage of marker width or height considered to be pattern space. Equal to 1.0 - 2*borderSize. Must be 0.5 in order to be compatible with ARToolKit versions 1.0 to 4.4.



        public const int AR_AREA_MAX = 1000000;     // Maximum area (in pixels) of connected regions considered valid candidate for marker detection.
        public const int AR_AREA_MIN = 70;      // Minimum area (in pixels) of connected regions considered valid candidate for marker detection.
        public const float AR_SQUARE_FIT_THRESH = 1.0f;

        public const int AR_LABELING_32_BIT = 1;     // 0 = 16 bits per label, 1 = 32 bits per label.
        public const int AR_LABELING_WORK_SIZE = 1024 * 32 * 64;
        //public const int AR_LABELING_LABEL_TYPE = ARInt32;

        public const int AR_SQUARE_MAX = 60;     // Maxiumum number of marker squares per frame.
        public const int AR_CHAIN_MAX = 10000;

        public const int AR_LABELING_THRESH_AUTO_INTERVAL_DEFAULT = 7; // Number of frames between auto-threshold calculations.
        public const int AR_LABELING_THRESH_MODE_DEFAULT = (int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL;
        public const int AR_LABELING_THRESH_ADAPTIVE_KERNEL_SIZE_DEFAULT = 9;
        public const int AR_LABELING_THRESH_ADAPTIVE_BIAS_DEFAULT = (-7);

        public const float AR_CONFIDENCE_CUTOFF_DEFAULT = 0.5f;
        public const int AR_MATRIX_CODE_TYPE_DEFAULT = (int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_3x3;

        #endregion

        #region Properties

        public static bool GetGFVisible()
        {
            return myGFVisibleHiRes;
        }

        public static void SetGFVisible( bool value)
        {
                myGFVisibleHiRes = value;
        }

        public static bool GetStepVisible()
        {
            return myStepVisibleHiRes;
        }

        public static void SetStepVisible( bool value)
        {
                myStepVisibleHiRes = value;
        }

        public static bool GetLeftBulkheadVisible()
        {
            return myLeftBulkheadVisibleHiRes;
        }

        public static void SetLeftBulkheadVisible( bool value)
        {
                myLeftBulkheadVisibleHiRes = value;
        }

        public static bool GetRightBulkheadVisible()
        {
            return myRightBulkheadVisibleHiRes;
        }

        public static void SetRightBulkheadVisible( bool value)
        {
                myRightBulkheadVisibleHiRes = value;
        }

        public static bool GetDoorHingeLeftVisible()
        {
            return myDoorHingeLeftVisibleHiRes;
        }

        public static void SetDoorHingeLeftVisible( bool value)
        {
                myDoorHingeLeftVisibleHiRes = value;
        }

        public static bool GetDoorHingeRightVisible()
        {
            return myDoorHingeRightVisibleHiRes;
        }

        public static void SetDoorHingeRightVisible( bool value)
        {
                myDoorHingeRightVisibleHiRes = value;
        }

        public static bool GetDoorFrameLeftVisible()
        {
            return myDoorFrameLeftVisibleHiRes;
        }

        public static void SetDoorFrameLeftVisible( bool value)
        {
                myDoorFrameLeftVisibleHiRes = value;
        }

        public static bool GetDoorFrameRightVisible()
        {
            return myDoorFrameRightVisibleHiRes;
        }

        public static void SetDoorFrameRightVisible( bool value)
        {
                myDoorFrameRightVisibleHiRes = value;
        }

        public static bool GetObstruct1Visible()
        {
            return myObstruct1VisibleHiRes;
        }

        public static void SetObstruct1Visible( bool value)
        {
                myObstruct1VisibleHiRes = value;
        }

        public static bool GetObstruct2Visible()
        {
            return myObstruct2VisibleHiRes;
        }

        public static void SetObstruct2Visible( bool value)
        {
                myObstruct2VisibleHiRes = value;
        }

        public static bool GetObstruct3Visible()
        {
            return myObstruct3VisibleHiRes;
        }

        public static void SetObstruct3Visible( bool value)
        {
                myObstruct3VisibleHiRes = value;
        }

        public static bool GetObstruct4Visible()
        {
            return myObstruct4VisibleHiRes;
        }

        public static void SetObstruct4Visible( bool value)
        {
                myObstruct4VisibleHiRes = value;
        }

        public static bool GetWall1Visible()
        {
            return myWall1VisibleHiRes;
        }

        public static void SetWall1Visible( bool value)
        {
                myWall1VisibleHiRes = value;
        }

        public static bool GetWall2Visible()
        {
            return myWall2VisibleHiRes;
        }

        public static void SetWall2Visible( bool value)
        {
                myWall2VisibleHiRes = value;
        }

        public static bool GetWall3Visible()
        {
            return myWall3VisibleHiRes;
        }

        public static void SetWall3Visible( bool value)
        {
                myWall3VisibleHiRes = value;
        }

        public static bool GetWall4Visible()
        {
            return myWall4VisibleHiRes;
        }

        public static void SetWall4Visible(bool value)
        {
                myWall4VisibleHiRes = value;
        }

        public static List<int> GetMarkersSeenID()
        {
            return myMarkersSeenIDHiRes;
        }

        public static void SetMarkersSeenID( List<int> value)
        {
                myMarkersSeenIDHiRes = value;
        }

        public static List<int> GetConfirmedMarkersSeenID()
        {
            return myConfirmedMarkersSeenIDHiRes;
        }

        public static List<int> GetSuspectedMarkersSeenID()
        {
            return mySuspectedMarkersSeenIDHiRes;
        }

        public static List<int> GetDoorMarkersSeenID()
        {
            return myDoorMarkersSeenIDHiRes;
        }

        public static List<int> GetObstructMarkersSeenID()
        {
            return myObstructMarkersSeenIDHiRes;
        }

        public static List<int> GetWallMarkersSeenID()
        {
            return myWallMarkersSeenIDHiRes;
        }

        public static int GetGFMultiMarkerIndex()
        {
            return myGFMultiMarkerIndexHiRes;
        }

        public static void SetGFMultiMarkerIndex( int value)
        {
                myGFMultiMarkerIndexHiRes = value;
        }

        public static int GetStepMultiMarkerIndex()
        {
            return myStepMultiMarkerIndexHiRes;
        }

        public static void SetStepMultiMarkerIndex( int value)
        {
                myStepMultiMarkerIndexHiRes = value;
        }

        public static int GetLeftBulkheadMarkerIndex()
        {
            return myLeftBulkheadMarkerIndexHiRes;
        }

        public static void SetLeftBulkheadMarkerIndex(int value)
        {
                myLeftBulkheadMarkerIndexHiRes = value;
        }

        public static int GetRightBulkheadMarkerIndex()
        {
            return myRightBulkheadMarkerIndexHiRes;
        }

        public static void SetRightBulkheadMarkerIndex( int value)
        {
                myRightBulkheadMarkerIndexHiRes = value;
        }

        public static int GetDoorHingeLeftIndex()
        {
            return myDoorHingeLeftIndexHiRes;
        }

        public static void SetDoorHingeLeftIndex( int value)
        {
                myDoorHingeLeftIndexHiRes = value;
        }

        public static int GetDoorHingeRightIndex()
        {
            return myDoorHingeRightIndexHiRes;
        }

        public static void SetDoorHingeRightIndex( int value)
        {
                myDoorHingeRightIndexHiRes = value;
        }

        public static int GetDoorFrameLeftIndex()
        {
            return myDoorFrameLeftIndexHiRes;
        }

        public static void SetDoorFrameLeftIndex( int value)
        {
                myDoorFrameLeftIndexHiRes = value;
        }

        public static int GetDoorFrameRightIndex()
        {
            return myDoorFrameRightIndexHiRes;
        }

        public static void SetDoorFrameRightIndex( int value)
        {
                myDoorFrameRightIndexHiRes = value;
        }

        public static int GetObstruct1Index()
        {
            return myObstruct1IndexHiRes;
        }

        public static void SetObstruct1Index( int value)
        {
                myObstruct1IndexHiRes = value;
        }

        public static int GetObstruct2Index()
        {
            return myObstruct2IndexHiRes;
        }

        public static void SetObstruct2Index( int value)
        {
                myObstruct2IndexHiRes = value;
        }

        public static int GetObstruct3Index()
        {
            return myObstruct3IndexHiRes;
        }

        public static void SetObstruct3Index( int value)
        {
                myObstruct3IndexHiRes = value;
        }

        public static int GetObstruct4Index()
        {
            return myObstruct4IndexHiRes;
        }

        public static void SetObstruct4Index( int value)
        {
                myObstruct4IndexHiRes = value;
        }

        public static int GetWall1Index()
        {
            return myWall1IndexHiRes;
        }

        public static void SetWall1Index( int value)
        {
                myWall1IndexHiRes = value;
        }

        public static int GetWall2Index()
        {
            return myWall2IndexHiRes;
        }

        public static void SetWall2Index( int value)
        {
                myWall2IndexHiRes = value;
        }

        public static int GetWall3Index()
        {
            return myWall3IndexHiRes;
        }

        public static void SetWall3Index( int value)
        {
                myWall3IndexHiRes = value;
        }

        public static int GetWall4Index()
        {
            return myWall4IndexHiRes;
        }

        public static void SetWall4Index( int value)
        {
                myWall4IndexHiRes = value;
        }

        public static List<int> GetConfirmedMarkersSeenIndex()
        {
            return myConfirmedMarkersSeenIndexHiRes;
        }

        public static List<int> GetSuspectedMarkersSeenIndex()
        {
            return mySuspectedMarkersSeenIndexHiRes;
        }

        public static List<int> GetDoorMarkersSeenIndex()
        {
            return myDoorMarkersSeenIndexHiRes;
        }

        public static List<int> GetObstructMarkersSeenIndex()
        {
            return myObstructMarkersSeenIndexHiRes;
        }

        public static List<int> GetWallMarkersSeenIndex()
        {
            return myWallMarkersSeenIndexHiRes;
        }

        public static List<float[]> GetModelViewMatrix()
        {
            return myModelViewMatrixHiRes;
        }

        public static void SetModelViewMatrix( List<float[]> value)
        {
                myModelViewMatrixHiRes = value;

        }

        public static float[] GetProjMatrix()
        {
            return myProjMatrixHiRes;
        }

        public static void SetProjMatrix( float[] value)
        {
                myProjMatrixHiRes = value;
        }
        
        #endregion

        public static bool StartTracking(int hiResX, int hiResY)
        {

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

            AddMarkersToARToolKit();

            //mySuspectedMarkers.Clear()
            if (StepMarker.Confirmed == false) {
                StepMarker = new clsMarkerPoint(myStepMultiMarkerID, myStepMultiMarkerID);
            }

            isTrackingEnabled = true;
            return true;
        }

        private static void AddMarkersToARToolKit() {
            int i;

            //!!!IMPORTANT NOTE:
            //In arConfig.h:
            //#define   AR_LABELING_32_BIT                  1     // 0 = 16 bits per label, 1 = 32 bits per label.
            //#  define AR_LABELING_WORK_SIZE      1024*32*64

            ARToolKitFunctions.Instance.arwSetPatternDetectionMode(AR_MATRIX_CODE_DETECTION);
            ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_4x4);
            //ARToolKitFunctions.Instance.arwSetMarkerExtractionMode(AR_USE_TRACKING_HISTORY_V2); //This doesn't work in ARToolKitX
            ARToolKitFunctions.Instance.arwSetVideoThreshold(50);
            ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL);
            ARToolKitFunctions.Instance.arwSetCornerRefinementMode(true);

            myMarkerIDs.Clear();
            DebugStringList.Clear();


            for (i = 1; i <= 100; i++) {
                myMarkerIDs.Add(ARToolKitFunctions.Instance.arwAddMarker("multi;data/MarkerLarge" + i.ToString("00") + ".dat"));
                //Path to markers is local
                if (myMarkerIDs[myMarkerIDs.Count - 1] > -1) {
                    ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 2);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 0.75f);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                    ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMarkerIDs[myMarkerIDs.Count - 1], ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);
                }
            }

            myGFMultiMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/GFMarker.dat");
            if (myGFMultiMarkerID > -1) {
                ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myGFMultiMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 4);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myGFMultiMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 0.75f);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myGFMultiMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myGFMultiMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);
            }

            myStepMultiMarkerID = ARToolKitFunctions.Instance.arwAddMarker("multi;data/StepMarker.dat");
            if (myStepMultiMarkerID > -1) {
                ARToolKitFunctions.Instance.arwSetTrackableOptionInt(myStepMultiMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_SUBMARKERS, 4);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myStepMultiMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_CONF_MATRIX, 0.75f);
                ARToolKitFunctions.Instance.arwSetTrackableOptionBool(myStepMultiMarkerID, ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, false);
                ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myStepMultiMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 0.75f);
            }

            myLeftBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;249;80;");
            myRightBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;250;80;");

            myDoorHingeRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;251;80;");
            myDoorFrameRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;252;80;");
            myDoorHingeLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;253;80;");
            myDoorFrameLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;254;80;");

            myObstruct1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;255;80;");
            myObstruct2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;256;80;");
            myObstruct3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;257;80;");
            myObstruct4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;258;80;");

            myWall1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;259;80;");
            myWall2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;260;80;");
            myWall3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;261;80;");
            myWall4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;262;80;");

            float[] myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            bool b = ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myGFMultiMarkerID, 0, myMatrix, out float width, out float height, out int imageSizeX, out int imageSizeY, out int barcodeID);
            string sConfig = "multi_auto;" + barcodeID + ";" + ((int)width) + ";";
            //string sConfig = "multi_auto;" + myGFMultiMarkerID + ";" + ((int)width) + ";";
            myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);

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

        private static void AddDatumMarkersToARToolKit() {
            int i;

            //!!!IMPORTANT NOTE:
            //In arConfig.h:
            //#define   AR_LABELING_32_BIT                  1     // 0 = 16 bits per label, 1 = 32 bits per label.
            //#  define AR_LABELING_WORK_SIZE      1024*32*64

            ARToolKitFunctions.Instance.arwSetPatternDetectionMode(AR_MATRIX_CODE_DETECTION);
            //ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_4x4);
            ARToolKitFunctions.Instance.arwSetMatrixCodeType((int)AR_MATRIX_CODE_TYPE.AR_MATRIX_CODE_5x5_BCH_22_12_5);
            //ARToolKitFunctions.Instance.arwSetMarkerExtractionMode(AR_USE_TRACKING_HISTORY_V2); //This doesn't work in ARToolKitX
            ARToolKitFunctions.Instance.arwSetVideoThreshold(50);
            ARToolKitFunctions.Instance.arwSetVideoThresholdMode((int)AR_LABELING_THRESH_MODE.AR_LABELING_THRESH_MODE_MANUAL);
            ARToolKitFunctions.Instance.arwSetCornerRefinementMode(false);

            myMarkerIDs.Clear();
            DebugStringList.Clear();

            myGFMultiMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;0;80;");
            myStepMultiMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;1;80;");

            for (i = 1; i <= 100; i++) {
                myMarkerIDs.Add(ARToolKitFunctions.Instance.arwAddMarker("single_barcode;" + (i + 1).ToString("00") + ";80"));
            }

            myLeftBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;102;80;");
            myRightBulkheadMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;103;80;");

            myDoorHingeRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;104;80;");
            myDoorFrameRightMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;105;80;");
            myDoorHingeLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;106;80;");
            myDoorFrameLeftMarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;107;80;");

            myObstruct1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;108;80;");
            myObstruct2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;109;80;");
            myObstruct3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;110;80;");
            myObstruct4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;111;80;");

            myWall1MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;112;80;");
            myWall2MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;113;80;");
            myWall3MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;114;80;");
            myWall4MarkerID = ARToolKitFunctions.Instance.arwAddMarker("single_barcode;115;80;");

            //float[] myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //bool b = ARToolKitFunctions.Instance.arwGetTrackablePatternConfig(myGFMultiMarkerID, 0, myMatrix, out float width, out float height, out int imageSizeX, out int imageSizeY, out int barcodeID);
            //string sConfig = "multi_auto;" + barcodeID + ";" + ((int)width) + ";";
            string sConfig = "multi_auto;" + myGFMultiMarkerID + ";80;";
            myMapperMarkerID = ARToolKitFunctions.Instance.arwAddMarker(sConfig);
            ARToolKitFunctions.Instance.arwSetTrackableOptionFloat(myMapperMarkerID, ARW_TRACKABLE_OPTION_MULTI_MIN_INLIER_PROB, 1.0f);

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

        public static void RecogniseMarkers(byte[] imageBytes)
        {
            //int i, j;
            //float[] myMatrix;
            //clsPoint3d pt;
            //bool retB;

            //GetModelViewMatrix().Clear();
            //GetMarkersSeenID().Clear();
            //SetGFVisible(false);
            //SetStepVisible(false);
            //SetGFMultiMarkerIndex(-1);
            //SetStepMultiMarkerIndex(-1);
            //SetLeftBulkheadVisible(false);
            //SetLeftBulkheadMarkerIndex(-1);
            //SetRightBulkheadVisible(false);
            //SetRightBulkheadMarkerIndex(-1);

            //SetDoorHingeLeftVisible(false);
            //SetDoorHingeLeftIndex(-1);
            //SetDoorHingeRightVisible(false);
            //SetDoorHingeRightIndex(-1);
            //SetDoorFrameLeftVisible(false);
            //SetDoorFrameLeftIndex(-1);
            //SetDoorFrameRightVisible(false);
            //SetDoorFrameRightIndex(-1);

            //SetObstruct1Visible(false);
            //SetObstruct1Index(-1);
            //SetObstruct2Visible(false);
            //SetObstruct2Index(-1);
            //SetObstruct3Visible(false);
            //SetObstruct3Index(-1);
            //SetObstruct4Visible(false);
            //SetObstruct4Index(-1);

            //SetWall1Visible(false);
            //SetWall1Index(-1);
            //SetWall2Visible(false);
            //SetWall2Index(-1);
            //SetWall3Visible(false);
            //SetWall3Index(-1);
            //SetWall4Visible(false);
            //SetWall4Index( -1);

            ////ARToolKitFunctions.Instance.arwShowMessage("Hello");
            //retB = ARToolKitFunctions.Instance.arwUpdateARToolKit(imageBytes, 2); // 2 = AR_PIXEL_FORMAT_RGBA
            //ARToolKitFunctions.Instance.arwGetProjectionMatrix(GetProjMatrix());

            //for (i = 0; i <= myMarkerIDs.Count - 1; i++) {
            //    if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myMarkerIDs[i])) {
            //        myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //        ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myMarkerIDs[i], myMatrix);
            //        pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //        if (pt.Length < 2000) {
            //            GetMarkersSeenID().Add(myMarkerIDs[i]);
            //            GetModelViewMatrix().Add(myMatrix);
            //        }
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myStepMultiMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myStepMultiMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetStepVisible(true);
            //        GetMarkersSeenID().Add(myStepMultiMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetStepMultiMarkerIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myGFMultiMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myGFMultiMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetGFVisible(true);
            //        GetMarkersSeenID().Add(myGFMultiMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetGFMultiMarkerIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myLeftBulkheadMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myLeftBulkheadMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetLeftBulkheadVisible(true);
            //        GetMarkersSeenID().Add(myLeftBulkheadMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetLeftBulkheadMarkerIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myRightBulkheadMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myRightBulkheadMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetRightBulkheadVisible(true);
            //        GetMarkersSeenID().Add(myRightBulkheadMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetRightBulkheadMarkerIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myDoorHingeRightMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myDoorHingeRightMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetDoorHingeRightVisible(true);
            //        GetMarkersSeenID().Add(myDoorHingeRightMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetDoorHingeRightIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myDoorFrameRightMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myDoorFrameRightMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetDoorFrameRightVisible(true);
            //        GetMarkersSeenID().Add(myDoorFrameRightMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetDoorFrameRightIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myDoorHingeLeftMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myDoorHingeLeftMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetDoorHingeLeftVisible(true);
            //        GetMarkersSeenID().Add(myDoorHingeLeftMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetDoorHingeLeftIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myDoorFrameLeftMarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myDoorFrameLeftMarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetDoorFrameLeftVisible(true);
            //        GetMarkersSeenID().Add(myDoorFrameLeftMarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetDoorFrameLeftIndex(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myObstruct1MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myObstruct1MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetObstruct1Visible(true);
            //        GetMarkersSeenID().Add(myObstruct1MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetObstruct1Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myObstruct2MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myObstruct2MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetObstruct2Visible(true);
            //        GetMarkersSeenID().Add(myObstruct2MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetObstruct2Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myObstruct3MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myObstruct3MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetObstruct3Visible(true);
            //        GetMarkersSeenID().Add(myObstruct3MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetObstruct3Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myObstruct4MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myObstruct4MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetObstruct4Visible(true);
            //        GetMarkersSeenID().Add(myObstruct4MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetObstruct4Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myWall1MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myWall1MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetWall1Visible(true);
            //        GetMarkersSeenID().Add(myWall1MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetWall1Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myWall2MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myWall2MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetWall2Visible(true);
            //        GetMarkersSeenID().Add(myWall2MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetWall2Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myWall3MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myWall3MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetWall3Visible(true);
            //        GetMarkersSeenID().Add(myWall3MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetWall3Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //if (ARToolKitFunctions.Instance.arwQueryMarkerVisibility(myWall4MarkerID)) {
            //    myMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //    ARToolKitFunctions.Instance.arwQueryMarkerTransformation(myWall4MarkerID, myMatrix);
            //    pt = new clsPoint3d(myMatrix[12], myMatrix[13], myMatrix[14]);
            //    if (pt.Length < 2000) {
            //        SetWall4Visible(true);
            //        GetMarkersSeenID().Add(myWall4MarkerID);
            //        GetModelViewMatrix().Add(myMatrix);
            //        SetWall4Index(GetModelViewMatrix().Count - 1);
            //    }
            //}

            //try {
            //    ProcessMarkers();
            //    ProcessMarkers(false, true);
            //}
            //catch (Exception ex) {
            //    String s = ex.ToString();
            //}

        }

        public static void ProcessMarkers(bool straightToCalcs = false, bool onlySuspectedMarkers = false)
        {
            int i, j, k, k1;
            //clsPoint p2d1, p2d2, p2d1a, p2d2a, p2d;
            //clsPoint3d p1, p2, p3, p4;
            int i1;
            clsPoint3d pt1a;
            clsPoint3d pt1b;
            clsPoint3d pt1;
            clsPoint3d pt2;
            clsPoint3d pt3;
            clsPoint3d pt6a;
            clsPoint3d pt6b;
            clsPoint3d myCameraPoint;
            clsMarkerPoint pt;
            int myMarkerID, mySeenFromMarkerID;
            List<int> mySuspectConfirmedID = new List<int>();
            bool myGFConfirmed;
            bool myStepConfirmed;
            int myNumConfirmed;
            clsMarkerPoint myConfirmedMarker;
            List<clsPoint3d> pts1 = new List<clsPoint3d>();
            List<clsPoint3d> pts2 = new List<clsPoint3d>();
            List<clsPoint3d> pts3 = new List<clsPoint3d>();
            List<clsPoint3d> pts4 = new List<clsPoint3d>();
            List<clsPoint3d> pts5 = new List<clsPoint3d>();
            List<double> myDists = new List<double>();
            List<clsPoint3d> pts = new List<clsPoint3d>();
            List<clsMarkerPoint> myHistoricMarkers = new List<clsMarkerPoint>();
            string myErrorString = "";
            double a1 = 0, a2 = 0;
            clsPoint3d v1 = null, v2 = null, v3 = null, v4 = null;
            bool myMarkerConfirmed = false, b1 = false, b2 = false;
            bool mySuspectedMarkerAdded = false;
            List<int> myProcessedMarkerIDs = new List<int>();

            if (straightToCalcs) goto straightToCalcsStart;

            mySaveSnapshot = false;
            GetConfirmedMarkersSeenID().Clear();
            GetConfirmedMarkersSeenIndex().Clear();
            GetSuspectedMarkersSeenID().Clear();
            GetSuspectedMarkersSeenIndex().Clear();
            GetDoorMarkersSeenID().Clear();
            GetDoorMarkersSeenIndex().Clear();
            
            if (GetGFVisible()) {
                myCameraPoint = PointFromInvMatrix(GetGFMultiMarkerIndex());

                if (IsSameDbl(myCameraPoint.Length, 0) == false) {
                    myCameraPoint.Normalise();
                    a1 = Abs(myCameraPoint.AngleToHorizontal) * 180 / Math.PI;
                    if ((a1 < 45))
                        SetGFVisible(false);
                }
            }

            if (GetStepVisible()) {
                myCameraPoint = PointFromInvMatrix(GetStepMultiMarkerIndex());
                if (IsSameDbl(myCameraPoint.Length, 0) == false) {
                    myCameraPoint.Normalise();
                    a1 = Abs(myCameraPoint.AngleToHorizontal) * 180 / Math.PI;
                    if ((a1 < 45))
                        SetStepVisible(false);
                }
            }

            if (GetMarkersSeenID().Count > 0) {
                j = 0;
                while (j <= GetMarkersSeenID().Count - 1) {
                    myMarkerID = GetMarkersSeenID()[j];
                    if (myMarkerID <= myGFMultiMarkerID || myMarkerID == myStepMultiMarkerID) {
                        j = j + 1;
                        continue;
                    }

                    myCameraPoint = PointFromInvMatrix(j);
                    if (IsSameDbl(myCameraPoint.Length, 0) == false) {
                        a1 = Abs(myCameraPoint.AngleToHorizontal) * 180 / Math.PI;
                        if ((a1 < 45)) {
                            GetMarkersSeenID().RemoveAt(j);
                            GetModelViewMatrix().RemoveAt(j);
                            if (j < GetGFMultiMarkerIndex()) SetGFMultiMarkerIndex(GetGFMultiMarkerIndex() - 1);
                            if (j < GetStepMultiMarkerIndex()) SetStepMultiMarkerIndex(GetStepMultiMarkerIndex() - 1);
                            if (j < GetLeftBulkheadMarkerIndex()) SetLeftBulkheadMarkerIndex(GetLeftBulkheadMarkerIndex() - 1);
                            if (j < GetRightBulkheadMarkerIndex()) SetRightBulkheadMarkerIndex(GetRightBulkheadMarkerIndex() - 1);
                            if (j < GetDoorHingeRightIndex()) SetDoorHingeRightIndex(GetDoorHingeRightIndex() - 1);
                            if (j < GetDoorFrameRightIndex()) SetDoorFrameRightIndex(GetDoorFrameRightIndex() - 1);
                            if (j < GetDoorHingeLeftIndex()) SetDoorHingeLeftIndex(GetDoorHingeLeftIndex() - 1);
                            if (j < GetDoorFrameLeftIndex()) SetDoorFrameLeftIndex(GetDoorFrameLeftIndex() - 1);
                            if (j < GetObstruct1Index()) SetObstruct1Index(GetObstruct1Index() - 1);
                            if (j < GetObstruct2Index()) SetObstruct2Index(GetObstruct2Index() - 1);
                            if (j < GetObstruct3Index()) SetObstruct3Index(GetObstruct3Index() - 1);
                            if (j < GetObstruct4Index()) SetObstruct4Index(GetObstruct4Index() - 1);
                            if (j < GetWall1Index()) SetWall1Index(GetWall1Index() - 1);
                            if (j < GetWall2Index()) SetWall2Index(GetWall2Index() - 1);
                            if (j < GetWall3Index()) SetWall3Index(GetWall3Index() - 1);
                            if (j < GetWall4Index()) SetWall4Index(GetWall4Index() - 1);
                            continue;
                        }
                    }
                    j = j + 1;
                }
            }

            if (onlySuspectedMarkers == false && GetMarkersSeenID().Count == 0 && GetStepVisible() == false && GetLeftBulkheadVisible() == false && GetRightBulkheadVisible() == false && GetDoorHingeRightVisible() == false && GetDoorFrameRightVisible() == false && GetDoorHingeLeftVisible() == false && GetDoorFrameLeftVisible() == false && GetObstruct1Visible() == false && GetObstruct2Visible() == false && GetObstruct3Visible() == false && GetObstruct4Visible() == false && GetWall1Visible() == false && GetWall2Visible() == false && GetWall3Visible() == false && GetWall4Visible() == false)
                return;

            for (i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                j = ConfirmedMarkers[i].MarkerID;
                if (GetMarkersSeenID().Contains(j)) {
                    GetConfirmedMarkersSeenID().Add(j);
                    GetConfirmedMarkersSeenIndex().Add(i);
                }
            }

            mySuspectedMarkers.Sort(new SuspectedMarkerPointComparer());

            for (i = 0; i <= mySuspectedMarkers.Count - 1; i++) {
                j = mySuspectedMarkers[i].MarkerID;
                if (GetMarkersSeenID().Contains(j)) {
                    GetSuspectedMarkersSeenID().Add(j);
                    GetSuspectedMarkersSeenIndex().Add(i);
                }
            }

            for (i = myDoorMarkers.Count - 1; i >= 0; i--) {
                j = myDoorMarkers[i].MarkerID;
                if (GetMarkersSeenID().Contains(j) && GetDoorMarkersSeenID().Contains(j) == false) {
                    GetDoorMarkersSeenID().Add(j);
                    GetDoorMarkersSeenIndex().Add(i);
                }
            }

            for (i = myDoorMarkers.Count - 1; i >= 0; i--) {
                j = myDoorMarkers[i].MarkerID;
                if (GetMarkersSeenID().Contains(j) && GetDoorMarkersSeenID().Contains(j) == false) {
                    GetDoorMarkersSeenID().Add(j);
                    GetDoorMarkersSeenIndex().Add(i);
                }
            }

            for (i = myObstructMarkers.Count - 1; i >= 0; i--) {
                j = myObstructMarkers[i].MarkerID;
                if (GetMarkersSeenID().Contains(j) && GetObstructMarkersSeenID().Contains(j) == false) {
                    GetObstructMarkersSeenID().Add(j);
                    GetObstructMarkersSeenIndex().Add(i);
                }
            }

            for (i = myWallMarkers.Count - 1; i >= 0; i--) {
                j = myWallMarkers[i].MarkerID;
                if (GetMarkersSeenID().Contains(j) && GetWallMarkersSeenID().Contains(j) == false) {
                    GetWallMarkersSeenID().Add(j);
                    GetWallMarkersSeenIndex().Add(i);
                }
            }

            //Take a measurement of the step marker
            if (onlySuspectedMarkers == false && StepMarker.Confirmed == false && GetStepVisible()) {
                j = GetStepMultiMarkerIndex();

                if (GetGFVisible()) {
                    pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, GetGFMultiMarkerIndex(), 0);
                    pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, GetGFMultiMarkerIndex(), 1);
                    pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), j, GetGFMultiMarkerIndex());
                    pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), j, GetGFMultiMarkerIndex());
                    pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), j, GetGFMultiMarkerIndex());

                    RelevelMarkerFromGF(ref pt1a, ref pt1b, ref pt1, ref pt2, ref pt3);

                    pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, j, 0);
                    pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, j, 1);

                    myCameraPoint = PointFromInvMatrix(j);

                    if (AddSuspectedMarker(myStepMultiMarkerID, myGFMultiMarkerID, pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
                }

                if (GetConfirmedMarkersSeenID().Count >= 1) {
                    for (i = 0; i < GetConfirmedMarkersSeenID().Count; i++) {
                        i1 = GetMarkersSeenID().IndexOf(GetConfirmedMarkersSeenID()[i]);

                        pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, i1, 0);
                        pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, i1, 1);
                        pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), j, i1);
                        pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), j, i1);
                        pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), j, i1);
                        pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, j, 0);
                        pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), j, j, 1);
                        myCameraPoint = PointFromInvMatrix(j);

                        if (AddSuspectedMarker(myStepMultiMarkerID, GetConfirmedMarkersSeenID()[i], pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
                    }
                }
            }


            //Take a measurement of each of the other markers
            for (j = 0; j <= GetMarkersSeenID().Count - 1; j++) {
                myMarkerID = GetMarkersSeenID()[j];

                //Ignore confirmed markers, the GF marker and the step marker
                if (GetConfirmedMarkersSeenID().Contains(myMarkerID)) continue;
                if (myMarkerID == myGFMultiMarkerID) continue;
                if (myMarkerID == myStepMultiMarkerID) continue;

                if (CheckSuspectedMarker(myMarkerID, j, onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
            }


            straightToCalcsStart:

            //Check if we can convert a suspected marker to a confirmed one
            for (i = 0; i <= mySuspectedMarkers.Count - 1; i++) {
                if (mySuspectedMarkers[i].Confirmed == false) {
                    mySuspectedMarkers[i].SetEndPointBasedOnZVectors(straightToCalcs);
                    if (mySuspectedMarkers[i].Points1a.Count > 550) {
                        if (mySuspectedMarkers[i].MarkerID == myStepMultiMarkerID) {
                            DebugStringList.Add("Step Marker Reset.");
                        }
                        else if (mySuspectedMarkers[i].MarkerID == myLeftBulkheadMarkerID || mySuspectedMarkers[i].MarkerID == myRightBulkheadMarkerID) {
                            DebugStringList.Add("Bulkhead Marker Reset.");
                        }
                        else if (mySuspectedMarkers[i].MarkerID == myDoorHingeRightMarkerID || mySuspectedMarkers[i].MarkerID == myDoorFrameRightMarkerID || mySuspectedMarkers[i].MarkerID == myDoorHingeLeftMarkerID || mySuspectedMarkers[i].MarkerID == myDoorFrameLeftMarkerID) {
                            DebugStringList.Add("Door Marker Reset.");
                        }
                        else {
                            DebugStringList.Add("Marker " + (mySuspectedMarkers[i].MarkerID + 1) + " Reset.");
                        }
                        mySuspectedMarkers[i].Clear();
                    }
                    else if (mySuspectedMarkers[i].Points1a.Count > 1 && mySuspectedMarkers[i].Origin.Length > myTol &&
                        mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {


                        ////DEBUG:
                        //if (mySuspectedMarkers[i].Points1a.Count > 1 && mySuspectedMarkers[i].Origin.Length > myTol &&
                        //  mySuspectedMarkers[i].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2)) {
                        //    mySuspectedMarkers[i].SetEndPointBasedOnZVectors(straightToCalcs);
                        //}

                        mySuspectedMarkers[i].Confirmed = true;
                        v1 = myVerticalVector.Copy();
                        v1.Normalise();
                        double a = Abs(Asin(mySuspectedMarkers[i].VZ.Cross(v1).Length)) * 180 / Math.PI;

                        if (mySuspectedMarkers[i].MarkerID == myStepMultiMarkerID) {
                            DebugStringList.Add(string.Format("Step Marker Confirmed. {0:0.0}° From Vertical.", a));
                        }
                        else if (mySuspectedMarkers[i].MarkerID == myLeftBulkheadMarkerID || mySuspectedMarkers[i].MarkerID == myRightBulkheadMarkerID) {
                            DebugStringList.Add(string.Format("Bulkhead Marker Confirmed."));
                        }
                        else if (mySuspectedMarkers[i].MarkerID == myDoorHingeRightMarkerID || mySuspectedMarkers[i].MarkerID == myDoorFrameRightMarkerID || mySuspectedMarkers[i].MarkerID == myDoorHingeLeftMarkerID || mySuspectedMarkers[i].MarkerID == myDoorFrameLeftMarkerID) {
                            DebugStringList.Add(string.Format("Door Marker Confirmed."));
                        }
                        else {
                            DebugStringList.Add(string.Format("Marker {0:0} Confirmed. {1:0.0}° From Vertical.", mySuspectedMarkers[i].MarkerID + 1, a));
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
                if (myProcessedMarkerIDs.Contains(myMarkerID)) { i = i + 1; continue; }
                myProcessedMarkerIDs.Add(myMarkerID);
                myGFConfirmed = false;
                myStepConfirmed = false;
                myNumConfirmed = 0;

                //Takes the average measurement from the other confirmed markers:
                pts1.Clear();
                pts2.Clear();
                pts3.Clear();
                pts4.Clear();
                pts5.Clear();
                myDists.Clear();
                myHistoricMarkers.Clear();

                for (j = 0; j <= mySuspectedMarkers.Count - 1; j++) {
                    //if (mySuspectedMarkers[j].Confirmed == false) continue;

                    if (mySuspectedMarkers[j].MarkerID == myMarkerID && mySuspectedMarkers[j].Origin.Length > myTol) {
                        if (mySuspectedMarkers[j].SeenFromMarkerID == myGFMultiMarkerID) {
                            pts1.Add(mySuspectedMarkers[j].Origin.Copy());
                            pts2.Add(mySuspectedMarkers[j].XAxisPoint.Copy());
                            pts3.Add(mySuspectedMarkers[j].YAxisPoint.Copy());
                            pts4.Add(mySuspectedMarkers[j].ZAxisPoint.Copy());
                            pts5.Add(mySuspectedMarkers[j].Point.Copy());
                            myDists.Add(mySuspectedMarkers[j].Origin.Length);
                            if (mySuspectedMarkers[j].Confirmed) myGFConfirmed = true;
                        }
                        else if (mySuspectedMarkers[j].SeenFromMarkerID == myStepMultiMarkerID) {
                            pts1.Add(StepMarker.Origin + mySuspectedMarkers[j].Origin.X * StepMarker.VX + mySuspectedMarkers[j].Origin.Y * StepMarker.VY + mySuspectedMarkers[j].Origin.Z * StepMarker.VZ);
                            pts2.Add(StepMarker.Origin + mySuspectedMarkers[j].XAxisPoint.X * StepMarker.VX + mySuspectedMarkers[j].XAxisPoint.Y * StepMarker.VY + mySuspectedMarkers[j].XAxisPoint.Z * StepMarker.VZ);
                            pts3.Add(StepMarker.Origin + mySuspectedMarkers[j].YAxisPoint.X * StepMarker.VX + mySuspectedMarkers[j].YAxisPoint.Y * StepMarker.VY + mySuspectedMarkers[j].YAxisPoint.Z * StepMarker.VZ);
                            pts4.Add(StepMarker.Origin + mySuspectedMarkers[j].ZAxisPoint.X * StepMarker.VX + mySuspectedMarkers[j].ZAxisPoint.Y * StepMarker.VY + mySuspectedMarkers[j].ZAxisPoint.Z * StepMarker.VZ);
                            pts5.Add(StepMarker.Origin + mySuspectedMarkers[j].Point.X * StepMarker.VX + mySuspectedMarkers[j].Point.Y * StepMarker.VY + mySuspectedMarkers[j].Point.Z * StepMarker.VZ);
                            myDists.Add(mySuspectedMarkers[j].Origin.Length);
                            if (mySuspectedMarkers[j].Confirmed) myStepConfirmed = true;
                        }
                        else if (mySuspectedMarkers[j].SeenFromMarkerID == myDoorHingeRightMarkerID || mySuspectedMarkers[j].SeenFromMarkerID == myDoorFrameRightMarkerID || mySuspectedMarkers[j].SeenFromMarkerID == myDoorHingeLeftMarkerID || mySuspectedMarkers[j].SeenFromMarkerID == myDoorFrameLeftMarkerID) {
                            for (k = myDoorMarkers.Count - 1; k >= 0; k--) {
                                if (myDoorMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                    pt = myDoorMarkers[k];
                                    pts1.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                                    pts2.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                                    pts3.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                                    pts4.Add(pt.Origin + mySuspectedMarkers[j].ZAxisPoint.X * pt.VX + mySuspectedMarkers[j].ZAxisPoint.Y * pt.VY + mySuspectedMarkers[j].ZAxisPoint.Z * pt.VZ);
                                    pts5.Add(pt.Origin + mySuspectedMarkers[j].Point.X * pt.VX + mySuspectedMarkers[j].Point.Y * pt.VY + mySuspectedMarkers[j].Point.Z * pt.VZ);
                                    myDists.Add(mySuspectedMarkers[j].Origin.Length);
                                    if (mySuspectedMarkers[j].Confirmed) myNumConfirmed = myNumConfirmed + 1;
                                    break;
                                }
                            }
                        }
                        else if (mySuspectedMarkers[j].SeenFromMarkerID == myObstruct1MarkerID || mySuspectedMarkers[j].SeenFromMarkerID == myObstruct2MarkerID || mySuspectedMarkers[j].SeenFromMarkerID == myObstruct3MarkerID || mySuspectedMarkers[j].SeenFromMarkerID == myObstruct4MarkerID) {
                            for (k = myObstructMarkers.Count - 1; k >= 0; k--) {
                                if (myObstructMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                    pt = myObstructMarkers[k];
                                    pts1.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                                    pts2.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                                    pts3.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                                    pts4.Add(pt.Origin + mySuspectedMarkers[j].ZAxisPoint.X * pt.VX + mySuspectedMarkers[j].ZAxisPoint.Y * pt.VY + mySuspectedMarkers[j].ZAxisPoint.Z * pt.VZ);
                                    pts5.Add(pt.Origin + mySuspectedMarkers[j].Point.X * pt.VX + mySuspectedMarkers[j].Point.Y * pt.VY + mySuspectedMarkers[j].Point.Z * pt.VZ);
                                    myDists.Add(mySuspectedMarkers[j].Origin.Length);
                                    if (mySuspectedMarkers[j].Confirmed) myNumConfirmed = myNumConfirmed + 1;
                                    break;
                                }
                            }
                        }
                        else {
                            for (k = 0; k <= ConfirmedMarkers.Count - 1; k++) {
                                if (ConfirmedMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                    pt = ConfirmedMarkers[k];
                                    pts1.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                                    pts2.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                                    pts3.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                                    pts4.Add(pt.Origin + mySuspectedMarkers[j].ZAxisPoint.X * pt.VX + mySuspectedMarkers[j].ZAxisPoint.Y * pt.VY + mySuspectedMarkers[j].ZAxisPoint.Z * pt.VZ);
                                    pts5.Add(pt.Origin + mySuspectedMarkers[j].Point.X * pt.VX + mySuspectedMarkers[j].Point.Y * pt.VY + mySuspectedMarkers[j].Point.Z * pt.VZ);
                                    myDists.Add(mySuspectedMarkers[j].Origin.Length);
                                    if (mySuspectedMarkers[j].Confirmed) myNumConfirmed = myNumConfirmed + 1;
                                    break;
                                }
                            }

                            ////New! Also allow measurement from other suspected markers, but only if they can be seen from confirmed markers
                            //for (k = 0; k <= mySuspectedMarkers.Count - 1; k++) {
                            //    if (mySuspectedMarkers[k].MarkerID == mySuspectedMarkers[j].MarkerID) continue;
                            //    if (mySuspectedMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                            //        if (mySuspectedMarkers[k].Origin.Length > myTol) {
                            //            if (ConfirmedMarkersCopy.Count > 0) {
                            //                pt = mySuspectedMarkers[k].Copy();
                            //                SetSuspectedMarkerAxes(ref pt);
                            //                pt.Confirmed = true;
                            //            }
                            //            else {
                            //                pt = mySuspectedMarkers[k].Copy();
                            //                pt.Confirmed = false;
                            //                SetSuspectedMarkerAxes2(ref pt);
                            //            }

                            //            if (pt.Confirmed) {
                            //                pts1.Add(pt.Origin + mySuspectedMarkers[j].Origin.X * pt.VX + mySuspectedMarkers[j].Origin.Y * pt.VY + mySuspectedMarkers[j].Origin.Z * pt.VZ);
                            //                pts2.Add(pt.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt.VZ);
                            //                pts3.Add(pt.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt.VZ);
                            //                pts4.Add(pt.Origin + mySuspectedMarkers[j].ZAxisPoint.X * pt.VX + mySuspectedMarkers[j].ZAxisPoint.Y * pt.VY + mySuspectedMarkers[j].ZAxisPoint.Z * pt.VZ);
                            //                pts5.Add(pt.Origin + mySuspectedMarkers[j].Point.X * pt.VX + mySuspectedMarkers[j].Point.Y * pt.VY + mySuspectedMarkers[j].Point.Z * pt.VZ);
                            //                myDists.Add(mySuspectedMarkers[j].Origin.Length);
                            //                break;
                            //            }
                            //        }
                            //    }
                            //}

                        }
                        myHistoricMarkers.Add(mySuspectedMarkers[j].Copy());
                    }
                }

                //Now we can convert our suspected marker to a Confirmed marker
                if (myGFConfirmed || myStepConfirmed || (myNumConfirmed >= 2 || (myNumConfirmed >= 1 && (myMarkerID == myLeftBulkheadMarkerID || myMarkerID == myRightBulkheadMarkerID || myMarkerID == myDoorHingeRightMarkerID || myMarkerID == myDoorFrameRightMarkerID || myMarkerID == myDoorHingeLeftMarkerID || myMarkerID == myDoorFrameLeftMarkerID || myMarkerID == myObstruct1MarkerID || myMarkerID == myObstruct2MarkerID || myMarkerID == myObstruct3MarkerID || myMarkerID == myObstruct4MarkerID || myMarkerID == myWall1MarkerID || myMarkerID == myWall2MarkerID || myMarkerID == myWall3MarkerID || myMarkerID == myWall4MarkerID)))) {
                    if (myMarkerID == myStepMultiMarkerID) {
                        myMarkerConfirmed = true; //So we can auto-save
                        StepMarker.Confirmed = true;

                        StepMarker.SeenFromMarkerID = mySeenFromMarkerID;
                        StepMarker.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                        StepMarker.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                        StepMarker.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                        StepMarker.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                        StepMarker.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                        StepMarker.SetZNormal();
                        StepMarker.ModelViewMatrix = GetModelViewMatrixFromPoints(StepMarker.Origin, StepMarker.XAxisPoint, StepMarker.YAxisPoint);
                        StepMarker.Levelled = false;
                        StepMarker.Stitched = false;

                        ////If seen from the GF Marker, relevel
                        //if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        //    RelevelMarkerFromGF(StepMarker);
                        //}

                        ConfirmedMarkers.Add(StepMarker.Copy());
                        ConfirmedMarkers.Last().MarkerID = myMaximumMarkerID;
                        myMaximumMarkerID = myMaximumMarkerID + 1;
                        ConfirmedMarkers.Last().ActualMarkerID = myMarkerID;
                        ConfirmedMarkers.Last().History.AddRange(myHistoricMarkers.ToArray());
                        ConfirmedMarkers.Last().History[0].MarkerID = ConfirmedMarkers.Last().MarkerID;
                        ConfirmedMarkers.Last().History[0].ActualMarkerID = ConfirmedMarkers.Last().ActualMarkerID;
                        ConfirmedMarkers.Last().History[0].VerticalVect = StepMarker.VerticalVect.Copy();

                        //Clear all old bulkhead and door markers
                        for (j = 0; j < myBulkheadMarkers.Count; j++) {
                            myBulkheadMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myDoorMarkers.Count; j++) {
                            myDoorMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myObstructMarkers.Count; j++) {
                            myObstructMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myWallMarkers.Count; j++) {
                            myWallMarkers[j].Confirmed = false;
                        }

                    }
                    else if (myMarkerID == myLeftBulkheadMarkerID || myMarkerID == myRightBulkheadMarkerID) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint(myMarkerID, -1);
                        myConfirmedMarker.SeenFromMarkerID = mySeenFromMarkerID;
                        myConfirmedMarker.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                        myConfirmedMarker.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                        myConfirmedMarker.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                        myConfirmedMarker.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                        myConfirmedMarker.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                        myConfirmedMarker.SetZNormal();
                        myConfirmedMarker.ModelViewMatrix = GetModelViewMatrixFromPoints(myConfirmedMarker.Origin, myConfirmedMarker.XAxisPoint, myConfirmedMarker.YAxisPoint);
                        myBulkheadMarkers.Add(myConfirmedMarker.Copy());
                        myBulkheadMarkers.Last().Confirmed = true;
                        myBulkheadMarkers.Last().History.AddRange(myHistoricMarkers.ToArray());

                        ////If seen from the GF Marker, relevel
                        //if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        //    clsMarkerPoint m = myBulkheadMarkers.Last();
                        //    RelevelMarkerFromGF(m);
                        //}

                        //If seen from Step Marker, reset the SeenFromMarkerID
                        if (mySeenFromMarkerID == myStepMultiMarkerID) {
                            j = myStepMultiMarkerID;
                            for (k = 0; k < ConfirmedMarkers.Count; k++) {
                                if (ConfirmedMarkers[k].ActualMarkerID == myStepMultiMarkerID) j = ConfirmedMarkers[k].MarkerID;
                            }
                            myBulkheadMarkers.Last().SeenFromMarkerID = j;
                        }

                        //Clear all old door markers
                        for (j = 0; j < myDoorMarkers.Count; j++) {
                            myDoorMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myObstructMarkers.Count; j++) {
                            myObstructMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myWallMarkers.Count; j++) {
                            myWallMarkers[j].Confirmed = false;
                        }

                        //((IARViewController)ViewController).ShowHeightInput((HeightZ)=> { double z; if (double.TryParse(HeightZ, out z) && z > myTol) myBulkheadMarkers.Last().BulkheadHeight = z; else myBulkheadMarkers.Remove(myConfirmedMarkers.Last()); });
                    }
                    else if (myMarkerID == myDoorHingeRightMarkerID || myMarkerID == myDoorFrameRightMarkerID || myMarkerID == myDoorHingeLeftMarkerID || myMarkerID == myDoorFrameLeftMarkerID) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint(myMarkerID, -1);
                        myConfirmedMarker.SeenFromMarkerID = mySeenFromMarkerID;
                        myConfirmedMarker.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                        myConfirmedMarker.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                        myConfirmedMarker.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                        myConfirmedMarker.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                        myConfirmedMarker.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                        myConfirmedMarker.SetZNormal();
                        myConfirmedMarker.ModelViewMatrix = GetModelViewMatrixFromPoints(myConfirmedMarker.Origin, myConfirmedMarker.XAxisPoint, myConfirmedMarker.YAxisPoint);
                        myDoorMarkers.Add(myConfirmedMarker.Copy());
                        myDoorMarkers.Last().Confirmed = true;
                        myDoorMarkers.Last().History.AddRange(myHistoricMarkers.ToArray());

                        ////If seen from the GF Marker, relevel
                        //if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        //    clsMarkerPoint m = myDoorMarkers.Last();
                        //    RelevelMarkerFromGF(m);
                        //}

                        //If seen from Step Marker, reset the SeenFromMarkerID
                        if (mySeenFromMarkerID == myStepMultiMarkerID) {
                            j = myStepMultiMarkerID;
                            for (k = 0; k < ConfirmedMarkers.Count; k++) {
                                if (ConfirmedMarkers[k].ActualMarkerID == myStepMultiMarkerID) j = ConfirmedMarkers[k].MarkerID;
                            }
                            myDoorMarkers.Last().SeenFromMarkerID = j;
                        }

                        //Clear all old bulkhead  markers
                        for (j = 0; j < myBulkheadMarkers.Count; j++) {
                            myBulkheadMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myObstructMarkers.Count; j++) {
                            myObstructMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myWallMarkers.Count; j++) {
                            myWallMarkers[j].Confirmed = false;
                        }

                    }
                    else if (myMarkerID == myObstruct1MarkerID || myMarkerID == myObstruct2MarkerID || myMarkerID == myObstruct3MarkerID || myMarkerID == myObstruct4MarkerID) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint(myMarkerID, -1);
                        myConfirmedMarker.SeenFromMarkerID = mySeenFromMarkerID;
                        myConfirmedMarker.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                        myConfirmedMarker.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                        myConfirmedMarker.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                        myConfirmedMarker.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                        myConfirmedMarker.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                        myConfirmedMarker.SetZNormal();
                        myConfirmedMarker.ModelViewMatrix = GetModelViewMatrixFromPoints(myConfirmedMarker.Origin, myConfirmedMarker.XAxisPoint, myConfirmedMarker.YAxisPoint);
                        myObstructMarkers.Add(myConfirmedMarker.Copy());
                        myObstructMarkers.Last().Confirmed = true;
                        myObstructMarkers.Last().History.AddRange(myHistoricMarkers.ToArray());

                        ////If seen from the GF Marker, relevel
                        //if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        //    clsMarkerPoint m = myObstructMarkers.Last();
                        //    RelevelMarkerFromGF(m);
                        //}

                        //If seen from Step Marker, reset the SeenFromMarkerID
                        if (mySeenFromMarkerID == myStepMultiMarkerID) {
                            j = myStepMultiMarkerID;
                            for (k = 0; k < ConfirmedMarkers.Count; k++) {
                                if (ConfirmedMarkers[k].ActualMarkerID == myStepMultiMarkerID) j = ConfirmedMarkers[k].MarkerID;
                            }
                            myObstructMarkers.Last().SeenFromMarkerID = j;
                        }

                        //Clear all old bulkhead  markers
                        for (j = 0; j < myBulkheadMarkers.Count; j++) {
                            myBulkheadMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myDoorMarkers.Count; j++) {
                            myDoorMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myWallMarkers.Count; j++) {
                            myWallMarkers[j].Confirmed = false;
                        }

                    }
                    else if (myMarkerID == myWall1MarkerID || myMarkerID == myWall2MarkerID || myMarkerID == myWall3MarkerID || myMarkerID == myWall4MarkerID) {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint(myMarkerID, -1);
                        myConfirmedMarker.SeenFromMarkerID = mySeenFromMarkerID;
                        myConfirmedMarker.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                        myConfirmedMarker.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                        myConfirmedMarker.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                        myConfirmedMarker.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                        myConfirmedMarker.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                        myConfirmedMarker.SetZNormal();
                        myConfirmedMarker.ModelViewMatrix = GetModelViewMatrixFromPoints(myConfirmedMarker.Origin, myConfirmedMarker.XAxisPoint, myConfirmedMarker.YAxisPoint);
                        myWallMarkers.Add(myConfirmedMarker.Copy());
                        myWallMarkers.Last().Confirmed = true;
                        myWallMarkers.Last().History.AddRange(myHistoricMarkers.ToArray());

                        ////If seen from the GF Marker, relevel
                        //if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        //    clsMarkerPoint m = myWallMarkers.Last();
                        //    RelevelMarkerFromGF(m);
                        //}

                        //If seen from Step Marker, reset the SeenFromMarkerID
                        if (mySeenFromMarkerID == myStepMultiMarkerID) {
                            j = myStepMultiMarkerID;
                            for (k = 0; k < ConfirmedMarkers.Count; k++) {
                                if (ConfirmedMarkers[k].ActualMarkerID == myStepMultiMarkerID) j = ConfirmedMarkers[k].MarkerID;
                            }
                            myWallMarkers.Last().SeenFromMarkerID = j;
                        }

                        //Clear all old bulkhead  markers
                        for (j = 0; j < myBulkheadMarkers.Count; j++) {
                            myBulkheadMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myDoorMarkers.Count; j++) {
                            myDoorMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myObstructMarkers.Count; j++) {
                            myObstructMarkers[j].Confirmed = false;
                        }

                    }
                    else {
                        myMarkerConfirmed = true; //So we can auto-save
                        myConfirmedMarker = new clsMarkerPoint(myMarkerID, -1);
                        myConfirmedMarker.SeenFromMarkerID = mySeenFromMarkerID;
                        //double dMax = myDists.Max();
                        //double dMin = myDists.Min();
                        //double dRange = dMax - dMin;
                        //double d;
                        //double dTot = 0;
                        //for (j = 0; j < pts1.Count; j++) {
                        //    if (dRange > myTol) {
                        //        d = 1 + (dMax - myDists[j]) / dRange;
                        //    } else { d = 1; }
                        //    dTot = dTot + d;
                        //    myConfirmedMarker.Origin = myConfirmedMarker.Origin + d * pts1[j];
                        //    myConfirmedMarker.XAxisPoint = myConfirmedMarker.XAxisPoint + d * pts2[j];
                        //    myConfirmedMarker.YAxisPoint = myConfirmedMarker.YAxisPoint + d * pts3[j];
                        //    myConfirmedMarker.ZAxisPoint = myConfirmedMarker.ZAxisPoint + d * pts4[j];
                        //    myConfirmedMarker.Point = myConfirmedMarker.Point + d * pts5[j];
                        //}
                        //myConfirmedMarker.Origin = myConfirmedMarker.Origin / dTot;
                        //myConfirmedMarker.XAxisPoint = myConfirmedMarker.XAxisPoint / dTot;
                        //myConfirmedMarker.YAxisPoint = myConfirmedMarker.YAxisPoint / dTot;
                        //myConfirmedMarker.ZAxisPoint = myConfirmedMarker.ZAxisPoint / dTot;
                        //myConfirmedMarker.Point = myConfirmedMarker.Point / dTot;
                        myConfirmedMarker.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                        myConfirmedMarker.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                        myConfirmedMarker.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                        myConfirmedMarker.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                        myConfirmedMarker.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                        myConfirmedMarker.SetZNormal();
                        myConfirmedMarker.ModelViewMatrix = GetModelViewMatrixFromPoints(myConfirmedMarker.Origin, myConfirmedMarker.XAxisPoint, myConfirmedMarker.YAxisPoint);
                        myConfirmedMarker.History.AddRange(myHistoricMarkers.ToArray());
                        ConfirmedMarkers.Add(myConfirmedMarker.Copy());
                        ConfirmedMarkers.Last().ActualMarkerID = myMarkerID;

                        ////If seen from the GF Marker, relevel
                        //if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        //    clsMarkerPoint m = ConfirmedMarkers.Last();
                        //    RelevelMarkerFromGF(m);
                        //}

                        //If seen from Step Marker, reset the SeenFromMarkerID
                        if (mySeenFromMarkerID == myStepMultiMarkerID) {
                            j = myStepMultiMarkerID;
                            for (k = 0; k < ConfirmedMarkers.Count - 1; k++) {
                                if (ConfirmedMarkers[k].ActualMarkerID == myStepMultiMarkerID) j = ConfirmedMarkers[k].MarkerID;
                            }
                            ConfirmedMarkers.Last().SeenFromMarkerID = j;
                        }

                        //Clear all old bulkhead and door markers
                        for (j = 0; j < myBulkheadMarkers.Count; j++) {
                            myBulkheadMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myDoorMarkers.Count; j++) {
                            myDoorMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myObstructMarkers.Count; j++) {
                            myObstructMarkers[j].Confirmed = false;
                        }
                        for (j = 0; j < myWallMarkers.Count; j++) {
                            myWallMarkers[j].Confirmed = false;
                        }

                    }
                    j = 0;
                    while (j <= mySuspectedMarkers.Count - 1) {
                        if (mySuspectedMarkers[j].MarkerID == myMarkerID) {
                            mySuspectedMarkers.RemoveAt(j);
                        }
                        else {
                            j = j + 1;
                        }
                    }

                    mySaveSnapshot = true;
                    //SaveSnapshot(); //Not doing this any more
                    continue;
                }

                i = i + 1;
            }
            if (myMarkerConfirmed) {
                AutoSaveSurveyData();
            }
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
            sw.WriteLine("GFMarkerID," + myGFMultiMarkerID);
            sw.WriteLine("StepMarkerID," + myStepMultiMarkerID);
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
            sw.WriteLine("GFMarkerID," + myGFMultiMarkerID);
            sw.WriteLine("StepMarkerID," + myStepMultiMarkerID);
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
            ConfirmedMarkers.Sort(new MarkerPointComparer());

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
                            myGFMultiMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "StepMarkerID")
                            myStepMultiMarkerID = Convert.ToInt32(mySplit[1]);
                    }
                    myLine = sr.ReadLine();
                }
                myLine = sr.ReadLine();
            }

            int n = Convert.ToInt32(myLine);
            clsMarkerPoint myMarkerPoint;
            for (i = 1; i <= n; i++) {
                myMarkerPoint = new clsMarkerPoint();
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

        private static void SetSuspectedMarkerAxes(ref clsMarkerPoint pt)
        {
            for (int i = 0; i < ConfirmedMarkersCopy.Count; i++) {
                if (ConfirmedMarkersCopy[i].MarkerID == pt.MarkerID) {
                    pt.Confirmed = true;
                    pt.Origin = ConfirmedMarkersCopy[i].Origin.Copy();
                    pt.XAxisPoint = ConfirmedMarkersCopy[i].XAxisPoint.Copy();
                    pt.YAxisPoint = ConfirmedMarkersCopy[i].YAxisPoint.Copy();
                    pt.ZAxisPoint = ConfirmedMarkersCopy[i].ZAxisPoint.Copy();
                    pt.Point = ConfirmedMarkersCopy[i].Point.Copy();
                    pt.SetZNormal();
                }
            }
            return;
        }


        private static void SetSuspectedMarkerAxes2(ref clsMarkerPoint pt)
        {
            pt.Confirmed = false;
            int myMarkerID = pt.MarkerID;
            List<clsPoint3d> pts1 = new List<clsPoint3d>();
            List<clsPoint3d> pts2 = new List<clsPoint3d>();
            List<clsPoint3d> pts3 = new List<clsPoint3d>();
            List<clsPoint3d> pts4 = new List<clsPoint3d>();
            List<clsPoint3d> pts5 = new List<clsPoint3d>();

            for (int j = 0; j <= mySuspectedMarkers.Count - 1; j++) {
                if (mySuspectedMarkers[j].MarkerID == myMarkerID && mySuspectedMarkers[j].Origin.Length > myTol) {
                    if (mySuspectedMarkers[j].SeenFromMarkerID == myGFMultiMarkerID) {
                        pts1.Add(mySuspectedMarkers[j].Origin.Copy());
                        pts2.Add(mySuspectedMarkers[j].XAxisPoint.Copy());
                        pts3.Add(mySuspectedMarkers[j].YAxisPoint.Copy());
                        pts4.Add(mySuspectedMarkers[j].ZAxisPoint.Copy());
                        pts5.Add(mySuspectedMarkers[j].Point.Copy());
                    }
                    else if (mySuspectedMarkers[j].SeenFromMarkerID == myStepMultiMarkerID) {
                        pts1.Add(StepMarker.Origin + mySuspectedMarkers[j].Origin.X * StepMarker.VX + mySuspectedMarkers[j].Origin.Y * StepMarker.VY + mySuspectedMarkers[j].Origin.Z * StepMarker.VZ);
                        pts2.Add(StepMarker.Origin + mySuspectedMarkers[j].XAxisPoint.X * StepMarker.VX + mySuspectedMarkers[j].XAxisPoint.Y * StepMarker.VY + mySuspectedMarkers[j].XAxisPoint.Z * StepMarker.VZ);
                        pts3.Add(StepMarker.Origin + mySuspectedMarkers[j].YAxisPoint.X * StepMarker.VX + mySuspectedMarkers[j].YAxisPoint.Y * StepMarker.VY + mySuspectedMarkers[j].YAxisPoint.Z * StepMarker.VZ);
                        pts4.Add(StepMarker.Origin + mySuspectedMarkers[j].ZAxisPoint.X * StepMarker.VX + mySuspectedMarkers[j].ZAxisPoint.Y * StepMarker.VY + mySuspectedMarkers[j].ZAxisPoint.Z * StepMarker.VZ);
                        pts5.Add(StepMarker.Origin + mySuspectedMarkers[j].Point.X * StepMarker.VX + mySuspectedMarkers[j].Point.Y * StepMarker.VY + mySuspectedMarkers[j].Point.Z * StepMarker.VZ);
                    }
                    else {
                        for (int k = 0; k <= ConfirmedMarkers.Count - 1; k++) {
                            if (ConfirmedMarkers[k].MarkerID == mySuspectedMarkers[j].SeenFromMarkerID) {
                                clsMarkerPoint pt2 = ConfirmedMarkers[k];
                                pts1.Add(pt2.Origin + mySuspectedMarkers[j].Origin.X * pt2.VX + mySuspectedMarkers[j].Origin.Y * pt2.VY + mySuspectedMarkers[j].Origin.Z * pt2.VZ);
                                pts2.Add(pt2.Origin + mySuspectedMarkers[j].XAxisPoint.X * pt2.VX + mySuspectedMarkers[j].XAxisPoint.Y * pt2.VY + mySuspectedMarkers[j].XAxisPoint.Z * pt2.VZ);
                                pts3.Add(pt2.Origin + mySuspectedMarkers[j].YAxisPoint.X * pt2.VX + mySuspectedMarkers[j].YAxisPoint.Y * pt2.VY + mySuspectedMarkers[j].YAxisPoint.Z * pt2.VZ);
                                pts4.Add(pt2.Origin + mySuspectedMarkers[j].ZAxisPoint.X * pt2.VX + mySuspectedMarkers[j].ZAxisPoint.Y * pt2.VY + mySuspectedMarkers[j].ZAxisPoint.Z * pt2.VZ);
                                pts5.Add(pt2.Origin + mySuspectedMarkers[j].Point.X * pt2.VX + mySuspectedMarkers[j].Point.Y * pt2.VY + mySuspectedMarkers[j].Point.Z * pt2.VZ);
                            }
                        }
                    }
                }
            }

            if (pts1.Count > 0) {
                pt.Confirmed = true;
                pt.Origin = new clsPoint3d(pts1.Average(p => p.X), pts1.Average(p => p.Y), pts1.Average(p => p.Z));
                pt.XAxisPoint = new clsPoint3d(pts2.Average(p => p.X), pts2.Average(p => p.Y), pts2.Average(p => p.Z));
                pt.YAxisPoint = new clsPoint3d(pts3.Average(p => p.X), pts3.Average(p => p.Y), pts3.Average(p => p.Z));
                pt.ZAxisPoint = new clsPoint3d(pts4.Average(p => p.X), pts4.Average(p => p.Y), pts4.Average(p => p.Z));
                pt.Point = new clsPoint3d(pts5.Average(p => p.X), pts5.Average(p => p.Y), pts5.Average(p => p.Z));
                pt.SetZNormal();
            }
        }

        private static clsPoint3d PointFromInvMatrix(int n)
        {
            int i;
            float[] mv = new float[16];
            OpenTK.Matrix4 myModel, modelViewInv;

            for (i = 0; i <= 15; i++) {
                mv[i] = GetModelViewMatrix()[n][i];
            }
            myModel = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            modelViewInv = OpenTK.Matrix4.Invert(myModel);
            return new clsPoint3d(modelViewInv.M41, modelViewInv.M42, modelViewInv.M43);
        }

        public static void RelevelMarkerFromGF(clsMarkerPoint myMarker, bool goBack = false)
        {
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

                for (j = 0; j <= myMarker.Points1.Count - 1; j++) {
                    myMarker.Points1[j].RotateAboutLine(p3.Line(), b);
                }
                for (j = 0; j <= myMarker.Points2.Count - 1; j++) {
                    myMarker.Points2[j].RotateAboutLine(p3.Line(), b);
                }
                for (j = 0; j <= myMarker.Points3.Count - 1; j++) {
                    myMarker.Points3[j].RotateAboutLine(p3.Line(), b);
                }
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


        private static bool CheckSuspectedMarker(int myMarkerID, int myMarkerIndex, bool onlySuspectedMarkers = false)
        {
            int i, i1, k;
            clsPoint3d pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint;
            float[] mv = new float[16];
            bool mySuspectedMarkerAdded = false;
            List<int> SeenFromMarkersAdded = new List<int>();

            for (i = 0; i <= 15; i++) {
                mv[i] = GetModelViewMatrix()[myMarkerIndex][i];
            }
            OpenTK.Matrix4 myModel = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            OpenTK.Matrix4 modelviewInv = OpenTK.Matrix4.Invert(myModel);
            myCameraPoint = new clsPoint3d(modelviewInv.M41, modelviewInv.M42, modelviewInv.M43);

            if (onlySuspectedMarkers) goto onlySuspectedMarkersStart;

            //Get the coordinates from as many of the GF marker, the step marker and the other confirmed markers as are visible
            //Start with the Confirmed markers
            for (k = 0; k <= GetConfirmedMarkersSeenID().Count - 1; k++) {
                i1 = GetMarkersSeenID().IndexOf(GetConfirmedMarkersSeenID()[k]);

                pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 0);
                pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 1);
                pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, i1);
                pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, i1);
                pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, i1);

                RelevelMarkerFromGF(ref pt1a, ref pt1b, ref pt1, ref pt2, ref pt3);

                pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 0);
                pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 1);

                if (AddSuspectedMarker(myMarkerID, GetConfirmedMarkersSeenID()[k], pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
            }

            //Now from the GF marker
            if (GetGFVisible()) {
                pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, GetGFMultiMarkerIndex(), 0);
                pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, GetGFMultiMarkerIndex(), 1);
                pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, GetGFMultiMarkerIndex());
                pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, GetGFMultiMarkerIndex());
                pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, GetGFMultiMarkerIndex());
                pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 0);
                pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 1);

                if (AddSuspectedMarker(myMarkerID, myGFMultiMarkerID, pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
            }

            //Now from the Step marker
            if (GetStepVisible() & StepMarker.Confirmed) {
                pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, GetStepMultiMarkerIndex(), 0);
                pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, GetStepMultiMarkerIndex(), 1);
                pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, GetStepMultiMarkerIndex());
                pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, GetStepMultiMarkerIndex());
                pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, GetStepMultiMarkerIndex());
                pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 0);
                pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 1);

                if (AddSuspectedMarker(myMarkerID, myStepMultiMarkerID, pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
            }

            onlySuspectedMarkersStart:

            //New: now from other suspected markers
            if (!(myMarkerID == myDoorHingeRightMarkerID || myMarkerID == myDoorFrameRightMarkerID || myMarkerID == myDoorHingeLeftMarkerID || myMarkerID == myDoorFrameLeftMarkerID || myMarkerID == myObstruct1MarkerID || myMarkerID == myObstruct2MarkerID || myMarkerID == myObstruct3MarkerID || myMarkerID == myObstruct4MarkerID)) {
                for (k = 0; k <= GetSuspectedMarkersSeenID().Count - 1; k++) {
                    if (SeenFromMarkersAdded.Contains(GetSuspectedMarkersSeenID()[k])) continue;
                    SeenFromMarkersAdded.Add(GetSuspectedMarkersSeenID()[k]);
                    if (GetSuspectedMarkersSeenID()[k] == myMarkerID) continue;
                    i1 = GetMarkersSeenID().IndexOf(GetSuspectedMarkersSeenID()[k]);
                    if (i1 == myMarkerIndex) continue;

                    pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 0);
                    pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 1);
                    pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, i1);
                    pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, i1);
                    pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, i1);
                    pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 0);
                    pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 1);

                    if (AddSuspectedMarker(myMarkerID, GetSuspectedMarkersSeenID()[k], pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint, onlySuspectedMarkers)) mySuspectedMarkerAdded = true;
                }
            }

            if (onlySuspectedMarkers) return mySuspectedMarkerAdded;

            //For Door markers, allow them to be seen from other Door markers
            if (myMarkerID == myDoorHingeRightMarkerID || myMarkerID == myDoorFrameRightMarkerID || myMarkerID == myDoorHingeLeftMarkerID || myMarkerID == myDoorFrameLeftMarkerID) {
                for (k = 0; k <= GetDoorMarkersSeenID().Count - 1; k++) {
                    for (int k1 = 0; k1 < myDoorMarkers.Count; k1++) {
                        if (myDoorMarkers[k1].Confirmed && myDoorMarkers[k1].MarkerID == GetDoorMarkersSeenID()[k]) {
                            i1 = GetMarkersSeenID().IndexOf(GetDoorMarkersSeenID()[k]);

                            pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 0);
                            pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 1);
                            pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, i1);
                            pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, i1);
                            pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, i1);
                            pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 0);
                            pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 1);

                            if (AddSuspectedMarker(myMarkerID, GetDoorMarkersSeenID()[k], pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
                            break;
                        }
                    }
                }
            }

            //For Obstruction markers, allow them to be seen from other Obstruction markers
            if (myMarkerID == myObstruct1MarkerID || myMarkerID == myObstruct2MarkerID || myMarkerID == myObstruct3MarkerID || myMarkerID == myObstruct4MarkerID) {
                for (k = 0; k <= GetObstructMarkersSeenID().Count - 1; k++) {
                    for (int k1 = 0; k1 < myObstructMarkers.Count; k1++) {
                        if (myObstructMarkers[k1].Confirmed && myObstructMarkers[k1].MarkerID == GetObstructMarkersSeenID()[k]) {
                            i1 = GetMarkersSeenID().IndexOf(GetObstructMarkersSeenID()[k]);

                            pt1a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 0);
                            pt1b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, i1, 1);
                            pt1 = UnProjectProject(new clsPoint3d(0, 0, 0), myMarkerIndex, i1);
                            pt2 = UnProjectProject(new clsPoint3d(100, 0, 0), myMarkerIndex, i1);
                            pt3 = UnProjectProject(new clsPoint3d(0, 100, 0), myMarkerIndex, i1);
                            pt6a = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 0);
                            pt6b = UnProjectProjectZ(new clsPoint3d(0, 0, 0), myMarkerIndex, myMarkerIndex, 1);

                            if (AddSuspectedMarker(myMarkerID, GetObstructMarkersSeenID()[k], pt1a, pt1b, pt1, pt2, pt3, pt6a, pt6b, myCameraPoint)) mySuspectedMarkerAdded = true;
                            break;
                        }
                    }
                }
            }
            return mySuspectedMarkerAdded;
        }


        private static bool AddSuspectedMarker(int myMarkerID, int mySeenFromMarkerID, clsPoint3d pt1a, clsPoint3d pt1b, clsPoint3d pt1, clsPoint3d pt2, clsPoint3d pt3, clsPoint3d pt6a, clsPoint3d pt6b, clsPoint3d myCamerPoint, bool onlySuspectedMarkers = false)
        {
            int k;
            clsPoint3d v1 = null, v2 = null, v3 = null, v4 = null;
            TimeSpan t;
            double a, a1 = 0, a2 = 0;
            System.DateTime t1;
            System.DateTime t2;
            bool myHasTime;
            string myStr;
            string myMarkerStr;
            double myAngleXY = 0;
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

            v1 = pt6b - pt6a;
            v1.Normalise();

            t1 = DateTime.Now;
            t2 = t1;

            k = -1;
            for (i = 0; i <= mySuspectedMarkers.Count - 1; i++) {
                if (mySuspectedMarkers[i].MarkerID == myMarkerID & mySuspectedMarkers[i].SeenFromMarkerID == mySeenFromMarkerID) {
                    k = i;
                    break; // TODO: might not be correct. Was : Exit For
                }
            }

            if (onlySuspectedMarkers && k > -1) return false;

            if (k == -1) {
                mySuspectedMarkers.Add(new clsMarkerPoint(myMarkerID, mySeenFromMarkerID));
                k = mySuspectedMarkers.Count - 1;
                mySuspectedMarkers[k].SeenFromMarkerID = mySeenFromMarkerID;
                mySuspectedMarkers[k].LastTime = t1;
                mySuspectedMarkers[k].LastVector = v1;
                mySuspectedMarkers[k].FirstPointRemoved = false;
            } else {
                myHasTime = (mySuspectedMarkers[k].LastTime != null);
                myHasTime = false;
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

                    if (mySeenFromMarkerID == myGFMultiMarkerID) {
                        myMarkerStr = "GF";
                    } else if (mySeenFromMarkerID == myStepMultiMarkerID) {
                        myMarkerStr = "ST";
                    } else {
                        myMarkerStr = Convert.ToString(myMarkerIDs.IndexOf(mySeenFromMarkerID) + 1);
                    }

                    mySuspectedMarkers[k].OKToConfirm(ref myErrorString, ref v1, ref v2, ref v3, ref v4, ref b1, ref b2, ref a1, ref a2);
                    myStr = myMarkerIDs.IndexOf(myMarkerID) + 1 + " / " + myMarkerStr + " " + mySuspectedMarkers[k].Points1a.Count + " - MaxA=" + Round(mySuspectedMarkers[k].MaxAngle(ref v1, ref v2) * 180 / PI, 1) + " - MaxA2=" + Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1);
                    if (myStr != mySuspectedMarkers[k].Label) {
                        mySuspectedMarkers[k].Label = myStr;
                        mySuspectedMarkers[k].MarkerName = Convert.ToString(myMarkerIDs.IndexOf(myMarkerID) + 1);
                        mySuspectedMarkers[k].SeenFromMarkerName = myMarkerStr;
                        mySuspectedMarkers[k].NumPoints = Convert.ToString(mySuspectedMarkers[k].Points1a.Count);
                        mySuspectedMarkers[k].MaximumAngleA = Convert.ToString(Round(mySuspectedMarkers[k].MaxAngle(ref v1, ref v2) * 180 / PI, 1));
                        mySuspectedMarkers[k].MaximumAngleXY = Convert.ToString(Round(mySuspectedMarkers[k].MaxAnglePerpendicular(ref v1, ref v2) * 180 / PI, 1));
                    }
                }
            }

            mySuspectedMarkers[k].Points1a.Add(pt1a);
            mySuspectedMarkers[k].Points1b.Add(pt1b);
            mySuspectedMarkers[k].Points1.Add(pt1);
            mySuspectedMarkers[k].Points2.Add(pt2);
            mySuspectedMarkers[k].Points3.Add(pt3);
            mySuspectedMarkers[k].Points6a.Add(pt6a);
            mySuspectedMarkers[k].Points6b.Add(pt6b);
            mySuspectedMarkers[k].CameraPoints.Add(myCamerPoint);
            
            if (mySuspectedMarkers[k].FirstPointRemoved == false & mySuspectedMarkers[k].Points1a.Count == 2) {
                mySuspectedMarkers[k].Points1a.RemoveAt(0);
                mySuspectedMarkers[k].Points1b.RemoveAt(0);
                mySuspectedMarkers[k].Points1.RemoveAt(0);
                mySuspectedMarkers[k].Points2.RemoveAt(0);
                mySuspectedMarkers[k].Points3.RemoveAt(0);
                mySuspectedMarkers[k].Points6a.RemoveAt(0);
                mySuspectedMarkers[k].Points6b.RemoveAt(0);

                if (mySuspectedMarkers[k].GyroData.Count>0) mySuspectedMarkers[k].GyroData.RemoveAt(0);
                if (mySuspectedMarkers[k].LastGyroData.Count > 0) mySuspectedMarkers[k].LastGyroData.RemoveAt(0);
                if (mySuspectedMarkers[k].AccelData.Count > 0) mySuspectedMarkers[k].AccelData.RemoveAt(0);
                if (mySuspectedMarkers[k].LastAccelData.Count > 0) mySuspectedMarkers[k].LastAccelData.RemoveAt(0);

                mySuspectedMarkers[k].FirstPointRemoved = true;
            }

            mySuspectedMarkers[k].SetEndPointBasedOnZVectors();

            if (mySuspectedMarkers[k].MarkerID == myStepMultiMarkerID) {
                if (DebugStringList.Count == 0 || !(DebugStringList[DebugStringList.Count - 1].StartsWith("Step Marker Measured"))) {
                    DebugStringList.Add("Step Marker Measured (" + mySuspectedMarkers[k].Points1a.Count + " Times)");
                } else {
                    DebugStringList[DebugStringList.Count - 1] = "Step Marker Measured (" + mySuspectedMarkers[k].Points1a.Count + " Times)";
                }
            } else {
                if (DebugStringList.Count == 0 || !(DebugStringList[DebugStringList.Count - 1].StartsWith("Marker " + (mySuspectedMarkers[k].MarkerID + 1) + " Measured"))) {
                    DebugStringList.Add("Marker " + (mySuspectedMarkers[k].MarkerID + 1) + " Measured (" + mySuspectedMarkers[k].Points1a.Count + " Times)");
                } else {
                    DebugStringList[DebugStringList.Count - 1] = "Marker " + (mySuspectedMarkers[k].MarkerID + 1) + " Measured (" + mySuspectedMarkers[k].Points1a.Count + " Times)";
                }
            }

            return true;
        }

        private static void RemoveSuspectedMarker(int mySeenFromMarkerID)
        {
            int i;

            i = 0;
            while (i <= mySuspectedMarkers.Count - 1) {
                if (mySuspectedMarkers[i].SeenFromMarkerID == mySeenFromMarkerID) {
                    mySuspectedMarkers.RemoveAt(i);
                } else {
                    i = i + 1;
                }
            }

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
                StepMarker = new clsMarkerPoint(myStepMultiMarkerID, myStepMultiMarkerID);
            }
        }
        
        public static clsPoint3d Project3d(clsPoint3d p1, int n)
        {
            int[] vp = new int[4];
            clsPoint3d p2;

            //GL1.GetInteger(All1.Viewport, vp);
            vp = GetViewport();

            OpenTK.Matrix4 myProj = new OpenTK.Matrix4(GetProjMatrix()[0], GetProjMatrix()[1], GetProjMatrix()[2], GetProjMatrix()[3], GetProjMatrix()[4], GetProjMatrix()[5], GetProjMatrix()[6], GetProjMatrix()[7], GetProjMatrix()[8], GetProjMatrix()[9], GetProjMatrix()[10], GetProjMatrix()[11], GetProjMatrix()[12], GetProjMatrix()[13], GetProjMatrix()[14], GetProjMatrix()[15]);
            OpenTK.Matrix4 myModel = new OpenTK.Matrix4(GetModelViewMatrix()[n][0], GetModelViewMatrix()[n][1], GetModelViewMatrix()[n][2], GetModelViewMatrix()[n][3], GetModelViewMatrix()[n][4], GetModelViewMatrix()[n][5], GetModelViewMatrix()[n][6], GetModelViewMatrix()[n][7], GetModelViewMatrix()[n][8], GetModelViewMatrix()[n][9], GetModelViewMatrix()[n][10], GetModelViewMatrix()[n][11], GetModelViewMatrix()[n][12], GetModelViewMatrix()[n][13], GetModelViewMatrix()[n][14], GetModelViewMatrix()[n][15]);

            p2 = gluProject(myProj, myModel, vp, p1);

            return p2;
        }

        public static clsPoint3d UnProject3d(clsPoint3d p1, int n)
        {
            int[] vp = new int[4];
            clsPoint3d p2;

            vp = GetViewport();

            OpenTK.Matrix4 myProj = new OpenTK.Matrix4(GetProjMatrix()[0], GetProjMatrix()[1], GetProjMatrix()[2], GetProjMatrix()[3], GetProjMatrix()[4], GetProjMatrix()[5], GetProjMatrix()[6], GetProjMatrix()[7], GetProjMatrix()[8], GetProjMatrix()[9], GetProjMatrix()[10], GetProjMatrix()[11], GetProjMatrix()[12], GetProjMatrix()[13], GetProjMatrix()[14], GetProjMatrix()[15]);
            OpenTK.Matrix4 myModel = new OpenTK.Matrix4(GetModelViewMatrix()[n][0], GetModelViewMatrix()[n][1], GetModelViewMatrix()[n][2], GetModelViewMatrix()[n][3], GetModelViewMatrix()[n][4], GetModelViewMatrix()[n][5], GetModelViewMatrix()[n][6], GetModelViewMatrix()[n][7], GetModelViewMatrix()[n][8], GetModelViewMatrix()[n][9], GetModelViewMatrix()[n][10], GetModelViewMatrix()[n][11], GetModelViewMatrix()[n][12], GetModelViewMatrix()[n][13], GetModelViewMatrix()[n][14], GetModelViewMatrix()[n][15]);

            p2 = gluUnProject(myProj, myModel, vp, p1);

            return p2;
        }

        public static clsPoint3d UnProjectProjectZ(clsPoint3d p1, int n1, int n2, double winZ)
        {
            int i;
            float[] mv = new float[16];
            float[] pv = new float[16];
            int[] vp = new int[4];
            clsPoint3d p2, p3;

            for (i = 0; i <= 15; i++) {
                mv[i] = GetModelViewMatrix()[n1][i];
                pv[i] = GetProjMatrix()[i];
            }
            vp = GetViewport();

            OpenTK.Matrix4 myProj = new OpenTK.Matrix4(pv[0], pv[1], pv[2], pv[3], pv[4], pv[5], pv[6], pv[7], pv[8], pv[9], pv[10], pv[11], pv[12], pv[13], pv[14], pv[15]);
            OpenTK.Matrix4 myModel = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);

            p2 = gluProject(myProj, myModel, vp, p1);
            p2.Z = winZ;

            for (i = 0; i <= 15; i++) {
                mv[i] = GetModelViewMatrix()[n2][i];
            }

            myModel = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);

            p3 = gluUnProject(myProj, myModel, vp, p2);

            return p3;
        }

        public static clsPoint3d UnProjectProject(clsPoint3d p1, int n1, int n2)
        {
            int i;
            float[] mv = new float[16];

            for (i = 0; i <= 15; i++) {
                mv[i] = GetModelViewMatrix()[n1][i];
            }

            OpenTK.Matrix4 myModel = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            OpenTK.Vector4 vec;
            vec.X = (float)p1.X;
            vec.Y = (float)p1.Y;
            vec.Z = (float)p1.Z;
            vec.W = 1.0f;
            OpenTK.Vector4.Transform(ref vec, ref myModel, out vec);

            for (i = 0; i <= 15; i++) {
                mv[i] = GetModelViewMatrix()[n2][i];
            }

            myModel = new OpenTK.Matrix4(mv[0], mv[1], mv[2], mv[3], mv[4], mv[5], mv[6], mv[7], mv[8], mv[9], mv[10], mv[11], mv[12], mv[13], mv[14], mv[15]);
            OpenTK.Matrix4 modelviewInv = OpenTK.Matrix4.Invert(myModel);
            OpenTK.Vector4.Transform(ref vec, ref modelviewInv, out vec);

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

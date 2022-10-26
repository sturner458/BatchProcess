/*
 *  ARToolKitFunctions.cs
 *  ARToolKit for Unity
 *
 *  This file is part of ARToolKit for Unity.
 *
 *  ARToolKit for Unity is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ARToolKit for Unity is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with ARToolKit for Unity.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  As a special exception, the copyright holders of this library give you
 *  permission to link this library with independent modules to produce an
 *  executable, regardless of the license terms of these independent modules, and to
 *  copy and distribute the resulting executable under terms of your choice,
 *  provided that you also meet, for each linked independent module, the terms and
 *  conditions of the license of that module. An independent module is a module
 *  which is neither derived from nor based on this library. If you modify this
 *  library, you may extend this exception to your version of the library, but you
 *  are not obligated to do so. If you do not wish to do so, delete this exception
 *  statement from your version.
 *
 *  Copyright 2015 Daqri, LLC.
 *  Copyright 2010-2015 ARToolworks, Inc.
 *
 *  Author(s): Philip Lamb, Julian Looser
 *
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

/// <summary>
/// This class makes the functions of the ARWrapper accessible in C#. For function documentation please 
/// refer to the ARToolKitWrapperExportedAPI.h file located in %ARTOOLKIT5_ROOT%/include/ARWrapper.
/// For the implementation the start point is the coresponding ARToolKitWrapperExportedAPI.cpp file located in 
/// %ARTOOLKIT5_ROOT%/lib/SRC/ARWrapper
/// </summary>
public class ARToolKitFunctions
{
	[NonSerialized]
	private bool inited = false;

    // Delegate type declaration.
    public delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

    // Delegate instance.
    private LogCallback logCallback = null;
    private GCHandle logCallbackGCH;

    private ARToolKitFunctions() { }

    public static ARToolKitFunctions Instance { get { return Nested.instance; } }

    private class Nested
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Nested() {
        }

        internal static readonly ARToolKitFunctions instance = new ARToolKitFunctions();
    }

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

    /// <summary>
    /// Registers a callback function to use when a message is logged. 
    /// If the callback is to become invalid, be sure to call this function with NULL
    /// first so that the callback is unregistered.
    /// </summary>
    /// <param name="lcb">The LCB.</param>
    public void arwRegisterLogCallback(LogCallback lcb) {
        if (lcb != null) {
            logCallback = lcb;
            logCallbackGCH = GCHandle.Alloc(logCallback); // Does not need to be pinned, see http://stackoverflow.com/a/19866119/316487 
            ARNativePlugin.arwRegisterLogCallback(logCallback);
        } else {
            ARNativePlugin.arwRegisterLogCallback(lcb);
            if (logCallback != null) {
                logCallback = null;
                logCallbackGCH.Free();
            }
        }
    }

    public void arwSetLogLevel(int logLevel) {
        ARNativePlugin.arwSetLogLevel(logLevel);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SetDllDirectory(string lpPathName);

    /// <summary>
    /// Initialises the ARToolKit.
    /// </summary>
    /// <param name="pattSize"></param>
    /// <param name="pattCountMax"></param>
    /// <returns>true if successful, false if an error occurred.</returns>
    /// <remarks>For any square template (pattern) markers, the number of rows and columns in the template defaults to AR_PATT_SIZE1 and the maximum number of markers that may be loaded for a single matching pass defaults to AR_PATT_NUM_MAX.</remarks>
    /// <seealso cref="arwShutdownAR"/>
    public bool arwInitialiseAR() {
        string dllDir = "";
        dllDir = Environment.GetEnvironmentVariable("ARTOOLKIT5_ROOT64");
        if (string.IsNullOrEmpty(dllDir)) dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (dllDir.Equals("")) {
            throw new System.Exception("ARToolKitCSharpIntegration.ARToolKitFunctions.arwInitialiseAR(): ARTOOLKIT5_ROOT64 not set. Please set ARTOOLKIT5_ROOT64 in your environment variables to the path where you installed ARToolKit to.");
        } else {
            SetDllDirectory(dllDir + "\\");
        }

        bool ok;
        ok = ARNativePlugin.arwInitialiseAR();
        if (ok) inited = true;
        return ok;
    }

    /// <summary>
    /// Gets the ARToolKit version as a string, such as "4.5.1".
    /// Must not be called prior to arwInitialiseAR().
    /// </summary>
    /// <returns>true if successful, false if an error occurred.</returns>
    public string arwGetARToolKitVersion() {
        StringBuilder sb = new StringBuilder(128);
        bool ok;
        ok = ARNativePlugin.arwGetARToolKitVersion(sb, sb.Capacity);
        if (ok) return sb.ToString();
        else return "unknown";
    }

    /// <summary>
    /// Return error information.
    /// 
    /// Initially, all error flags are set to AR_ERROR_NONE.
    /// </summary>
    /// <returns>enum with error code.</returns>
    /// <remarks>
    /// Returns the value of the error flag.  Each detectable error
    /// is assigned a numeric code and symbolic name.  When  an  error  occurs,
    /// the  error  flag  is set to the appropriate error code value.  No other
    /// errors are recorded until arwGetError  is  called,  the  error  code  is
    /// returned,  and  the  flag  is  reset  to  AR_ERROR_NONE.   If  a  call to
    /// arwGetError returns AR_ERROR_NONE, there  has  been  no  detectable  error
    /// since the last call to arwGetError, or since the the library was initialized.
    /// 
    /// To  allow  for  distributed implementations, there may be several error
    /// flags.  If any single error flag has recorded an error,  the  value  of
    /// that  flag  is  returned  and  that  flag  is reset to AR_ERROR_NONE when
    /// arwGetError is called.  If more than one flag  has  recorded  an  error,
    /// arwGetError  returns  and  clears  an arbitrary error flag value.  Thus,
    /// arwGetError should  always  be  called  in  a  loop,  until  it  returns
    /// AR_ERROR_NONE, if all error flags are to be reset.
    /// </remarks>
    public int arwGetError() {
        return ARNativePlugin.arwGetError();
    }

    /// <summary>
    /// Shuts down the ARToolKit and frees all resources.
    /// N.B.: If this is being called from the destructor of the same module which
    /// supplied the log callback, be sure to set the logCallback = NULL
    /// prior to calling this function.
    /// </summary>
    /// <returns>true if successful, false if an error occurred.</returns>
    /// <seealso cref="arwInitialiseAR"/>
    public bool arwShutdownAR() {
        bool ok = false;
        ok = ARNativePlugin.arwShutdownAR();
        if (ok) inited = false;
        return ok;
    }

    /// <summary>
    /// Initialises and starts video capture.
    /// </summary>
    /// <param name="vconf">The video configuration string.</param>
    /// <param name="cparaName">The camera parameter file, which is used to form the projection matrix.</param>
    /// <param name="nearPlane">The distance to the near plane of the viewing frustum formed by the camera parameters.</param>
    /// <param name="farPlane">The distance to the far plane of the viewing frustum formed by the camera parameters.</param>
    /// <returns>true if successful, false if an error occurred.</returns>
    /// <seealso cref="arwStopRunning"/>
    public bool arwStartRunning(string vconf, string cparaName) {
        return ARNativePlugin.arwStartRunning(vconf, cparaName);
    }

    public void arwInitARToolKit(string vconf, string cparaName) {

        var result = File.Exists(cparaName);

        ARNativePlugin.arwInitARToolKit(vconf, cparaName);
    }

    //public void arwResetOpenGLContext() {
    //    ARNativePlugin.arwResetOpenGLContext();
    //}

    //public void arwSetBrightnessContrast(int inBright, int inContrast) {
    //    ARNativePlugin.arwSetBrightnessContrast(inBright, inContrast);
    //}

    /// <summary>
    /// Initialises and starts video capture.
    /// </summary>
    /// <param name="vconf">The video configuration string.</param>
    /// <param name="cparaBuff">A string containing the contents of a camera parameter file, which is used to form the projection matrix.</param>
    /// <param name="cparaBuffLen">Number of characters in cparaBuff.</param>
    /// <param name="nearPlane">The distance to the near plane of the viewing frustum formed by the camera parameters.</param>
    /// <param name="farPlane">The distance to the far plane of the viewing frustum formed by the camera parameters.</param>
    /// <returns>true if successful, false if an error occurred.</returns>
    public bool arwStartRunningB(string vconf, byte[] cparaBuff, int cparaBuffLen, float nearPlane, float farPlane) {
        //return ARNativePlugin.arwStartRunningB(vconf, cparaBuff, cparaBuffLen, nearPlane, farPlane);
        return false;
    }

    public bool arwStartRunningStereoB(string vconfL, byte[] cparaBuffL, int cparaBuffLenL, string vconfR, byte[] cparaBuffR, int cparaBuffLenR, byte[] transL2RBuff, int transL2RBuffLen, float nearPlane, float farPlane) {
        //return ARNativePlugin.arwStartRunningStereoB(vconfL, cparaBuffL, cparaBuffLenL, vconfR, cparaBuffR, cparaBuffLenR, transL2RBuff, transL2RBuffLen, nearPlane, farPlane);
        return false;
    }

    /// <summary>
    /// Returns true if ARToolKit is running, i.e. detecting markers.
    /// </summary>
    /// <returns>true when running, otherwise false.</returns>
    public bool arwIsRunning() {
        return ARNativePlugin.arwIsRunning();
    }

    /// <summary>
    /// Stops video capture and frees video capture resources.
    /// </summary>
    /// <returns>true if successful, false if an error occurred.</returns>
    /// <seealso cref="arwStartRunning"/>
    public bool arwStopRunning() {
        return ARNativePlugin.arwStopRunning();
    }

    /// <summary>
    /// Populates the given float array with the projection matrix computed from camera parameters for the video source.
    /// </summary>
    /// <param name="matrix">Float array to populate with OpenGL compatible projection matrix.</param>
    /// <returns>true if successful, false if an error occurred.</returns>
    public bool arwGetProjectionMatrix(float nearPlane, float farPlane, double[] matrix) {
        return ARNativePlugin.arwGetProjectionMatrix(nearPlane, farPlane, matrix);
    }

    /// <summary>
    /// Populates the given float arrays with the projection matrices computed from camera parameters for each of the stereo video sources.
    /// </summary>
    /// <param name="matrixL">Float array to populate with OpenGL compatible projection matrix for the left camera of the stereo video pair.</param>
    /// <param name="matrixR">Float array to populate with OpenGL compatible projection matrix for the right camera of the stereo video pair.</param>
    /// <returns>true if successful, false if an error occurred.</returns>
    public bool arwGetProjectionMatrixStereo(float[] matrixL, float[] matrixR) {
        //return ARNativePlugin.arwGetProjectionMatrixStereo(matrixL, matrixR);
        return false;
    }

    /// <summary>
    /// Returns the parameters of the video source frame.
    /// </summary>
    /// <param name="width">Pointer to an int which will be filled with the width (in pixels) of the video frame, or NULL if this information is not required.</param>
    /// <param name="height">Pointer to an int which will be filled with the height (in pixels) of the video frame, or NULL if this information is not required.</param>
    /// <param name="pixelSize">Pointer to an int which will be filled with the numbers of bytes per pixel of the source frame.</param>
    /// <param name="pixelFormatString">Pointer to a buffer which will be filled with the symolic name of the pixel format (as a nul-terminated C-string) of the video frame, or NULL if this information is not required. The name will be of the form "AR_PIXEL_FORMAT_xxx".</param>
    /// <returns>True if the values were returned OK, false if there is currently no video source or an error int[].</returns>
    /// <seealso cref="arwGetVideoParamsStereo"/>
    public bool arwGetVideoParams(out int width, out int height, out int pixelSize, out String pixelFormatString) {
        StringBuilder sb = new StringBuilder(128);
        bool ok = false;
        width = 0;
        height = 0;
        pixelSize = 0;
        pixelFormatString = "";
        ok = ARNativePlugin.arwGetVideoParams(out width, out height, out pixelSize, sb, sb.Capacity);
        if (!ok) pixelFormatString = "";
        else pixelFormatString = sb.ToString();
        return ok;
    }

    /// <summary>
    /// Returns the parameters of the video source frames.
    /// </summary>
    /// <param name="widthL">Pointer to an int which will be filled with the width (in pixels) of the video frame, or NULL if this information is not required.</param>
    /// <param name="heightL">Pointer to an int which will be filled with the height (in pixels) of the video frame, or NULL if this information is not required.</param>
    /// <param name="pixelSizeL">Pointer to an int which will be filled with the numbers of bytes per pixel of the source frame, or NULL if this information is not required.</param>
    /// <param name="pixelFormatL">Pointer to a buffer which will be filled with the symbolic name of the pixel format (as a nul-terminated C-string) of the video frame, or NULL if this information is not required. The name will be of the form "AR_PIXEL_FORMAT_xxx".</param>
    /// <param name="widthR">Pointer to an int which will be filled with the width (in pixels) of the video frame, or NULL if this information is not required.</param>
    /// <param name="heightR">Pointer to an int which will be filled with the height (in pixels) of the video frame, or NULL if this information is not required.</param>
    /// <param name="pixelSizeR">Pointer to an int which will be filled with the numbers of bytes per pixel of the source frame, or NULL if this information is not required.</param>
    /// <param name="pixelFormatR">Pointer to a buffer which will be filled with the symbolic name of the pixel format (as a nul-terminated C-string) of the video frame, or NULL if this information is not required. The name will be of the form "AR_PIXEL_FORMAT_xxx".</param>
    /// <returns></returns>
    public bool arwGetVideoParamsStereo(out int widthL, out int heightL, out int pixelSizeL, out String pixelFormatL, out int widthR, out int heightR, out int pixelSizeR, out String pixelFormatR) {
        StringBuilder sbL = new StringBuilder(128);
        StringBuilder sbR = new StringBuilder(128);
        bool ok = false;
        widthL = 0;
        heightL = 0;
        pixelSizeL = 0;
        pixelFormatL = "";
        widthR = 0;
        heightR = 0;
        pixelSizeR = 0;
        pixelFormatR = "";
        //ok = ARNativePlugin.arwGetVideoParamsStereo(out widthL, out heightL, out pixelSizeL, sbL, sbL.Capacity, out widthR, out heightR, out pixelSizeR, sbR, sbR.Capacity);
        if (!ok) {
            pixelFormatL = "";
            pixelFormatR = "";
        } else {
            pixelFormatL = sbL.ToString();
            pixelFormatR = sbR.ToString();
        }
        return ok;
    }

    /// <summary>
    /// Captures a newest frame from the video source.
    /// </summary>
    /// <returns>true if successful, false if an error occurred.</returns>
    public bool arwCapture() {
        return ARNativePlugin.arwCapture();
    }

    /// <summary>
    /// Performs detection and marker updates. The newest frame from the video source is retrieved and
    /// analysed. All loaded markers are updated.
    /// </summary>
    /// <returns>true if successful, false if an error occurred.</returns>
    public bool arwUpdateAR() {
        return ARNativePlugin.arwUpdateAR();
    }

    /// <summary>
    /// Populates the provided floating-point color buffer with the current video frame.
    /// </summary>
    /// <param name="colors">The color buffer to fill with video.</param>
    /// <returns>true if successful, false if an error occurred.</returns>
    public bool arwUpdateTexture([In, Out]Color[] colors) {
        bool ok = false;
        GCHandle handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
        IntPtr address = handle.AddrOfPinnedObject();
        //ok = ARNativePlugin.arwUpdateTexture(address);
        handle.Free();
        return ok;
    }

    public bool arwUpdateTextureStereo([In, Out]Color[] colorsL, [In, Out]Color[] colorsR) {
        bool ok = false;
        GCHandle handle0 = GCHandle.Alloc(colorsL, GCHandleType.Pinned);
        GCHandle handle1 = GCHandle.Alloc(colorsR, GCHandleType.Pinned);
        IntPtr address0 = handle0.AddrOfPinnedObject();
        IntPtr address1 = handle1.AddrOfPinnedObject();
        //ok = ARNativePlugin.arwUpdateTextureStereo(address0, address1);
        handle0.Free();
        handle1.Free();
        return ok;
    }

    public bool arwUpdateTexture32([In, Out]Color32[] colors32) {
        bool ok = false;
        //int[] myBytes = new int[colors32.Length];
        GCHandle handle = GCHandle.Alloc(colors32, GCHandleType.Pinned);
        IntPtr address = handle.AddrOfPinnedObject();
        ok = ARNativePlugin.arwUpdateTexture32(address);
        handle.Free();
        return ok;
    }

    public bool arwUpdateTexture32Stereo([In, Out]Color32[] colors32L, [In, Out]Color32[] colors32R) {
        bool ok = false;
        GCHandle handle0 = GCHandle.Alloc(colors32L, GCHandleType.Pinned);
        GCHandle handle1 = GCHandle.Alloc(colors32R, GCHandleType.Pinned);
        IntPtr address0 = handle0.AddrOfPinnedObject();
        IntPtr address1 = handle1.AddrOfPinnedObject();
        //ok = ARNativePlugin.arwUpdateTexture32Stereo(address0, address1);
        handle0.Free();
        handle1.Free();
        return ok;
    }

    public bool arwUpdateTextureGL(int textureID) {
        //return ARNativePlugin.arwUpdateTextureGL(textureID);
        return false;
    }

    public bool arwUpdateTextureGLStereo(int textureID_L, int textureID_R) {
        //return ARNativePlugin.arwUpdateTextureGLStereo(textureID_L, textureID_R);
        return false;
    }

    public void arwSetUnityRenderEventUpdateTextureGLTextureID(int textureID) {
        //ARNativePlugin.arwSetUnityRenderEventUpdateTextureGLTextureID(textureID);
    }

    public void arwSetUnityRenderEventUpdateTextureGLStereoTextureIDs(int textureID_L, int textureID_R) {
        //ARNativePlugin.arwSetUnityRenderEventUpdateTextureGLStereoTextureIDs(textureID_L, textureID_R);
    }

    public int arwGetMarkerPatternCount(int markerID) {
        //return ARNativePlugin.arwGetMarkerPatternCount(markerID);
        return 0;
    }

    public bool arwGetMarkerPatternConfig(int markerID, int patternID, float[] matrix, out float width, out float height, out int imageSizeX, out int imageSizeY) {
        //return ARNativePlugin.arwGetMarkerPatternConfig(markerID, patternID, matrix, out width, out height, out imageSizeX, out imageSizeY);
        width = 0;
        height = 0;
        imageSizeX = 0;
        imageSizeY = 0;
        return false;
    }

    public bool arwGetMarkerPatternImage(int markerID, int patternID, [In, Out]Color[] colors) {
        bool ok = false;
        //ok = ARNativePlugin.arwGetMarkerPatternImage(markerID, patternID, colors);
        return ok;
    }

    public bool arwGetTrackableOptionBool(int markerID, int option) {
        return ARNativePlugin.arwGetTrackableOptionBool(markerID, option);
    }

    public void arwSetTrackableOptionBool(int markerID, int option, bool value) {
        ARNativePlugin.arwSetTrackableOptionBool(markerID, option, value);
    }

    public int arwGetTrackableOptionInt(int markerID, int option) {
        return ARNativePlugin.arwGetTrackableOptionInt(markerID, option);
    }

    public void arwSetTrackableOptionInt(int markerID, int option, int value) {
        ARNativePlugin.arwSetTrackableOptionInt(markerID, option, value);
    }

    public float arwGetTrackableOptionFloat(int markerID, int option) {
        return ARNativePlugin.arwGetTrackableOptionFloat(markerID, option);
    }

    public void arwSetTrackableOptionFloat(int markerID, int option, float value) {
        ARNativePlugin.arwSetTrackableOptionFloat(markerID, option, value);
    }

    /// <summary>
    /// Enables or disables debug mode in the tracker. When enabled, a black and white debug
    /// image is generated during marker detection. The debug image is useful for visualising
    /// the binarization process and choosing a threshold value.
    /// </summary>
    /// <param name="debug">true to enable debug mode, false to disable debug mode.</param>
    /// <seealso cref="arwGetVideoDebugMode"/>
    public void arwSetVideoDebugMode(bool debug) {
        ARNativePlugin.arwSetTrackerOptionBool(ARW_TRACKER_OPTION_SQUARE_DEBUG_MODE, debug);
    }

    /// <summary>
    /// Returns whether debug mode is currently enabled.
    /// </summary>
    /// <returns>true when debug mode is enabled, false when debug mode is disabled.</returns>
    /// <seealso cref="arwSetVideoDebugMode"/>
    public bool arwGetVideoDebugMode() {
        return ARNativePlugin.arwGetTrackerOptionBool(ARW_TRACKER_OPTION_SQUARE_DEBUG_MODE);
    }

    /// <summary>
    /// Enables or disables corner refinement in the tracker.
    /// </summary>
    /// <param name="mode">true to enable corner refinement, false to disable corner refinement.</param>
    /// <seealso cref="arwGetCornerRefinementMode"/>
    public void arwSetCornerRefinementMode(bool mode) {
        ARNativePlugin.arwSetTrackerOptionBool(ARW_TRACKER_OPTION_2D_CORNER_REFINEMENT, mode);
    }

    /// <summary>
    /// Returns whether corner refinement is currently enabled.
    /// </summary>
    /// <returns>true when corner refinement is enabled, false when corner refinement is disabled.</returns>
    /// <seealso cref="arwSetCornerRefinementMode"/>
    public bool arwGetCornerRefinementMode() {
        return ARNativePlugin.arwGetTrackerOptionBool(ARW_TRACKER_OPTION_2D_CORNER_REFINEMENT);
    }

    public void arwSetVideoThreshold(int threshold) {
        ARNativePlugin.arwSetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_THRESHOLD, (int)(threshold * 255.0 / 100.0));
    }

    public int arwGetVideoThreshold() {
        return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_THRESHOLD);
    }

    public void arwSetVideoThresholdMode(int mode) {
        ARNativePlugin.arwSetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_THRESHOLD_MODE, mode);
    }

    public int arwGetVideoThresholdMode() {
        return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_THRESHOLD_MODE);
    }

    public void arwSetLabelingMode(int mode) {
        ARNativePlugin.arwSetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_LABELING_MODE, mode);
    }

    public int arwGetLabelingMode() {
        return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_LABELING_MODE);
    }

    /**
    @param size specify the width of the pattern border, as a percentage of the marker width. 
                If you have a marker with 10cm width and set border to 0.5 you state that you have a border of 5cm meaning 2.5cm on each side.
    **/
    public void arwSetBorderSize(float size) {
        ARNativePlugin.arwSetTrackerOptionFloat(ARW_TRACKER_OPTION_SQUARE_BORDER_SIZE, size);
    }

    public float arwGetBorderSize() {
        return ARNativePlugin.arwGetTrackerOptionFloat(ARW_TRACKER_OPTION_SQUARE_BORDER_SIZE);
    }

    public void arwSetPatternDetectionMode(int mode) {
        ARNativePlugin.arwSetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_PATTERN_DETECTION_MODE, mode);
    }

    public int arwGetPatternDetectionMode() {
        return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_PATTERN_DETECTION_MODE);
    }

    public void arwSetMarkerExtractionMode(int mode) {
        //ARNativePlugin.arwSetTrackerOptionInt(6, mode); //TODO
    }

    public int arwGetMarkerExtractionMode() {
        //return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_MATRIX_CODE_TYPE);
        return 0;
    }

    public void arwSetMatrixCodeType(int type) {
        ARNativePlugin.arwSetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_MATRIX_CODE_TYPE, type);
    }

    public int arwGetMatrixCodeType() {
        return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_MATRIX_CODE_TYPE);
    }

    public void arwSetImageProcMode(int mode) {
        ARNativePlugin.arwSetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_IMAGE_PROC_MODE, mode);
    }

    public int arwGetImageProcMode() {
        return ARNativePlugin.arwGetTrackerOptionInt(ARW_TRACKER_OPTION_SQUARE_IMAGE_PROC_MODE);
    }

    public void arwSetNFTMultiMode(bool on) {
        ARNativePlugin.arwSetTrackerOptionBool(ARW_TRACKER_OPTION_NFT_MULTIMODE, on);
    }

    public bool arwGetNFTMultiMode() {
        return ARNativePlugin.arwGetTrackerOptionBool(ARW_TRACKER_OPTION_NFT_MULTIMODE);
    }

    /// <summary>
    /// Takes the marker configuration string
    /// </summary>
    /// <param name="cfg">Sample configurations:
    /// single;data/hiro.patt;80
    /// single_buffer;80;buffer=234 221 237...
    /// single_barcode;0;80
    /// multi;data/multi/marker.dat
    /// nft;data/nft/pinball</param>
    /// <returns> marker id for further usage</returns>
    public int arwAddMarker(string cfg) {
        return ARNativePlugin.arwAddTrackable(cfg);
    }

    public bool arwRemoveMarker(int markerID) {
        return ARNativePlugin.arwRemoveTrackable(markerID);
    }

    public int arwRemoveAllMarkers() {
        return ARNativePlugin.arwRemoveAllTrackables();
    }


    //public bool arwQueryMarkerVisibility(int markerID) {
    //    return ARNativePlugin.arwQueryMarkerVisibility(markerID);
    //    return false;
    //}

    public bool arwQueryMarkerTransformation(int markerID, double[] matrix, double[] corners, out int numCorners) {
        return ARNativePlugin.arwQueryTrackableVisibilityAndTransformation(markerID, matrix, corners, out numCorners);
    }

    public bool arwQueryTrackableMapperTransformation(int gMapUID, int trackableUID, double[] matrix) {
        GCHandle handle1 = GCHandle.Alloc(matrix, GCHandleType.Pinned);
        IntPtr address1 = handle1.AddrOfPinnedObject();

        bool ret = ARNativePlugin.arwQueryTrackableMapperTransformation(gMapUID, trackableUID, address1);
        handle1.Free();
        return ret;
    }

    public int arwResetMapperTrackable(int gMapUID, string cfg) {
        return ARNativePlugin.arwResetMapperTrackable(gMapUID, cfg);
    }

    public void arwSetMappedMarkersVisible(int nMarkers, double[] markerTrans, int[] uids, double[] corners) {

        GCHandle handle1 = GCHandle.Alloc(markerTrans, GCHandleType.Pinned);
        IntPtr address1 = handle1.AddrOfPinnedObject();

        GCHandle handle2 = GCHandle.Alloc(uids, GCHandleType.Pinned);
        IntPtr address2 = handle2.AddrOfPinnedObject();

        GCHandle handle3 = GCHandle.Alloc(corners, GCHandleType.Pinned);
        IntPtr address3 = handle3.AddrOfPinnedObject();

        ARNativePlugin.arwSetMappedMarkersVisible(nMarkers, address1, address2, address3);
        handle1.Free();
        handle2.Free();
        handle3.Free();
    }

    public bool arwAddMappedMarkers(int gMapUID, int GFMarkerID, int nMarkers, double[] markerTrans, int[] uids, double[] corners) {

        GCHandle handle1 = GCHandle.Alloc(markerTrans, GCHandleType.Pinned);
        IntPtr address1 = handle1.AddrOfPinnedObject();

        GCHandle handle2 = GCHandle.Alloc(uids, GCHandleType.Pinned);
        IntPtr address2 = handle2.AddrOfPinnedObject();

        GCHandle handle3 = GCHandle.Alloc(corners, GCHandleType.Pinned);
        IntPtr address3 = handle3.AddrOfPinnedObject();

        var res = ARNativePlugin.arwAddMappedMarkers(gMapUID, GFMarkerID, nMarkers, address1, address2, address3);
        handle1.Free();
        handle2.Free();
        handle3.Free();
        return res;
    }

    public int arwUpdateMultiMarker(int gMapUID, int GFMarkerID, int nMarkers, double[] markerTrans, int[] uids, double[] corners, bool initialiseMultiMarker) {

        GCHandle handle1 = GCHandle.Alloc(markerTrans, GCHandleType.Pinned);
        IntPtr address1 = handle1.AddrOfPinnedObject();

        GCHandle handle2 = GCHandle.Alloc(uids, GCHandleType.Pinned);
        IntPtr address2 = handle2.AddrOfPinnedObject();

        GCHandle handle3 = GCHandle.Alloc(corners, GCHandleType.Pinned);
        IntPtr address3 = handle3.AddrOfPinnedObject();

        var res = ARNativePlugin.arwUpdateMultiMarker(gMapUID, GFMarkerID, nMarkers, address1, address2, address3, initialiseMultiMarker);
        handle1.Free();
        handle2.Free();
        handle3.Free();
        return res;
    }

    //ARX_EXTERN bool arwLastUpdateSuccessful(int gMapUID, int* numMarkers, int* numSuccessfulUpdates, float lastTrans[12]);
    //ARX_EXTERN void arwGetMappedMarkerTrans(int gMapUID, int nMarker, float trans[12]);
    //ARX_EXTERN void arwResetMapperTrackable(int gMapUID, const char* cfg);
    //ARX_EXTERN void arwAddMappedMarkers(int gMapUID, int nMarkers, float *thisTrans, float* markerTrans, int* uids);

    public void arwListTrackables(int gMapUID) {
        ARNativePlugin.arwListTrackables(gMapUID);
    }

    public bool arwGetTrackablePatternConfig(int trackableUID, int patternID, double[] matrix, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID) {
        return ARNativePlugin.arwGetTrackablePatternConfig(trackableUID, patternID, matrix, out width, out height, out imageSizeX, out imageSizeY, out barcodeID);
    }

    public int arwGetTrackablePatternCount(int trackableUID) {
        return ARNativePlugin.arwGetTrackablePatternCount(trackableUID);
    }

    public bool arwQueryMarkerTransformationStereo(int markerID, float[] matrixL, float[] matrixR) {
        //return ARNativePlugin.arwQueryMarkerTransformationStereo(markerID, matrixL, matrixR);
        return false;
    }

    public bool arwLoadOpticalParams(string optical_param_name, byte[] optical_param_buff, int optical_param_buffLen, out float fovy_p, out float aspect_p, float[] m, float[] p) {
        //return ARNativePlugin.arwLoadOpticalParams(optical_param_name, optical_param_buff, optical_param_buffLen, out fovy_p, out aspect_p, m, p);
        fovy_p = 0;
        aspect_p = 0;
        return false;
    }

    public bool arwInitChessboardCorners(int nHoriztonal, int nVertical, float spacing, int xSize, int ySize, int calibImageNum) {

        string dllDir = "";
        dllDir = Environment.GetEnvironmentVariable("ARTOOLKIT5_ROOT64");
        if (string.IsNullOrEmpty(dllDir))  dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (dllDir.Equals("")) {
            throw new System.Exception("ARToolKitCSharpIntegration.ARToolKitFunctions.arwInitialiseAR(): ARTOOLKIT5_ROOT64 not set. Please set ARTOOLKIT5_ROOT64 in your environment variables to the path where you installed ARToolKit to.");
        } else {
            SetDllDirectory(dllDir + "\\");
        }

        return ARNativePlugin.arwInitChessboardCorners(nHoriztonal, nVertical, spacing, calibImageNum, xSize, ySize);
    }

    public int arwFindChessboardCorners(float[] corners, out int corner_count, byte[] imageBytes) {
        int ok;

        GCHandle handle1 = GCHandle.Alloc(corners, GCHandleType.Pinned);
        IntPtr address1 = handle1.AddrOfPinnedObject();

        GCHandle handle2 = GCHandle.Alloc(imageBytes, GCHandleType.Pinned);
        IntPtr address2 = handle2.AddrOfPinnedObject();

        ok = ARNativePlugin.arwFindChessboardCorners(address1, out corner_count, address2);
        handle1.Free();
        handle2.Free();
        return ok;
    }

    public int arwCaptureChessboardCorners(int n = -1) {
        return ARNativePlugin.arwCaptureChessboardCorners(n);
    }

    /// <summary>
    /// Calibrates the camera based on the images passed using <see cref="arwCaptureChessboardCorners" />.
    /// </summary>
    /// <param name="numImages">The number images.</param>
    /// <param name="file_name">Name of the file.</param>
    /// <param name="xSize">Size of the x.</param>
    /// <param name="ySize">Size of the y.</param>
    /// <param name="projectErrorResults">The project error for each image.</param>
    /// <returns>
    /// The average projection error for all images calibrated.
    /// </returns>
    public float arwCalibChessboardCorners(int numImages, string file_name, out float[] projectErrorResults) {
        projectErrorResults = new float[numImages];
        GCHandle handle = GCHandle.Alloc(projectErrorResults, GCHandleType.Pinned);
        IntPtr address = handle.AddrOfPinnedObject();
        float averageProjectionError = ARNativePlugin.arwCalibChessboardCorners(file_name, address);
        handle.Free();
        return averageProjectionError;
    }

    // Uses 4 calibration factors
    public float arwCalibChessboardCornersSimple(int numImages, string file_name, out float[] projectErrorResults) {
        projectErrorResults = new float[numImages];
        GCHandle handle = GCHandle.Alloc(projectErrorResults, GCHandleType.Pinned);
        IntPtr address = handle.AddrOfPinnedObject();
        float averageProjectionError = ARNativePlugin.arwCalibChessboardCornersSimple(file_name, address);
        handle.Free();
        return averageProjectionError;
    }

    public void arwCleanupChessboardCorners() {
        ARNativePlugin.arwCleanupChessboardCorners();
    }

    public bool arwUpdateARToolKit(byte[] imageBytes, int markerType) {
        bool ok = false;
        GCHandle handle = GCHandle.Alloc(imageBytes, GCHandleType.Pinned);
        IntPtr address = handle.AddrOfPinnedObject();
        ok = ARNativePlugin.arwUpdateARToolKit(address, markerType);
        handle.Free();
        return ok;
    }

    public void arwCleanupARToolKit() {
        ARNativePlugin.arwCleanupARToolKit();
    }

    public void arwConvertObserv2Ideal(float ox, float oy, out float ix, out float iy) {
        ix = 0;
        iy = 0;
        //ARNativePlugin.arwConvertObserv2Ideal(ox, oy, out ix, out iy);
    }

    public void arwConvertIdeal2Observ(float ox, float oy, out float ix, out float iy) {
        ix = 0;
        iy = 0;
        //ARNativePlugin.arwConvertIdeal2Observ(ox, oy, out ix, out iy);
    }
}


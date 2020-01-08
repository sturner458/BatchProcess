/*
 *  ARNativePlugin.cs
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
 *  Author(s): Philip Lamb
 *
 */

using System;
using System.Runtime.InteropServices;
using System.Text;

public class ARNativePlugin
{
    // The name of the external library containing the native functions
     // private const string LIBRARY_NAME = "ARWrapperd.dll";
     private const string LIBRARY_NAME = "ARX.dll";

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwRegisterLogCallback(ARToolKitFunctions.LogCallback callback);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetLogLevel(int logLevel);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwInitialiseAR();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwInitialiseARWithOptions(int pattSize, int pattCountMax);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwGetARToolKitVersion([MarshalAs(UnmanagedType.LPStr)]StringBuilder buffer, int length);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwGetError();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwShutdownAR();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwStartRunning(string vconf, string cparaName);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern void arwInitARToolKit(string vconf, string cparaName);

    ////[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    ////internal static extern void arwResetOpenGLContext();

    ////[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    ////internal static extern void arwSetBrightnessContrast(int inBright, int inContrast);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwStartRunningB(string vconf, byte[] cparaBuff, int cparaBuffLen, float nearPlane, float farPlane);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwStartRunningStereoB(string vconfL, byte[] cparaBuffL, int cparaBuffLenL, string vconfR, byte[] cparaBuffR, int cparaBuffLenR, byte[] transL2RBuff, int transL2RBuffLen, float nearPlane, float farPlane);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwIsRunning();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwStopRunning();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwGetProjectionMatrix(float nearPlane, float farPlane, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] matrix);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwGetProjectionMatrixStereo([MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] matrixL, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] matrixR);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwGetVideoParams(out int width, out int height, out int pixelSize, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pixelFormatBuffer, int pixelFormatBufferLen);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwGetVideoParamsStereo(out int widthL, out int heightL, out int pixelSizeL, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pixelFormatBufferL, int pixelFormatBufferLenL, out int widthR, out int heightR, out int pixelSizeR, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pixelFormatBufferR, int pixelFormatBufferLenR);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwCapture();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwUpdateAR();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    ////public static extern bool arwUpdateTexture([In, Out]Color[] colors);
    //internal static extern bool arwUpdateTexture(IntPtr colors);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    ////public static extern bool arwUpdateTextureStereo([In, Out]Color[] colorsL, [In, Out]Color[] colorsR);
    //internal static extern bool arwUpdateTextureStereo(IntPtr colorsL, IntPtr colorsR);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    //public static extern bool arwUpdateTexture32([In, Out]Color32[] colors32);
    internal static extern bool arwUpdateTexture32(IntPtr colors32);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    ////public static extern bool arwUpdateTexture32Stereo([In, Out]Color32[] colors32L, [In, Out]Color32[] colors32R);
    //internal static extern bool arwUpdateTexture32Stereo(IntPtr colors32L, IntPtr colors32R);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwUpdateTextureGL(int textureID);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwUpdateTextureGLStereo(int textureID_L, int textureID_R);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetUnityRenderEventUpdateTextureGLTextureID(int textureID);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetUnityRenderEventUpdateTextureGLStereoTextureIDs(int textureID_L, int textureID_R);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetMarkerPatternCount(int markerID);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwGetMarkerPatternConfig(int markerID, int patternID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] matrix, out float width, out float height, out int imageSizeX, out int imageSizeY);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwGetMarkerPatternImage(int markerID, int patternID, [In, Out]Color[] colors);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwGetTrackableOptionBool(int markerID, int option);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetTrackableOptionBool(int markerID, int option, bool value);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwGetTrackableOptionInt(int markerID, int option);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetTrackableOptionInt(int markerID, int option, int value);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern float arwGetTrackableOptionFloat(int markerID, int option);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetTrackableOptionFloat(int markerID, int option, float value);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetVideoDebugMode(bool debug);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwGetVideoDebugMode();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetVideoThreshold(int threshold);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetVideoThreshold();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetVideoThresholdMode(int mode);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetVideoThresholdMode();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetLabelingMode(int mode);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetLabelingMode();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetBorderSize(float mode);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern float arwGetBorderSize();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetPatternDetectionMode(int mode);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetPatternDetectionMode();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetMarkerExtractionMode(int mode);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetMarkerExtractionMode();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetMatrixCodeType(int type);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetMatrixCodeType();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetImageProcMode(int mode);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwGetImageProcMode();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern void arwSetNFTMultiMode(bool on);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwGetNFTMultiMode();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetTrackerOptionBool(int option, bool value);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetTrackerOptionInt(int option, int value);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetTrackerOptionFloat(int option, float value);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool arwGetTrackerOptionBool(int option);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwGetTrackerOptionInt(int option);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern float arwGetTrackerOptionFloat(int option);


    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int arwAddTrackable(string cfg);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwRemoveTrackable(int markerID);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwRemoveAllTrackables();


    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwQueryMarkerVisibility(int markerID);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwQueryTrackableVisibilityAndTransformation(int markerID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] matrix, [MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] double[] corners, out int numCorners);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwQueryTrackableMapperTransformation(int gMapUID, int trackableUID, IntPtr matrix);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwResetMapperTrackable(int gMapUID, string cfg);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwSetMappedMarkersVisible(int nMarkers, IntPtr markerTrans, IntPtr uids, IntPtr corners);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwAddMappedMarkers(int gMapUID, int GFMarkerID, int nMarkers, IntPtr markerTrans, IntPtr uids, IntPtr corners);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwListTrackables(int gMapUID);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwGetTrackablePatternConfig(int trackableUID, int patternID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] matrix, out double width, out double height, out int imageSizeX, out int imageSizeY, out int barcodeID);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwGetTrackablePatternCount(int trackableUID);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwQueryMarkerTransformationStereo(int markerID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] matrixL, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] matrixR);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    //[return: MarshalAs(UnmanagedType.I1)]
    //internal static extern bool arwLoadOpticalParams(string optical_param_name, byte[] optical_param_buff, int optical_param_buffLen, out float fovy_p, out float aspect_p, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] m, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] p);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool arwUpdateARToolKit(IntPtr imageBytes, bool doDatums);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool arwInitChessboardCorners(int nHorizontal, int nVertical, float patternSpacing, int calibImageNum, int xsize, int ysize);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwFindChessboardCorners(IntPtr corners, out int corner_count, IntPtr imageBytes);
    //internal static extern int arwFindChessboardCorners([MarshalAs(UnmanagedType.LPArray, SizeConst = 70)] float[] corners, out int corner_count, IntPtr imageBytes);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int arwCaptureChessboardCorners(int n);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern float arwCalibChessboardCorners(string file_name, IntPtr results);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern float arwCalibChessboardCornersSimple(string file_name, IntPtr results);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwCleanupChessboardCorners();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void arwCleanupARToolKit();

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwConvertObserv2Ideal(float ox, float oy, out float ix, out float iy);

    //[DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    //internal static extern int arwConvertIdeal2Observ(float ox, float oy, out float ix, out float iy);

}


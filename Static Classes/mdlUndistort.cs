using System;
using GL1 = OpenTK.Graphics.ES11.GL;
using All1 = OpenTK.Graphics.ES11.All;
using System.Diagnostics;

namespace BatchProcess {

    public enum AR_PIXEL_FORMAT {
        AR_PIXEL_FORMAT_INVALID = -1,
        AR_PIXEL_FORMAT_RGB = 0,
        AR_PIXEL_FORMAT_BGR,
        AR_PIXEL_FORMAT_Rgba,
        AR_PIXEL_FORMAT_Bgra,
        AR_PIXEL_FORMAT_ABGR,
        AR_PIXEL_FORMAT_MONO,
        AR_PIXEL_FORMAT_ARGB,
        AR_PIXEL_FORMAT_2vuy,
        AR_PIXEL_FORMAT_yuvs,
        AR_PIXEL_FORMAT_RGB_565,
        AR_PIXEL_FORMAT_Rgba_5551,
        AR_PIXEL_FORMAT_Rgba_4444,
        AR_PIXEL_FORMAT_420v,
        AR_PIXEL_FORMAT_420f,
        AR_PIXEL_FORMAT_NV21
    };

    public class ARParam {
        public int xsize;
        public int ysize;
        public double[,] mat = new double[3, 4];
        public double[] dist_factor = new double[17];
        public int dist_function_version; // Must be last field in structure (as will not be written to disk).
    };

    public class ARGL_CONTEXT_SETTINGS {
        public ARParam arParam;
        public int texture;
        public float[] t2;
        public float[] v2;
        public int t2bo;     // Vertex buffer object for t2 data.
        public int v2bo;     // Vertex buffer object for v2 data.
        public float zoom;
        public int textureSizeMax;
        public int textureSizeX;
        public int textureSizeY;
        public int pixIntFormat;
        public int pixFormat;
        public int pixType;
        public int pixSize;
        public AR_PIXEL_FORMAT format;
        public bool disableDistortionCompensation;
        public bool textureGeometryHasBeenSetup;
        public bool textureObjectsHaveBeenSetup;
        public bool rotate90;
        public bool flipH;
        public bool flipV;
        public int bufSizeX;
        public int bufSizeY;
        public bool bufSizeIsTextureSize;
        public bool textureDataReady;
        public int arglTexmapMode;
    };

    public static class mdlUndistort {

        public static ARGL_CONTEXT_SETTINGS gArglSettings = null;

        public static bool arglSetupTextureGeometry(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            float ty_prev, tx, ty;
            float y_prev, x, y;
            double x1, x2, y1, y2;
            float xx1, xx2, yy1, yy2;
            int i, j;
            int vertexCount, t2count, v2count;
            float imageSizeX, imageSizeY;
            float zoom;

            // Delete previous geometry, unless this is our first time here.
            if (contextSettings.textureGeometryHasBeenSetup) {
                GL1.DeleteBuffers(1, ref contextSettings.t2bo);
                GL1.DeleteBuffers(1, ref contextSettings.v2bo);
                contextSettings.textureGeometryHasBeenSetup = false;
            }

            // Set up the geometry for the surface which we will texture upon.
            imageSizeX = (float)contextSettings.arParam.xsize;
            imageSizeY = (float)contextSettings.arParam.ysize;
            zoom = contextSettings.zoom;
            if (contextSettings.disableDistortionCompensation) vertexCount = 4;
            else vertexCount = 840; // 20 rows of 2 x 21 vertices.
            contextSettings.t2 = new float[2 * vertexCount];
            contextSettings.v2 = new float[2 * vertexCount];
            t2count = v2count = 0;
            if (contextSettings.disableDistortionCompensation) {
                contextSettings.t2[t2count++] = 0.0f; // Top-left.
                contextSettings.t2[t2count++] = 0.0f;
                contextSettings.v2[v2count++] = 0.0f;
                contextSettings.v2[v2count++] = imageSizeY * zoom;
                contextSettings.t2[t2count++] = 0.0f; // Bottom-left.
                contextSettings.t2[t2count++] = imageSizeY / (float)contextSettings.textureSizeY;
                contextSettings.v2[v2count++] = 0.0f;
                contextSettings.v2[v2count++] = 0.0f;
                contextSettings.t2[t2count++] = imageSizeX / (float)contextSettings.textureSizeX; // Top-right.
                contextSettings.t2[t2count++] = 0.0f;
                contextSettings.v2[v2count++] = imageSizeX * zoom;
                contextSettings.v2[v2count++] = imageSizeY * zoom;
                contextSettings.t2[t2count++] = imageSizeX / (float)contextSettings.textureSizeX; // Bottom-right.
                contextSettings.t2[t2count++] = imageSizeY / (float)contextSettings.textureSizeY;
                contextSettings.v2[v2count++] = imageSizeX * zoom;
                contextSettings.v2[v2count++] = 0.0f;
            }
            else {
                y = 0.0f;
                ty = 0.0f;
                for (j = 1; j <= 20; j++) {    // Do 20 rows of triangle strips.
                    y_prev = y;
                    ty_prev = ty;
                    y = imageSizeY * (float)j / 20.0f;
                    ty = y / (float)contextSettings.textureSizeY;


                    for (i = 0; i <= 20; i++) { // 21 columns of triangle strip vertices, 2 vertices per column.
                        x = imageSizeX * (float)i / 20.0f;
                        tx = x / (float)contextSettings.textureSizeX;

                        arParamObserv2Ideal(contextSettings.arParam.dist_factor, (double)x, (double)y_prev, out x1, out y1, contextSettings.arParam.dist_function_version);
                        arParamObserv2Ideal(contextSettings.arParam.dist_factor, (double)x, (double)y, out x2, out y2, contextSettings.arParam.dist_function_version);

                        xx1 = (float)x1 * zoom;
                        yy1 = (imageSizeY - (float)y1) * zoom;
                        xx2 = (float)x2 * zoom;
                        yy2 = (imageSizeY - (float)y2) * zoom;

                        contextSettings.t2[t2count++] = tx; // Top.
                        contextSettings.t2[t2count++] = ty_prev;
                        contextSettings.v2[v2count++] = xx1;
                        contextSettings.v2[v2count++] = yy1;
                        contextSettings.t2[t2count++] = tx; // Bottom.
                        contextSettings.t2[t2count++] = ty;
                        contextSettings.v2[v2count++] = xx2;
                        contextSettings.v2[v2count++] = yy2;
                    } // columns.
                } // rows.
            }

            // Now setup VBOs.
            GL1.GenBuffers(1, out contextSettings.t2bo);
            GL1.GenBuffers(1, out contextSettings.v2bo);
            GL1.BindBuffer(All1.ArrayBuffer, contextSettings.t2bo);
            GL1.BufferData(All1.ArrayBuffer, new IntPtr((sizeof(float) * 2 * vertexCount)), contextSettings.t2, All1.StaticDraw);
            GL1.BindBuffer(All1.ArrayBuffer, contextSettings.v2bo);
            GL1.BufferData(All1.ArrayBuffer, new IntPtr((sizeof(float) * 2 * vertexCount)), contextSettings.v2, All1.StaticDraw);
            GL1.BindBuffer(All1.ArrayBuffer, 0);

            contextSettings.textureGeometryHasBeenSetup = true;
            return true;
        }


        // Set up the texture objects.
        static bool arglSetupTextureObjects(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            int textureWrapMode;

            // Delete previous textures, unless this is our first time here.
            if (contextSettings.textureObjectsHaveBeenSetup) {
                GL1.ActiveTexture(All1.Texture0);
                GL1.BindTexture(All1.Texture2D, 0);
                GL1.DeleteTextures(1, ref contextSettings.texture);
                contextSettings.textureObjectsHaveBeenSetup = false;
            }

            GL1.GenTextures(1, out contextSettings.texture);
            GL1.ActiveTexture(All1.Texture0);
            GL1.BindTexture(All1.Texture2D, contextSettings.texture);
            GL1.TexParameterx(All1.Texture2D, All1.TextureMinFilter, (int)All1.Linear);
            GL1.TexParameterx(All1.Texture2D, All1.TextureMagFilter, (int)All1.Linear);
            // Decide whether we can use GL_CLAMP_TO_EDGE.
            //if (arglGLCapabilityCheck(0x0120, (unsigned char *)"GL_SGIS_texture_edge_clamp")) {
            textureWrapMode = (int)All1.ClampToEdge;
            //} else {
            //    textureWrapMode = (int)All1.Repeat;
            //}
            GL1.TexParameterx(All1.Texture2D, All1.TextureWrapS, textureWrapMode);
            GL1.TexParameterx(All1.Texture2D, All1.TextureWrapT, textureWrapMode);

            contextSettings.textureObjectsHaveBeenSetup = true;
            return true;
        }

        //
        // Convert a camera parameter structure into an OpenGL projection matrix.
        //
        static void arglCameraFrustum(ARParam cparam, double focalmin, double focalmax, double[] m_projection)
        {
            double[,] icpara = new double[3, 4];
            double[,] trans = new double[3, 4];
            double[,] p = new double[3, 3], q = new double[4, 4];
            int width, height;
            int i, j;

            width = cparam.xsize;
            height = cparam.ysize;

            if (arParamDecompMat(cparam.mat, icpara, trans) < 0) {
                Debug.Print("arglCameraFrustum(): arParamDecompMat() indicated parameter error.\n");
                return;
            }

            for (i = 0; i < 4; i++) {
                icpara[1, i] = (height - 1) * (icpara[2, i]) - icpara[1, i];
            }

            for (i = 0; i < 3; i++) {
                for (j = 0; j < 3; j++) {
                    p[i, j] = icpara[i, j] / icpara[2, 2];
                }
            }
            q[0, 0] = (2.0 * p[0, 0] / (width - 1));
            q[0, 1] = (2.0 * p[0, 1] / (width - 1));
            q[0, 2] = ((2.0 * p[0, 2] / (width - 1)) - 1.0);
            q[0, 3] = 0.0;

            q[1, 0] = 0.0;
            q[1, 1] = (2.0 * p[1, 1] / (height - 1));
            q[1, 2] = ((2.0 * p[1, 2] / (height - 1)) - 1.0);
            q[1, 3] = 0.0;

            q[2, 0] = 0.0;
            q[2, 1] = 0.0;
            q[2, 2] = (focalmax + focalmin) / (focalmax - focalmin);
            q[2, 3] = -2.0 * focalmax * focalmin / (focalmax - focalmin);

            q[3, 0] = 0.0;
            q[3, 1] = 0.0;
            q[3, 2] = 1.0;
            q[3, 3] = 0.0;

            for (i = 0; i < 4; i++) { // Row.
                                      // First 3 columns of the current row.
                for (j = 0; j < 3; j++) { // Column.
                    m_projection[i + j * 4] = q[i, 0] * trans[0, j] +
                                            q[i, 1] * trans[1, j] +
                                            q[i, 2] * trans[2, j];
                }
                // Fourth column of the current row.
                m_projection[i + 3 * 4] = q[i, 0] * trans[0, 3] +
                                        q[i, 1] * trans[1, 3] +
                                        q[i, 2] * trans[2, 3] +
                                        q[i, 3];
            }
        }

        static void arglCameraFrustumRH(ARParam cparam, double focalmin, double focalmax, double[] m_projection)
        {
            double[,] icpara = new double[3, 4];
            double[,] trans = new double[3, 4];
            double[,] p = new double[3, 3], q = new double[4, 4];
            int width, height;
            int i, j;

            width = cparam.xsize;
            height = cparam.ysize;

            if (arParamDecompMat(cparam.mat, icpara, trans) < 0) {
                Debug.Print("arglCameraFrustum(): arParamDecompMat() indicated parameter error.\n");
                return;
            }
            for (i = 0; i < 4; i++) {
                icpara[1, i] = (height - 1) * (icpara[2, i]) - icpara[1, i];
            }

            for (i = 0; i < 3; i++) {
                for (j = 0; j < 3; j++) {
                    p[i, j] = icpara[i, j] / icpara[2, 2];
                }
            }
            q[0, 0] = (2.0 * p[0, 0] / (width - 1));
            q[0, 1] = (2.0 * p[0, 1] / (width - 1));
            q[0, 2] = -((2.0 * p[0, 2] / (width - 1)) - 1.0);
            q[0, 3] = 0.0;

            q[1, 0] = 0.0;
            q[1, 1] = -(2.0 * p[1, 1] / (height - 1));
            q[1, 2] = -((2.0 * p[1, 2] / (height - 1)) - 1.0);
            q[1, 3] = 0.0;

            q[2, 0] = 0.0;
            q[2, 1] = 0.0;
            q[2, 2] = (focalmax + focalmin) / (focalmin - focalmax);
            q[2, 3] = 2.0 * focalmax * focalmin / (focalmin - focalmax);

            q[3, 0] = 0.0;
            q[3, 1] = 0.0;
            q[3, 2] = -1.0;
            q[3, 3] = 0.0;

            for (i = 0; i < 4; i++) { // Row.
                                      // First 3 columns of the current row.
                for (j = 0; j < 3; j++) { // Column.
                    m_projection[i + j * 4] = q[i, 0] * trans[0, j] +
                                            q[i, 1] * trans[1, j] +
                                            q[i, 2] * trans[2, j];
                }
                // Fourth column of the current row.
                m_projection[i + 3 * 4] = q[i, 0] * trans[0, 3] +
                                        q[i, 1] * trans[1, 3] +
                                        q[i, 2] * trans[2, 3] +
                                        q[i, 3];
            }
        }

        static int arParamDecompMat(double[,] source, double[,] cpara, double[,] trans)
        {
            int r, c;
            double[,] Cpara = new double[3, 4];
            double rem1, rem2, rem3;

            if (source[2, 3] >= 0) {
                for (r = 0; r < 3; r++) {
                    for (c = 0; c < 4; c++) {
                        Cpara[r, c] = source[r, c];
                    }
                }
            }
            else {
                for (r = 0; r < 3; r++) {
                    for (c = 0; c < 4; c++) {
                        Cpara[r, c] = -(source[r, c]);
                    }
                }
            }

            for (r = 0; r < 3; r++) {
                for (c = 0; c < 4; c++) {
                    cpara[r, c] = 0.0;
                }
            }
            cpara[2, 2] = norm(Cpara[2, 0], Cpara[2, 1], Cpara[2, 2]);
            trans[2, 0] = Cpara[2, 0] / cpara[2, 2];
            trans[2, 1] = Cpara[2, 1] / cpara[2, 2];
            trans[2, 2] = Cpara[2, 2] / cpara[2, 2];
            trans[2, 3] = Cpara[2, 3] / cpara[2, 2];

            cpara[1, 2] = dot(trans[2, 0], trans[2, 1], trans[2, 2],
                               Cpara[1, 0], Cpara[1, 1], Cpara[1, 2]);
            rem1 = Cpara[1, 0] - cpara[1, 2] * trans[2, 0];
            rem2 = Cpara[1, 1] - cpara[1, 2] * trans[2, 1];
            rem3 = Cpara[1, 2] - cpara[1, 2] * trans[2, 2];
            cpara[1, 1] = norm(rem1, rem2, rem3);
            trans[1, 0] = rem1 / cpara[1, 1];
            trans[1, 1] = rem2 / cpara[1, 1];
            trans[1, 2] = rem3 / cpara[1, 1];

            cpara[0, 2] = dot(trans[2, 0], trans[2, 1], trans[2, 2],
                               Cpara[0, 0], Cpara[0, 1], Cpara[0, 2]);
            cpara[0, 1] = dot(trans[1, 0], trans[1, 1], trans[1, 2],
                               Cpara[0, 0], Cpara[0, 1], Cpara[0, 2]);
            rem1 = Cpara[0, 0] - cpara[0, 1] * trans[1, 0] - cpara[0, 2] * trans[2, 0];
            rem2 = Cpara[0, 1] - cpara[0, 1] * trans[1, 1] - cpara[0, 2] * trans[2, 1];
            rem3 = Cpara[0, 2] - cpara[0, 1] * trans[1, 2] - cpara[0, 2] * trans[2, 2];
            cpara[0, 0] = norm(rem1, rem2, rem3);
            trans[0, 0] = rem1 / cpara[0, 0];
            trans[0, 1] = rem2 / cpara[0, 0];
            trans[0, 2] = rem3 / cpara[0, 0];

            trans[1, 3] = (Cpara[1, 3] - cpara[1, 2] * trans[2, 3]) / cpara[1, 1];
            trans[0, 3] = (Cpara[0, 3] - cpara[0, 1] * trans[1, 3]
                                       - cpara[0, 2] * trans[2, 3]) / cpara[0, 0];

            for (r = 0; r < 3; r++) {
                for (c = 0; c < 3; c++) {
                    cpara[r, c] /= cpara[2, 2];
                }
            }

            return 0;
        }

        static double norm(double a, double b, double c)
        {
            return (Math.Sqrt(a * a + b * b + c * c));
        }

        static double dot(double a1, double a2, double a3,
           double b1, double b2, double b3)
        {
            return (a1 * b1 + a2 * b2 + a3 * b3);
        }

        // para's type is also equivalent to (double(*)[4]).
        static void arglCameraView(double[,] para, double[] m_modelview, double scale)
        {
            m_modelview[0 + 0 * 4] = para[0, 0]; // R1C1
            m_modelview[0 + 1 * 4] = para[0, 1]; // R1C2
            m_modelview[0 + 2 * 4] = para[0, 2];
            m_modelview[0 + 3 * 4] = para[0, 3];
            m_modelview[1 + 0 * 4] = para[1, 0]; // R2
            m_modelview[1 + 1 * 4] = para[1, 1];
            m_modelview[1 + 2 * 4] = para[1, 2];
            m_modelview[1 + 3 * 4] = para[1, 3];
            m_modelview[2 + 0 * 4] = para[2, 0]; // R3
            m_modelview[2 + 1 * 4] = para[2, 1];
            m_modelview[2 + 2 * 4] = para[2, 2];
            m_modelview[2 + 3 * 4] = para[2, 3];
            m_modelview[3 + 0 * 4] = 0.0;
            m_modelview[3 + 1 * 4] = 0.0;
            m_modelview[3 + 2 * 4] = 0.0;
            m_modelview[3 + 3 * 4] = 1.0;
            if (scale != 0.0) {
                m_modelview[12] *= scale;
                m_modelview[13] *= scale;
                m_modelview[14] *= scale;
            }
        }

        // para's type is also equivalent to (double(*)[4]).
        static void arglCameraViewRH(double[,] para, double[] m_modelview, double scale)
        {
            m_modelview[0 + 0 * 4] = para[0, 0]; // R1C1
            m_modelview[0 + 1 * 4] = para[0, 1]; // R1C2
            m_modelview[0 + 2 * 4] = para[0, 2];
            m_modelview[0 + 3 * 4] = para[0, 3];
            m_modelview[1 + 0 * 4] = -para[1, 0]; // R2
            m_modelview[1 + 1 * 4] = -para[1, 1];
            m_modelview[1 + 2 * 4] = -para[1, 2];
            m_modelview[1 + 3 * 4] = -para[1, 3];
            m_modelview[2 + 0 * 4] = -para[2, 0]; // R3
            m_modelview[2 + 1 * 4] = -para[2, 1];
            m_modelview[2 + 2 * 4] = -para[2, 2];
            m_modelview[2 + 3 * 4] = -para[2, 3];
            m_modelview[3 + 0 * 4] = 0.0;
            m_modelview[3 + 1 * 4] = 0.0;
            m_modelview[3 + 2 * 4] = 0.0;
            m_modelview[3 + 3 * 4] = 1.0;
            if (scale != 0.0) {
                m_modelview[12] *= scale;
                m_modelview[13] *= scale;
                m_modelview[14] *= scale;
            }
        }

        public static ARGL_CONTEXT_SETTINGS arglSetupForCurrentContext(ARParam cparam, AR_PIXEL_FORMAT pixelFormat)
        {
            ARGL_CONTEXT_SETTINGS contextSettings;

            contextSettings = new ARGL_CONTEXT_SETTINGS();
            contextSettings.arParam = cparam; // Copy it.
            contextSettings.zoom = 1.0f;
            // Because of calloc used above, these are redundant.
            //contextSettings.rotate90 = contextSettings.flipH = contextSettings.flipV = false;
            //contextSettings.disableDistortionCompensation = false;
            //contextSettings.textureGeometryHasBeenSetup = false;
            //contextSettings.textureObjectsHaveBeenSetup = false;
            //contextSettings.textureDataReady = false;

            // This sets pixIntFormat, pixFormat, pixType, pixSize, and resets textureDataReady.
            arglPixelFormatSet(contextSettings, pixelFormat);

            // Set pixel buffer sizes to incoming image size, by default.
            if (!arglPixelBufferSizeSet(contextSettings, cparam.xsize, cparam.ysize)) {
                Debug.Print("ARGL: Error setting pixel buffer size.\n");
                contextSettings = null;
                return null;
            }

            return (contextSettings);
        }

        static void arglCleanup(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            if (contextSettings == null) return; // Sanity check.

            if (contextSettings.textureObjectsHaveBeenSetup) {
                GL1.ActiveTexture(All1.Texture0);
                GL1.BindTexture(All1.Texture2D, 0);
                GL1.DeleteTextures(1, ref contextSettings.texture);
            }

            if (contextSettings.textureGeometryHasBeenSetup) {
                GL1.DeleteBuffers(1, ref contextSettings.t2bo);
                GL1.DeleteBuffers(1, ref contextSettings.v2bo);
            }

            contextSettings = null;
        }

        static void arglDispImage(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            float left, right, bottom, top;
            bool lightingSave;
            bool depthTestSave;

            if (contextSettings == null) return;

            // Prepare an orthographic projection, set camera position for 2D drawing, and save GL state.
            GL1.MatrixMode(All1.Projection);
            GL1.PushMatrix();
            GL1.LoadIdentity();
            if (contextSettings.rotate90) GL1.Rotate(90.0f, 0.0f, 0.0f, -1.0f);

            if (contextSettings.flipV) {
                bottom = (float)contextSettings.arParam.ysize;
                top = 0.0f;
            }
            else {
                bottom = 0.0f;
                top = (float)contextSettings.arParam.ysize;
            }
            if (contextSettings.flipH) {
                left = (float)contextSettings.arParam.xsize;
                right = 0.0f;
            }
            else {
                left = 0.0f;
                right = (float)contextSettings.arParam.xsize;
            }
            GL1.Ortho(left, right, bottom, top, -1.0f, 1.0f);
            GL1.MatrixMode(All1.Modelview);
            GL1.PushMatrix();
            GL1.LoadIdentity();

            lightingSave = GL1.IsEnabled(All1.Lighting);            // Save enabled state of lighting.
            if (lightingSave == true) GL1.Disable(All1.Lighting);
            depthTestSave = GL1.IsEnabled(All1.DepthTest);      // Save enabled state of depth test.
            if (depthTestSave == true) GL1.Disable(All1.DepthTest);

            arglDispImageStateful(contextSettings);

            if (depthTestSave == true) GL1.Enable(All1.DepthTest);          // Restore enabled state of depth test.
            if (lightingSave == true) GL1.Enable(All1.Lighting);         // Restore enabled state of lighting.

            // Restore previous projection & camera position.
            GL1.MatrixMode(All1.Projection);
            GL1.PopMatrix();
            GL1.MatrixMode(All1.Modelview);
            GL1.PopMatrix();

            // Report any errors we generated.
            int err = (int)GL1.GetError();
            while (err != (int)All1.NoError) {
                Debug.Print("ARGL: GL error 0x%04X\n", (int)err);
            }
        }

        static void arglDispImageStateful(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            int texEnvModeSave;
            int i;

            if (contextSettings == null) return;
            if (contextSettings.textureObjectsHaveBeenSetup) return;
            if (contextSettings.textureGeometryHasBeenSetup) return;
            if (contextSettings.textureDataReady) return;

            GL1.ActiveTexture(All1.Texture0);

            GL1.MatrixMode(All1.Texture);
            GL1.LoadIdentity();
            GL1.MatrixMode(All1.Modelview);

            GL1.BindTexture(All1.Texture2D, contextSettings.texture);
            GL1.GetTexEnv(All1.TextureEnv, All1.TextureEnvMode, out texEnvModeSave); // Save GL texture environment mode.
            if (texEnvModeSave != (int)All1.Replace) GL1.TexEnv(All1.TextureEnv, All1.TextureEnvMode, (int)All1.Replace);
            GL1.Enable(All1.Texture2D);

            GL1.ClientActiveTexture(All1.Texture0);
            GL1.BindBuffer(All1.ArrayBuffer, contextSettings.t2bo);
            GL1.TexCoordPointer(2, All1.Float, 0, IntPtr.Zero);
            GL1.EnableClientState(All1.TextureCoordArray);

            GL1.BindBuffer(All1.ArrayBuffer, contextSettings.v2bo);
            GL1.VertexPointer(2, All1.Float, 0, IntPtr.Zero);
            GL1.EnableClientState(All1.VertexArray);
            GL1.DisableClientState(All1.NormalArray);

            if (contextSettings.disableDistortionCompensation) {
                GL1.DrawArrays(All1.TriangleStrip, 0, 4);
            }
            else {
                for (i = 0; i < 20; i++) {
                    GL1.DrawArrays(All1.TriangleStrip, i * 42, 42);
                }
            }

            GL1.BindBuffer(All1.ArrayBuffer, 0);
            GL1.DisableClientState(All1.VertexArray);
            GL1.DisableClientState(All1.TextureCoordArray);

            GL1.Disable(All1.Texture2D);
            if (texEnvModeSave != (int)All1.Replace) GL1.TexEnv(All1.TextureEnv, All1.TextureEnvMode, texEnvModeSave); // Restore GL texture environment mode.
        }

        static bool arglDistortionCompensationSet(ARGL_CONTEXT_SETTINGS contextSettings, bool enable)
        {
            if (contextSettings == null) return (false);
            contextSettings.disableDistortionCompensation = !enable;
            return (arglSetupTextureGeometry(contextSettings));
        }

        static bool arglDistortionCompensationGet(ARGL_CONTEXT_SETTINGS contextSettings, ref bool enable)
        {
            if (contextSettings == null || !enable) return (false);
            enable = contextSettings.disableDistortionCompensation;
            return (true);
        }

        static bool arglSetPixelZoom(ARGL_CONTEXT_SETTINGS contextSettings, float zoom)
        {
            if (contextSettings == null) return (false);
            contextSettings.zoom = zoom;

            // Changing the zoom invalidates the geometry, so set it up.
            return (arglSetupTextureGeometry(contextSettings));
        }

        static bool arglGetPixelZoom(ARGL_CONTEXT_SETTINGS contextSettings, ref float zoom)
        {
            if (contextSettings == null) return (false);
            zoom = contextSettings.zoom;
            return (true);
        }

        static bool arglPixelFormatSet(ARGL_CONTEXT_SETTINGS contextSettings, AR_PIXEL_FORMAT format)
        {
            if (contextSettings == null) return (false);
            switch (format) {
                case AR_PIXEL_FORMAT.AR_PIXEL_FORMAT_Rgba:
                    contextSettings.pixIntFormat = (int)All1.Rgba;
                    contextSettings.pixFormat = (int)All1.Rgba;
                    contextSettings.pixType = (int)All1.UnsignedByte;
                    contextSettings.pixSize = 4;
                    break;
                case AR_PIXEL_FORMAT.AR_PIXEL_FORMAT_Bgra:  // Windows.
                    contextSettings.pixIntFormat = (int)All1.Rgba;
                    contextSettings.pixFormat = (int)All1.Bgra;
                    contextSettings.pixType = (int)All1.UnsignedByte;
                    contextSettings.pixSize = 4;
                    break;
                case AR_PIXEL_FORMAT.AR_PIXEL_FORMAT_ABGR:  // SGI.
                        contextSettings.pixIntFormat = (int)All1.Rgba;
                        contextSettings.pixFormat = (int)All1.AbgrExt;
                        contextSettings.pixType = (int)All1.UnsignedByte;
                        contextSettings.pixSize = 4;
                    break;
                case AR_PIXEL_FORMAT.AR_PIXEL_FORMAT_ARGB:  // Mac.
                    contextSettings.pixIntFormat = (int)All1.Rgba;
                    contextSettings.pixFormat = (int)All1.Bgra;
                    contextSettings.pixType = (int)All1.UnsignedInt8888;
                    contextSettings.pixSize = 4;
                    break;
                case AR_PIXEL_FORMAT.AR_PIXEL_FORMAT_RGB:
                    contextSettings.pixIntFormat = (int)All1.Rgb;
                    contextSettings.pixFormat = (int)All1.Rgb;
                    contextSettings.pixType = (int)All1.UnsignedByte;
                    contextSettings.pixSize = 3;
                    break;
                case AR_PIXEL_FORMAT.AR_PIXEL_FORMAT_BGR:
                    contextSettings.pixIntFormat = (int)All1.Rgb;
                    contextSettings.pixFormat = (int)All1.Rgb;
                    contextSettings.pixType = (int)All1.UnsignedByte;
                    contextSettings.pixSize = 3;
                    break;
                default:
                    return (false);
                    break;
            }
            contextSettings.format = format;
            contextSettings.textureDataReady = false;

            if (!arglSetupTextureObjects(contextSettings)) return (false);

            return (true);
        }

        static bool arglPixelFormatGet(ARGL_CONTEXT_SETTINGS contextSettings, ref AR_PIXEL_FORMAT format, ref int size)
        {
            if (contextSettings == null) return (false);

            format = contextSettings.format;
            size = contextSettings.pixSize;

            return (true);
        }

        static void arglSetRotate90(ARGL_CONTEXT_SETTINGS contextSettings, bool rotate90)
        {
            if (contextSettings == null) return;
            contextSettings.rotate90 = rotate90;
        }

        static bool arglGetRotate90(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            if (contextSettings == null) return false;
            return (contextSettings.rotate90);
        }

        static void arglSetFlipH(ARGL_CONTEXT_SETTINGS contextSettings, bool flipH)
        {
            if (contextSettings == null) return;
            contextSettings.flipH = flipH;
        }

        static bool arglGetFlipH(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            if (contextSettings == null) return false;
            return (contextSettings.flipH);
        }

        static void arglSetFlipV(ARGL_CONTEXT_SETTINGS contextSettings, bool flipV)
        {
            if (contextSettings == null) return;
            contextSettings.flipV = flipV;
        }

        static bool arglGetFlipV(ARGL_CONTEXT_SETTINGS contextSettings)
        {
            if (contextSettings == null) return false;
            return (contextSettings.flipV);
        }

        static bool arglPixelBufferSizeSet(ARGL_CONTEXT_SETTINGS contextSettings, int bufWidth, int bufHeight)
        {
            if (contextSettings == null) return (false);

            // Check texturing capabilities (sets textureSizeX, textureSizeY, textureSizeMax).
            GL1.GetInteger(All1.MaxTextureSize, out contextSettings.textureSizeMax);
            if (bufWidth > contextSettings.textureSizeMax || bufHeight > contextSettings.textureSizeMax) {
                Debug.Print("Error: ARGL: Your OpenGL implementation and/or hardware's texturing capabilities are insufficient.\n");
                return (false);
            }

            contextSettings.textureSizeX = bufWidth;
            contextSettings.textureSizeY = bufHeight;
            contextSettings.bufSizeIsTextureSize = true;

            // Changing the size of the data we'll be receiving invalidates the geometry, so set it up.
            return (arglSetupTextureGeometry(contextSettings));
        }

        static bool arglPixelBufferSizeGet(ARGL_CONTEXT_SETTINGS contextSettings, ref int bufWidth, ref int bufHeight)
        {
            if (contextSettings == null) return (false);
            if (contextSettings.textureGeometryHasBeenSetup) return (false);

            if (contextSettings.bufSizeIsTextureSize) {
                bufWidth = contextSettings.textureSizeX;
                bufHeight = contextSettings.textureSizeY;
            }
            else {
                bufWidth = contextSettings.bufSizeX;
                bufHeight = contextSettings.bufSizeY;
            }
            return (true);
        }

        static bool arglPixelBufferDataUpload(ARGL_CONTEXT_SETTINGS contextSettings, byte[] bufDataPtr)
        {
            if (contextSettings == null) return (false);
            if (contextSettings.textureObjectsHaveBeenSetup || contextSettings.textureGeometryHasBeenSetup || contextSettings.pixSize == 0) return (false);

            GL1.ActiveTexture(All1.Texture0);
            GL1.BindTexture(All1.Texture2D, contextSettings.texture);

            GL1.PixelStore(All1.UnpackAlignment, (((contextSettings.bufSizeX * contextSettings.pixSize) & 0x3) == 0 ? 4 : 1));

            if (contextSettings.bufSizeIsTextureSize) {
                GL1.TexImage2D(All1.Texture2D, 0, (OpenTK.Graphics.ES11.All)contextSettings.pixIntFormat, contextSettings.textureSizeX, contextSettings.textureSizeY, 0, (All1)contextSettings.pixFormat, (All1)contextSettings.pixType, bufDataPtr);
            }
            else {
                // Request OpenGL allocate memory internally for a power-of-two texture of the appropriate size.
                // Then send the NPOT-data as a subimage.
                GL1.TexImage2D(All1.Texture2D, 0, (OpenTK.Graphics.ES11.All)contextSettings.pixIntFormat, contextSettings.textureSizeX, contextSettings.textureSizeY, 0, (All1)contextSettings.pixFormat, (All1)contextSettings.pixType, IntPtr.Zero);
                GL1.TexSubImage2D(All1.Texture2D, 0, 0, 0, contextSettings.bufSizeX, contextSettings.bufSizeY, (All1)contextSettings.pixFormat, (All1)contextSettings.pixType, bufDataPtr);
            }

            contextSettings.textureDataReady = true;

            return true;
        }

        static int arParamObserv2Ideal(double[] dist_factor, double ox, double oy,
                                 out double ix, out double iy, int dist_function_version)
        {
            // ----------------------------------------
            if (dist_function_version == 4) {

                // OpenCV distortion model, with addition of a scale factor so that
                // entire image fits onscreen.
                double k1, k2, p1, p2, fx, fy, x0, y0, s;
                double px, py, x02, y02;
                int i;

                k1 = dist_factor[0];
                k2 = dist_factor[1];
                p1 = dist_factor[2];
                p2 = dist_factor[3];
                fx = dist_factor[4];
                fy = dist_factor[5];
                x0 = dist_factor[6];
                y0 = dist_factor[7];
                s = dist_factor[8];

                px = (ox - x0) / fx;
                py = (oy - y0) / fy;

                x02 = px * px;
                y02 = py * py;

                for (i = 1; ; i++) {
                    if (x02 != 0.0 || y02 != 0.0) {
                        px = px - ((1.0 + k1 * (x02 + y02) + k2 * (x02 + y02) * (x02 + y02)) * px + 2.0 * p1 * px * py + p2 * (x02 + y02 + 2.0 * x02) - ((ox - x0) / fx)) / (1.0 + k1 * (3.0 * x02 + y02) + k2 * (5.0 * x02 * x02 + 3.0 * x02 * y02 + y02 * y02) + 2.0 * p1 * py + 6.0 * p2 * px);
                        //px = px - ((1.0 + k1*(x02+y02) + k2*(x02+y02)*(x02+y02))*px + 2.0*p1*px*py + p2*(x02 + y02 + 2.0*x02)-((ox - x0)/fx))/(1.0+k1*(3.0*x02+y02)+k2*(5.0*x02*x02+6.0*x02*y02+y02*y02)+2.0*p1*py+6.0*p2*px);

                        py = py - ((1.0 + k1 * (x02 + y02) + k2 * (x02 + y02) * (x02 + y02)) * py + p1 * (x02 + y02 + 2.0 * y02) + 2.0 * p2 * px * py - ((oy - y0) / fy)) / (1.0 + k1 * (x02 + 3.0 * y02) + k2 * (x02 * x02 + 3.0 * x02 * y02 + 5.0 * y02 * y02) + 6.0 * p1 * py + 2.0 * p2 * px);
                        //py = py - ((1.0 + k1*(x02+y02) + k2*(x02+y02)*(x02+y02))*py + p1*(x02 + y02 + 2.0*y02) + 2.0*p2*px*py-((oy - y0)/fy))/(1.0+k1*(x02+3.0*y02)+k2*(x02*x02+6.0*x02*y02+5.0*y02*y02)+6.0*p1*py+2.0*p2*px);
                    }
                    else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == 4) break;

                    x02 = px * px;
                    y02 = py * py;
                }


                ix = px * fx / s + x0;
                iy = py * fy / s + y0;

                return 0;

                // ----------------------------------------
            }
            else if (dist_function_version == 3) {

                double z02, z0, p1, p2, q, z, px, py, ar;
                int i;

                ar = dist_factor[3];
                px = (ox - dist_factor[0]) / ar;
                py = oy - dist_factor[1];
                p1 = dist_factor[4] / 100000000.0;
                p2 = dist_factor[5] / 100000000.0 / 100000.0;
                z02 = px * px + py * py;
                q = z0 = Math.Sqrt(px * px + py * py);

                for (i = 1; ; i++) {
                    if (z0 != 0.0) {
                        z = z0 - ((1.0 - p1 * z02 - p2 * z02 * z02) * z0 - q) / (1.0 - 3.0 * p1 * z02 - 5.0 * p2 * z02 * z02);
                        px = px * z / z0;
                        py = py * z / z0;
                    }
                    else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == 3) break;

                    z02 = px * px + py * py;
                    z0 = Math.Sqrt(px * px + py * py);
                }

                ix = px / dist_factor[2] + dist_factor[0];
                iy = py / dist_factor[2] + dist_factor[1];

                return 0;

                // ----------------------------------------
            }
            else if (dist_function_version == 2) {

                double z02, z0, p1, p2, q, z, px, py;
                int i;

                px = ox - dist_factor[0];
                py = oy - dist_factor[1];
                p1 = dist_factor[3] / 100000000.0;
                p2 = dist_factor[4] / 100000000.0 / 100000.0;
                z02 = px * px + py * py;
                q = z0 = Math.Sqrt(px * px + py * py);

                for (i = 1; ; i++) {
                    if (z0 != 0.0) {
                        z = z0 - ((1.0 - p1 * z02 - p2 * z02 * z02) * z0 - q) / (1.0 - 3.0 * p1 * z02 - 5.0 * p2 * z02 * z02);
                        px = px * z / z0;
                        py = py * z / z0;
                    }
                    else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == 3) break;

                    z02 = px * px + py * py;
                    z0 = Math.Sqrt(px * px + py * py);
                }

                ix = px / dist_factor[2] + dist_factor[0];
                iy = py / dist_factor[2] + dist_factor[1];

                return 0;

                // ----------------------------------------
            }
            else if (dist_function_version == 1) {

                double z02, z0, p, q, z, px, py;
                int i;

                px = ox - dist_factor[0];
                py = oy - dist_factor[1];
                p = dist_factor[3] / 100000000.0;
                z02 = px * px + py * py;
                q = z0 = Math.Sqrt(px * px + py * py);

                for (i = 1; ; i++) {
                    if (z0 != 0.0) {
                        z = z0 - ((1.0 - p * z02) * z0 - q) / (1.0 - 3.0 * p * z02);
                        px = px * z / z0;
                        py = py * z / z0;
                    }
                    else {
                        px = 0.0;
                        py = 0.0;
                        break;
                    }
                    if (i == 3) break;

                    z02 = px * px + py * py;
                    z0 = Math.Sqrt(px * px + py * py);
                }

                ix = px / dist_factor[2] + dist_factor[0];
                iy = py / dist_factor[2] + dist_factor[1];

                return 0;

                // ----------------------------------------
            }
            else {
                ix = 0;
                iy = 0;
                return -1;

            }
        }

        static int arParamIdeal2Observ(double[] dist_factor, double ix, double iy,
                                 out double ox, out double oy, int dist_function_version)
        {
            // ----------------------------------------
            if (dist_function_version == 4) {

                double k1, k2, p1, p2, fx, fy, x0, y0, s;
                double l, x, y;

                k1 = dist_factor[0];
                k2 = dist_factor[1];
                p1 = dist_factor[2];
                p2 = dist_factor[3];
                fx = dist_factor[4];
                fy = dist_factor[5];
                x0 = dist_factor[6];
                y0 = dist_factor[7];
                s = dist_factor[8];

                x = (ix - x0) * s / fx;
                y = (iy - y0) * s / fy;
                l = x * x + y * y;
                ox = (x * (1.0 + k1 * l + k2 * l * l) + 2.0 * p1 * x * y + p2 * (l + 2.0 * x * x)) * fx + x0;
                oy = (y * (1.0 + k1 * l + k2 * l * l) + p1 * (l + 2.0 * y * y) + 2.0 * p2 * x * y) * fy + y0;

                return 0;

                // ----------------------------------------
            }
            else if (dist_function_version == 3) {

                double x, y, l, d, ar;

                ar = dist_factor[3];
                x = (ix - dist_factor[0]) * dist_factor[2];
                y = (iy - dist_factor[1]) * dist_factor[2];
                if (x == 0.0 && y == 0.0) {
                    ox = dist_factor[0];
                    oy = dist_factor[1];
                }
                else {
                    l = x * x + y * y;
                    d = 1.0 - dist_factor[4] / 100000000.0 * l - dist_factor[5] / 100000000.0 / 100000.0 * l * l;
                    ox = x * d * ar + dist_factor[0];
                    oy = y * d + dist_factor[1];
                }

                return 0;

                // ----------------------------------------
            }
            else if (dist_function_version == 2) {

                double x, y, l, d;

                x = (ix - dist_factor[0]) * dist_factor[2];
                y = (iy - dist_factor[1]) * dist_factor[2];
                if (x == 0.0 && y == 0.0) {
                    ox = dist_factor[0];
                    oy = dist_factor[1];
                }
                else {
                    l = x * x + y * y;
                    d = 1.0 - dist_factor[3] / 100000000.0 * l - dist_factor[4] / 100000000.0 / 100000.0 * l * l;
                    ox = x * d + dist_factor[0];
                    oy = y * d + dist_factor[1];
                }

                return 0;

                // ----------------------------------------
            }
            else if (dist_function_version == 1) {

                double x, y, d;

                x = (ix - dist_factor[0]) * dist_factor[2];
                y = (iy - dist_factor[1]) * dist_factor[2];
                if (x == 0.0 && y == 0.0) {
                    ox = dist_factor[0];
                    oy = dist_factor[1];
                }
                else {
                    d = 1.0 - dist_factor[3] / 100000000.0 * (x * x + y * y);
                    ox = x * d + dist_factor[0];
                    oy = y * d + dist_factor[1];
                }

                return 0;
                // ----------------------------------------
            }
            else {
                ox = 0;
                oy = 0;
                return -1;
            }
        }
    }
}

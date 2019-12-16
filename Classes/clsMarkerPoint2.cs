using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using static BatchProcess.mdlDetectPhotos;

namespace BatchProcess
{
    public class clsMarkerPoint2
    {
        int myMarkerID;
        int mySeenFromMarkerID;
        List<int> mySeenFromMarkerIDs = new List<int>();
        int myActualMarkerID; //For when we renumber the markers, e.g. due to stitching a new flight

        //Coordinates of key points on the marker - in the model space of the SeenFrom marker, until turned into a Confirmed Marker
        clsPoint3d myOrigin = new clsPoint3d(); //Origin Point
        clsPoint3d myEndXAxis = new clsPoint3d(); //End of X Axis
        clsPoint3d myEndYAxis = new clsPoint3d(); //End of Y Axis
        clsPoint3d myPoint = new clsPoint3d(); //Tip of marker

        //Coordinate vectors - in the model space of the SeenFrom marker, until turned into a Confirmed Marker
        clsPoint3d myVx = new clsPoint3d();
        clsPoint3d myVy = new clsPoint3d();
        clsPoint3d myVz = new clsPoint3d();

        //Lists of measured points - in the model space of the SeenFrom marker, except for CameraPoints
        List<clsPoint3d> myCameraPoints = new List<clsPoint3d>(); //Location of camera, in coordinate system of this marker
        List<clsPoint3d> mySeenFromCameraPoints = new List<clsPoint3d>(); //Camera Point, as seen in the model view matrix of the SeenFrom marker
        List<clsPoint3d> myPts1 = new List<clsPoint3d>(); //Origin Point
        List<clsPoint3d> myPts2 = new List<clsPoint3d>(); //End of X Axis
        List<clsPoint3d> myPts3 = new List<clsPoint3d>(); //End of Y Axis

        clsPoint3d myVerticalVect = null;

        float[] myModelViewMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        List<clsMarkerPoint2> myHistory = new List<clsMarkerPoint2>();
        //Dim myVectors As New List(Of clsPoint3d)

        public bool Visible = false;

        private bool _confirmed = false;
        public bool Confirmed {
            get { return _confirmed; }
            set {
                _confirmed = value;
                ConfirmedValueChanged?.Invoke();
            }
        }
        public event BlankEventHandler ConfirmedValueChanged;

        public void SetConfirmedFlag(bool inConfirmedFlag) { _confirmed = inConfirmedFlag; } //To avoid triggering the ConfirmedValueChanged delegate

        private bool _levelled = false;
        public bool Levelled {
            get { return _levelled; }
            set {
                _levelled = value;
                LevelledValueChanged?.Invoke();
            }
        }
        public event BlankEventHandler LevelledValueChanged;

        private bool _stitched = false;
        public bool Stitched {
            get { return _stitched; }
            set {
                _stitched = value;
                StichedValueChanged?.Invoke();
            }
        }
        public event BlankEventHandler StichedValueChanged;

        Nullable<System.DateTime> myLastTime;
        clsPoint3d myLastVector = null;
        public bool FirstPointRemoved = false;
        //List<int> myCalculatedPoints = new List<int>();
        public double MaxAngle3 = 0;
        public double MinDist3 = 0;

        public double MaxDist3 = 0;
        string myLabel;
        public string MarkerName { get; set; }
        public string SeenFromMarkerName { get; set; }
        public string NumPoints { get; set; }
        public string MaximumAngleA { get; set; }
        public string MaximumAngleXY { get; set; }

        public double BulkheadHeight { get; set; }

        //Public StepSeenIndex As Integer = 0
        //Public MaxVector1 As New clsPoint3d
        //Public MaxVector2 As New clsPoint3d

        public List<clsPoint3d> GyroData { get; }
        public List<clsPoint3d> LastGyroData { get; }
        public List<clsPoint3d> AccelData { get; }
        public List<clsPoint3d> LastAccelData { get; }

        public int MarkerID {
            get { return myMarkerID; }
            set { myMarkerID = value; }
        }

        public int NewMarkerID() {
            int newMarkerID = myMarkerID + 1;
            if (newMarkerID <= 25) {
                //Do Nothing
            } else if (newMarkerID <= 50) {
                return newMarkerID + 25;
            } else if (myMarkerID <= 75) {
                return newMarkerID - 25;
            } else if (newMarkerID <= 100) {
                //Do Nothing
            }
            return newMarkerID;
        }

        public int SeenFromMarkerID {
            get { return mySeenFromMarkerID; }
            set { mySeenFromMarkerID = value; }
        }

        public List<int> SeenFromMarkerIDs {
            get { return mySeenFromMarkerIDs; }
        }

        public int ActualMarkerID {
            get { return myActualMarkerID; }
            set { myActualMarkerID = value; }
        }

        public clsPoint3d Origin {
            get { return myOrigin; }
            set { myOrigin = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d XAxisPoint {
            get { return myEndXAxis; }
            set { myEndXAxis = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d YAxisPoint {
            get { return myEndYAxis; }
            set { myEndYAxis = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d Point {
            get { return myPoint; }
            set { myPoint = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d VX {
            get { return myVx ?? new clsPoint3d(0, 0, 0); }
            set { myVx = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d VY {
            get { return myVy ?? new clsPoint3d(0, 0, 0); }
            set { myVy = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d VZ {
            get { return myVz ?? new clsPoint3d(0, 0, 0); }
            set { myVz = (value == null ? new clsPoint3d(0, 0, 0) : value.Copy()); }
        }

        public clsPoint3d VerticalVect {
            get { return myVerticalVect; }
            set { myVerticalVect = value?.Copy(); }
        }

        public float[] ModelViewMatrix {
            get { return myModelViewMatrix; }
            set {
                int i;

                for (i = 0; i <= 15; i++) {
                    myModelViewMatrix[i] = value[i];
                }
            }
        }

        public List<clsMarkerPoint2> History {
            get { return myHistory; }
        }

        public clsMarkerPoint2() {
            GyroData = new List<clsPoint3d>();
            LastGyroData = new List<clsPoint3d>();
            AccelData = new List<clsPoint3d>();
            LastAccelData = new List<clsPoint3d>();
            VX = new clsPoint3d(1, 0, 0);
            VY = new clsPoint3d(0, 1, 0);
            VZ = new clsPoint3d(0, 0, 1);
        }

        //Public Sub New(aMarkerID As Integer)
        //    myMarkerID = aMarkerID
        //End Sub

        public clsMarkerPoint2(int aMarkerID, int aSeenFromID) {
            myMarkerID = aMarkerID;
            myActualMarkerID = myMarkerID;
            mySeenFromMarkerID = aSeenFromID;
            GyroData = new List<clsPoint3d>();
            LastGyroData = new List<clsPoint3d>();
            AccelData = new List<clsPoint3d>();
            LastAccelData = new List<clsPoint3d>();
        }

        public bool OKToConfirm(ref string myErrString, ref clsPoint3d maxAV1, ref clsPoint3d maxAV2, ref clsPoint3d maxAV3, ref clsPoint3d maxAV4, ref bool myAngleOK, ref bool myAngle2OK, ref double a1, ref double a2) {
            myErrString = "";
            myAngleOK = false;
            myAngle2OK = false;

            //-DEVELOPMENT CHANGE
            a1 = MaxDistance(ref maxAV1, ref maxAV2);
            if (a1 < 500) {
                myErrString = "Distance = " + Round(a1) + " < 500mm";
            } else {
                myAngleOK = true;
            }

            //a1 = MaxAngle(ref maxAV1, ref maxAV2);
            //if (a1 < 20 * PI / 180) {
            //    myErrString = "Angle Range = " + Round(a1 * 180 / PI, 1) + " < 20°";
            //} else {
            //    myAngleOK = true;
            //}

            a2 = MaxAnglePerpendicular(ref maxAV3, ref maxAV4);
            a2 = 25 * PI / 180;
            if (a2 < 20 * PI / 180) {
                if (myErrString == "") myErrString = "Perpendicular Angle Range = " + Round(a2 * 180 / PI, 1) + " < 20°";
                return false;
            } else {
                myAngle2OK = true;
            }

            //Make sure that if we are using bundle adjustment, then this marker has been recorded by GTSAM
            //if (myAngleOK && myAngle2OK && Stannah_API.AppPreferences.GTSAMBundleAdjustment && !HasMarkerBeenSeenByGTSAM(MarkerID)) return false;

            return (myAngleOK && myAngle2OK);
        }

        public double MaxAngle(ref clsPoint3d maxAV1, ref clsPoint3d maxAV2) {
            double a;
            double maxA;
            clsPoint3d p1, p2;
            clsPoint3d px, py, pz;

            maxA = 0;
            pz = new clsPoint3d(0, 0, 1.0);
            for (int j = 0; j <= myCameraPoints.Count - 2; j++) {
                p1 = myCameraPoints[j];
                for (int k = j + 1; k <= myCameraPoints.Count - 1; k++) {
                    p2 = myCameraPoints[k];
                    py = new clsPoint3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2).Point2D().Point3d(0);
                    if (IsSameDbl(py.Length, 0)) continue;
                    py.Normalise();
                    px = pz.Cross(py);
                    px.Normalise();
                    p1 = px * p1.Dot(px) + pz * p1.Dot(pz);
                    p1.Normalise();
                    p2 = px * p2.Dot(px) + pz * p2.Dot(pz);
                    p2.Normalise();
                    a = Acos(p1.Dot(p2));
                    if (Abs(a) > maxA) {
                        maxA = Abs(a);
                        maxAV1 = p1.Copy();
                        maxAV2 = p2.Copy();
                    }
                }
            }

            return maxA;
        }

        public double MaxDistance(ref clsPoint3d maxAV1, ref clsPoint3d maxAV2) {
            double d;
            double maxD;
            clsPoint3d p1, p2;
            clsPoint3d px, py, pz;

            maxD = 0;
            pz = new clsPoint3d(0, 0, 1.0);
            for (int j = 0; j <= myCameraPoints.Count - 2; j++) {
                p1 = myCameraPoints[j];
                for (int k = j + 1; k <= myCameraPoints.Count - 1; k++) {
                    p2 = myCameraPoints[k];
                    py = new clsPoint3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2).Point2D().Point3d(0);
                    if (IsSameDbl(py.Length, 0)) continue;
                    py.Normalise();
                    px = pz.Cross(py);
                    px.Normalise();
                    p1 = px * p1.Dot(px) + pz * p1.Dot(pz);
                    p1.Normalise();
                    p2 = px * p2.Dot(px) + pz * p2.Dot(pz);
                    p2.Normalise();
                    d = myCameraPoints[j].Dist(myCameraPoints[k]);
                    if (Abs(d) > maxD) {
                        maxD = Abs(d);
                        maxAV1 = p1.Copy();
                        maxAV2 = p2.Copy();
                    }
                }
            }

            return maxD;
        }

        public double MaxAnglePerpendicular(ref clsPoint3d maxAV1, ref clsPoint3d maxAV2) {
            double a;
            double maxA;
            clsPoint3d py, pz;
            clsPoint3d p1 = null, p2 = null, p3 = null, p4 = null;

            if (MaxAngle(ref p3, ref p4) < myTol) return 0;

            pz = new clsPoint3d(0, 0, 1.0);
            py = pz.Cross(p3);
            if (IsSameDbl(py.Length, 0)) py = pz.Cross(p4);
            if (IsSameDbl(py.Length, 0)) return 0;
            py.Normalise();

            maxA = 0;
            for (int j = 0; j <= myCameraPoints.Count - 2; j++) {
                for (int k = j + 1; k <= myCameraPoints.Count - 1; k++) {
                    p1 = myCameraPoints[j];
                    p1 = py * py.Dot(p1) + pz * pz.Dot(p1);
                    p1.Normalise();
                    p2 = myCameraPoints[k];
                    p2 = py * py.Dot(p2) + pz * pz.Dot(p2);
                    p2.Normalise();
                    a = Acos(p1.Dot(p2));
                    if (Abs(a) > maxA) {
                        maxA = Abs(a);
                        maxAV1 = p1.Copy();
                        maxAV2 = p2.Copy();
                    }
                }
            }

            return maxA;
        }

        public List<clsPoint3d> CameraPoints {
            get { return myCameraPoints; }
        }

        public List<clsPoint3d> SeenFromCameraPoints {
            get { return mySeenFromCameraPoints; }
        }

        public List<clsPoint3d> OriginPoints {
            get { return myPts1; }
        }

        public List<clsPoint3d> EndXAxisPoints {
            get { return myPts2; }
        }

        public List<clsPoint3d> EndYAxisPoints {
            get { return myPts3; }
        }

        public Nullable<System.DateTime> LastTime {
            get { return myLastTime; }
            set { myLastTime = value; }
        }

        public clsPoint3d LastVector {
            get { return myLastVector; }
            set { myLastVector = value.Copy(); }
        }

        public string Label {
            get { return myLabel; }
            set { myLabel = value; }
        }

        public void SetZNormal() {
            VX = myEndXAxis - myOrigin;
            if (IsSameDbl(VX.Length, 0))
                return;
            VX.Normalise();
            VY = myEndYAxis - myOrigin;
            if (IsSameDbl(VY.Length, 0))
                return;
            VY.Normalise();
            VZ = VX.Cross(VY);
            if (IsSameDbl(VZ.Length, 0))
                return;
            VZ.Normalise();
            VY = VZ.Cross(VX);
            if (IsSameDbl(VY.Length, 0))
                return;
            VY.Normalise();
        }

        public clsMarkerPoint2 Copy(bool includeConfirmedFlag = false, bool avoidHistory = false) {
            clsMarkerPoint2 myCopy = new clsMarkerPoint2();
            int i;

            myCopy.MarkerID = myMarkerID;
            myCopy.SeenFromMarkerID = mySeenFromMarkerID;
            myCopy.ActualMarkerID = myActualMarkerID;
            myCopy.Origin = myOrigin;
            myCopy.XAxisPoint = myEndXAxis;
            myCopy.YAxisPoint = myEndYAxis;
            myCopy.Point = myPoint?.Copy();
            myCopy.VX = VX;
            myCopy.VY = VY;
            myCopy.VZ = VZ;
            if (includeConfirmedFlag) myCopy.SetConfirmedFlag(_confirmed);

            foreach (int myID in mySeenFromMarkerIDs) {
                myCopy.SeenFromMarkerIDs.Add(myID);
            }
            foreach (clsPoint3d p1 in myCameraPoints) {
                myCopy.CameraPoints.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in mySeenFromCameraPoints) {
                myCopy.SeenFromCameraPoints.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts1) {
                myCopy.OriginPoints.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts2) {
                myCopy.EndXAxisPoints.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts3) {
                myCopy.EndYAxisPoints.Add(p1.Copy());
            }

            for (i = 0; i <= 15; i++) {
                myCopy.ModelViewMatrix[i] = myModelViewMatrix[i];
            }

            for (i = 0; i < GyroData.Count; i++) {
                myCopy.GyroData.Add(GyroData[i]?.Copy());
            }
            for (i = 0; i < LastGyroData.Count; i++) {
                myCopy.LastGyroData.Add(LastGyroData[i]?.Copy());
            }
            for (i = 0; i < AccelData.Count; i++) {
                myCopy.AccelData.Add(AccelData[i]?.Copy());
            }
            for (i = 0; i < LastAccelData.Count; i++) {
                myCopy.LastAccelData.Add(LastAccelData[i]?.Copy());
            }
            if (myVerticalVect != null) myCopy.VerticalVect = myVerticalVect.Copy();

            myCopy.BulkheadHeight = BulkheadHeight;


            if (!avoidHistory) {
                foreach (clsMarkerPoint2 myHistoricPoint in myHistory) {
                    myCopy.History.Add(myHistoricPoint.Copy());
                }
            }

            return myCopy;
        }

        public void Save(System.IO.StreamWriter sw) {
            sw.WriteLine("MARKER_POINT_SETTINGS");
            sw.WriteLine("MarkerID," + myMarkerID.ToString());
            sw.WriteLine("SeenFromMarkerID," + mySeenFromMarkerID.ToString());
            sw.WriteLine("ActualMarkerID," + myActualMarkerID.ToString());
            if (myVerticalVect != null) {
                sw.WriteLine("VerticalVectorX," + myVerticalVect.X.ToString());
                sw.WriteLine("VerticalVectorY," + myVerticalVect.Y.ToString());
                sw.WriteLine("VerticalVectorZ," + myVerticalVect.Z.ToString());
            }
            sw.WriteLine("BulkheadHeight," + BulkheadHeight.ToString());
            foreach (int myID in mySeenFromMarkerIDs) {
                sw.WriteLine("SeenFromMarkerIDs," + myID.ToString());
            }
            sw.WriteLine("Confirmed," + (_confirmed ? "1" : "0"));
            sw.WriteLine("END_MARKER_POINT_SETTINGS");

            myOrigin.Save(sw);
            myEndXAxis.Save(sw);
            myEndYAxis.Save(sw);
            myPoint.Save(sw);
            VX.Save(sw);
            VY.Save(sw);
            VZ.Save(sw);

            sw.WriteLine(myCameraPoints.Count);
            foreach (clsPoint3d p1 in myCameraPoints) {
                p1.Save(sw);
            }

            sw.WriteLine(mySeenFromCameraPoints.Count);
            foreach (clsPoint3d p1 in mySeenFromCameraPoints) {
                p1.Save(sw);
            }

            sw.WriteLine(myPts1.Count);
            foreach (clsPoint3d p1 in myPts1) {
                p1.Save(sw);
            }

            sw.WriteLine(myPts2.Count);
            foreach (clsPoint3d p1 in myPts2) {
                p1.Save(sw);
            }

            sw.WriteLine(myPts3.Count);
            foreach (clsPoint3d p1 in myPts3) {
                p1.Save(sw);
            }

            sw.WriteLine(GyroData.Count);
            for (int i = 0; i < GyroData.Count; i++) {
                GyroData[i].Save(sw);
            }
            sw.WriteLine(LastGyroData.Count);
            for (int i = 0; i < LastGyroData.Count; i++) {
                LastGyroData[i].Save(sw);
            }
            sw.WriteLine(AccelData.Count);
            for (int i = 0; i < AccelData.Count; i++) {
                AccelData[i].Save(sw);
            }
            sw.WriteLine(LastAccelData.Count);
            for (int i = 0; i < LastAccelData.Count; i++) {
                LastAccelData[i].Save(sw);
            }

            sw.WriteLine(myHistory.Count);
            foreach (clsMarkerPoint2 myHistoricPoint in myHistory) {
                myHistoricPoint.Save(sw);
            }

        }

        public void Load(System.IO.StreamReader sr) {
            clsPoint3d p1;

            //var version = myPGLoadedVersion.Split('.');
            //var nLoadedVersionMajor = version.Count() >= 1 ? Convert.ToInt32(version[0]) : 0;
            //var nLoadedVersionMinor = version.Count() >= 2 ? Convert.ToInt32(version[1]) : 0;

            while (sr.EndOfStream == false) {
                var myLine = sr.ReadLine();
                if (myLine == "END_MARKER_POINT_SETTINGS")
                    break;
                if (myLine.IndexOf(",") > -1) {
                    var mySplit = myLine.Split(',');
                    if (mySplit.GetUpperBound(0) == 1) {
                        if (mySplit[0] == "MarkerID")
                            myMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "SeenFromMarkerID")
                            mySeenFromMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "SeenFromMarkerIDs")
                            mySeenFromMarkerIDs.Add(Convert.ToInt32(mySplit[1]));
                        if (mySplit[0] == "ActualMarkerID")
                            myActualMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "VerticalVectorX") {
                            if (myVerticalVect == null) myVerticalVect = new clsPoint3d();
                            myVerticalVect.X = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorY") {
                            if (myVerticalVect == null) myVerticalVect = new clsPoint3d();
                            myVerticalVect.Y = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorZ") {
                            if (myVerticalVect == null) myVerticalVect = new clsPoint3d();
                            myVerticalVect.Z = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "BulkheadHeight") {
                            BulkheadHeight = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "Confirmed") {
                            _confirmed = (mySplit[1] == "1");
                        }
                    }
                }
            }

            if (mySeenFromMarkerIDs.Contains(mySeenFromMarkerID) == false) { mySeenFromMarkerIDs.Add(mySeenFromMarkerID); }

            myOrigin.Load(sr);
            myEndXAxis.Load(sr);
            myEndYAxis.Load(sr);
            myPoint.Load(sr);
            VX.Load(sr);
            VY.Load(sr);
            VZ.Load(sr);

            var n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myCameraPoints.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                mySeenFromCameraPoints.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts1.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts2.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts3.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                GyroData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                LastGyroData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                AccelData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                LastAccelData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                var myHistoricPoint = new clsMarkerPoint2();
                myHistoricPoint.Load(sr);
                myHistory.Add(myHistoricPoint);
            }
        }

        public void SetEndPointBasedOnZVectors() {
            int n = 0;

            if (myPts1.Count == 0) return;

            //Let's try without this for a while:
            //CompactMarkers()

            myOrigin = ProcessPointListPair(myPts1, mySeenFromCameraPoints, ref n);
            if (myOrigin == null || myOrigin.Length < myTol || n == 0) goto quitOut;

            VX = AverageAxis(myPts1, myPts2);
            if (VX == null || VX.Length < myTol) goto quitOut;
            VX.Normalise();
            myEndXAxis = myOrigin + VX;

            VY = AverageAxis(myPts1, myPts3);
            if (VY == null || VY.Length < myTol) goto quitOut;
            VY.Normalise();
            myEndYAxis = myOrigin + VY;

            SetZNormal();
            myEndXAxis = myOrigin + VX;
            myEndYAxis = myOrigin + VY;

            SetEndPoint();

            return;

            quitOut:
            myOrigin = new clsPoint3d();
            myEndXAxis = new clsPoint3d();
            myEndYAxis = new clsPoint3d();
            myPoint = new clsPoint3d();
        }

        public void SetEndPoint() {
            if (myActualMarkerID < myGFMarkerID) {
                if (myActualMarkerID >= 0 && myActualMarkerID < 50) {
                    myPoint = myOrigin + VX * 160 - VY * 45;
                } else if (myActualMarkerID >= 50 && myActualMarkerID < 100) {
                    myPoint = myOrigin - VX * 160 - VY * 45;
                }
            } else if (myActualMarkerID >= myGFMarkerID && myActualMarkerID <= myRightBulkheadMarkerID) //GF, Step & Bulkhead Markers
              {
                myPoint = myOrigin.Copy();
            } else if (myActualMarkerID >= myDoorHingeRightMarkerID && myActualMarkerID <= myDoorFrameLeftMarkerID) //Door Markers
              {
                myPoint = myOrigin + VY * 65.0;
            } else if (myActualMarkerID >= myObstruct1MarkerID && myActualMarkerID <= myObstruct4MarkerID) //Obstruction Markers
              {
                myPoint = myOrigin + VY * 65.0;
            } else if (myActualMarkerID >= myWall1MarkerID && myActualMarkerID <= myWall4MarkerID) //Wall Markers
              {
                myPoint = myOrigin + VY * 65.0;
            } else {
                myPoint = myOrigin.Copy();
            }
        }

        /// <summary>
        /// Processes the point list pair.
        /// </summary>
        /// <param name="estimatedLocationPoints">The estimated location points (from AR Toolkit).</param>
        /// <param name="axisStartPoints">The axis start point (from the camera).</param>
        /// <param name="axisEndPoints">The axis end point (from the camera).</param>
        /// <param name="myNumPtsCalculated">Nnumber of points calculated.</param>
        /// <returns>The average point of all processed points if successful, otherwise a zero length vector.</returns>
        /// <remarks>estimatedLocationPoints and seenFromCameraPoints must be matched lists.</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">estimatedLocationPoints and seenFromCameraPoints should be matched lists.</exception>
        private clsPoint3d ProcessPointListPair(List<clsPoint3d> estimatedLocationPoints, List<clsPoint3d> seenFromCameraPoints, ref int myNumPtsCalculated) {
            if (estimatedLocationPoints == null || seenFromCameraPoints == null) throw new ArgumentNullException();
            if (estimatedLocationPoints.Count != seenFromCameraPoints.Count) throw new ArgumentException("estimatedLocationPoints and seenFromCameraPoints should be matched lists.");
            myNumPtsCalculated = 0;

            // Take copies of the list, such as not to affect the passed list
            estimatedLocationPoints = estimatedLocationPoints.ToList();
            seenFromCameraPoints = seenFromCameraPoints.ToList();

            // Get the points within 12.5mm of the average
            var points = GetPointsWithinDistanceOfAveragePoint(estimatedLocationPoints, 12.5);

            // Align the matched lists by removing the axis points matched to the estimated location points that have been removed
            var removedPoints = estimatedLocationPoints.Where(p => !points.Contains(p)).ToList();
            removedPoints.ForEach(p => {
                var index = estimatedLocationPoints.IndexOf(p);
                estimatedLocationPoints.RemoveAt(index);
                seenFromCameraPoints.RemoveAt(index);
            });

            // Don't process anything if we have less than 3 usable points
            if (points.Count <= 3) {
                return new clsPoint3d(0, 0, 0);
            }


            var intersectionPoints = new List<clsPoint3d>();
            // Get the first line - don't loop through all as we compare to the next line
            for (var i = 0; i <= estimatedLocationPoints.Count - 2; i++) {
                var line1 = new clsLine3d(estimatedLocationPoints[i], seenFromCameraPoints[i]);

                // Compare to the next line
                for (var j = i + 1; j <= estimatedLocationPoints.Count - 1; j++) {
                    var line2 = new clsLine3d(estimatedLocationPoints[j].Copy(), seenFromCameraPoints[j].Copy());

                    // Get midpoint of each line
                    var p1 = line1.Point;
                    p1.Normalise();
                    var p2 = line2.Point;
                    p2.Normalise();

                    // Determine angle between lines
                    var a = Acos(p1.Dot(p2)) * 180 / PI;
                    if (a > myPGAngleTol) {
                        // Angle is above minimum threshold, so determine the point on each line which is closest to the other line
                        var p3 = line1.ClosestPointToLine(line2);
                        var p4 = line2.ClosestPointToLine(line1);

                        // Determine the distance between the closest points
                        var d = p3.Dist(p4);
                        if (d < myPGPointTol) {
                            // Distance is below threshold so add the intersection point as the midpoint between them
                            intersectionPoints.Add(new clsPoint3d((p3.X + p4.X) / 2, (p3.Y + p4.Y) / 2, (p3.Z + p4.Z) / 2));
                        }
                    }
                }
            }

            var intersectionPointsToFind = 10;
            if (intersectionPoints.Count >= intersectionPointsToFind) {
                myNumPtsCalculated = intersectionPoints.Count;
                return GetAveragePoint(intersectionPoints);
            } else {
                DebugStringList.Add($"{intersectionPoints.Count} / {intersectionPointsToFind}");
            }
            return new clsPoint3d(0, 0, 0);
        }

        /// <summary>
        /// Averages the axis.
        /// </summary>
        /// <param name="axisStartPoints">The axis start points.</param>
        /// <param name="axisEndPoints">The axis end points.</param>
        /// <param name="tolerance">The tolerance (maximum distance away from the average point that is allowable, in mm).</param>
        /// <param name="pointsToMatch">The number of points to match to the average (within the tolerance).</param>
        /// <returns>
        /// The averaged point, or a zero length vector if one can not be found within the specified tolerance.
        /// </returns>
        /// <exception cref="ArgumentException">Axis start and end point lists are not matched.</exception>
        private clsPoint3d AverageAxis(List<clsPoint3d> axisStartPoints, List<clsPoint3d> axisEndPoints, double tolerance = 0.5, int pointsToMatch = 8) {
            if (axisStartPoints == null || axisEndPoints == null) throw new ArgumentNullException("Axis start or end point lists are null.");
            if (axisStartPoints.Count != axisEndPoints.Count) throw new ArgumentException("Axis start and end point lists are not matched.");

            var points = new List<clsPoint3d>();
            for (var i = 0; i <= axisStartPoints.Count - 1; i++) {
                points.Add(axisEndPoints[i] - axisStartPoints[i]);
            }

            // Return the point (if we have more than 7 points within the tolerance)
            points = GetPointsWithinDistanceOfAveragePoint(points, tolerance);
            if (points.Count >= pointsToMatch) return GetAveragePoint(points);
            return new clsPoint3d(0, 0, 0);
        }

        /// <summary>
        /// Gets the points which are within the given tolerance of the average point.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="allowableDistance">The maximum allowable distance away from the average point.</param>
        /// <returns>The points which are within the given tolerance of the average point.</returns>
        private List<clsPoint3d> GetPointsWithinDistanceOfAveragePoint(List<clsPoint3d> points, double allowableDistance) {
            points = points.ToList(); // Take a copy of the points list so as not to affect the passed list

            // Discard points that are too far from the average (> tolerance)
            while (points != null && points.Any()) {
                var averagePoint = GetAveragePoint(points);
                var maxDistanceAway = points.Select(p => p.Dist(averagePoint)).Max();
                if (maxDistanceAway <= allowableDistance) break;
                points.RemoveAll(p => p.Dist(averagePoint) == maxDistanceAway);
            }
            return points;
        }

        /// <summary>
        /// Gets the average point.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>
        /// The average point.
        /// </returns>
        private clsPoint3d GetAveragePoint(List<clsPoint3d> points) {
            if (points == null) throw new ArgumentNullException("points is null");
            return new clsPoint3d(points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z));
        }

        /// <summary>
        /// Gets the maximum distance between the points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>The maximum distance between the points.</returns>
        public double MaxSpread(List<clsPoint3d> points) {
            return points.Max(p => points.Max(p1 => p.Dist(p1)));
        }

        public bool AngleXYIsOK(ref double myMaxAngle, ref string myErrorString) {
            double a;
            List<double> myAngles = new List<double>();
            int j;
            int k = 0;
            clsPoint3d p1;
            clsPoint p3;
            int i1 = 0;
            int i2 = 0;
            bool isOK;
            double a1;
            double a2;
            double myAngleSoFar;

            myMaxAngle = 0;
            myErrorString = "";

            for (j = 0; j <= myCameraPoints.Count - 1; j++) {
                p1 = myCameraPoints[j];
                p1.Normalise();
                p3 = p1.Point2D();
                if (p3.Length > 0.1) {
                    p3.Normalise();
                    myAngles.Add(p3.Angle(true));
                }
            }
            if (myAngles.Count <= 1) {
                myErrorString = "Not Enough Measurements Taken";
                return false;
            }

            myAngles.Sort();
            for (j = 0; j <= myAngles.Count - 2; j++) {
                for (k = j + 1; k <= myAngles.Count - 1; k++) {
                    a = Min(myAngles[k] - myAngles[j], myAngles[j] + 2 * PI - myAngles[k]);
                    if (a > myMaxAngle) {
                        i1 = j;
                        i2 = k;
                        myMaxAngle = Abs(a);
                    }
                }
            }
            if (myMaxAngle < 15 * PI / 180) {
                myErrorString = "Horizontal Angle Range < 15°";
                return false;
            }

            //Now check that we can get from one angle to the next in jumps of 90 degrees or less
            isOK = true;
            myAngleSoFar = 0;
            for (j = i1; j <= i2 - 1; j++) {
                if (myAngles[j + 1] - myAngles[j] > 120 * PI / 180 + myTol) {
                    isOK = false;
                    break; // TODO: might not be correct. Was : Exit For
                } else {
                    myAngleSoFar = myAngleSoFar + myAngles[j + 1] - myAngles[j];
                    if (myAngleSoFar > 90 * PI / 180 + myTol)
                        return true;
                }
            }
            if (isOK)
                return true;

            try {
                j = i2;
                myAngleSoFar = 0;
                while (j != i1) {
                    k = j + 1;
                    if (k > myAngles.Count - 1)
                        k = 0;
                    a1 = myAngles[j];
                    a2 = myAngles[k];
                    if (a2 < a1)
                        a2 = a2 + 2 * PI;
                    if ((a2 - a1) > 120 * PI / 180 + myTol) {
                        myErrorString = "Horizontal Angle Range Has > 120° Gap";
                        return false;
                    } else {
                        myAngleSoFar = myAngleSoFar + a2 - a1;
                        if (myAngleSoFar > 15 * PI / 180 + myTol)
                            return true;
                    }
                    j = j + 1;
                    if (j > myAngles.Count - 1)
                        j = 0;
                }

            } catch (Exception ex) {
                string s = ex.ToString();
            }

            return true;
        }

        public void AngleRangeXY(ref double a1, ref double a2, ref List<double> myAngles) {
            double a;
            double maxA;
            clsPoint3d p1;
            clsPoint p3;
            int j;
            int k;

            a1 = 0;
            a2 = 0;
            myAngles = new List<double>();

            for (j = 0; j <= myCameraPoints.Count - 1; j++) {
                p1 = myCameraPoints[j];
                p1.Normalise();
                p3 = p1.Point2D();
                if (p3.Length > 0.1) {
                    p3.Normalise();
                    myAngles.Add(p3.Angle(true));
                }
            }
            if (myAngles.Count <= 1)
                return;

            myAngles.Sort();
            maxA = 0;
            for (j = 0; j <= myAngles.Count - 1; j++) {
                k = j + 1;
                if (k < myAngles.Count) {
                    a = myAngles[k] - myAngles[j];
                    if (a > maxA) {
                        maxA = a;
                        a1 = myAngles[j];
                        a2 = myAngles[k];
                    }
                    if (myAngles.Count == 2) {
                        a = myAngles[j] + 2 * PI - myAngles[k];
                        if (a > maxA) {
                            maxA = a;
                            a1 = myAngles[k];
                            a2 = myAngles[j];
                        }
                    }
                } else if (myAngles.Count > 2) {
                    k = 0;
                    a = myAngles[k] + 2 * PI - myAngles[j];
                    if (a > maxA) {
                        maxA = a;
                        a1 = myAngles[j];
                        a2 = myAngles[k] + 2 * PI;
                    }
                }
            }
        }

        public void Clear() {
            myOrigin = new clsPoint3d();
            myEndXAxis = new clsPoint3d();
            myEndYAxis = new clsPoint3d();
            myPoint = new clsPoint3d();
            VX = new clsPoint3d();
            VY = new clsPoint3d();
            VZ = new clsPoint3d();
            mySeenFromCameraPoints.Clear();
            myPts1.Clear();
            myPts2.Clear();
            myPts3.Clear();
            myCameraPoints.Clear();
            GyroData.Clear();
            LastGyroData.Clear();
            AccelData.Clear();
            LastAccelData.Clear();
            myHistory.Clear();
            myLastTime = null;
            myLastVector = null;
            FirstPointRemoved = false;
            mySeenFromMarkerIDs.Clear();
        }
    }

    public class MarkerPoint2Comparer : System.Collections.Generic.IComparer<clsMarkerPoint2>
    {

        int System.Collections.Generic.IComparer<clsMarkerPoint2>.Compare(clsMarkerPoint2 myItem1, clsMarkerPoint2 myItem2) {
            double myZTol;

            myZTol = 25;
            if (Abs(myItem1.Point.Z - myItem2.Point.Z) < myZTol) {
                if (myItem1.MarkerID < myItem2.MarkerID) {
                    return -1;
                } else if (myItem1.MarkerID == myItem2.MarkerID) {
                    return 0;
                } else {
                    return 1;
                }
            } else if (myItem1.Point.Z < myItem2.Point.Z) {
                return -1;
            } else {
                return 1;
            }
        }
    }

    public class SuspectedMarkerPoint2Comparer : System.Collections.Generic.IComparer<clsMarkerPoint2>
    {

        int System.Collections.Generic.IComparer<clsMarkerPoint2>.Compare(clsMarkerPoint2 myItem1, clsMarkerPoint2 myItem2) {
            if (myItem1.MarkerID < myItem2.MarkerID) {
                return -1;
            } else if (myItem1.MarkerID == myItem2.MarkerID) {
                if (myItem1.SeenFromMarkerID < myItem2.SeenFromMarkerID) {
                    return -1;
                } else if (myItem1.SeenFromMarkerID == myItem2.SeenFromMarkerID) {
                    return 0;
                } else {
                    return 1;
                }
            } else {
                return 1;
            }
        }
    }
}

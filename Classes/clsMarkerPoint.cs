using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;
using static BatchProcess.mdlGeometry;
using static BatchProcess.mdlRecognise;

namespace BatchProcess {
    public class clsMarkerPoint {
        int myMarkerID;
        int mySeenFromMarkerID;
        int myActualMarkerID; //For when we renumber the markers, e.g. due to stitching a new flight

        clsPoint3d pt1 = new clsPoint3d(); //Origin Point
        clsPoint3d pt2 = new clsPoint3d(); //End of X Axis
        clsPoint3d pt3 = new clsPoint3d(); //End of Y Axis
        clsPoint3d pt4 = new clsPoint3d(); //End of Z Axis
        clsPoint3d pt5 = new clsPoint3d(); //Tip of marker

        clsPoint3d myVx = new clsPoint3d();
        clsPoint3d myVy = new clsPoint3d();
        clsPoint3d myVz = new clsPoint3d();

        List<clsPoint3d> myCameraPoints = new List<clsPoint3d>(); //Location of camera, in coordinate system of this marker
        List<clsPoint3d> myPts1a = new List<clsPoint3d>(); //Origin Point. Vector with z=0, seen in the SeenFrom Matrix
        List<clsPoint3d> myPts1b = new List<clsPoint3d>(); //Origin Point. Vector with z=1, seen in the SeenFrom Matrix
        List<clsPoint3d> myPts1 = new List<clsPoint3d>(); //Origin Point
        List<clsPoint3d> myPts2 = new List<clsPoint3d>(); //End of X Axis
        List<clsPoint3d> myPts3 = new List<clsPoint3d>(); //End of Y Axis

        List<clsPoint3d> myPts6a = new List<clsPoint3d>(); //Origin Point. Vector with z=0, seen in this matrix
        List<clsPoint3d> myPts6b = new List<clsPoint3d>(); //Origin Point. Vector with z=1, seen in this matrix

        clsPoint3d myVerticalVect = null;

        float[] myModelViewMatrix = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        List<clsMarkerPoint> myHistory = new List<clsMarkerPoint>();
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

        public int SeenFromMarkerID {
            get { return mySeenFromMarkerID; }
            set { mySeenFromMarkerID = value; }
        }

        public int ActualMarkerID {
            get { return myActualMarkerID; }
            set { myActualMarkerID = value; }
        }

        public clsPoint3d Origin {
            get { return pt1; }
            set { pt1 = value.Copy(); }
        }

        public clsPoint3d XAxisPoint {
            get { return pt2; }
            set { pt2 = value.Copy(); }
        }

        public clsPoint3d YAxisPoint {
            get { return pt3; }
            set { pt3 = value.Copy(); }
        }

        public clsPoint3d ZAxisPoint {
            get { return pt4; }
            set { pt4 = value.Copy(); }
        }

        public clsPoint3d Point {
            get { return pt5; }
            set { pt5 = value.Copy(); }
        }

        public clsPoint3d VX {
            get { return myVx; }
            set { myVx = value.Copy(); }
        }

        public clsPoint3d VY {
            get { return myVy; }
            set { myVy = value.Copy(); }
        }

        public clsPoint3d VZ {
            get { return myVz; }
            set { myVz = value.Copy(); }
        }

        public clsPoint3d VerticalVect {
            get { return myVerticalVect; }
            set { myVerticalVect = null; if (value != null) myVerticalVect = value.Copy(); }
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

        public List<clsMarkerPoint> History {
            get { return myHistory; }
        }

        public clsMarkerPoint()
        {
            GyroData = new List<clsPoint3d>();
            LastGyroData = new List<clsPoint3d>();
            AccelData = new List<clsPoint3d>();
            LastAccelData = new List<clsPoint3d>();
            myVx = new clsPoint3d(1, 0, 0);
            myVy = new clsPoint3d(0, 1, 0);
            myVz = new clsPoint3d(0, 0, 1);
        }

        //Public Sub New(aMarkerID As Integer)
        //    myMarkerID = aMarkerID
        //End Sub

        public clsMarkerPoint(int aMarkerID, int aSeenFromID)
        {
            myMarkerID = aMarkerID;
            myActualMarkerID = myMarkerID;
            mySeenFromMarkerID = aSeenFromID;
            GyroData = new List<clsPoint3d>();
            LastGyroData = new List<clsPoint3d>();
            AccelData = new List<clsPoint3d>();
            LastAccelData = new List<clsPoint3d>();
        }

        public bool OKToConfirm(ref string myErrString, ref clsPoint3d maxAV1, ref clsPoint3d maxAV2, ref clsPoint3d maxAV3, ref clsPoint3d maxAV4, ref bool myAngleOK, ref bool myAngle2OK, ref double a1, ref double a2)
        {
            myErrString = "";
            myAngleOK = false;
            myAngle2OK = false;

            a1 = MaxAngle(ref maxAV1, ref maxAV2);
            if (a1 < 20 * PI / 180) {
                myErrString = "Angle Range = " + Round(a1 * 180 / PI, 1) + " < 20°";
            } else {
                myAngleOK = true;
            }

            a2 = MaxAnglePerpendicular(ref maxAV3, ref maxAV4);
            a2 = 25 * PI / 180;
            if (a2 < 20 * PI / 180) {
                if (myErrString == "") myErrString = "Perpendicular Angle Range = " + Round(a2 * 180 / PI, 1) + " < 20°";
                return false;
            } else {
                myAngle2OK = true;
            }

            return (myAngleOK && myAngle2OK);
        }

        public double MaxAngle(ref clsPoint3d maxAV1, ref clsPoint3d maxAV2)
        {
            double a;
            double maxA;
            clsPoint3d p1, p2;
            clsPoint3d px, py, pz;

            maxA = 0;
            pz = new clsPoint3d(0, 0, 1.0);
            for (int j = 0; j <= myPts1a.Count - 2; j++) {
                for (int k = j + 1; k <= myPts1a.Count - 1; k++) {
                    p1 = myPts1a[j] - myPts1b[j];
                    p2 = myPts1a[k] - myPts1b[k];
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

        public double MaxAnglePerpendicular(ref clsPoint3d maxAV1, ref clsPoint3d maxAV2)
        {
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
            for (int j = 0; j <= myPts1a.Count - 2; j++) {
                for (int k = j + 1; k <= myPts1a.Count - 1; k++) {
                    p1 = myPts1a[j] - myPts1b[j];
                    p1 = py * py.Dot(p1) + pz * pz.Dot(p1);
                    p1.Normalise();
                    p2 = myPts1a[k] - myPts1b[k];
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

        public List<clsPoint3d> Points1a {
            get { return myPts1a; }
        }

        public List<clsPoint3d> Points1b {
            get { return myPts1b; }
        }

        public List<clsPoint3d> Points1 {
            //I don't think this is currently used
            get { return myPts1; }
        }

        public List<clsPoint3d> Points2 {
            get { return myPts2; }
        }

        public List<clsPoint3d> Points3 {
            get { return myPts3; }
        }

        public List<clsPoint3d> Points6a {
            get { return myPts6a; }
        }

        public List<clsPoint3d> Points6b {
            get { return myPts6b; }
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

        public void SetZNormal()
        {
            myVx = pt2 - pt1;
            if (IsSameDbl(myVx.Length, 0))
                return;
            myVx.Normalise();
            myVy = pt3 - pt1;
            if (IsSameDbl(myVy.Length, 0))
                return;
            myVy.Normalise();
            myVz = myVx.Cross(VY);
            if (IsSameDbl(myVz.Length, 0))
                return;
            myVz.Normalise();
            myVy = myVz.Cross(myVx);
            if (IsSameDbl(myVy.Length, 0))
                return;
            myVy.Normalise();
        }

        public clsMarkerPoint Copy()
        {
            clsMarkerPoint myCopy = new clsMarkerPoint();
            int i;

            myCopy.MarkerID = myMarkerID;
            myCopy.SeenFromMarkerID = mySeenFromMarkerID;
            myCopy.ActualMarkerID = myActualMarkerID;
            myCopy.Origin = pt1.Copy();
            myCopy.XAxisPoint = pt2.Copy();
            myCopy.YAxisPoint = pt3.Copy();
            myCopy.ZAxisPoint = pt4.Copy();
            myCopy.Point = pt5.Copy();
            myCopy.VX = myVx.Copy();
            myCopy.VY = myVy.Copy();
            myCopy.VZ = myVz.Copy();

            foreach (clsPoint3d p1 in myCameraPoints) {
                myCopy.CameraPoints.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts1a) {
                myCopy.Points1a.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts1b) {
                myCopy.Points1b.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts1) {
                myCopy.Points1.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts2) {
                myCopy.Points2.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts3) {
                myCopy.Points3.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts6a) {
                myCopy.Points6a.Add(p1.Copy());
            }
            foreach (clsPoint3d p1 in myPts6b) {
                myCopy.Points6b.Add(p1.Copy());
            }

            for (i = 0; i <= 15; i++) {
                myCopy.ModelViewMatrix[i] = myModelViewMatrix[i];
            }

            for (i = 0; i < GyroData.Count; i++) {
                myCopy.GyroData.Add(GyroData[i].Copy());
            }
            for (i = 0; i < LastGyroData.Count; i++) {
                myCopy.LastGyroData.Add(LastGyroData[i].Copy());
            }
            for (i = 0; i < AccelData.Count; i++) {
                myCopy.AccelData.Add(AccelData[i].Copy());
            }
            for (i = 0; i < LastAccelData.Count; i++) {
                myCopy.LastAccelData.Add(LastAccelData[i].Copy());
            }
            if (myVerticalVect != null) myCopy.VerticalVect = myVerticalVect.Copy();

            myCopy.BulkheadHeight = BulkheadHeight;
            myCopy.FirstPointRemoved = FirstPointRemoved;

            foreach (clsMarkerPoint myHistoricPoint in myHistory) {
                myCopy.History.Add(myHistoricPoint.Copy());
            }

            return myCopy;
        }

        public void Save(System.IO.StreamWriter sw)
        {
            sw.WriteLine("MARKER_POINT_SETTINGS");
            sw.WriteLine("MarkerID," + myMarkerID);
            sw.WriteLine("SeenFromMarkerID," + mySeenFromMarkerID);
            sw.WriteLine("ActualMarkerID," + myActualMarkerID);
            if (myVerticalVect != null) {
                sw.WriteLine("VerticalVectorX," + myVerticalVect.X);
                sw.WriteLine("VerticalVectorY," + myVerticalVect.Y);
                sw.WriteLine("VerticalVectorZ," + myVerticalVect.Z);
                sw.WriteLine("BulkheadHeight," + BulkheadHeight);
            }
            sw.WriteLine("END_MARKER_POINT_SETTINGS");

            pt1.Save(sw);
            pt2.Save(sw);
            pt3.Save(sw);
            pt4.Save(sw);
            pt5.Save(sw);
            myVx.Save(sw);
            myVy.Save(sw);
            myVz.Save(sw);

            sw.WriteLine(myPts1a.Count);
            foreach (clsPoint3d p1 in myPts1a) {
                p1.Save(sw);
            }

            sw.WriteLine(myPts1b.Count);
            foreach (clsPoint3d p1 in myPts1b) {
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

            sw.WriteLine(myPts6a.Count);
            foreach (clsPoint3d p1 in myPts6a) {
                p1.Save(sw);
            }

            sw.WriteLine(myPts6b.Count);
            foreach (clsPoint3d p1 in myPts6b) {
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
            foreach (clsMarkerPoint myHistoricPoint in myHistory) {
                myHistoricPoint.Save(sw);
            }

        }

        public void Load(System.IO.StreamReader sr)
        {
            clsPoint3d p1;
            clsMarkerPoint myHistoricPoint;
            string myLine;
            string[] mySplit;
            int i;
            int n;

            while (sr.EndOfStream == false) {
                myLine = sr.ReadLine();
                if (myLine == "END_MARKER_POINT_SETTINGS")
                    break; // TODO: might not be correct. Was : Exit While
                if (myLine.IndexOf(",") > -1) {
                    mySplit = myLine.Split(',');
                    if (mySplit.GetUpperBound(0) == 1) {
                        if (mySplit[0] == "MarkerID")
                            myMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "SeenFromMarkerID")
                            mySeenFromMarkerID = Convert.ToInt32(mySplit[1]);
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
                    }
                }
            }

            pt1.Load(sr);
            pt2.Load(sr);
            pt3.Load(sr);
            pt4.Load(sr);
            pt5.Load(sr);
            myVx.Load(sr);
            myVy.Load(sr);
            myVz.Load(sr);

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts1a.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts1b.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts1.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts2.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts3.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts6a.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                myPts6b.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                GyroData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                LastGyroData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                AccelData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                LastAccelData.Add(p1);
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 1; i <= n; i++) {
                myHistoricPoint = new clsMarkerPoint();
                myHistoricPoint.Load(sr);
                myHistory.Add(myHistoricPoint);
            }
        }

        public void SetEndPointBasedOnZVectors(bool forceResult = false)
        {
            int n = 0;

            if (myPts1a.Count <= 3) return;

            //Let's try without this for a while:
            //CompactMarkers()

            try {
                //pt1 = ProcessPointListPair(myPts1a, myPts1b, ref n);
                //pt1 = AverageOriginPoint(myPts1, ref n);
                pt1 = ProcessPointListPairFromARToolkit(myPts1, myPts1a, myPts1b, ref n);
                //pt1 = ProcessPointListPairTest(myPts1, myPts1a, myPts1b, ref n, forceResult);
            } catch (Exception ex) {
                string s = ex.ToString();
            }
            if (pt1.Length < myTol || n == 0) goto quitOut;

            myVx = AverageAxis(myPts1, myPts2);
            if (myVx.Length < myTol) goto quitOut;
            myVx.Normalise();
            pt2 = pt1 + myVx;

            myVy = AverageAxis(myPts1, myPts3);
            if (myVy.Length < myTol) goto quitOut;
            myVy.Normalise();
            pt3 = pt1 + myVy;

            SetZNormal();
            pt2 = pt1 + myVx;
            pt3 = pt1 + myVy;
            pt4 = pt1 + myVz;

            if (UseNewStyleMarkers == false && myMarkerID < myGFMultiMarkerID) {
                if ((myMarkerID >= 0 && myMarkerID < 20) || (myMarkerID > 39 && myMarkerID < 60)) {
                    pt5 = pt1 + myVx * 237.5 - myVy * 32.5;
                } else if ((myMarkerID >= 20 && myMarkerID < 40) || (myMarkerID > 59 && myMarkerID < 80)) {
                    pt5 = pt1 + myVx * 237.5 + myVy * 32.5;
                }
            } else if (myMarkerID < myGFMultiMarkerID) {
                if (myMarkerID >= 0 && myMarkerID < 50) {
                    pt5 = pt1 + myVx * 225 - myVy * 45;
                } else if (myMarkerID >= 50 && myMarkerID < 100) {
                    pt5 = pt1 + myVx * 225 + myVy * 45;
                }
            } else if (myMarkerID >= myGFMultiMarkerID && myMarkerID <= myRightBulkheadMarkerID) //GF, Step & Bulkhead Markers
              {
                pt5 = pt1.Copy();
            } else if (myMarkerID >= myDoorHingeRightMarkerID && myMarkerID <= myDoorFrameLeftMarkerID) //Door Markers
              {
                pt5 = pt1 + myVy * 65.0;
            } else if (myMarkerID >= myObstruct1MarkerID && myMarkerID <= myObstruct4MarkerID) //Obstruction Markers
              {
                pt5 = pt1 + myVy * 65.0;
            } else if (myMarkerID >= myWall1MarkerID && myMarkerID <= myWall4MarkerID) //Wall Markers
              {
                pt5 = pt1 + myVy * 65.0;
            } else {
                pt5 = pt1.Copy();
            }

            return;

            quitOut:
            pt1 = new clsPoint3d();
            pt2 = new clsPoint3d();
            pt3 = new clsPoint3d();
            pt4 = new clsPoint3d();
            pt5 = new clsPoint3d();
        }


        private clsPoint3d ProcessPointListPair(List<clsPoint3d> pts1a, List<clsPoint3d> pts1b, ref int myNumPtsCalculated)
        {
            clsLine3d l1;
            clsLine3d l2;
            int i, j;
            clsPoint3d p1;
            clsPoint3d p2;
            clsPoint3d p3;
            clsPoint3d p4;
            //clsPoint3d p5;
            //clsPoint3d p6;
            //clsPoint3d p7;
            //clsPoint3d p8;
            double a;
            double d;
            List<clsPoint3d> pts = new List<clsPoint3d>();

            //myCalculatedPoints.Clear();

            if (pts1a.Count <= 3) {
                myNumPtsCalculated = 0;
                return new clsPoint3d();
            }
            
            for (i = 0; i <= pts1a.Count - 2; i++) {
                l1 = new clsLine3d(pts1a[i], pts1b[i]);

                for (j = i + 1; j <= pts1a.Count - 1; j++) {
                    l2 = new clsLine3d(pts1a[j].Copy(), pts1b[j].Copy());

                    p1 = l1.Point;
                    p1.Normalise();
                    p2 = l2.Point;
                    p2.Normalise();

                    a = Acos(p1.Dot(p2)) * 180 / PI;
                    if (a > myPGAngleTol) {
                        p3 = l1.ClosestPointToLine(l2);
                        p4 = l2.ClosestPointToLine(l1);
                        d = p3.Dist(p4);
                        if (d < myPGPointTol) {
                            pts.Add(new clsPoint3d((p3.X + p4.X) / 2, (p3.Y + p4.Y) / 2, (p3.Z + p4.Z) / 2));
                        }
                    }
                }
            }
                
            if (pts.Count > 5) {
                myNumPtsCalculated = pts.Count;
                return new clsPoint3d(pts.Average(p => p.X), pts.Average(p => p.Y), pts.Average(p => p.Z));
            }
                
            myNumPtsCalculated = 0;
            return new clsPoint3d();
        }

        private clsPoint3d ProcessPointListPairFromARToolkit(List<clsPoint3d> pts1, List<clsPoint3d> pts1a, List<clsPoint3d> pts1b, ref int myNumPtsCalculated)
        {
            clsLine3d l1;
            clsLine3d l2;
            int i;
            int j;
            clsPoint3d p1;
            clsPoint3d p2;
            clsPoint3d p3;
            clsPoint3d p4;
            double a;
            double d;
            List<clsPoint3d> pts = new List<clsPoint3d>();
            List<clsPoint3d> pts1z = new List<clsPoint3d>(), pts1x = new List<clsPoint3d>(), pts1y = new List<clsPoint3d>();
            clsPoint3d pt;

            //Discard points too far from the average (>12.5mm)
            i = 0;
            pts1z.AddRange(pts1.ToArray());
            pts1x.AddRange(pts1a.ToArray());
            pts1y.AddRange(pts1b.ToArray());
            pt = new clsPoint3d(pts1z.Average(p => p.X), pts1z.Average(p => p.Y), pts1z.Average(p => p.Z));
            while (i <= pts1x.Count - 1) {
                if (pts1z[i].Dist(pt) > 12.5) {
                    pts1x.RemoveAt(i);
                    pts1y.RemoveAt(i);
                    pts1z.RemoveAt(i);
                    pt = new clsPoint3d(pts1z.Average(p => p.X), pts1z.Average(p => p.Y), pts1z.Average(p => p.Z));
                }
                else {
                    i = i + 1;
                }
            }
            if (pts1x.Count <= 3) {
                myNumPtsCalculated = 0;
                return new clsPoint3d();
            }
            
            for (i = 0; i <= pts1x.Count - 2; i++) {
                l1 = new clsLine3d(pts1x[i], pts1y[i]);

                for (j = i + 1; j <= pts1x.Count - 1; j++) {
                    l2 = new clsLine3d(pts1x[j].Copy(), pts1y[j].Copy());

                    p1 = l1.Point;
                    p1.Normalise();
                    p2 = l2.Point;
                    p2.Normalise();

                    a = Acos(p1.Dot(p2)) * 180 / PI;
                    if (a > myPGAngleTol) {
                        p3 = l1.ClosestPointToLine(l2);
                        p4 = l2.ClosestPointToLine(l1);
                        d = p3.Dist(p4);
                        if (d < myPGPointTol) {
                            pts.Add(new clsPoint3d((p3.X + p4.X) / 2, (p3.Y + p4.Y) / 2, (p3.Z + p4.Z) / 2));
                        }
                    }
                }
            }
            
            if (pts.Count > 15) {
                myNumPtsCalculated = pts.Count;
                return new clsPoint3d(pts.Average(p => p.X), pts.Average(p => p.Y), pts.Average(p => p.Z));
            }

            myNumPtsCalculated = 0;
            return new clsPoint3d();
        }

        private clsPoint3d ProcessPointListPairTest(List<clsPoint3d> pts1, List<clsPoint3d> pts1a, List<clsPoint3d> pts1b, ref int myNumPtsCalculated, bool forceResult = false)
        {
            clsLine3d l1;
            clsLine3d l2;
            int i, j;
            clsPoint3d p1;
            clsPoint3d p2;
            clsPoint3d p3;
            clsPoint3d p4;
            //clsPoint3d p5;
            //clsPoint3d p6;
            //clsPoint3d p7;
            //clsPoint3d p8;
            double a;
            double d;
            List<clsPoint3d> pts = new List<clsPoint3d>();
            List<double> myDists = new List<double>();
            List<clsPoint3d> pts1z = new List<clsPoint3d>(), pts1x = new List<clsPoint3d>(), pts1y = new List<clsPoint3d>();
            clsPoint3d pt;
            bool PointsRemoved, PointRemoved;
            double PointRemoveDistance;

            //myCalculatedPoints.Clear();

            if (pts1.Count <= 3) {
                myNumPtsCalculated = 0;
                return new clsPoint3d();
            }

            //Discard points too far from the average (>12.5mm)
            PointsRemoved = true;
            PointRemoveDistance = 12.5;
            while (PointsRemoved) {
                PointsRemoved = false;
                PointRemoved = true;
                pts1x.Clear();
                pts1y.Clear();
                pts1z.Clear();
                pts1z.AddRange(pts1.ToArray());
                pts1x.AddRange(pts1a.ToArray());
                pts1y.AddRange(pts1b.ToArray());
                while (PointRemoved) {
                    PointRemoved = false;
                    pt = new clsPoint3d(pts1z.Average(p => p.x), pts1z.Average(p => p.y), pts1z.Average(p => p.z));
                    myDists.Clear();
                    for (i = 0; i < pts1z.Count; i++) {
                        myDists.Add(pts1z[i].Dist(pt));
                    }
                    d = myDists.Max();
                    if (d > PointRemoveDistance) {
                        i = myDists.IndexOf(d);
                        if (i > -1) {
                            pts1x.RemoveAt(i);
                            pts1y.RemoveAt(i);
                            pts1z.RemoveAt(i);
                            PointRemoved = true;
                            PointsRemoved = true;
                        }
                    }
                }

                if (pts1x.Count <= 3 && PointsRemoved) {
                    PointRemoveDistance = PointRemoveDistance + 5;
                    continue;
                }

                ////Discard points too far from the average (>5mm)
                //i = 0;
                //while (i <= xpts1c.Count - 1)
                //{
                //    p5 = new clsPoint3d(xpts1c.Average(p => p.x), xpts1c.Average(p => p.y), xpts1c.Average(p => p.z));
                //    p6 = new clsPoint3d(xpts2c.Average(p => p.x), xpts2c.Average(p => p.y), xpts2c.Average(p => p.z));
                //    p7 = new clsPoint3d(xpts3c.Average(p => p.x), xpts3c.Average(p => p.y), xpts3c.Average(p => p.z));
                //    p8 = new clsPoint3d(xpts5c.Average(p => p.x), xpts5c.Average(p => p.y), xpts5c.Average(p => p.z));
                //    if (xpts1c[i].Dist(p5) > 5 || xpts2c[i].Dist(p6) > 5 || xpts3c[i].Dist(p7) > 5 || xpts5c[i].Dist(p8) > 5)
                //    {
                //        pts1.RemoveAt(i);
                //        pts2.RemoveAt(i);
                //        xpts1c.RemoveAt(i);
                //        xpts2c.RemoveAt(i);
                //        xpts3c.RemoveAt(i);
                //        xpts5c.RemoveAt(i);
                //    }
                //    else
                //    {
                //        i = i + 1;
                //    }
                //}
                //if (xpts1c.Count <= 2)
                //{
                //    myNumPtsCalculated = 0;
                //    return new clsPoint3d();
                //}

                for (i = 0; i <= pts1x.Count - 2; i++) {
                    l1 = new clsLine3d(pts1x[i], pts1y[i]);

                    for (j = i + 1; j <= pts1x.Count - 1; j++) {
                        l2 = new clsLine3d(pts1x[j].Copy(), pts1y[j].Copy());

                        p1 = l1.Point;
                        p1.Normalise();
                        p2 = l2.Point;
                        p2.Normalise();

                        a = Acos(p1.Dot(p2)) * 180 / PI;
                        if (a > myPGAngleTol) {
                            p3 = l1.ClosestPointToLine(l2);
                            p4 = l2.ClosestPointToLine(l1);
                            d = p3.Dist(p4);
                            if (d < myPGPointTol) {
                                pts.Add(new clsPoint3d((p3.X + p4.X) / 2, (p3.Y + p4.Y) / 2, (p3.Z + p4.Z) / 2));
                                //if (myCalculatedPoints.Contains(i) == false)
                                //    myCalculatedPoints.Add(i);
                                //if (myCalculatedPoints.Contains(j) == false)
                                //    myCalculatedPoints.Add(j);
                            }
                        }
                    }
                }

                ////Discard points too far from the average. Remove furthest 25%
                //myNumPtsCalculated = pts.Count;
                //if (pts.Count > 4)
                //{
                //    j = Max(1, Convert.ToInt32(1 * Convert.ToDouble(pts.Count) / 4.0));
                //    for (i = 1; i <= j; i++)
                //    {
                //        myDists.Clear();
                //        p5 = new clsPoint3d(pts.Average(p => p.x), pts.Average(p => p.y), pts.Average(p => p.z));
                //        for (k = 0; k <= pts.Count - 1; k++)
                //        {
                //            myDists.Add(pts[k].Dist(p5));
                //        }

                //        d = myDists.Max();
                //        k = myDists.IndexOf(d);
                //        if (k == -1)
                //            break; // TODO: might not be correct. Was : Exit For
                //        myDists.RemoveAt(k);
                //        pts.RemoveAt(k);
                //        if (pts.Count == 4)
                //            break; // TODO: might not be correct. Was : Exit For
                //    }
                //}

                //Discard points too far from the average ( > 4.25mm)
                //bool isModified = true;
                //while (pts.Count > 2 & isModified)
                //{
                //    isModified = false;
                //    p5 = new clsPoint3d(pts.Average(p => p.X), pts.Average(p => p.Y), pts.Average(p => p.Z));
                //    for (i = 0; i <= pts.Count - 1; i++)
                //    {
                //        if (pts[i].Dist(p5) > 4.25)
                //        {
                //            pts.RemoveAt(i);
                //            isModified = true;
                //            break;
                //        }
                //    }
                //}

                if (pts.Count > 5 || (pts.Count > 2 && forceResult)) {
                    myNumPtsCalculated = pts.Count;
                    return new clsPoint3d(pts.Average(p => p.X), pts.Average(p => p.Y), pts.Average(p => p.Z));
                }

                PointRemoveDistance = PointRemoveDistance + 5;
            }

            myNumPtsCalculated = 0;
            return new clsPoint3d();
        }

        private clsPoint3d AverageAxis(List<clsPoint3d> pts1, List<clsPoint3d> pts2)
        {
            List<clsPoint3d> myPts = new List<clsPoint3d>();
            int i;
            clsPoint3d pt;
            List<double> myDists = new List<double>();
            double d;

            for (i = 0; i <= pts1.Count - 1; i++) {
                myPts.Add(pts2[i] - pts1[i]);
            }

            //Discard points too far from the average ( > 0.5mm)
            bool isModified = true;
            while (myPts.Count > 1 & isModified) {
                isModified = false;
                pt = new clsPoint3d(myPts.Average(p => p.X), myPts.Average(p => p.Y), myPts.Average(p => p.Z));
                myDists.Clear();
                for (i = 0; i < myPts.Count; i++) {
                    myDists.Add(myPts[i].Dist(pt));
                }
                d = myDists.Max();
                if (d > 0.5) {
                    i = myDists.IndexOf(d);
                    if (i > -1) {
                        myPts.RemoveAt(i);
                        isModified = true;
                    }
                }
            }

            if (myPts.Count > 10) {
                return new clsPoint3d(myPts.Average(p => p.X), myPts.Average(p => p.Y), myPts.Average(p => p.Z));
            }

            return new clsPoint3d();
        }

        private clsPoint3d AverageOriginPoint(List<clsPoint3d> pts1, ref int n)
        {
            List<clsPoint3d> myPts = new List<clsPoint3d>();
            int i;
            clsPoint3d pt;
            List<double> myDists = new List<double>();
            double d;

            for (i = 0; i <= pts1.Count - 1; i++) {
                myPts.Add(pts1[i]);
            }

            //'Discard points too far from the average ( > 0.5mm)
            bool isModified = true;
            while (myPts.Count > 1 & isModified) {
                isModified = false;
                pt = new clsPoint3d(myPts.Average(p => p.X), myPts.Average(p => p.Y), myPts.Average(p => p.Z));
                myDists.Clear();
                for (i = 0; i < myPts.Count; i++) {
                    myDists.Add(myPts[i].Dist(pt));
                }
                d = myDists.Max();
                if (d > 0.5) {
                    i = myDists.IndexOf(d);
                    if (i > -1) {
                        myPts.RemoveAt(i);
                        isModified = true;
                    }
                }
            }

            n = myPts.Count;
            if (myPts.Count > 0) {
                return new clsPoint3d(myPts.Average(p => p.X), myPts.Average(p => p.Y), myPts.Average(p => p.Z));
            }

            return new clsPoint3d();
        }

        public double MaxSpread()
        {
            int i;
            int j;
            double d;
            double maxD;

            maxD = 0;
            for (i = 0; i <= myPts1.Count - 2; i++) {
                for (j = i + 1; j <= myPts1.Count - 1; j++) {
                    d = myPts1[i].Dist(myPts1[j]);
                    if (d > maxD) maxD = d;
                }
            }

            return maxD;
        }
        
        public bool AngleXYIsOK(ref double myMaxAngle, ref string myErrorString)
        {
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

            for (j = 0; j <= myPts6a.Count - 1; j++) {
                p1 = myPts6a[j] - myPts6b[j];
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

            }
            catch (Exception ex) {
                string s = ex.ToString();
            }

            return true;
        }

        public void AngleRangeXY(ref double a1, ref double a2, ref List<double> myAngles)
        {
            double a;
            double maxA;
            clsPoint3d p1;
            clsPoint p3;
            int j;
            int k;

            a1 = 0;
            a2 = 0;
            myAngles = new List<double>();

            for (j = 0; j <= myPts6a.Count - 1; j++) {
                p1 = myPts6a[j] - myPts6b[j];
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

        public void Clear()
        {
            pt1 = new clsPoint3d();
            pt2 = new clsPoint3d();
            pt3 = new clsPoint3d();
            pt5 = new clsPoint3d();
            myVx = new clsPoint3d();
            myVy = new clsPoint3d();
            myVz = new clsPoint3d();
            myPts1a.Clear();
            myPts1b.Clear();
            myPts1.Clear();
            myPts2.Clear();
            myPts3.Clear();
            myPts6a.Clear();
            myPts6b.Clear();
            myCameraPoints.Clear();
            GyroData.Clear();
            LastGyroData.Clear();
            AccelData.Clear();
            LastAccelData.Clear();
            myHistory.Clear();
            myLastTime = null;
            myLastVector = null;
            FirstPointRemoved = false;
        }
    }

    public class MarkerPointComparer : System.Collections.Generic.IComparer<clsMarkerPoint> {

        int System.Collections.Generic.IComparer<clsMarkerPoint>.Compare(clsMarkerPoint myItem1, clsMarkerPoint myItem2)
        {
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

    public class SuspectedMarkerPointComparer : System.Collections.Generic.IComparer<clsMarkerPoint> {

        int System.Collections.Generic.IComparer<clsMarkerPoint>.Compare(clsMarkerPoint myItem1, clsMarkerPoint myItem2)
        {
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

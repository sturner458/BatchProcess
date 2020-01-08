using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;
using static BatchProcess.mdlGlobals;
using static BatchProcess.mdlGeometry;
using static BatchProcess.mdlRecognise;

namespace BatchProcess
{
    public class clsMarkerPoint
    {
        public int MarkerID { get; set; }
        public int SeenFromMarkerID { get; set; }
        public int ActualMarkerID { get; set; } //For when we renumber the markers, e.g. due to stitching a new flight
        public int ConfirmedImageNumber { get; set; }

        //Coordinates of key points on the marker - in the model space of the SeenFrom marker, until turned into a Confirmed Marker
        public clsPoint3d Origin { get; set; } = new clsPoint3d(); //Origin Point
        public clsPoint3d Point { get; set; } = new clsPoint3d(); //Tip of marker
        public clsPoint3d EndXAxis { get; set; } = new clsPoint3d();
        public clsPoint3d EndYAxis { get; set; } = new clsPoint3d();

        //Coordinate vectors
        public clsPoint3d Vx { get; set; } = new clsPoint3d(1, 0, 0);
        public clsPoint3d Vy { get; set; } = new clsPoint3d(0, 1, 0);
        public clsPoint3d Vz { get; set; } = new clsPoint3d(0, 0, 1);

        public double[] ModelViewMatrix { get; set; } = new double[16]; // Matrix of this marker with respect to the origin marker

        public List<double[]> Matrixes { get; } = new List<double[]>(); // Model View Matrices as recorded by ARToolkit
        public List<int> PhotoNumbers { get; } = new List<int>();
        public List<clsPoint3d> CameraPoints { get; } = new List<clsPoint3d>();

        public clsPoint3d VerticalVect { get; set; } = null;

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

        public string Label { get; set; }
        public string MarkerName { get; set; }
        public string MaximumAngleA { get; set; }
        public string MaximumAngleXY { get; set; }
        public double BulkheadHeight { get; set; }


        public List<clsPoint3d> GyroData { get; } = new List<clsPoint3d>();
        public List<clsPoint3d> AccelData { get; } = new List<clsPoint3d>();
        public List<double> Velocity { get; } = new List<double>();
        public List<double> AngularVelocity { get; } = new List<double>();

        public int NewMarkerID() {
            int newMarkerID;
            if (BatchProcess.mdlRecognise.UseDatumMarkers) {
                newMarkerID = MarkerID - 1;
                if (newMarkerID <= 25) {
                    //Do Nothing
                } else if (newMarkerID <= 50) {
                    return newMarkerID + 25;
                } else if (MarkerID <= 75) {
                    return newMarkerID - 25;
                } else if (newMarkerID <= 100) {
                    //Do Nothing
                }
            } else {
                newMarkerID = MarkerID + 1;
                if (newMarkerID <= 25) {
                    //Do Nothing
                } else if (newMarkerID <= 50) {
                    return newMarkerID + 25;
                } else if (MarkerID <= 75) {
                    return newMarkerID - 25;
                } else if (newMarkerID <= 100) {
                    //Do Nothing
                }
                return newMarkerID;
            }
            return newMarkerID;
        }

        public clsMarkerPoint() {
        }

        //Public Sub New(aMarkerID As Integer)
        //    MarkerID = aMarkerID
        //End Sub

        public clsMarkerPoint(int aMarkerID, int seenFromMarkerID) {
            MarkerID = aMarkerID;
            ActualMarkerID = MarkerID;
            SeenFromMarkerID = seenFromMarkerID;
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

            if (myAngleOK) {
                myAngleOK = false;
                a1 = MaxAngle(ref maxAV1, ref maxAV2);
                if (a1 < 40 * PI / 180) {
                    myErrString = "Angle Range = " + Round(a1 * 180 / PI, 1) + " < " + 40.ToString("0.##") + "°";
                } else {
                    myAngleOK = true;
                }
            }

            a2 = MaxAnglePerpendicular(ref maxAV3, ref maxAV4);
            //a2 = 25 * PI / 180;
            if (a2 < 20 * PI / 180) {
                if (myErrString == "") myErrString = "Perpendicular Angle Range = " + Round(a2 * 180 / PI, 1) + " < " + 20.ToString("0.##") + "°";
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
            clsPoint3d px, pz;

            maxA = 0;
            pz = new clsPoint3d(0, 0, 1.0);
            for (int j = 0; j <= Matrixes.Count - 2; j++) {
                for (int k = j + 1; k <= Matrixes.Count - 1; k++) {
                    p1 = CameraPoints[j];
                    p2 = CameraPoints[k];
                    px = new clsPoint3d(p2.X - p1.X, p2.Y - p1.Y, 0);
                    if (IsSameDbl(px.Length, 0)) continue;
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
            clsPoint3d px, pz;

            maxD = 0;
            pz = new clsPoint3d(0, 0, 1.0);
            for (int j = 0; j <= CameraPoints.Count - 2; j++) {
                for (int k = j + 1; k <= CameraPoints.Count - 1; k++) {
                    p1 = CameraPoints[j];
                    p2 = CameraPoints[k];
                    d = p1.Dist(p2);
                    px = new clsPoint3d(p2.X - p1.X, p2.Y - p1.Y, 0);
                    if (IsSameDbl(px.Length, 0)) continue;
                    px.Normalise();
                    p1 = px * p1.Dot(px) + pz * p1.Dot(pz);
                    p1.Normalise();
                    p2 = px * p2.Dot(px) + pz * p2.Dot(pz);
                    p2.Normalise();
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

            p3 = (p4 - p3).Point2D().Point3d(0);
            if (IsSameDbl(p3.Length, 0)) return 0;
            p3.Normalise();
            pz = new clsPoint3d(0, 0, 1.0);
            py = pz.Cross(p3);
            if (IsSameDbl(py.Length, 0)) return 0;
            py.Normalise();

            maxA = 0;
            for (int j = 0; j <= Matrixes.Count - 2; j++) {
                for (int k = j + 1; k <= Matrixes.Count - 1; k++) {
                    p1 = CameraPoints[j];
                    p2 = CameraPoints[k];
                    p1 = py * py.Dot(p1) + pz * pz.Dot(p1);
                    p1.Normalise();
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

        public void SetZNormal() {
            Vx = EndXAxis - Origin;
            if (IsSameDbl(Vx.Length, 0)) return;
            Vx.Normalise();
            Vy = EndYAxis - Origin;
            if (IsSameDbl(Vy.Length, 0)) return;
            Vy.Normalise();
            Vz = Vx.Cross(Vy);
            if (IsSameDbl(Vz.Length, 0)) return;
            Vz.Normalise();
            Vy = Vz.Cross(Vx);
            if (IsSameDbl(Vy.Length, 0)) return;
            Vy.Normalise();
        }

        public clsMarkerPoint Copy(bool includeConfirmedFlag = false) {
            clsMarkerPoint myCopy = new clsMarkerPoint();

            myCopy.MarkerID = MarkerID;
            myCopy.SeenFromMarkerID = SeenFromMarkerID;
            myCopy.ActualMarkerID = ActualMarkerID;
            myCopy.ConfirmedImageNumber = ConfirmedImageNumber;
            myCopy.Origin = Origin.Copy();
            myCopy.EndXAxis = EndXAxis.Copy();
            myCopy.EndYAxis = EndYAxis.Copy();
            myCopy.Point = Point?.Copy();
            myCopy.Vx = Vx.Copy();
            myCopy.Vy = Vy.Copy();
            myCopy.Vz = Vz.Copy();
            if (includeConfirmedFlag) myCopy.SetConfirmedFlag(_confirmed);

            myCopy.PhotoNumbers.AddRange(PhotoNumbers.ToArray());

            myCopy.ModelViewMatrix = ModelViewMatrix.ToArray();

            for (int i = 0; i < Matrixes.Count; i++) {
                myCopy.Matrixes.Add(Matrixes[i].ToArray());
            }

            myCopy.CameraPoints.AddRange(CameraPoints.Select(p => p.Copy()).ToArray());

            for (int i = 0; i < GyroData.Count; i++) {
                myCopy.GyroData.Add(GyroData[i]?.Copy());
            }
            for (int i = 0; i < AccelData.Count; i++) {
                myCopy.AccelData.Add(AccelData[i]?.Copy());
            }
            myCopy.Velocity.AddRange(Velocity.ToList());
            myCopy.AngularVelocity.AddRange(AngularVelocity.ToList());

            if (VerticalVect != null) myCopy.VerticalVect = VerticalVect.Copy();

            myCopy.BulkheadHeight = BulkheadHeight;

            return myCopy;
        }

        public void Save(System.IO.StreamWriter sw) {
            sw.WriteLine("MARKER_POINT_SETTINGS");
            sw.WriteLine("MarkerID," + MarkerID.ToString());
            sw.WriteLine("SeenFromMarkerID," + SeenFromMarkerID.ToString());
            sw.WriteLine("ActualMarkerID," + ActualMarkerID.ToString());
            sw.WriteLine("ConfirmedImageNumber," + ConfirmedImageNumber.ToString());
            if (VerticalVect != null) {
                sw.WriteLine("VerticalVectorX," + VerticalVect.X.ToString());
                sw.WriteLine("VerticalVectorY," + VerticalVect.Y.ToString());
                sw.WriteLine("VerticalVectorZ," + VerticalVect.Z.ToString());
            }
            sw.WriteLine("BulkheadHeight," + BulkheadHeight.ToString());
            foreach (int myID in PhotoNumbers) {
                sw.WriteLine("PhotoNumbers," + myID.ToString());
            }
            foreach (var v in Velocity) {
                sw.WriteLine("Velocity," + v.ToString());
            }
            foreach (var v in AngularVelocity) {
                sw.WriteLine("AngularVelocity," + v.ToString());
            }
            sw.WriteLine("Confirmed," + (_confirmed ? "1" : "0"));
            sw.WriteLine("END_MARKER_POINT_SETTINGS");

            Origin.Save(sw);
            EndXAxis.Save(sw);
            EndYAxis.Save(sw);
            Point.Save(sw);
            Vx.Save(sw);
            Vy.Save(sw);
            Vz.Save(sw);

            for (int i = 0; i < 16; i++) {
                sw.WriteLine(ModelViewMatrix[i].ToString());
            }

            sw.WriteLine(Matrixes.Count);
            for (int i = 0; i < Matrixes.Count; i++) {
                for (int j = 0; j < 16; j++) {
                    sw.WriteLine(Matrixes[i][j].ToString());
                }
            }

            sw.WriteLine(CameraPoints.Count);
            for (int i = 0; i < CameraPoints.Count; i++) {
                CameraPoints[i].Save(sw);
            }

            sw.WriteLine(GyroData.Count);
            for (int i = 0; i < GyroData.Count; i++) {
                GyroData[i].Save(sw);
            }
            sw.WriteLine(AccelData.Count);
            for (int i = 0; i < AccelData.Count; i++) {
                AccelData[i].Save(sw);
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
                        if (mySplit[0] == "MarkerID") MarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "SeenFromMarkerID") SeenFromMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "ActualMarkerID") ActualMarkerID = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "ConfirmedImageNumber") ConfirmedImageNumber = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "PhotoNumber") PhotoNumbers.Add(Convert.ToInt32(mySplit[1]));
                        if (mySplit[0] == "Velocity") Velocity.Add(Convert.ToDouble(mySplit[1]));
                        if (mySplit[0] == "AngularVelocity") AngularVelocity.Add(Convert.ToDouble(mySplit[1]));
                        if (mySplit[0] == "VerticalVectorX") {
                            if (VerticalVect == null) VerticalVect = new clsPoint3d();
                            VerticalVect.X = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorY") {
                            if (VerticalVect == null) VerticalVect = new clsPoint3d();
                            VerticalVect.Y = Convert.ToDouble(mySplit[1]);
                        }
                        if (mySplit[0] == "VerticalVectorZ") {
                            if (VerticalVect == null) VerticalVect = new clsPoint3d();
                            VerticalVect.Z = Convert.ToDouble(mySplit[1]);
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

            Origin.Load(sr);
            EndXAxis.Load(sr);
            EndYAxis.Load(sr);
            Point.Load(sr);
            Vx.Load(sr);
            Vy.Load(sr);
            Vz.Load(sr);

            for (int i = 0; i < 16; i++) {
                ModelViewMatrix[i] = Convert.ToDouble(sr.ReadLine());
            }

            var n = Convert.ToInt32(sr.ReadLine());
            for (int i = 0; i < n; i++) {
                Matrixes.Add(new double[16]);
                for (int j = 0; j < 16; j++) {
                    Matrixes.Last()[j] = Convert.ToDouble(sr.ReadLine());
                }
            }

            n = Convert.ToInt32(sr.ReadLine());
            for (var i = 1; i <= n; i++) {
                p1 = new clsPoint3d();
                p1.Load(sr);
                CameraPoints.Add(p1);
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
                AccelData.Add(p1);
            }

        }

        public void SetEndPointBasedOnZVectors() {
            EndXAxis = Origin + Vx;
            EndYAxis = Origin + Vy;
            SetEndPoint();
            ModelViewMatrix = GetMatrixFromVectorsAndPoint(Origin, Vx, Vy, Vz);
        }

        public void SetEndPoint() {
            if (BatchProcess.mdlRecognise.UseDatumMarkers) {
                if (ActualMarkerID == myGFMarkerID || ActualMarkerID <= myStepMarkerID) { //GF, Step & Bulkhead Markers
                    Point = Origin.Copy();
                } else if (ActualMarkerID < myLeftBulkheadMarkerID) {
                    if (ActualMarkerID - 2 >= 0 && ActualMarkerID - 2 < 50) {
                        Point = Origin + Vx * 160 - Vy * 45;
                    } else if (ActualMarkerID - 2 >= 50 && ActualMarkerID - 2 < 100) {
                        Point = Origin - Vx * 160 - Vy * 45;
                    }
                } else if (ActualMarkerID >= myLeftBulkheadMarkerID && ActualMarkerID <= myRightBulkheadMarkerID) //GF, Step & Bulkhead Markers
                  {
                    Point = Origin.Copy();
                } else if (ActualMarkerID >= myDoorHingeRightMarkerID && ActualMarkerID <= myDoorFrameLeftMarkerID) //Door Markers
                  {
                    Point = Origin + Vy * 65.0;
                } else if (ActualMarkerID >= myObstruct1MarkerID && ActualMarkerID <= myObstruct4MarkerID) //Obstruction Markers
                  {
                    Point = Origin + Vy * 65.0;
                } else if (ActualMarkerID >= myWall1MarkerID && ActualMarkerID <= myWall4MarkerID) //Wall Markers
                  {
                    Point = Origin + Vy * 65.0;
                } else {
                    Point = Origin.Copy();
                }
            } else {
                if (ActualMarkerID < myGFMarkerID) {
                    if (ActualMarkerID >= 0 && ActualMarkerID < 50) {
                        Point = Origin + Vx * 140 - Vy * 45;
                    } else if (ActualMarkerID >= 50 && ActualMarkerID < 100) {
                        Point = Origin + Vx * 140 + Vy * 45;
                    }
                } else if (ActualMarkerID >= myGFMarkerID && ActualMarkerID <= myRightBulkheadMarkerID) //GF, Step & Bulkhead Markers
                  {
                    Point = Origin.Copy();
                } else if (ActualMarkerID >= myDoorHingeRightMarkerID && ActualMarkerID <= myDoorFrameLeftMarkerID) //Door Markers
                  {
                    Point = Origin + Vy * 65.0;
                } else if (ActualMarkerID >= myObstruct1MarkerID && ActualMarkerID <= myObstruct4MarkerID) //Obstruction Markers
                  {
                    Point = Origin + Vy * 65.0;
                } else if (ActualMarkerID >= myWall1MarkerID && ActualMarkerID <= myWall4MarkerID) //Wall Markers
                  {
                    Point = Origin + Vy * 65.0;
                } else {
                    Point = Origin.Copy();
                }
            }
        }

        /// <summary>
        /// Averages the axis.
        /// </summary>
        /// <param name="axisStartPoints">The axis start points.</param>
        /// <param name="axisEndPoints">The axis end points.</param>
        /// <returns>
        /// The averaged point, or a zero length vector if one can not be found within the specified tolerance.
        /// </returns>
        /// <exception cref="ArgumentException">Axis start and end point lists are not matched.</exception>
        private clsPoint3d AverageAxisSimple(List<clsPoint3d> axisStartPoints, List<clsPoint3d> axisEndPoints) {
            if (axisStartPoints == null || axisEndPoints == null) throw new ArgumentNullException("Axis start or end point lists are null.");
            if (axisStartPoints.Count != axisEndPoints.Count) throw new ArgumentException("Axis start and end point lists are not matched.");

            var points = new List<clsPoint3d>();
            for (var i = 0; i <= axisStartPoints.Count - 1; i++) {
                points.Add(axisEndPoints[i] - axisStartPoints[i]);
            }

            // Return the point (if we have more than 7 points within the tolerance)
            return new clsPoint3d(points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z));
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

        public void Clear() {
            Origin = new clsPoint3d();
            EndXAxis = new clsPoint3d();
            EndYAxis = new clsPoint3d();
            Point = new clsPoint3d();
            Vx = new clsPoint3d();
            Vy = new clsPoint3d();
            Vz = new clsPoint3d();
            PhotoNumbers.Clear();
            ModelViewMatrix = new double[16];
            Matrixes.Clear();
            CameraPoints.Clear();
            GyroData.Clear();
            AccelData.Clear();
        }
    }

    public class MarkerPointComparer : System.Collections.Generic.IComparer<clsMarkerPoint>
    {

        int System.Collections.Generic.IComparer<clsMarkerPoint>.Compare(clsMarkerPoint myItem1, clsMarkerPoint myItem2) {
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

    public class SuspectedMarkerPointComparer : System.Collections.Generic.IComparer<clsMarkerPoint>
    {

        int System.Collections.Generic.IComparer<clsMarkerPoint>.Compare(clsMarkerPoint myItem1, clsMarkerPoint myItem2) {
            if (myItem1.MarkerID < myItem2.MarkerID) {
                return -1;
            } else if (myItem1.MarkerID == myItem2.MarkerID) {
                if (myItem1.SeenFromMarkerID == myGFMarkerID) {
                    return -1;
                } else if (myItem2.SeenFromMarkerID == myGFMarkerID) {
                    return 1;
                } else if (myItem1.SeenFromMarkerID < myItem2.SeenFromMarkerID) {
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

using System;
using System.Collections.Generic;
using System.IO;
using static BatchProcess.mdlGeometry;

namespace BatchProcess
{
    public class clsPGPoint : clsPoint3d
    {

        private int myID;
        private int myParentID;
        private double myBulkheadZ;

        public int ID
        {
            get { return myID; }
            set { myID = value; }
        }

        public int ParentID
        {
            get { return myParentID; }
            set { myParentID = value; }
        }

        public double BulkheadZ
        {
            get { return myBulkheadZ; }
            set { myBulkheadZ = value; }
        }

        public clsPoint3d BulkheadPoint()
        {
            return new clsPoint3d(X, Y, Z + myBulkheadZ);
        }

        public clsPGPoint()
        {
            X = 0;
            Y = 0;
            Z = 0;
            myID = -1;
            myParentID = -1;
            myBulkheadZ = 0;
        }

        public clsPGPoint(double ax, double ay, double az, int anID, int aParentID, double inBulkheadZ)
        {
            X = ax;
            Y = ay;
            Z = az;
            myID = anID;
            myParentID = aParentID;
            myBulkheadZ = inBulkheadZ;
        }

        public clsPGPoint(double ax, double ay, double az, int anID)
        {
            X = ax;
            Y = ay;
            Z = az;
            myID = anID;
            myParentID = -1;
            myBulkheadZ = 0;
        }

        public clsPGPoint(double ax, double ay, double az, int anID, int aParentID)
        {
            X = ax;
            Y = ay;
            Z = az;
            myID = anID;
            myParentID = aParentID;
        }

        public clsPGPoint(double ax, double ay, double az)
        {
            X = ax;
            Y = ay;
            Z = az;
            myID = -1;
            myParentID = -1;
        }

        public clsPGPoint(clsPoint3d myPt, int anID, int aParentID)
        {
            X = myPt.X;
            Y = myPt.Y;
            Z = myPt.Z;
            myID = anID;
            myParentID = aParentID;
        }

        public new clsPGPoint Copy()
        {
            return new clsPGPoint(X, Y, Z, ID, myParentID, myBulkheadZ);
        }

        public override void Save(StreamWriter sr)
        {
            base.Save(sr);
            sr.WriteLine(myID);
            sr.WriteLine(myParentID);
            sr.WriteLine(myBulkheadZ);
        }

        public void Save(StreamWriter sr, double myScale = 1.0, bool avoidScaleBulkhead = false)
        {
            base.Save(sr);
            sr.WriteLine(myID);
            sr.WriteLine(myParentID);
            if (avoidScaleBulkhead == false) {
                sr.WriteLine(myBulkheadZ / myScale);
            } else {
                sr.WriteLine(myBulkheadZ);
            }
        }

        public override void Load(StreamReader sr)
        {
            base.Load(sr);
            myID = Convert.ToInt32(sr.ReadLine());
            myParentID = Convert.ToInt32(sr.ReadLine());
            myBulkheadZ = Convert.ToDouble(sr.ReadLine());
        }

        public void Load(StreamReader sr, double myScale = 1.0, bool avoidScaleBulkhead = false)
        {
            base.Load(sr);
            myID = Convert.ToInt32(sr.ReadLine());
            myParentID = Convert.ToInt32(sr.ReadLine());
            if (avoidScaleBulkhead == false ) {
                myBulkheadZ = Convert.ToDouble(sr.ReadLine()) * myScale;
            } else {
                myBulkheadZ = Convert.ToDouble(sr.ReadLine());
            }
        }

    }

    public class PGPointComparer : IComparer<clsPGPoint>
    {

        int System.Collections.Generic.IComparer<clsPGPoint>.Compare(clsPGPoint p1, clsPGPoint p2)
        {
            if (p1.Z < p2.Z)
                return -1;
            if (IsSameDbl(p1.Z, p2.Z))
                return 0;
            return 1;
        }
    }
    public class PGPointComparerFromPoint : IComparer<clsPGPoint>
    {
        clsPoint3d myPt;

        public PGPointComparerFromPoint(clsPoint3d p1)
        {
            myPt = p1.Copy();
        }

        int System.Collections.Generic.IComparer<clsPGPoint>.Compare(clsPGPoint p1, clsPGPoint p2)
        {
            if (p1.Dist(myPt) < p2.Dist(myPt))
                return -1;
            if (IsSameDbl(p1.Dist(myPt), p2.Dist(myPt)))
                return 0;
            return 1;
        }
    }
}

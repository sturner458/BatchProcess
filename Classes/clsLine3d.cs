using System;
using System.IO;
using System.Collections.Generic;
using static System.Math;

namespace BatchProcess
{
    public class clsLine3d
    {
        clsPoint3d myP1;

        clsPoint3d myP2;
        public double X1
        {
            get { return myP1.X; }
            set { myP1.X = value; }
        }

        public double Y1
        {
            get { return myP1.Y; }
            set { myP1.Y = value; }
        }

        public double Z1
        {
            get { return myP1.Z; }
            set { myP1.Z = value; }
        }

        public double X2
        {
            get { return myP2.X; }
            set { myP2.X = value; }
        }

        public double Y2
        {
            get { return myP2.Y; }
            set { myP2.Y = value; }
        }

        public double Z2
        {
            get { return myP2.Z; }
            set { myP2.Z = value; }
        }

        public double DX()
        {
            return X2 - X1;
        }

        public double DY()
        {
            return Y2 - Y1;
        }

        public double DZ()
        {
            return Z2 - Z1;
        }

        public clsPoint3d P1
        {
            get { return myP1; }
            set { myP1 = value; }
        }

        public clsPoint3d P2
        {
            get { return myP2; }
            set { myP2 = value; }
        }

        public clsPoint3d GetPoint(int i)
        {
            {
                if (i == 0)
                    return myP1;
                if (i == 1)
                    return myP2;
                return null;
            }
        }

        public clsPoint3d Point
        {
            //Convert line to point
            get { return new clsPoint3d(DX(), DY(), DZ()); }
        }

        public clsPoint3d EndPoint(int i)
        {
            if (i == 0)
                return myP1;
            if (i == 1)
                return myP2;
            return null;
        }

        public clsPoint3d PointNormalised
        {
            //Convert line to point
            get
            {
                double d = 0;

				d = Sqrt(Pow((X2 - X1), 2) + Pow((Y2 - Y1), 2) + Pow((Z2 - Z1), 2));
				if (d > mdlGeometry.myTol)
                    return new clsPoint3d((X2 - X1) / d, (Y2 - Y1) / d, (Z2 - Z1) / d);
                return new clsPoint3d(0, 0, 0);
            }
        }

        public clsPoint3d ClosestPointToLine(clsLine3d l2)
        {
            clsPoint3d v1;
            clsPoint3d v2;
            clsPoint3d M;
            double m2;
            clsPoint3d R;
            clsPoint3d P21;
            double t1;
            clsPoint3d Q1;

            //Find the actual point on this line where the perpendicular distance hits.
            v1 = new clsPoint3d(DX(), DY(), DZ());
            v1.Normalise();
            v2 = new clsPoint3d(l2.DX(), l2.DY(), l2.DZ());
            v2.Normalise();

            M = v2.Cross(v1);
            m2 = M.Dot(M);
            if (m2 < mdlGeometry.myTol)
                return l2.P1;
            //Parallel

            P21 = new clsPoint3d(l2.X1 - X1, l2.Y1 - Y1, l2.Z1 - Z1);
            R = P21.Cross(M);
            R.Scale(1 / m2);
            t1 = R.Dot(v2);
            Q1 = P1 + t1 * v1;

            return Q1;
        }

        public clsPoint3d DP()
        {
            return new clsPoint3d(DX(), DY(), DZ());
        }

        public void Flip()
        {
            clsPoint3d p3 = default(clsPoint3d);

            p3 = P1.Copy();
            P1 = P2.Copy();
            P2 = p3.Copy();
        }

        public void Rotate(double myAngle)
        {
            double r = 0;
            double theta = 0;

            r = Line2D().Length;
            theta = Line2D().Angle;
            P2.X = P1.X + r * Cos(theta + myAngle);
            P2.Y = P1.Y + r * Sin(theta + myAngle);
        }

        public double MinHeight()
        {
            return Min(Z1, Z2);
        }

        public double MaxHeight()
        {
            return Min(Z1, Z2);
        }

        public clsPoint3d DistanceAlongLine(double d)
        {
            clsLine3d l1 = default(clsLine3d);

            l1 = Copy();
            l1.Normalise();
            return new clsPoint3d(X1 + d * l1.DX(), Y1 + d * l1.DY(), Z1 + d * l1.DZ());
        }

        public clsLine3d()
        {
            myP1 = new clsPoint3d();
            myP2 = new clsPoint3d();
        }

        public clsLine3d(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            myP1 = new clsPoint3d(x1, y1, z1);
            myP2 = new clsPoint3d(x2, y2, z2);
        }

        public clsLine3d(clsPoint3d pt1, clsPoint3d pt2)
        {
            myP1 = pt1.Copy();
            myP2 = pt2.Copy();
        }

        public clsPoint3d MidPoint()
        {
            return new clsPoint3d((X1 + X2) / 2, (Y1 + Y2) / 2, (Z1 + Z2) / 2);
        }

        public double Length
        {
			get { return Sqrt(Pow(DX(), 2) + Pow(DY(), 2) + Pow(DZ(), 2)); }
            set
            {
                double d = 0;
                double ddx = 0;
                double ddy = 0;
                double ddz = 0;

				d = Sqrt(Pow(DX(), 2) + Pow(DY(), 2) + Pow(DZ(), 2));
                ddx = DX() / d;
                ddy = DY() / d;
                ddz = DZ() / d;

                P2.X = P1.X + value * ddx;
                P2.Y = P1.Y + value * ddy;
                P2.Z = P1.Z + value * ddz;
            }
        }

        public double Length2
        {
			get { return Sqrt(Pow(DX(), 2) + Pow(DY(), 2) + Pow(DZ(), 2)); }
            set
            {
                double d = 0;
                double ddx = 0;
                double ddy = 0;
                double ddz = 0;

				d = Sqrt(Pow(DX(), 2) + Pow(DY(), 2) + Pow(DZ(), 2));
                ddx = DX() / d;
                ddy = DY() / d;
                ddz = DZ() / d;

                P1.X = P2.X - value * ddx;
                P1.Y = P2.Y - value * ddy;
                P1.Z = P2.Z - value * ddz;
            }
        }

        public void Reverse()
        {
            clsPoint3d pt1 = default(clsPoint3d);

            pt1 = myP1;
            myP1 = myP2;
            myP2 = pt1;
        }

        public clsLine3d Copy()
        {
            clsPoint3d pt1 = new clsPoint3d();
            clsPoint3d pt2 = new clsPoint3d();

            pt1 = P1.Copy();
            pt2 = P2.Copy();
            return new clsLine3d(pt1, pt2);
        }

        public void Normalise()
        {
            double d = 0;

            d = Length;
            if (mdlGeometry.IsSameDbl(d, 0))
                return;

            P2.X = P1.X + (P2.X - P1.X) / d;
            P2.Y = P1.Y + (P2.Y - P1.Y) / d;
            P2.Z = P1.Z + (P2.Z - P1.Z) / d;
        }

        public void Scale(double myScale)
        {
            double d = 0;

            d = Length;
            P2.X = P1.X + myScale * (P2.X - P1.X);
            P2.Y = P1.Y + myScale * (P2.Y - P1.Y);
            P2.Z = P1.Z + myScale * (P2.Z - P1.Z);
        }

        public void FlipX()
        {
            myP1.x = -myP1.x;
            myP2.x = -myP2.x;
        }

        public double Dot(clsLine3d l1)
        {
            return (P2.X - P1.X) * (l1.P2.X - l1.P1.X) + (P2.Y - P1.Y) * (l1.P2.Y - l1.P1.Y) + (P2.Z - P1.Z) * (l1.P2.Z - l1.P1.Z);
        }

        public clsLine3d Cross(clsLine3d l1)
        {
            double x = 0;
            double y = 0;
            double z = 0;

            x = DY() * l1.DZ() - DZ() * l1.DY();
            y = -(DX() * l1.DZ() - DZ() * l1.DX());
            z = DX() * l1.DY() - DY() * l1.DX();
            return new clsLine3d(0, 0, 0, x, y, z);
        }

        //Moves the line
        public void Move(double x, double y, double z)
        {
            X1 = X1 + x;
            X2 = X2 + x;
            Y1 = Y1 + y;
            Y2 = Y2 + y;
            Z1 = Z1 + z;
            Z2 = Z2 + z;
        }

        //Moves the line
        public void Move(clsLine l2)
        {
            X1 = X1 + l2.DX();
            X2 = X2 + l2.DX();
            Y1 = Y1 + l2.DY();
            Y2 = Y2 + l2.DY();
        }

        //Moves the line
        public void Move(clsPoint pt)
        {
            X1 = X1 + pt.x;
            X2 = X2 + pt.x;
            Y1 = Y1 + pt.y;
            Y2 = Y2 + pt.y;
        }

        //Rotates the line about the origin, ignores the Z coordinate
        public void Rotate2D(double theta)
        {
            P1.Rotate(theta);
            P2.Rotate(theta);
        }

        public void RotateAboutPoint(clsPoint pt1, double theta)
        {
            P1.RotateAboutPoint(pt1, theta);
            P2.RotateAboutPoint(pt1, theta);
        }

        public void RotateAboutLine(clsLine3d l1, double a)
        {
            P1.RotateAboutLine(l1, a);
            P2.RotateAboutLine(l1, a);
        }

        public void RotateAboutZ(double a)
        {
            P1.RotateAboutZ(a);
            P2.RotateAboutZ(a);
        }

        public double VerticalHeight(clsPoint pt1)
        {
            //Returns the height of the line where is passes over p1
            clsLine l1 = default(clsLine);
            clsLine l2 = default(clsLine);
            double d = 0;
            double d1 = 0;
            double d2 = 0;

            l1 = new clsLine(P1.X, P1.Y, P2.X, P2.Y);
            if (l1.IsOnLine(pt1) == false)
                return 0;

            d1 = l1.Length;
            l1.Normalise();

            l2 = new clsLine(l1.P1, pt1);
            d2 = l1.Dot(l2);
            d = d2 / d1;
            return P1.Z + (P2.Z - P1.Z) * d;
        }

        public clsPoint3d VerticalPoint(clsPoint pt1)
        {
            //Returns the point on the line where is passes over p1
            return new clsPoint3d(pt1.X, pt1.Y, VerticalHeight(pt1));
        }

        public clsLine Line2D()
        {
            return new clsLine(X1, Y1, X2, Y2);
        }

        public double DistanceToPoint(clsPoint3d aPt)
        {
            double d1 = 0;
            double d2 = 0;
            clsLine3d l2 = default(clsLine3d);
            clsLine3d l3 = default(clsLine3d);

            l2 = Copy();
            l2.Normalise();
            l3 = new clsLine3d(P1, aPt);
            d1 = l2.Dot(l3);
            d2 = l3.Length;
			return Sqrt(Pow(d2, 2) - Pow(d1, 2));
        }

        public double Lambda(clsPoint3d pt1)
        {
            clsLine3d l1 = default(clsLine3d);
            clsLine3d l2 = default(clsLine3d);

            l1 = new clsLine3d(P1, pt1);
            l2 = Copy();
            l2.Normalise();
            return l1.Dot(l2) / Length;
        }

        public clsPoint3d PointFromLambda(double l)
        {
            clsLine3d l1 = default(clsLine3d);

            l1 = Copy();
            l1.Scale(l);
            return l1.P2;
        }

        public void Save(StreamWriter sr)
        {
            myP1.Save(sr);
            myP2.Save(sr);
        }

        public void Load(StreamReader sr)
        {
            myP1.Load(sr);
            myP2.Load(sr);
        }

        public clsPoint3d Intersect(clsLine3d l1)
        {
            //Both infinite. More accurate that IntersectQuick
            double l = 0;
            clsLine3d l2 = default(clsLine3d);
            clsLine3d l3 = default(clsLine3d);

			if (Length < mdlGeometry.myTol | l1.Length < mdlGeometry.myTol)
                return null;
            l2 = Copy();
            l3 = l1.Copy();
            l2.Length = 1000;
            l3.Length = 1000;

			if (Abs(((l2.X2 - l2.X1) * (l3.Y1 - l3.Y2) + (l2.Y2 - l2.Y1) * (l3.X2 - l3.X1))) < mdlGeometry.myTol / 10)
                return null;
            //No intersection
            l = ((l3.X2 - l2.X1) * (l3.Y1 - l3.Y2) + (l3.Y2 - l2.Y1) * (l3.X2 - l3.X1)) / ((l2.X2 - l2.X1) * (l3.Y1 - l3.Y2) + (l2.Y2 - l2.Y1) * (l3.X2 - l3.X1));
            return new clsPoint3d(l2.X1 + l * (l2.X2 - l2.X1), l2.Y1 + l * (l2.Y2 - l2.Y1), 0);
        }

        public clsPoint3d IntersectQuick(clsLine3d l1)
        {
            //Both infinite
            double lambda = 0;

			if (Abs((X2 - X1) * (l1.Y1 - l1.Y2) + (Y2 - Y1) * (l1.X2 - l1.X1)) < mdlGeometry.myTol / 10)
                return null;
            //No intersection

            lambda = ((l1.X2 - X1) * (l1.Y1 - l1.Y2) + (l1.Y2 - Y1) * (l1.X2 - l1.X1)) / ((X2 - X1) * (l1.Y1 - l1.Y2) + (Y2 - Y1) * (l1.X2 - l1.X1));
            return new clsPoint3d(X1 + lambda * (X2 - X1), Y1 + lambda * (Y2 - Y1), 0);
        }

        public bool IsOnLine(clsPoint3d aPt)
        {
            clsLine3d l1 = default(clsLine3d);

            l1 = new clsLine3d(P2, aPt);
            if (mdlGeometry.IsSameDbl(Cross(l1).Length, 0))
                return true;
            return false;
        }

        public bool Overlaps(clsLine3d aLine, double aTol = 0)
        {
            clsLine3d l1 = default(clsLine3d);
            clsPoint3d p3 = default(clsPoint3d);
            clsPoint3d p4 = default(clsPoint3d);

            if (aTol == 0)
				aTol = mdlGeometry.myTol;
            l1 = aLine.Copy();
            if ((!IsOnLine(l1.P1)) | (!IsOnLine(l1.P2)))
                return false;
            if (Dot(l1) < 0)
                l1.Reverse();
            p3 = l1.P1;
            p4 = l1.P2;
            if (P1 == p3)
                return true;
            if (P1 == p4 | P2 == p3)
                return false;
            if (IsOnShortLine(p4))
                return true;
            if (l1.IsOnShortLine(P1, aTol) | l1.IsOnShortLine(P2, aTol))
                return true;
            return false;
        }
        
        public bool IsOnShortLine(clsPoint3d pt1, double aTol = 0, bool excludeEnds = false)
        {
            clsLine3d l1 = default(clsLine3d);
            clsLine3d l2 = default(clsLine3d);
            double d = 0;

            if (aTol == 0)
				aTol = mdlGeometry.myTol;
			if (Abs(DistanceToPoint(pt1)) > aTol)
                return false;
            l1 = new clsLine3d(P1, pt1);
            l2 = Copy();
            l2.Normalise();
            d = l1.Dot(l2);
            if (d < -aTol)
                return false;
            if (d > Length + aTol)
                return false;
            if (excludeEnds && (P1 == pt1 | P2 == pt1))
                return false;
            return true;
        }

        public double DistanceToShortLine(clsPoint3d pt1)
        {
            clsPoint3d pt2 = default(clsPoint3d);

            pt2 = mdlGeometry.ProjectPoint(pt1, this);
            if (Lambda(pt2) < 0)
                pt2 = P1;
            if (Lambda(pt2) > 1)
                pt2 = P2;
            if (mdlGeometry.IsSameDbl(Length, 0))
                pt2 = P1;
            return pt1.Dist(pt2);
        }

        public double DistanceToLine(clsLine3d l2)
        {
            //Perpendicular distance between 3d lines. Limited to the line segments.
            double myNormalDist;
            clsPoint3d v1;
            clsPoint3d v2;
            clsPoint3d M;
            double m2;
            clsPoint3d R;
            clsPoint3d P21;
            double t1;
            double t2;
            clsPoint3d Q1;
            clsPoint3d Q2;

            //Find the actual point on this line where the perpendicular distance hits. If it is off the line, then find the minimum distance between the end points
            v1 = new clsPoint3d(DX(), DY(), DZ());
            v1.Normalise();
            v2 = new clsPoint3d(l2.DX(), l2.DY(), l2.DZ());
            v2.Normalise();

            P21 = new clsPoint3d(l2.X1 - X1, l2.X1 - X1, l2.X1 - X1);
            M = v2.Cross(v1);
            m2 = M.Dot(M);
            if (m2 < mdlGeometry.myTol)
                return DistanceToPoint(l2.P1);
            //Parallel
            myNormalDist = Abs(P21.Dot(M)) / Sqrt(m2);
            //Perpendicular distance

            R = P21.Cross(M);
            R.Scale(1 / m2);
            t1 = R.Dot(v2);
            Q1 = P1 + t1 * v1;
            if (t1 < 0)
                Q1 = P1;
            if (t1 > Length)
                Q1 = P2;

            t2 = R.Dot(v1);
            Q2 = l2.P1 + t2 * v2;
            if (t2 < 0)
                Q2 = l2.P1;
            if (t2 > l2.Length)
                Q2 = l2.P2;

            return Q1.Dist(Q2);
        }

        public double AngleToHorizontal
        {
            get
            {
                double x = 0;

                x = Line2D().Length;
                if (mdlGeometry.IsSameDbl(x, 0))
					return PI / 2;
				return Atan(DZ() / x);
            }
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace BatchProcess
{
    public class clsPoint3d
    {
        protected double myX;
        protected double myY;

        protected double myZ;

        public double x
        {
            get { return myX; }
            set { myX = value; }
        }


        public double y
        {
            get { return myY; }
            set { myY = value; }
        }


        public double z
        {
            get { return myZ; }
            set { myZ = value; }
        }

        public double X
        {
            get { return myX; }
            set { myX = value; }
        }


        public double Y
        {
            get { return myY; }
            set { myY = value; }
        }


        public double Z
        {
            get { return myZ; }
            set { myZ = value; }
        }

        public clsPoint3d()
        {
            myX = 0;
            myY = 0;
            myZ = 0;
        }

        public clsPoint3d(double ax, double ay, double az)
        {
            myX = ax;
            myY = ay;
            myZ = az;
        }

        public clsPoint3d(clsPoint p1, double az)
        {
            myX = p1.x;
            myY = p1.y;
            myZ = az;
        }

        public virtual clsPoint3d Copy()
        {
            return new clsPoint3d(myX, myY, myZ);
        }

        public void Add(clsPoint3d p1)
        {
            x = x + p1.x;
            y = y + p1.y;
            z = z + p1.z;
        }

        public virtual void Load(StreamReader sr)
        {
            myX = Convert.ToDouble(sr.ReadLine());
            myY = Convert.ToDouble(sr.ReadLine());
            myZ = Convert.ToDouble(sr.ReadLine());
        }

        public virtual void Save(StreamWriter sw)
        {
            sw.WriteLine(myX);
            sw.WriteLine(myY);
            sw.WriteLine(myZ);
        }

        public clsLine3d Line()
        {
            return new clsLine3d(0, 0, 0, X, Y, Z);
        }

        public void Move(double x1, double y1, double z1)
        {
            x = x + x1;
            y = y + y1;
            z = z + z1;
        }

        public void Move(clsPoint3d p1, clsPoint3d p2)
        {
            x = x + p2.x - p1.x;
            y = y + p2.y - p1.y;
            z = z + p2.z - p1.z;
        }

        public void Move(clsPoint3d p1)
        {
            x = x + p1.x;
            y = y + p1.y;
            z = z + p1.z;
        }

        public void Move(clsPoint pt1)
        {
            x = x + pt1.x;
            y = y + pt1.y;
        }

        public void Rotate(double theta)
        {
            double r = 0;
            double theta2 = 0;

			r = Sqrt(x * x + y * y);
            theta2 = mdlGeometry.Angle(x, y);
			x = r * Cos(theta + theta2);
			y = r * Sin(theta + theta2);
        }

        //Public Sub RotateAboutX(ByVal theta As Double)
        //    Dim r, theta2 As Double

        //    r = Sqrt(y * y + z * z)
        //    theta2 = mdlGeometry.Angle(y, z)
        //    y = r * Cos(theta + theta2)
        //    z = r * Sin(theta + theta2)
        //End Sub

        public void RotateAboutPoint(clsPoint p1, double theta)
        {
            double r = 0;
            double theta2 = 0;

			r = Sqrt((x - p1.x) * (x - p1.x) + (y - p1.y) * (y - p1.y));
            theta2 = mdlGeometry.Angle(x - p1.x, y - p1.y);
			x = p1.x + r * Cos(theta + theta2);
			y = p1.y + r * Sin(theta + theta2);
        }

        public void RotateAboutLine(clsLine3d l1, double theta)
        {
            clsPoint3d p1 = default(clsPoint3d);
            clsPoint3d p2 = default(clsPoint3d);
            clsPoint3d p2a = default(clsPoint3d);
            clsPoint3d p2b = default(clsPoint3d);
            clsPoint3d p3 = default(clsPoint3d);
            clsLine3d l2 = default(clsLine3d);

            if (l1.IsOnLine(this))
                return;

            //Setup a coordinate system with X running along l1 and the origin at l1.p2
            l2 = new clsLine3d(l1.P2.Copy(), Copy());
            p1 = new clsPoint3d(l1.DX(), l1.DY(), l1.DZ());
            p1.Length = 1;
            p2 = new clsPoint3d(l2.DX(), l2.DY(), l2.DZ());
            p2a = new clsPoint3d(l2.DX(), l2.DY(), l2.DZ());
            p2.Length = 1;
            p3 = p1.Cross(p2);
            p3.Length = 1;
            p2 = p1.Cross(p3);
            p2.Length = 1;
            if (p2.Dot(p2a) < 0)
                p2.Scale(-1);

            p2b = new clsPoint3d(p1.Dot(p2a), p2.Dot(p2a), p3.Dot(p2a));
            p2b.RotateAboutX(theta);

            myX = l1.X2 + p2b.x * p1.x + p2b.y * p2.x + p2b.z * p3.x;
            myY = l1.Y2 + p2b.x * p1.y + p2b.y * p2.y + p2b.z * p3.y;
            myZ = l1.Z2 + p2b.x * p1.z + p2b.y * p2.z + p2b.z * p3.z;
        }

        public void RotateAboutX(double a)
        {
            clsPoint3d p1 = default(clsPoint3d);

            p1 = new clsPoint3d(y, z, 0);
            p1.Rotate(a);
            y = p1.x;
            z = p1.y;
        }

        public void RotateAboutY(double a)
        {
            clsPoint3d p1 = default(clsPoint3d);

            p1 = new clsPoint3d(-x, z, 0);
            p1.Rotate(a);
            x = -p1.x;
            z = p1.y;
        }

        public void RotateAboutZ(double a)
        {
            clsPoint3d p1 = default(clsPoint3d);

            p1 = new clsPoint3d(x, y, 0);
            p1.Rotate(a);
            x = p1.x;
            y = p1.y;
        }

        public clsPoint Point2D()
        {
            return new clsPoint(x, y);
        }

        public clsPoint SWPoint2D()
        {
            return new clsPoint(x, z);
        }

        //Used when we are pretending that a point in the plane represents a vector from the origin
        public void Normalise()
        {
            double d = Sqrt(Pow(x, 2) + Pow(y, 2) + Pow(z, 2));
			if (Abs(d) < double.Epsilon) return;
            x = x / d;
            y = y / d;
            z = z / d;
        }

        public clsPoint3d Normalised()
        {
            //Returns a copy of the normalised point
            clsPoint3d p1 = default(clsPoint3d);

            p1 = Copy();
            p1.Normalise();
            return p1;
        }

        public clsPoint3d Normalised2()
        {
            //Returns this point, normalised
            Normalise();
            return this;
        }

        public double Length
        {
			get { return Sqrt(x * x + y * y + z * z); }
            set
            {
                double d = 0;

				d = Sqrt(x * x + y * y + z * z);
				if (mdlGeometry.IsSameDbl(d, 0))
                    return;
                Scale(value / d);
            }
        }

        public double Dist(clsPoint3d p1)
        {
            return Sqrt(Pow((x - p1.x), 2) + Pow((y - p1.y), 2) + Pow((z - p1.z), 2));
        }

        public clsPoint3d Cross(clsPoint3d p1)
        {
            double x1 = 0;
            double y1 = 0;
            double z1 = 0;

            x1 = myY * p1.z - myZ * p1.y;
            y1 = -(myX * p1.z - myZ * p1.x);
            z1 = myX * p1.y - myY * p1.x;
            return new clsPoint3d(x1, y1, z1);
        }

        public void Scale(double d)
        {
            x = x * d;
            y = y * d;
            z = z * d;
        }

        public double Dot(clsPoint3d p1)
        {
            return x * p1.x + y * p1.y + z * p1.z;
        }

        public double Volume()
        {
            return x * y * z;
        }

        public static clsPoint3d operator +(clsPoint3d p1, clsPoint3d p2)
        {
            return new clsPoint3d(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
        }

        public static clsPoint3d operator -(clsPoint3d p1, clsPoint3d p2)
        {
            return new clsPoint3d(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
        }

        public static clsPoint3d operator -(clsPoint3d p1)
        {
            return new clsPoint3d(-p1.x, -p1.y, -p1.z);
        }

        public override bool Equals(object obj)
        {
            return this == (clsPoint3d)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(clsPoint3d p1, clsPoint3d p2)
        {
            if ((object)p1 == null && (object)p2 == null)
                return true;

            if ((object)p1 == null | (object)p2 == null)
                return false;

            return Pow((p1.x - p2.x), 2) + Pow((p1.y - p2.y), 2) + Pow((p1.z - p2.z), 2) < mdlGeometry.myTol * mdlGeometry.myTol;
        }

        public static bool operator !=(clsPoint3d p1, clsPoint3d p2)
        {
            if ((object)p1 == null && (object)p2 == null)
                return false;

            if ((object)p1 == null | (object)p2 == null)
                return true;

            return Pow((p1.x - p2.x), 2) + Pow((p1.y - p2.y), 2) + Pow((p1.z - p2.z), 2) >= mdlGeometry.myTol * mdlGeometry.myTol;
        }

        public static clsPoint3d operator *(clsPoint3d p1, double d)
        {
            return new clsPoint3d(p1.x * d, p1.y * d, p1.z * d);
        }

        public static clsPoint3d operator *(double d, clsPoint3d p1)
        {
            return new clsPoint3d(p1.x * d, p1.y * d, p1.z * d);
        }

        public static clsPoint3d operator /(clsPoint3d p1, double d)
        {
			if (mdlGeometry.IsSameDbl(d, 0))
                return new clsPoint3d(0, 0, 0);
            return new clsPoint3d(p1.x / d, p1.y / d, p1.z / d);
        }

        public bool IsSameTol(clsPoint3d p1, double aTol = 0)
        {
            if ((object)p1 == null)
                return false;
            if (aTol == 0)
				aTol = mdlGeometry.myTol;
            return Dist(p1) < aTol;
        }

        public bool IsOrigin()
        {
			return myX * myX + myY * myY + myZ * myZ < mdlGeometry.myTol * mdlGeometry.myTol;
        }

        public object SafeArray()
        {
            double[] p = new double[3];

            p[0] = x;
            p[1] = z;
            p[2] = -y;
            return p;
        }

        public object SafeArray2()
        {
            double[] p = new double[3];

            p[0] = x;
            p[1] = y;
            p[2] = z;
            return p;
        }

        public double AngleToHorizontal
        {
            get
            {
                var d = Sqrt(Pow(myX, 2) + Pow(myY, 2));

				if (Abs(d) < double.Epsilon) 
                    return PI / 2;
				else
					return Atan(myZ / d);
            }
        }
        
        //Only works with ArrayData from MathTransforms
        public void MultiplyTransform(object myMatrix)
        {
            double x1 = 0;
            double y1 = 0;
            double z1 = 0;
            double[] myMatrix2;

            myMatrix2 = ((System.Collections.IEnumerable)myMatrix).Cast<object>().Select(x => Convert.ToDouble(x)).ToArray();

            x1 = (x * myMatrix2 [0] + y * myMatrix2[1] + z * myMatrix2[2]) * myMatrix2[12] + myMatrix2[9];
			y1 = (x * myMatrix2[3] + y * myMatrix2[4] + z * myMatrix2[5]) * myMatrix2[12] + myMatrix2[10];
			z1 = (x * myMatrix2[6] + y * myMatrix2[7] + z * myMatrix2[8]) * myMatrix2[12] + myMatrix2[11];
            x = x1;
            y = y1;
            z = z1;
        }

        //Use this one to replicate MathTransform multiplication
        //Only works with ArrayData from MathTransforms
        public void MultiplyTransformSW(object myMatrix)
        {
            double x1 = 0;
            double y1 = 0;
            double z1 = 0;
            double[] myMatrix2;

            myMatrix2 = ((System.Collections.IEnumerable)myMatrix).Cast<object>().Select(x => Convert.ToDouble(x)).ToArray();

            x1 = (x * myMatrix2[0] + y * myMatrix2[3] + z * myMatrix2[6]) * myMatrix2[12] + myMatrix2[9];
			y1 = (x * myMatrix2[1] + y * myMatrix2[4] + z * myMatrix2[7]) * myMatrix2[12] + myMatrix2[10];
			z1 = (x * myMatrix2[2] + y * myMatrix2[5] + z * myMatrix2[8]) * myMatrix2[12] + myMatrix2[11];
            x = x1;
            y = y1;
            z = z1;
        }

        public clsPoint3d SWPoint()
        {
            return new clsPoint3d(x, z, -y);
        }
    }
}


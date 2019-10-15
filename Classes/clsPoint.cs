using System;
using System.IO;
using static BatchProcess.mdlGeometry;
using static System.Math;

namespace BatchProcess
{
    public class clsPoint
    {

        double myX;
        double myY;

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

        public clsPoint()
        {
            myX = 0;
            myY = 0;
        }

        public clsPoint(double ax, double ay)
        {
            myX = ax;
            myY = ay;
        }

        //Create a new point at an angle and distance from another
        public clsPoint(clsPoint p1, double ang, double d)
        {
            myX = p1.x + d * Cos(ang);
            myY = p1.y + d * Sin(ang);
        }

        public clsPoint Copy()
        {
            return new clsPoint(myX, myY);
        }

        public void Move(double a, double b)
        {
            x = x + a;
            y = y + b;
        }

        public void Move(clsPoint p1)
        {
            x = x + p1.x;
            y = y + p1.y;
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

        public void RotateAboutPoint(clsPoint p1, double theta)
        {
            double r = 0;
            double theta2 = 0;

            r = Dist(p1);
            theta2 = mdlGeometry.Angle(x - p1.x, y - p1.y);
            x = p1.x + r * Cos(theta + theta2);
            y = p1.y + r * Sin(theta + theta2);
        }

        public void SwapXY()
        {
            double d = 0;

            d = x;
            x = y;
            y = d;
        }

        public double Angle(bool normaliseAngle = false)
        {
            double a = 0;
            a = mdlGeometry.Angle(x, y);

            //Make angle lie between -pi and +pi
            if (normaliseAngle)
            {
                while (a < -PI)
                {
                    a = a + 2 * PI;
                }
                while (a > PI)
                {
                    a = a - 2 * PI;
                }
            }
            return a;
        }

        public clsPoint Normal()
        {
                return new clsPoint(y, -x);
        }

        public void Scale(double d)
        {
            x = x * d;
            y = y * d;
        }

        public double Dist(clsPoint p1)
        {
            return Sqrt(Pow((x - p1.x), 2) + Pow((y - p1.y), 2));
        }

        public double Dist(clsLine l1)
        {
            clsLine l2 = default(clsLine);
            clsLine l3 = default(clsLine);

            l2 = l1.Normal();
            l3 = new clsLine(l1.P1, this);
            return Abs(l2.Dot(l3));
        }

        public void Multiply(double d)
        {
            x = x * d;
            y = y * d;
        }

        public bool IsOrigin()
        {
            return myX * myX + myY * myY < myTol * myTol;
        }

        //Used when we are pretending that a point in the plane represents a vector from the origin
        public void Normalise()
        {
            double d = 0;

            d = Sqrt(Pow(x, 2) + Pow(y, 2));
            if (IsSameDbl(d, 0))
            {
                x = 0;
                y = 0;
            }
            else
            {
                x = x / d;
                y = y / d;
            }
        }

        public clsPoint Normalised()
        {
            double d = 0;

            d = Sqrt(Pow(x, 2) + Pow(y, 2));
            if (IsSameDbl(d, 0))
                return new clsPoint(0, 0);
            return new clsPoint(x / d, y / d);
        }

        public double Length
        {
            get { return Sqrt(Pow(x, 2) + Pow(y, 2)); }
            set
            {
                Normalise();
                Multiply(value);
            }
        }

        public void Reverse()
        {
            x = -x;
            y = -y;
        }

        public clsPoint3d Point3d(double z = 0.0)
        {
            return new clsPoint3d(myX, myY, z);
        }

        public void Load(StreamReader sr)
        {
            Double.TryParse(sr.ReadLine(), out myX);
            Double.TryParse(sr.ReadLine(), out myY);
        }

        public void Save(StreamWriter sr)
        {
            sr.WriteLine(myX);
            sr.WriteLine(myY);
        }

        public double Cross(clsPoint p1)
        {
            return myY * p1.x - myX * p1.y;
        }

        public double Dot(clsPoint p1)
        {
            return x * p1.x + y * p1.y;
        }

        public static clsPoint operator +(clsPoint p1, clsPoint p2)
        {
            return new clsPoint(p1.x + p2.x, p1.y + p2.y);
        }

        public static clsPoint operator -(clsPoint p1)
        {
            return new clsPoint(-p1.x, -p1.y);
        }

        public static clsPoint operator -(clsPoint p1, clsPoint p2)
        {
            return new clsPoint(p1.x - p2.x, p1.y - p2.y);
        }

        public override bool Equals(object obj)
        {
            return this == (clsPoint)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(clsPoint p1, clsPoint p2)
        {
            if ((object)p1 == null && (object)p2 == null)
                return true;

            if ((object)p1 == null | (object)p2 == null)
                return false;

            return Pow((p1.x - p2.x), 2) + Pow((p1.y - p2.y), 2) < myTol * myTol;
        }

        public static bool operator !=(clsPoint p1, clsPoint p2)
        {
            if ((object)p1 == null && (object)p2 == null)
                return false;

            if ((object)p1 == null | (object)p2 == null)
                return true;

            return Pow((p1.x - p2.x), 2) + Pow((p1.y - p2.y), 2) >= myTol * myTol;
        }

        public bool IsSameTol(clsPoint p1, double aTol = 0)
        {
            if ((object)p1 == null)
                return false;
            if (aTol == 0)
                aTol = myTol;
            return Dist(p1) < aTol;
        }
    }
}

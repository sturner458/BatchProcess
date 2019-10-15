using System;
using System.IO;
using System.Collections.Generic;
using static System.Math;

namespace BatchProcess
{
    public class clsArc : clsCircle
    {

        double myAngle1;
        double myAngle2;
        //1 = CCW; -1 = CW
        int myDirection;

        public double Angle1
        {
            get { return myAngle1; }
            set
            {
                myAngle1 = value;
                while (myAngle1 > 2 * PI)
                {
                    myAngle1 = myAngle1 - 2 * PI;
                }
                while (myAngle1 < 0)
                {
                    myAngle1 = myAngle1 + 2 * PI;
                }
            }
        }
        
        public double Angle2
        {
            get { return myAngle2; }
            set
            {
                myAngle2 = value;
                while (myAngle2 > 2 * PI)
                {
                    myAngle2 = myAngle2 - 2 * PI;
                }
                while (myAngle2 < 0)
                {
                    myAngle2 = myAngle2 + 2 * PI;
                }
            }
        }
        
        public double IncludedAngle()
        {
            if (Direction == 1)
            {
                if (myAngle1 > myAngle2)
                    return myAngle2 + 2 * PI - myAngle1;
                return myAngle2 - myAngle1;
            }
            else
            {
                if (myAngle2 > myAngle1)
                    return myAngle1 + 2 * PI - myAngle2;
                return myAngle1 - myAngle2;
            }
        }

        public int Direction
        {
            get { return myDirection; }
            set { myDirection = value; }
        }
        
        public clsPoint StartPoint(bool avoidLeadIn = false)
        {
            //Now caters for the transitions including a lead in/out dimension
            double d;

            if (avoidLeadIn)
            {
                d = 0;
            }
            else
            {
                d = 0;
            }

            return new clsPoint(Centre.x + Radius * Cos(Angle1) - d * Cos(StartAngle()), Centre.y + Radius * Sin(Angle1) - d * Sin(StartAngle()));
        }

        public clsPoint StartStraight()
        {
            //Vector representing the start straight
            double d;

            d = 0;

            return new clsPoint(d * Cos(StartAngle()), d * Sin(StartAngle()));
        }

        public double StartAngle()
        {
            //This differs from Angle1 in that it returns the angle of the line leading into the arc
            //Grad Increase
            if (Direction == 1)
            {
                return Angle1 - 3 * PI / 2;
                //Grad Decrease
            }
            else
            {
                return Angle1 - PI / 2;
            }
        }

        public clsPoint EndPoint(bool avoidLeadIn = false)
        {
            //Now caters for the transitions including a lead in/out dimension
            double d;

            if (avoidLeadIn)
            {
                d = 0;
            }
            else
            {
                d = 0;
            }

            return new clsPoint(Centre.x + Radius * Cos(Angle2) + d * Cos(EndAngle()), Centre.y + Radius * Sin(Angle2) + d * Sin(EndAngle()));
        }

        public clsPoint EndStraight()
        {
            //Vector representing the end straight
            double d;

            d = 0;

            return new clsPoint(d * Cos(EndAngle()), d * Sin(EndAngle()));
        }

        public double EndAngle()
        {
            //This differs from Angle2 in that it returns the angle of the line leading out of the arc
            //Grad Increase
            if (Direction == 1)
            {
                return Angle2 - 3 * PI / 2;
                //Grad Decrease
            }
            else
            {
                return Angle2 - PI / 2;
            }
        }

        public double Length(bool includeLeadInOutDims)
        {
            //Returns the length of the arc
            double a1;
            double a2;
            double r;
            double d = 0.0;

            if (includeLeadInOutDims)
                d = 2 * 0;

            a1 = Angle1;
            a2 = Angle2;
            while (a1 > 2 * PI)
            {
                a1 = a1 - 2 * PI;
            }
            while (a2 > 2 * PI)
            {
                a2 = a2 - 2 * PI;
            }
            while (a1 < 0)
            {
                a1 = a1 + 2 * PI;
            }
            while (a2 < 0)
            {
                a2 = a2 + 2 * PI;
            }

            r = Radius;

            if (Direction == 1)
            {
                if (a1 <= a2)
                    return r * (a2 - a1) + d;
                return r * ((2 * PI - a1) + a2) + d;
            }
            if (a1 >= a2)
                return r * (a1 - a2) + d;
            return r * ((2 * PI - a2) + a1) + d;
        }

        public clsPoint DistanceAlongArc(double d, bool avoidLeadIn)
        {
            //Returns a point a given distance around the arc from the start point
            clsPoint p1;

            if (Radius == 0)
                return Centre;
            if (avoidLeadIn)
                return Point(Angle1 + Direction * (d / Radius));
            if (d < 0 + mdlGeometry.myTol)
            {
                p1 = Tangent(Angle1);
                p1.Scale(-(0 - d));
                return Point(Angle1) + p1;
            }
            else if (d > 0 + Length(false) - mdlGeometry.myTol)
            {
                p1 = Tangent(Angle2);
                p1.Scale(d - 0 - Length(false));
                return Point(Angle2) + p1;
            }
            return Point(Angle1 + Direction * ((d - 0) / Radius));
        }

        public clsPoint TangentAtDistanceAlongArc(double d, bool avoidLeadIn)
        {
            //Returns a point a given distance around the arc from the start point
            clsPoint p1;

            if (Radius == 0)
                return Centre;
            if (avoidLeadIn)
                return Tangent(Angle1 + Direction * (d / Radius));
            if (d < 0 + mdlGeometry.myTol)
            {
                p1 = Tangent(Angle1);
                return p1;
            }
            else if (d > 0 + Length(false) - mdlGeometry.myTol)
            {
                p1 = Tangent(Angle2);
                return p1;
            }
            return Tangent(Angle1 + Direction * ((d - 0) / Radius));
        }


        public clsArc()
        {
        }

        public clsArc(clsPoint aCentre, double aRadius, double anAngle1, double anAngle2, int aDirection)
        {
            Centre = aCentre.Copy();
            Radius = aRadius;
            Angle1 = anAngle1;
            Angle2 = anAngle2;
            Direction = aDirection;
        }

        public void Save(StreamWriter sr)
        {
            Centre.Save(sr);
            sr.WriteLine(Radius);
            sr.WriteLine(myAngle1);
            sr.WriteLine(myAngle2);
            sr.WriteLine(myDirection);
        }

        public void Load(StreamReader sr)
        {
            Centre = new clsPoint();
            Centre.Load(sr);
            Radius = Convert.ToDouble(sr.ReadLine());
            myAngle1 = Convert.ToDouble(sr.ReadLine());
            myAngle2 = Convert.ToDouble(sr.ReadLine());
            myDirection = Convert.ToInt32(sr.ReadLine());
        }

        public new clsArc Copy()
        {
            return new clsArc(Centre, Radius, Angle1, Angle2, Direction);
        }


        public bool PointLiesOnArc(clsPoint p1)
        {
            double a;

            a = AngleFromPoint(p1);
            if (myAngle1 <= myAngle2 & Direction == 1)
            {
                if (a >= myAngle1 - mdlGeometry.myTol & a <= myAngle2 + mdlGeometry.myTol)
                    return true;
            }
            else if (myAngle1 > myAngle2 & Direction == 1)
            {
                if (a >= myAngle1 - mdlGeometry.myTol | a <= myAngle2 + mdlGeometry.myTol)
                    return true;
            }
            else if (myAngle1 <= myAngle2 & Direction == -1)
            {
                if (a <= myAngle1 + mdlGeometry.myTol | a >= myAngle2 - mdlGeometry.myTol)
                    return true;
            }
            else if (myAngle1 > myAngle2 & Direction == -1)
            {
                if (a <= myAngle1 + mdlGeometry.myTol & a >= myAngle2 - mdlGeometry.myTol)
                    return true;
            }
            return false;
        }

        public bool PointLiesOnArc(double a)
        {
            return PointLiesOnArc(Point(a));
        }

        public new clsPoint Tangent(double a)
        {
            //Reurns the tangent in the direction of the arc
            clsPoint aPt;

            aPt = base.Tangent(a);
            aPt.Scale(Direction);
            return aPt;
        }

        public new clsLine TangentLine(double a)
        {
            //Returns the tangent in the direction of the arc
            clsLine l1;

            l1 = base.TangentLine(a);
            l1.Scale(Direction);
            return l1;
        }

        public clsLine TangentToPointDown(clsPoint pt1)
        {
            //Goes on the downside of a transition down
            double a;
            double x;
            clsLine l1;

            if (pt1.x > Centre.x - mdlGeometry.myTol)
                return null;
            a = Atan((Centre.y - pt1.y) / Abs((Centre.x - pt1.x)));
            x = Radius / pt1.Dist(Centre);
            if (x >= 1)
                return null;

            a = Acos(x) - a;
            l1 = new clsLine(pt1.Copy(), new clsPoint(Centre.x - Radius * Cos(a), Centre.y + Radius * Sin(a)));
            return l1;
        }

        public clsPoint Intersect(clsLine l1)
        {
            //Limits the intersection to the arc
            clsPoint aPt;

            aPt = base.Intersect(l1);
            if (aPt == null)
                return null;
            if (PointLiesOnArc(aPt) == false)
            {
                aPt = base.Intersect(l1, true);
                if (aPt == null)
                    return null;
                if (PointLiesOnArc(aPt) == false)
                    return null;
            }
            return aPt;
        }

        public clsLine TangentToHeight(double h)
        {
            clsPoint pt1;
            clsPoint pt2;

            pt2 = StartPoint();
            if (StartAngle() == 0 | pt2.Y <= h)
                return null;
            pt1 = new clsPoint(pt2.X - (pt2.Y - h) / Tan(StartAngle()), h);
            return new clsLine(pt1, pt2);
        }

        public clsLine TangentAtAngle(double a, int aDir, double d = 1)
        {
            //aDir = -1 => downwards
            clsPoint pt1;
            clsPoint pt2;

            if (Direction == -1)
            {
                pt1 = Point(PI / 2 + a);
                pt2 = new clsPoint(pt1.x + d * aDir * Cos(a), pt1.y + d * aDir * Sin(a));
                return new clsLine(pt1, pt2);
            }
            else
            {
                pt1 = Point(3 * PI / 2 + a);
                pt2 = new clsPoint(pt1.x + d * aDir * Cos(a), pt1.y + d * aDir * Sin(a));
                return new clsLine(pt1, pt2);
            }
        }

        public clsLine TangentToPoint(clsPoint pt1)
        {
            double a;
            double x;
            clsLine l1 = new clsLine();

            if (pt1.X == Centre.X)
            {
                if (pt1.Y > Centre.Y)
                {
                    a = PI / 2;
                }
                else
                {
                    a = 3 * PI / 2;
                }
            }
            else
            {
                a = Atan((pt1.Y - Centre.Y) / Abs((pt1.X - Centre.X)));
                if (pt1.X > Centre.X)
                    a = PI - a;
            }
            x = Radius / pt1.Dist(Centre);
            if (x >= 1)
                return null;

            a = a - (Atan(-x / Sqrt(-x * x + 1)) + 2 * Atan(1));
            l1.P1.X = Centre.X - Radius * Cos(a);
            l1.P1.Y = Centre.Y + Radius * Sin(a);
            l1.P2.X = pt1.X;
            l1.P2.Y = pt1.Y;
            //l1.p2.y = l1.p1.y - (pt1.x - l1.p1.x) * Tan((180 - a) * Pi / 180)
            Angle2 = a;
            return l1;
        }

        public clsLine TangentToPointUp(clsPoint pt1)
        {
            //Goes on a transition up
            double a;
            double x;
            clsLine l1 = new clsLine();

            if (pt1.X < Centre.X + mdlGeometry.myTol)
                return null;
            a = Atan((pt1.Y - Centre.Y) / Abs((pt1.X - Centre.X)));
            x = Radius / pt1.Dist(Centre);
            if (x >= 1)
                return null;

            a = a - Acos(x);
            l1.P1.X = Centre.X + Radius * Cos(a);
            l1.P1.Y = Centre.Y + Radius * Sin(a);
            l1.P2.X = pt1.X;
            l1.P2.Y = pt1.Y;
            return l1;
        }

        public new void Rotate(double theta)
        {
            Centre.Rotate(theta);
            Angle1 = Angle1 + theta;
            Angle2 = Angle2 + theta;
        }

        public clsPoint FilletPoint()
        {
            clsLine l1;
            clsLine l2;
            clsPoint p1;
            clsPoint p2;

            p1 = StartPoint();
            p2 = Tangent(Angle1);
            l1 = new clsLine(p1.X, p1.Y, p1.X + p2.X, p1.Y + p2.Y);
            p1 = EndPoint();
            p2 = Tangent(Angle2);
            l2 = new clsLine(p1.X, p1.Y, p1.X + p2.X, p1.Y + p2.Y);
            return l1.Intersect(l2);
        }
        
    }
}


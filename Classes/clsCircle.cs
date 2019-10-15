using System;
using System.IO;
using System.Collections.Generic;
using static System.Math;

namespace BatchProcess
{
    public class clsCircle
    {

        clsPoint myCentre;

        double myRadius;
        public clsPoint Centre
        {
            get { return myCentre; }
            set { myCentre = value; }
        }

        public double Radius
        {
            get { return myRadius; }
            set { myRadius = value; }
        }

        public clsCircle(clsPoint aCentre, double aRadius)
        {
            Centre = aCentre.Copy();
            Radius = aRadius;
        }

        public clsCircle Copy()
        {
            return new clsCircle(Centre, Radius);
        }

        public clsPoint Point(double a)
        {
            //Returns a point on the circumference of the circle
            return new clsPoint(Centre.x + Radius * Cos(a), Centre.y + Radius * Sin(a));
        }

        public double AngleFromPoint(clsPoint3d aPoint)
        {
            //Returns the angle of a 3d point with relation to the centre of the circle
            return mdlGeometry.Angle(aPoint.x - Centre.x, aPoint.y - Centre.y);
        }

        public double AngleFromPoint(clsPoint aPoint)
        {
            //Returns the angle of a point with relation to the centre of the circle
            return mdlGeometry.Angle(aPoint.x - Centre.x, aPoint.y - Centre.y);
        }

        public clsPoint Tangent(double a)
        {
            //Unit vector in the CCW direction
            clsPoint aPt = default(clsPoint);
            double x = 0;
            double y = 0;
            double d = 0;

            aPt = Point(a);
            x = -(aPt.y - Centre.y);
            y = (aPt.x - Centre.x);
            d = Sqrt(Pow(x, 2) + Pow(y, 2));
            return new clsPoint(x / d, y / d);
        }

        public clsLine TangentLine(double a)
        {
            //Unit vector in the CCW direction, starting from the Tangent point
            clsPoint aPt = default(clsPoint);
            double x = 0;
            double y = 0;
            double d = 0;

            aPt = Point(a);
            x = -(aPt.y - Centre.y);
            y = (aPt.x - Centre.x);
            d = Sqrt(Pow(x, 2) + Pow(y, 2));
            return new clsLine(aPt.x, aPt.y, aPt.x + x / d, aPt.y + y / d);
        }

        public double Distance(double a1, double a2)
        {
            //Returns the distance along the circumference between 2 points, starting from a1 and travelling in CCW direction
            if (a1 <= a2)
                return (a2 - a1) * Radius;
            return (a1 + (2 * PI - a2)) * Radius;
        }

        public double Distance(clsPoint p1, clsPoint p2)
        {
            //Returns the distance along the circumference between 2 points, starting from p1 and travelling in CCW direction
            double a1 = 0;
            double a2 = 0;

            a1 = AngleFromPoint(p1);
            a2 = AngleFromPoint(p2);
            return Distance(a1, a2);
        }


        public clsCircle()
        {
        }

        public clsPoint Intersect(clsLine l1, bool getSecond = false)
        {
            double a = 0;
            double b = 0;
            double c = 0;
            double xc = 0;
            double yc = 0;
            double lambda1 = 0;
            double lambda2 = 0;

			if (mdlGeometry.Dist(Centre, l1) > Radius)
                return null;
            xc = Centre.x;
            yc = Centre.y;

            a = Pow((l1.X2 - l1.X1), 2) + Pow((l1.Y2 - l1.Y1), 2);
            b = 2 * ((l1.X2 - l1.X1) * (l1.X1 - xc) + (l1.Y2 - l1.Y1) * (l1.Y1 - yc));
            c = Pow((l1.X1 - xc), 2) + Pow((l1.Y1 - yc), 2) - Pow((Radius), 2);
            lambda1 = (-b + Sqrt(Abs(Pow(b, 2) - 4 * a * c))) / (2 * a);
            lambda2 = (-b - Sqrt(Abs(Pow(b, 2) - 4 * a * c))) / (2 * a);
            if (getSecond == false)
            {
                if (lambda2 < lambda1 & lambda2 > 0)
                    lambda1 = lambda2;
            }
            else
            {
                if (lambda1 < lambda2)
                    lambda1 = lambda2;
            }
            return new clsPoint(l1.X1 + lambda1 * (l1.X2 - l1.X1), l1.Y1 + lambda1 * (l1.Y2 - l1.Y1));
        }

        public clsPoint Intersect(clsCircle c1, bool firstSolution)
        {
            double a = 0;
            clsLine l1 = default(clsLine);

            if (mdlGeometry.Dist(Centre, c1.Centre) > Radius + c1.Radius)
                return null;
			if (mdlGeometry.Dist(Centre, c1.Centre) < Abs(Radius - c1.Radius))
                return null;
            l1 = new clsLine(Centre.Copy(), c1.Centre.Copy());
            a = Acos((Radius * Radius + l1.Length * l1.Length - c1.Radius * c1.Radius) / (2 * Radius * l1.Length));
            //Cosine rule
            if (firstSolution == false)
                a = -a;
            l1.Rotate(a);
            l1.Length = Radius;
            return l1.P2;
        }

        public void Move(double x, double y)
        {
            myCentre.Move(x, y);
        }

        public void Move(clsPoint fromPt, clsPoint toPt)
        {
            myCentre.Move(toPt.x - fromPt.x, toPt.y - fromPt.y);
        }

        public void Move(clsPoint p1)
        {
            myCentre.Move(p1.x, p1.y);
        }

        public void Rotate(double theta)
        {
            Centre.Rotate(theta);
        }
    }
}


//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik, @toddanglin
//Facebook: facebook.com/telerik
//=======================================================

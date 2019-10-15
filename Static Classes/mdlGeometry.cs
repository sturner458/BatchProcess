using System;
using static System.Math;

namespace BatchProcess
{
    public static class mdlGeometry
    {
        //Now working in millimetres
        public static double myTol = 0.01;
        //Now working in millimetres
        public static double myTolFine = 0.01;
        //Now working in millimetres
        public static double myAngTol = 0.001;

        public static double Angle(double x0, double y0, double x1, double y1)
        {
            double x = 0;
            double y = 0;

            x = x1 - x0;
            y = y1 - y0;
            return Angle(x, y);
        }

        public static double Angle(clsPoint v1, clsPoint v2)
        {
            return AngleVect(v1.X, v1.Y, v2.X, v2.Y);
        }

        public static double AngleVect(double x0, double y0, double x1, double y1)
        {
            double functionReturnValue = 0;
            double theta = 0;
            double theta2 = 0;

            theta = Angle(x0, y0);
            theta2 = Angle(x1, y1);
            functionReturnValue = theta2 - theta;
            if (functionReturnValue > PI)
                functionReturnValue = functionReturnValue - 2 * PI;
            if (functionReturnValue < -PI)
                functionReturnValue = functionReturnValue + 2 * PI;
            return functionReturnValue;
        }

        public static double AngleDiff(double a1, double a2)
        {
            double a;

            a = a1 - a2;
            //Make angle lie between 0 and pi
            while (a < 0)
            {
                a = a + 2 * PI;
            }
            while (a > 2 * PI)
            {
                a = a - 2 * PI;
            }
            if (a > PI)
            {
                a = 2 * PI - a;
            }

            return a;
        }

        public static double Angle(double x, double y)
        {
            double a = 0;
            double aTol = myTol;

            if (IsOrigin(x, y)) aTol = myTol / 1000;
            
            if (IsOrigin(x, y, aTol))
            {
                return 0;
            }
            if (x >= 0)
            {
                //top right quadrant
                if (y >= 0)
                {
                    if (Abs(x) < aTol)
                    {
                        return PI / 2;
                    }
                    else
                    {
                        return Atan(y / x);
                    }
                    //bottom right quadrant
                }
                else
                {
                    if (Abs(x) < aTol)
                    {
                        return 3 * PI / 2;
                    }
                    else
                    {
                        a = 2 * PI - Atan(Abs(y) / x);
                        if (Abs(a - 2 * PI) < aTol)
                            a = 0;
                        return a;
                    }
                }
            }
            else
            {
                //top left quadrant
                if (y >= 0)
                {
                    if (Abs(x) < aTol)
                    {
						return PI / 2;
                    }
                    else
                    {
						return PI - Atan(y / Abs(x));
                    }
                    // bottom left quadrant
                }
                else
                {
					if (Abs(x) < aTol)
                    {
						return 3 * PI / 2;
                    }
                    else
                    {
						return PI + Atan(Abs(y) / Abs(x));
                    }
                }
            }
        }

        public static double Angle(clsLine l1, clsLine l2)
        {
            return AngleVect(l1.P2.X - l1.P1.X, l1.P2.Y - l1.P1.Y, l2.P2.X - l2.P1.X, l2.P2.Y - l2.P1.Y);
        }

        public static bool IsSameDbl(double d1, double d2, double aTol = 0)
        {
            if (aTol == 0)
                aTol = myTol / 10;
            return (Abs(d1 - d2) < aTol);
        }

        public static bool IsSameAngle(double d1, double d2, double aTol = 0)
        {
            if (aTol == 0)
                aTol = 0.1 * PI / 180;
            //Gets it less than 0.1 degrees

            //Allow the angles to vary between 0 and 2*pi + aTol. Then identify angles separated by 0 or 2*pi.
            while (d1 < 0)
            {
				d1 = d1 + 2 * PI;
            }
			while (d1 > 2 * PI + aTol)
            {
				d1 = d1 - 2 * PI;
            }
            while (d2 < 0)
            {
				d2 = d2 + 2 * PI;
            }
			while (d2 > 2 * PI + aTol)
            {
				d2 = d2 - 2 * PI;
            }

			if (Abs(d1 - d2) < aTol)
                return true;
			if (Abs(Abs(d1 - d2) - 2 * PI) < aTol)
                return true;
            return false;
        }

        public static bool IsSameAngleDegree(double d1, double d2, double aTol = 0)
        {
            if (aTol == 0)
                aTol = 0.1;
            //Gets it less than 0.1 degrees

            //Allow the angles to vary between 0 and 360 + aTol. Then identify angles separated by 0 or 360.
            while (d1 < 0)
            {
                d1 = d1 + 360;
            }
            while (d1 > 360 + aTol)
            {
                d1 = d1 - 360;
            }
            while (d2 < 0)
            {
                d2 = d2 + 360;
            }
            while (d2 > 360 + aTol)
            {
                d2 = d2 - 360;
            }

			if (Abs(d1 - d2) < aTol)
                return true;
			if (Abs(Abs(d1 - d2) - 360) < aTol)
                return true;
            return false;
        }

        public static double Dot(double x0, double y0, double x1, double y1)
        {
            return x0 * x1 + y0 * y1;
        }

        public static bool IsZero(double d)
        {
			return (Abs(d) < myTol);
        }

        public static double Dist(clsPoint p1, clsPoint p2)
        {
			return Sqrt(Pow((p1.X - p2.X), 2) + Pow((p1.Y - p2.Y), 2));
        }

        public static double Dist(clsPoint3d p1, clsPoint3d p2)
        {
			return Sqrt(Pow((p1.X - p2.X), 2) + Pow((p1.Y - p2.Y), 2) + Pow((p1.Z - p2.Z), 2));
        }

        public static double Dist(clsPoint p1, clsLine l1)
        {
            //+ve for outside the wall, left handed; Also, +ve for above straight.
            clsLine l2 = new clsLine();
            clsLine l3 = new clsLine();

            l2 = (clsLine)l1.Normal();
            l3 = new clsLine(l1.P1, p1);

            return l2.Dot(l3);
        }

        public static double Dist3d(clsPoint3d p1, clsPoint3d p2)
        {
			return Sqrt(Pow((p1.X - p2.X), 2) + Pow((p1.Y - p2.Y), 2) + Pow((p1.Z - p2.Z), 2));
        }

        public static double Dist3d(clsPoint3d p1, clsLine3d l1)
        {
            double d1 = 0;
            double d2 = 0;
            clsLine3d l2 = new clsLine3d();
            clsLine3d l3 = new clsLine3d();

            l2 = l1.Copy();
            l2.Normalise();
            l3 = new clsLine3d(l1.P1, p1);
            d1 = l2.Dot(l3);
            d2 = l3.Length;
			return Sqrt(Pow(d2, 2) - Pow(d1, 2));
        }

        public static double DistancePointLine3D(clsPoint3d p1, clsLine3d l1)
        {
            //Distance between 3D point and truncated 3D line
            clsPoint3d p3d = new clsPoint3d();
            double l = 0;

            p3d = ProjectPoint(p1, l1);
            l = l1.Lambda(p3d);
            if (l < 0)
                return p1.Dist(l1.P1);
            if (l > 1)
                return p1.Dist(l1.P2);
            return p1.Dist(p3d);
        }

        public static double DistPlan(clsPoint3d p1, clsPoint3d p2)
        {
			return Sqrt(Pow((p1.X - p2.X), 2) + Pow((p1.Y - p2.Y), 2));
        }

        public static bool IsOnLine(clsPoint p1, clsPoint p2, clsPoint p3)
        {
			if (Abs(Dist(p1, new clsLine(p2, p3))) > myTol)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static clsPoint ProjectPoint(clsPoint p1, clsLine l1)
        {
            double d = 0;
            clsPoint pt1 = new clsPoint();
            clsLine l2 = new clsLine();

            d = Dist(p1, l1);
            pt1 = p1.Copy();
            l2 = (clsLine)l1.Normal();
            l2.Normalise();
            l2.Scale(-d);
            pt1.Move(l2.P2.X, l2.P2.Y);
            return pt1;
        }

        public static clsPoint3d ProjectPoint(clsPoint3d p1, clsLine3d l1)
        {
            double d1 = 0;
            clsLine3d l2 = new clsLine3d();
            clsLine3d l3 = new clsLine3d();

            l2 = l1.Copy();
            l2.Normalise();
            l3 = new clsLine3d(l1.P1, p1);
            d1 = l2.Dot(l3);
            l2.Length = d1;
            return l2.P2;
        }

        public static clsPoint Origin2d()
        {
            return new clsPoint(0, 0);
        }

        public static clsPoint3d Origin3d()
        {
            return new clsPoint3d(0, 0, 0);
        }

        public static bool IsOrigin(double x, double y, double aTol = 0)
        {
            if (aTol == 0) aTol = myTol;
			if (Sqrt(Pow(x, 2) + Pow(y, 2)) < aTol)
                return true;
            return false;
        }
        
        public static clsLine LinePointToTangent(clsPoint p1, clsCircle c1)
        {
            double a = 0;
            double b = 0;
            double r = 0;
            double d = 0;
            clsLine l1 = new clsLine();
            clsPoint p2 = new clsPoint();

            r = c1.Radius;
            d = p1.Dist(c1.Centre);
            if (d < r)
                return null;
			a = Asin(r / d);
            l1 = new clsLine(p1, c1.Centre);
            b = l1.Angle;

			p2 = c1.Point(PI / 2 + a + b);
            return new clsLine(p1, p2);
        }

        public static clsLine LineTangentToArcs(clsCircle c1, clsCircle c2, int n = -1)
        {
            clsArc myArc1 = new clsArc();
            clsArc myArc2 = new clsArc();

            //n is the solution number, from 1 to 4 (see document SolutionsToTangencies.doc)
            //1 = Plus - Minus
            //2 = Plus - Plus
            //3 = Minus - Minus
            //4 = Minus - Plus
            //In this case, you must pass in an arc
            if (n == -1)
            {
                myArc1 = (clsArc)c1;
                myArc2 = (clsArc)c2;
                if (myArc1.Direction == 1 & myArc2.Direction == -1)
                    n = 1;
                if (myArc1.Direction == 1 & myArc2.Direction == 1)
                    n = 2;
                if (myArc1.Direction == -1 & myArc2.Direction == -1)
                    n = 3;
                if (myArc1.Direction == -1 & myArc2.Direction == 1)
                    n = 4;
            }

            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            double r1 = 0;
            double r2 = 0;
            double d = 0;
            double theta = 0;
            double alpha = 0;
            double a = 0;
            clsPoint p1 = new clsPoint();
            clsPoint p2 = new clsPoint();

            x1 = c1.Centre.X;
            y1 = c1.Centre.Y;
            x2 = c2.Centre.X;
            y2 = c2.Centre.Y;
            r1 = c1.Radius;
            r2 = c2.Radius;

			d = Sqrt(Pow((x2 - x1), 2) + Pow((y2 - y1), 2));
            theta = Angle(x1, y1, x2, y2);

            if (n == 1)
            {
                if (d < r1 + r2)
                    return null;
				alpha = Acos((r1 + r2) / d);
                a = theta - alpha;
				p1 = new clsPoint(x1 + r1 * Cos(a), y1 + r1 * Sin(a));
				p2 = new clsPoint(x2 - r2 * Cos(a), y2 - r2 * Sin(a));
            }
            else if (n == 2)
            {
				if (d < Abs(r2 - r1))
                    return null;
				alpha = Acos((r2 - r1) / d);
                a = alpha - theta;
				p1 = new clsPoint(x1 + r1 * Cos(a), y1 - r1 * Sin(a));
				p2 = new clsPoint(x2 + r2 * Cos(a), y2 - r2 * Sin(a));
            }
            else if (n == 3)
            {
				if (d < Abs(r2 - r1))
                    return null;
				alpha = PI / 2 + Asin((r2 - r1) / d);
                a = alpha + theta;
				p1 = new clsPoint(x1 + r1 * Cos(a), y1 + r1 * Sin(a));
				p2 = new clsPoint(x2 + r2 * Cos(a), y2 + r2 * Sin(a));
            }
            else if (n == 4)
            {
                if (d < r1 + r2)
                    return null;
				alpha = Acos((r1 + r2) / d);
                a = theta + alpha;
				p1 = new clsPoint(x1 + r1 * Cos(a), y1 + r1 * Sin(a));
				p2 = new clsPoint(x2 - r2 * Cos(a), y2 - r2 * Sin(a));
            }
            else
            {
                return null;
            }

            return new clsLine(p1, p2);
        }

        public static clsPoint3d ProjectPointOntoPlaneAlongY(clsPoint p, clsLine3d l1)
        {
            clsLine3d v1 = new clsLine3d();
            double u = 0;
            double r = 0;
            double z = 0;
            clsPoint3d p1 = new clsPoint3d();

            v1 = new clsLine3d(new clsPoint3d(0, 0, 0), new clsPoint3d(0, 1, 0));
            u = v1.Dot(l1);
            //This is the length of the normal vector, measured in the vertical direction

            p1 = new clsPoint3d(p.X - l1.X1, 0 - l1.Y1, -p.Y - l1.Z1);
            r = p1.Dot(l1.DP());
            //r is the distance of our point to the nearest point on the plane
            //r = v1.Dot(myV)
            z = r / u;
            //z is the vertical distance of our point to the plane. It is sign sensitive - positive is above the plane.

            return new clsPoint3d(p.X, -z, -p.Y);
        }

        public static clsPoint3d ProjectPointOntoPlaneAlongZ(clsPoint p, clsLine3d l1)
        {
            clsLine3d v1 = new clsLine3d();
            double u = 0;
            double r = 0;
            double z = 0;
            clsPoint3d p1 = new clsPoint3d();

            v1 = new clsLine3d(new clsPoint3d(0, 0, 0), new clsPoint3d(0, 0, 1));
            u = v1.Dot(l1);
            //This is the length of the normal vector, measured in the vertical direction

            p1 = new clsPoint3d(p.X - l1.X1, p.Y - l1.Y1, 0 - l1.Z1);
            r = p1.Dot(l1.DP());
            //r is the distance of our point to the nearest point on the plane
            //r = v1.Dot(myV)
            z = r / u;
            //z is the vertical distance of our point to the plane. It is sign sensitive - positive is above the plane.

            return new clsPoint3d(p.X, p.Y, -z);
        }

        public static double GetAngle(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            double dot_product = 0;
            double cross_product = 0;

            // Get the dot product and cross product.
            dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);
            cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

            // Calculate the angle.
            return ATan2(cross_product, dot_product);
        }

        public static double DotProduct(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            double BAx = 0;
            double BAy = 0;
            double BCx = 0;
            double BCy = 0;

            // Get the vectors' coordinates.
            BAx = Ax - Bx;
            BAy = Ay - By;
            BCx = Cx - Bx;
            BCy = Cy - By;

            // Calculate the dot product.
            return BAx * BCx + BAy * BCy;
        }

        public static double CrossProductLength(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            double BAx = 0;
            double BAy = 0;
            double BCx = 0;
            double BCy = 0;

            // Get the vectors' coordinates.
            BAx = Ax - Bx;
            BAy = Ay - By;
            BCx = Cx - Bx;
            BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return BAx * BCy - BAy * BCx;
        }

        public static double ATan2(double opp, double adj)
        {
            double angle = 0;

            // Get the basic angle.
			if (Abs(adj) < 0.0001)
            {
				angle = PI / 2;
            }
            else
            {
				angle = Abs(Atan(opp / adj));
            }

            // See if we are in quadrant 2 or 3.
            if (adj < 0)
            {
                // angle > PI/2 or angle < -PI/2.
				angle = PI - angle;
            }

            // See if we are in quadrant 3 or 4.
            if (opp < 0)
            {
                angle = -angle;
            }

            // Return the result.
            return angle;
        }
    }
}


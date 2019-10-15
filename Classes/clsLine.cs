using System;
using System.IO;
using System.Collections.Generic;
using static System.Math;

namespace BatchProcess
{
    public class clsLine
    {
        clsPoint myP1;

        clsPoint myP2;

        public double X1
        {
            get { return myP1.x; }
            set { myP1.x = value; }
        }

        public double Y1
        {
            get { return myP1.y; }
            set { myP1.y = value; }
        }

        public double X2
        {
            get { return myP2.x; }
            set { myP2.x = value; }
        }

        public double Y2
        {
            get { return myP2.y; }
            set { myP2.y = value; }
        }

        public double DX()
        {
            return X2 - X1;
        }

        public double DY()
        {
            return Y2 - Y1;
        }

        public clsPoint P1
        {
            get { return myP1; }
            set { myP1 = value; }
        }

        public clsPoint P2
        {
            get { return myP2; }
            set { myP2 = value; }
        }

        public clsLine()
        {
            myP1 = new clsPoint();
            myP2 = new clsPoint();
        }

        public clsLine(double x1, double y1, double x2, double y2)
        {
            myP1 = new clsPoint(x1, y1);
            myP2 = new clsPoint(x2, y2);
        }

        public clsLine(clsPoint pt1, clsPoint pt2)
        {
            myP1 = pt1.Copy();
            myP2 = pt2.Copy();
        }

        public clsLine(clsPoint pt1, double ang, double len)
        {
            myP1 = new clsPoint(pt1.x, pt1.y);
            myP2 = new clsPoint(pt1.x + len * Cos(ang), pt1.y + len * Sin(ang));
        }
        
        public clsPoint Point
        {
            get { return new clsPoint(DX(), DY()); }
        }

        public double Length
        {
            get { return Sqrt(Pow(DX(), 2) + Pow(DY(), 2)); }
            set
            {
                double theta = 0;

                theta = Angle;
                P2.X = P1.X + value * Cos(theta);
                P2.Y = P1.Y + value * Sin(theta);
            }
        }

        public double Length2
        {
            //Moves the first point
            get { return Sqrt(Pow(DX(), 2) + Pow(DY(), 2)); }
            set
            {
                double theta = 0;

                theta = Angle;
                P1.X = P2.X - value * Cos(theta);
                P1.Y = P2.Y - value * Sin(theta);
            }
        }

        public double Angle
        {
            get
            {
                double a = mdlGeometry.Angle(DX(), DY());
                if (mdlGeometry.IsSameDbl(a, 2 * PI))
                    return 0;
                return a;
            }
            set
            {
                double r = 0;

                r = Length;
                X2 = X1 + r * Cos(value);
                Y2 = Y1 + r * Sin(value);
            }
        }

        public double Angle2
        {
            get
            {
                double a = mdlGeometry.Angle(DX(), DY());
                if (mdlGeometry.IsSameDbl(a, 2 * PI))
                    return 0;
                return a;
            }
            set
            {
                double r = 0;

                r = Length;
                X1 = X2 - r * Cos(value);
                Y1 = Y2 - r * Sin(value);
            }
        }

        public double AngleDegree
        {
            get { return mdlGeometry.Angle(DX(), DY()) * 180 / PI; }
            set
            {
                double r = 0;

                r = Length;
                X2 = X1 + r * Cos(value * PI / 180);
                Y2 = Y1 + r * Sin(value * PI / 180);
            }
        }

        public clsLine3d Line3D(double z1 = 0.0, double z2 = 0.0)
        {
            return new clsLine3d(myP1.x, myP1.y, z1, myP2.x, myP2.y, z2);
        }

        public void Reverse()
        {
            clsPoint pt1 = default(clsPoint);

            pt1 = myP1;
            myP1 = myP2;
            myP2 = pt1;
        }

        //Mirror the line, then reverse it
        public void Flip()
        {
            Scale(-1);
            Reverse();
        }

        public void Rotate(double myAngle)
        {
            double r = 0;
            double theta = 0;

            r = Length;
            theta = Angle;
            P2.X = P1.X + r * Cos(theta + myAngle);
            P2.Y = P1.Y + r * Sin(theta + myAngle);
        }

        //Rotates the line about the origin, rather than the end point of the line
        public void RotateLine(double theta)
        {
            P1.Rotate(theta);
            P2.Rotate(theta);
        }

        public clsLine Copy()
        {
            clsPoint pt1 = new clsPoint();
            clsPoint pt2 = new clsPoint();

            pt1 = P1.Copy();
            pt2 = P2.Copy();
            return new clsLine(pt1, pt2);
        }

        public void Normalise()
        {
            double theta = 0;

            if (mdlGeometry.IsSameDbl(Length, 0))
                return;
            theta = Angle;
            P2.X = P1.X + Cos(theta);
            P2.Y = P1.Y + Sin(theta);
        }

        public void Scale(double myScale)
        {
            double theta = 0;
            double r = 0;

            theta = Angle;
            r = Length;
            P2.X = P1.X + r * myScale * Cos(theta);
            P2.Y = P1.Y + r * myScale * Sin(theta);
        }

        public double Dot(clsLine l1)
        {
            return (P2.X - P1.X) * (l1.P2.X - l1.P1.X) + (P2.Y - P1.Y) * (l1.P2.Y - l1.P1.Y);
        }

        public clsPoint MidPoint()
        {
            return new clsPoint((X1 + X2) / 2, (Y1 + Y2) / 2);
        }

        public clsLine Normal()
        {
            clsLine functionReturnValue = default(clsLine);
            //Special routine to help with finding "signed" distance of points to lines
            double theta = 0;

            theta = Angle;
            functionReturnValue = new clsLine(0, 0, 0, 1);
            functionReturnValue.Rotate(Angle);
            return functionReturnValue;
        }

        //Offsets the line
        public void Move(double d)
        {
            double theta = 0;

            theta = Angle;
            RotateLine(-theta);
            P1.Y = P1.Y + d;
            P2.Y = P2.Y + d;
            RotateLine(theta);
        }

        //Moves it by a fixed amount
        public void Move(double x, double y)
        {
            X1 = X1 + x;
            Y1 = Y1 + y;
            X2 = X2 + x;
            Y2 = Y2 + y;
        }
        //Moves it by a fixed amount
        public void Move(clsPoint pt1)
        {
            X1 = X1 + pt1.x;
            Y1 = Y1 + pt1.y;
            X2 = X2 + pt1.x;
            Y2 = Y2 + pt1.y;
        }

        public bool IsSame(clsLine l1)
        {
            //Is this line the same as another (up to orientation)?
            if ((P1 == l1.P1 & P2 == l1.P2) | (P1 == l1.P2 & P2 == l1.P1))
                return true;
            return false;
        }

        public clsPoint IntersectShortLines(clsLine l1)
        {
            //Both truncated
            if (IntersectShortLine1(l1) == null)
                return null;
            return IntersectShortLine2(l1);
        }

        public clsPoint IntersectShortLine1(clsLine l1)
        {
            //Leaves Me as infinite, but truncates l1
            double l = 0;
            clsPoint pt1 = default(clsPoint);

            pt1 = Intersect(l1);
            if (pt1 == null)
                return null;
            l = l1.Lambda(pt1);
            if (l < 0 | l > 1)
                return null;
            return pt1;
        }

        public clsPoint IntersectShortLine2(clsLine l1)
        {
            //Truncates Me but leaves l1 infinite
            double l = 0;
            clsPoint pt1 = default(clsPoint);

            pt1 = Intersect(l1);
            if (pt1 == null)
                return null;
            l = Lambda(pt1);
            if (l < 0 | l > 1)
                return null;
            return pt1;
        }

        //Public Function IntersectShortLine1(ByVal l1 As clsLine) As clsPoint  'Leaves Me as infinite, but truncates l1
        //    Dim lambda As Double

        //    If Abs((l1.X2 - l1.X1) * (Y1 - Y2) + (l1.Y2 - l1.Y1) * (X2 - X1)) < mdlGeometry.myTol Then Return Nothing 'No intersection
        //    lambda = ((X2 - l1.X1) * (Y1 - Y2) + (Y2 - l1.Y1) * (X2 - X1)) / ((l1.X2 - l1.X1) * (Y1 - Y2) + (l1.Y2 - l1.Y1) * (X2 - X1))
        //    If lambda < 0 Or lambda > 1 Then Return Nothing
        //    Return New clsPoint(l1.X1 + lambda * (l1.X2 - l1.X1), l1.Y1 + lambda * (l1.Y2 - l1.Y1))
        //End Function

        //Public Function IntersectShortLine2(ByVal l1 As clsLine) As clsPoint 'Truncates Me but leaves l1 infinite
        //    Dim lambda As Double

        //    If Abs((X2 - X1) * (l1.Y1 - l1.Y2) + (Y2 - Y1) * (l1.X2 - l1.X1)) < mdlGeometry.myTol Then Return Nothing 'No intersection
        //    lambda = ((l1.X2 - X1) * (l1.Y1 - l1.Y2) + (l1.Y2 - Y1) * (l1.X2 - l1.X1)) / ((X2 - X1) * (l1.Y1 - l1.Y2) + (Y2 - Y1) * (l1.X2 - l1.X1))
        //    If lambda < 0 Or lambda > 1 Then Return Nothing
        //    Return New clsPoint(X1 + lambda * (X2 - X1), Y1 + lambda * (Y2 - Y1))
        //End Function

        public clsPoint Intersect(clsLine l1)
        {
            //Both infinite. More accurate that IntersectQuick
            double l = 0;
            clsLine l2 = default(clsLine);
            clsLine l3 = default(clsLine);

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
            return new clsPoint(l2.X1 + l * (l2.X2 - l2.X1), l2.Y1 + l * (l2.Y2 - l2.Y1));
        }

        public clsPoint IntersectQuick(clsLine l1)
        {
            //Both infinite
            double lambda = 0;

            if (Abs((X2 - X1) * (l1.Y1 - l1.Y2) + (Y2 - Y1) * (l1.X2 - l1.X1)) < mdlGeometry.myTol / 10)
                return null;
            //No intersection

            lambda = ((l1.X2 - X1) * (l1.Y1 - l1.Y2) + (l1.Y2 - Y1) * (l1.X2 - l1.X1)) / ((X2 - X1) * (l1.Y1 - l1.Y2) + (Y2 - Y1) * (l1.X2 - l1.X1));
            return new clsPoint(X1 + lambda * (X2 - X1), Y1 + lambda * (Y2 - Y1));
        }

        public bool Overlaps(clsLine aLine, double aTol = 0)
        {
            clsLine l1 = default(clsLine);
            clsPoint p3 = default(clsPoint);
            clsPoint p4 = default(clsPoint);

            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            l1 = aLine.Copy();
            if ((!IsOnLine(l1.P1, aTol)) | (!IsOnLine(l1.P2, aTol)))
                return false;
            if (Dot(l1) < 0)
                l1.Reverse();
            p3 = l1.P1;
            p4 = l1.P2;
            if (P1 == p3)
                return true;
            if (P1 == p4 | P2 == p3)
                return false;
            if (IsOnShortLine(p4, aTol))
                return true;
            if (l1.IsOnShortLine(P1, aTol) | l1.IsOnShortLine(P2, aTol))
                return true;
            return false;
        }

        public clsPoint Intersect(clsArc c1)
        {
            double a = 0;
            double b = 0;
            double c = 0;
            double xc = 0;
            double yc = 0;
            double lambda1 = 0;
            double lambda2 = 0;

            if (c1.Centre.Dist(this) > c1.Radius)
                return null;
            xc = c1.Centre.X;
            yc = c1.Centre.Y;

            a = Pow((X2 - X1), 2) + Pow((Y2 - Y1), 2);
            b = 2 * ((X2 - X1) * (X1 - xc) + (Y2 - Y1) * (Y1 - yc));
            c = Pow((X1 - xc), 2) + Pow((Y1 - yc), 2) - Pow((c1.Radius), 2);
            lambda1 = (-b + Sqrt(Abs(Pow(b, 2) - 4 * a * c))) / (2 * a);
            lambda2 = (-b - Sqrt(Abs(Pow(b, 2) - 4 * a * c))) / (2 * a);
            if (lambda2 < lambda1 & lambda2 > 0)
                lambda1 = lambda2;
            return new clsPoint(X1 + lambda1 * (X2 - X1), Y1 + lambda1 * (Y2 - Y1));
        }

        public bool IsOnLine(clsPoint aPt, double aTol = 0)
        {
            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            if (Abs(aPt.Dist(this)) > aTol)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool IsOnShortLine(clsPoint pt1, double aTol = 0, bool excludeEnds = false)
        {
            clsLine l1 = default(clsLine);
            clsLine l2 = default(clsLine);
            double d = 0;

            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            if (Abs(pt1.Dist(this)) > aTol)
                return false;
            l1 = new clsLine(P1, pt1);
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

        public bool IsOnHalfLine(clsPoint pt1, double aTol = 0)
        {
            clsLine l1 = default(clsLine);
            clsLine l2 = default(clsLine);
            double d = 0;

            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            if (Abs(pt1.Dist(this)) > aTol)
                return false;
            l1 = new clsLine(P1, pt1);
            l2 = Copy();
            l2.Normalise();
            d = l1.Dot(l2);
            if (d < -aTol)
                return false;
            return true;
        }

        public double Lambda(clsPoint pt1)
        {
            clsPoint p1a = default(clsPoint);
            clsPoint p2a = default(clsPoint);
            double d = 0;

            d = Length;
            if (mdlGeometry.IsSameDbl(d, 0))
                return 0;
            p1a = pt1 - P1;
            p2a = Point.Normalised();
            return p1a.Dot(p2a) / d;
        }

        public clsPoint PointFromLambda(double l)
        {
            clsLine l1 = default(clsLine);

            l1 = Copy();
            l1.Scale(l);
            return l1.P2;
        }
        public clsArc ArcTanMinus(bool atStart, bool downSide, bool avoidLeadInOut = false)
        {
            clsPoint pt1;
            double a1;
            double a2;
            clsArc c1;

            if (atStart == true)
            {
                pt1 = P1;
            }
            else
            {
                pt1 = P2;
            }

            if (downSide == false)
            {
                a1 = PI / 2 + Angle;
                a2 = PI / 2;
            }
            else
            {
                a1 = PI / 2 + PI / 4;
                a2 = PI / 2 + Angle;
            }

            c1 = new clsArc(new clsPoint(pt1.x + 250 * Sin(Angle), pt1.y - 250 * Cos(Angle)), 250, a1, a2, -1);
            if (avoidLeadInOut == false)
            {
                if (downSide)
                {
                    c1.Move(-c1.EndStraight());
                }
                else
                {
                    c1.Move(c1.StartStraight());
                }
            }

            return c1;
        }

        public clsArc ArcTanPlus(bool atStart, bool downSide, bool avoidLeadInOut = false)
        {
            clsPoint pt1;
            double a1;
            double a2;
            clsArc c1;

            if (atStart == true)
            {
                pt1 = P1;
            }
            else
            {
                pt1 = P2;
            }

            if (downSide)
            {
                a1 = 3 * PI / 2;
                a2 = 3 * PI / 2 + Angle;
            }
            else
            {
                a1 = 3 * PI / 2 + Angle;
                a2 = 3 * PI / 2 + PI / 4;
            }

            c1 = new clsArc(new clsPoint(pt1.x - 250 * Sin(Angle), pt1.y + 250 * Cos(Angle)), 250, a1, a2, 1);
            if (avoidLeadInOut == false)
            {
                if (downSide)
                {
                    c1.Move(-c1.EndStraight());
                }
                else
                {
                    c1.Move(c1.StartStraight());
                }
            }

            return c1;
        }

        public clsPoint DistanceAlongLine(double d)
        {
            clsLine l1 = default(clsLine);

            l1 = Copy();
            l1.Normalise();
            return new clsPoint(X1 + d * l1.DX(), Y1 + d * l1.DY());
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

        public void SetEntryHeight(double h)
        {
            double a = 0;

            a = Angle;
            if (a == 0 | a == PI)
                return;
            X1 = X2 - (Y2 - h) / Tan(a);
            Y1 = h;
        }
        

        public void SetEntryX(double x)
        {
            double a = 0;

            a = Angle;
            X1 = x;
            Y1 = Y2 + (x - X2) * Tan(a);
        }

        public void SetExitHeight(double h)
        {
            double a = 0;

            a = Angle;
            if (a == 0)
                return;
            X2 = X1 + (h - Y1) / Tan(a);
            Y2 = h;
        }

        public void SetExitX(double x)
        {
            double a = 0;

            a = Angle;
            if (a == 0)
                return;
            X2 = x;
            Y2 = Y1 + (x - X1) * Tan(a);
        }
        
        public double DistanceToShortLine(clsPoint pt1)
        {
            clsPoint pt2 = default(clsPoint);

            pt2 = mdlGeometry.ProjectPoint(pt1, this);
            if (Lambda(pt2) < 0)
                pt2 = P1;
            if (Lambda(pt2) > 1)
                pt2 = P2;
            if (mdlGeometry.IsSameDbl(Length, 0))
                pt2 = P1;
            return pt1.Dist(pt2);
        }
        
        public double NearestAngle(bool forceSteeper = false, bool forceLower = false)
        {
            double a = 0;
            double a1 = 0;

            a = AngleDegree;
            a1 = Round((AngleDegree / 5), 0, MidpointRounding.AwayFromZero) * 5;
            if (mdlGeometry.IsSameDbl(a, a1))
                return a * PI / 180;

            if (forceSteeper)
            {
                if (a1 > a)
                {
                    return a1 * PI / 180;
                }
                else
                {
                    return (a1 + 5) * PI / 180;
                }
            }
            else if (forceLower)
            {
                if (a1 < a)
                {
                    return a1 * PI / 180;
                }
                else
                {
                    return (a1 - 5) * PI / 180;
                }
            }

            return a1 * PI / 180;
        }
        
    }
}


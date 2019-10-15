using System;
using System.Collections.Generic;
using static System.Math;

namespace BatchProcess
{
    public class clsSketch3d
    {

        List<clsPoint3d> myPoints = new List<clsPoint3d>();
        public List<clsPoint3d> Points
        {
            get
            {
                return myPoints;
            }
        }

        public clsPoint3d Point(int n)
        {
            if (n < 0 | n > myPoints.Count - 1)
            {
                return null;
            }
            else
            {
                return myPoints[n].Copy();
            }
        }

        public clsPoint3d PointNoCopy(int n)
        {
            if (n < 0 | n > myPoints.Count - 1)
            {
                return null;
            }
            else
            {
                return myPoints[n];
            }
        }

        public clsLine3d Egde(int n)
        {
            int n1;

            n1 = n + 1;
            if (n1 > myPoints.Count - 1)
                n1 = 0;
            return new clsLine3d(Point(n), Point(n1));
        }

        public int NumPoints
        {
            get { return myPoints.Count - 1; }
        }


        public clsSketch3d()
        {
        }

        public clsSketch3d(List<clsPoint3d> p)
        {
            int i;

            for (i = 0; i <= p.Count - 1; i++)
            {
                myPoints.Add(p[i].Copy());
            }
        }
        
        public clsSketch3d Copy()
        {
            clsSketch3d aSketch = new clsSketch3d();
            int i;

            aSketch = new clsSketch3d();
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                aSketch.AddPoint(myPoints[i]);
            }
            return aSketch;
        }

        public double MinHeight()
        {
            int i;
            double d;

            if (NumPoints == -1)
                return 0;
            d = Point(0).Z;
            for (i = 1; i <= NumPoints; i++)
            {
                if (Point(i).Z < d)
                    d = Point(i).Z;
            }
            return d;
        }

        public double MaxHeight()
        {
            int i;
            double d;

            if (NumPoints == -1)
                return 0;
            d = Point(0).Z;
            for (i = 1; i <= NumPoints; i++)
            {
                if (Point(i).Z > d)
                    d = Point(i).Z;
            }
            return d;
        }

        public clsSketch Sketch2d()
        {
            int i;
            clsSketch mySketch;

            mySketch = new clsSketch();
            for (i = 0; i <= NumPoints; i++)
            {
                mySketch.AddPoint(myPoints[i].Point2D());
            }
            return mySketch;
        }

        public void Move(double z)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].Move(0, 0, z);
            }
        }

        public void Clear()
        {
            while (myPoints.Count - 1 > -1)
            {
                RemovePoint(myPoints.Count - 1);
            }
        }

        public void RemovePoint(clsPoint p1, double aTol = 0)
        {
            int i;

            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            i = 0;
            while (i <= NumPoints)
            {
                if (myPoints[i].Point2D().Dist(p1) < aTol)
                {
                    RemovePoint(i);
                }
                else
                {
                    i = i + 1;
                }
            }
        }

        public void RemovePoint(int i)
        {
            if (i < 0 | i > myPoints.Count - 1)
                return;
            myPoints.RemoveAt(i);
        }

        public clsPoint3d ProjectOntoPlane(clsPoint3d v, bool useRiser = true)
        {
            double x1;
            double y1;
            double z1;
            clsPoint3d v1;
            clsPoint3d v2;
            double u;
            double r;
            double z;
            int j;
            int n;

            if (useRiser)
            {
                j = NearestPoint(v);
                x1 = Point(j).x;
                y1 = Point(j).y;
                z1 = Point(j).z;
            }
            else
            {
                n = NumPoints;
                x1 = 0;
                y1 = 0;
                z1 = 0;
                for (j = 0; j <= n; j++)
                {
                    x1 += Point(j).x;
                    y1 += Point(j).y;
                    z1 += Point(j).z;
                }
                x1 = x1 / (n + 1);
                y1 = y1 / (n + 1);
                z1 = z1 / (n + 1);
            }

            v2 = NormalVector(useRiser);
            v1 = new clsPoint3d(0, 0, 1);
            u = v1.Dot(v2);
            //This is the length of the normal vector, measured in the vertical direction
            if (u < 0.7)
            {
                u = 1;
                v2 = v1.Copy();
            }

            v1 = new clsPoint3d(v.x - x1, v.y - y1, v.z - z1);
            r = v1.Dot(v2);
            //r is the distance of our point to the nearest point on the plane
            //r = v1.Dot(myV)
            z = r / u;
            //z is the vertical distance of our point to the plane. It is sign sensitive - positive is above the plane.

            return new clsPoint3d(v.x, v.y, v.z - z);
        }
        
        public void RemovePoint(clsPoint3d p1, double aTol = 0)
        {
            int i;

            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            i = 0;
            while (i <= NumPoints)
            {
                if (myPoints[i].Dist(p1) < aTol)
                {
                    RemovePoint(i);
                }
                else
                {
                    i = i + 1;
                }
            }
        }

        public int NearestPoint(clsPoint3d p1)
        {
            int i;
            double d;
            double d1;
            int ret;

            if (NumPoints == -1)
                return -1;
            ret = 0;
            d = Point(0).Dist(p1);
            for (i = 1; i <= NumPoints; i++)
            {
                d1 = Point(i).Dist(p1);
                if (d1 < d)
                {
                    d = d1;
                    ret = i;
                }
            }

            return ret;
        }

        public void AddPoint(clsPoint3d aPoint, int i = -1)
        {
            if (aPoint == null)
                return;
            if (i == -1)
            {
                myPoints.Add(aPoint.Copy());
            }
            else
            {
                myPoints.Insert(i, aPoint.Copy());
            }
        }

        public void AddPoint(double x, double y, double z)
        {
            myPoints.Add(new clsPoint3d(x, y, z));
        }

        //Rotates the order of points anti clockwise
        public void RotatePoints()
        {
            myPoints.Add(myPoints[0]);
            myPoints.RemoveAt(0);
        }

        //Rotates the order of points clockwise
        public void RotatePointsClockwise()
        {
            myPoints.Insert(0, myPoints[myPoints.Count - 1]);
            myPoints.RemoveAt(myPoints.Count - 1);
        }

        public void Mirror()
        {
            int i;
            int j;
            int n;
            clsPoint3d tempPoint;

            n = Convert.ToInt32(myPoints.Count - 1 / 2);

            //Start by swapping points 0 and 1
            //Then swap points myPoints.Count-1 and 2
            //Then swap points myPoints.Count-1-1 and 3
            //etc

            tempPoint = myPoints[0];
            myPoints[0] = myPoints[1];
            myPoints[1] = tempPoint;

            i = myPoints.Count - 1;
            j = 2;
            while (i > j)
            {
                tempPoint = myPoints[i];
                myPoints[i] = myPoints[j];
                myPoints[j] = tempPoint;
                i = i - 1;
                j = j + 1;
            }
        }

        public void LoadSketch(System.IO.StreamReader sr)
        {
            double x;
            double y;
            double z;
            int i;
            int n;

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 0; i <= n; i++)
            {
                x = Convert.ToDouble(sr.ReadLine());
                y = Convert.ToDouble(sr.ReadLine());
                z = Convert.ToDouble(sr.ReadLine());
                myPoints.Add(new clsPoint3d(x, y, z));
            }
        }

        public void SaveSketch(System.IO.StreamWriter sr)
        {
            int i;

            sr.WriteLine(myPoints.Count - 1);
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                sr.WriteLine(myPoints[i].X);
                sr.WriteLine(myPoints[i].Y);
                sr.WriteLine(myPoints[i].Z);
            }
        }

        public void Move(double x, double y)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].Move(x, y, 0);
            }
        }

        public void Move(double x, double y, double z)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].Move(x, y, z);
            }
        }

        public void Rotate(double ang)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].Rotate(ang);
            }
        }

        public void RotateAboutPoint(clsPoint p1, double ang)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].RotateAboutPoint(p1, ang);
            }
        }
        
        public bool HasCommonEdge(clsSketch3d aSketch, double aTol)
        {
            int i;
            int j;
            clsPoint3d pt1;
            clsPoint3d pt2;
            clsPoint3d pt3;
            clsPoint3d pt4;

            for (i = 0; i <= NumPoints; i++)
            {
                pt1 = Point(i);
                if (i < NumPoints)
                {
                    pt2 = Point(i + 1);
                }
                else
                {
                    pt2 = Point(0);
                }

                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    pt3 = aSketch.Point(j);
                    if (j < aSketch.NumPoints)
                    {
                        pt4 = aSketch.Point(j + 1);
                    }
                    else
                    {
                        pt4 = aSketch.Point(0);
                    }

                    if ((pt1 == pt3 & pt2 == pt4) | (pt1 == pt4 & pt2 == pt3))
                        return true;
                }
            }
            return false;
        }

        public int NumCommonPoints(clsSketch3d aSketch, double aTol)
        {
            int i;
            int j;
            int n;

            n = -1;
            for (i = 0; i <= NumPoints; i++)
            {
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (Point(i) == aSketch.Point(j))
                        n = n + 1;
                }
            }
            return n;
        }

        public clsPoint3d CommonPoint(clsSketch3d aSketch, double aTol)
        {
            //In case NumCommomyPoints.Count-1 = 0
            int i;
            int j;

            for (i = 0; i <= NumPoints; i++)
            {
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (Point(i) == aSketch.Point(j))
                        return Point(i).Copy();
                }
            }
            return null;
        }


        //Assumes the 2 sketches have an edge in common
        public void MergeSketchesByEdge(clsSketch3d aSketch, double aTol)
        {
            int i;
            int j;
            int n1 = 0;
            int n2 = 0;
            int n3 = 0;
            int n4 = 0;
            bool isFound;
            bool isReversed = false;

            isFound = false;
            for (i = 0; i <= NumPoints; i++)
            {
                n1 = i;
                if (i < NumPoints)
                {
                    n2 = i + 1;
                }
                else
                {
                    n2 = 0;
                }

                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    n3 = j;
                    if (j < aSketch.NumPoints)
                    {
                        n4 = j + 1;
                    }
                    else
                    {
                        n4 = 0;
                    }

                    if ((Point(n1) == aSketch.Point(n3) & Point(n2) == aSketch.Point(n4)))
                    {
                        isReversed = false;
                        isFound = true;
                        break; // TODO: might not be correct. Was : Exit For
                    }
                    if ((Point(n1) == aSketch.Point(n4) & Point(n2) == aSketch.Point(n3)))
                    {
                        isReversed = true;
                        isFound = true;
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }
                if (isFound)
                    break; // TODO: might not be correct. Was : Exit For
            }

            //Cycle first sketch around until n1 is the first point and n2 is the last point
            while (n1 > 0)
            {
                CycleLeft();
                n1 = n1 - 1;
                n2 = n2 - 1;
                if (n2 < 0)
                    n2 = NumPoints;
            }

            if (n2 != NumPoints)
                Reflect();

            //Cycle second face around until n3 is the first point and n4 is the second point
            if (isReversed == false)
            {
                while (n3 > 0)
                {
                    aSketch.CycleLeft();
                    n3 = n3 - 1;
                    n4 = n4 - 1;
                    if (n3 < 0)
                        n3 = aSketch.NumPoints;
                    if (n4 < 0)
                        n4 = aSketch.NumPoints;
                }
                if (n4 != 1)
                    aSketch.Reflect();
            }
            else
            {
                while (n4 > 0)
                {
                    aSketch.CycleLeft();
                    n3 = n3 - 1;
                    n4 = n4 - 1;
                    if (n3 < 0)
                        n3 = aSketch.NumPoints;
                    if (n4 < 0)
                        n4 = aSketch.NumPoints;
                }
                if (n3 != 1)
                    aSketch.Reflect();
            }

            for (i = 2; i <= aSketch.NumPoints; i++)
            {
                AddPoint(aSketch.Point(i));
            }
        }

        //Assumes the 2 sketches have one point in common
        public void MergeSketchesByPoint(clsSketch3d aSketch, double aTol)
        {
            int i;
            int j;
            int n1;
            int n2;
            bool isFound;

            isFound = false;
            n1 = -1;
            n2 = -1;
            for (i = 0; i <= NumPoints; i++)
            {
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (Point(i) == aSketch.Point(j))
                    {
                        n1 = i;
                        n2 = j;
                        isFound = true;
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }
                if (isFound)
                    break; // TODO: might not be correct. Was : Exit For
            }

            //Cycle first sketch around until n1 is the last point
            while (n1 != NumPoints)
            {
                CycleLeft();
                n1 = n1 - 1;
                if (n1 < 0)
                    n1 = NumPoints;
            }

            //Cycle second face around until n2 is the first point
            while (n2 > 0)
            {
                aSketch.CycleLeft();
                n2 = n2 - 1;
            }

            for (i = 1; i <= aSketch.NumPoints; i++)
            {
                AddPoint(aSketch.Point(i));
            }
            AddPoint(aSketch.Point(0));
        }

        //More elaborate method of merging sketches
        public void MergeSketches(clsSketch3d aSketch, double aTol)
        {
            int i;
            int j;
            int n;
            int n5;
            int n1;
            int n2;
            int n3;
            int n4;
            bool isReversed;
            clsSketch3d mySk1;
            clsSketch3d mySk2;
            clsSketch3d mySk3;
            clsSketch3d mySk4;

            //Get this sketch clockwise, and the other one anti-clockwise
            if (Area() > 0 & aSketch.Area() > 0)
                aSketch.Mirror();
            if (Area() < 0 & aSketch.Area() < 0)
                aSketch.Mirror();
            
            n1 = -1;
            n2 = -1;
            n3 = -1;
            n4 = -1;
            for (i = 0; i <= NumPoints; i++)
            {
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (j != n3)
                    {
                        if (Point(i) == aSketch.Point(j))
                        {
                            if (n1 == -1)
                            {
                                n1 = i;
                                n3 = j;
                            }
                            else
                            {
                                n2 = i;
                                n4 = j;
                            }
                        }
                    }
                }
            }
            isReversed = false;
            if (n4 < n3)
                isReversed = true;

            //Cycle first sketch around until n1 is the first point
            while (n1 > 0)
            {
                CycleLeft();
                n1 = n1 - 1;
                n2 = n2 - 1;
                if (n2 < 0)
                    n2 = NumPoints;
            }

            if (isReversed == false)
            {
                //Cycle second face around until n3 is the first point
                while (n3 > 0)
                {
                    aSketch.CycleLeft();
                    n3 = n3 - 1;
                    n4 = n4 - 1;
                    if (n4 < 0)
                        n4 = aSketch.NumPoints;
                }
                n = n4;
                n5 = n3;
            }
            else
            {
                //Cycle second face around until n4 is the first point
                while (n4 > 0)
                {
                    aSketch.CycleLeft();
                    n3 = n3 - 1;
                    n4 = n4 - 1;
                    if (n3 < 0)
                        n3 = aSketch.NumPoints;
                    if (n4 < 0)
                        n4 = aSketch.NumPoints;
                }
                n = n3;
                n5 = n4;
            }

            //Four options. Choose the one with the biggest area (how?!)
            mySk1 = new clsSketch3d();
            mySk2 = new clsSketch3d();
            i = 0;
            do
            {
                mySk1.AddPoint(Point(i));
                mySk2.AddPoint(Point(i));
                if (i == n2)
                    break; // TODO: might not be correct. Was : Exit Do
                i = i + 1;
            } while (true);

            i = n + 1;
            if (i > aSketch.NumPoints)
                i = 0;
            while (i != n5)
            {
                mySk1.AddPoint(aSketch.Point(i));
                i = i + 1;
                if (i > aSketch.NumPoints)
                    i = 0;
            }

            i = n - 1;
            if (i < 0)
                i = aSketch.NumPoints;
            while (i != n5)
            {
                mySk2.AddPoint(aSketch.Point(i));
                i = i - 1;
                if (i < 0)
                    i = aSketch.NumPoints;
            }

            mySk3 = new clsSketch3d();
            mySk4 = new clsSketch3d();
            i = 0;
            do
            {
                mySk3.AddPoint(Point(i));
                mySk4.AddPoint(Point(i));
                if (i == n2)
                    break; // TODO: might not be correct. Was : Exit Do
                i = i - 1;
                if (i < 0)
                    i = NumPoints;
            } while (true);

            i = n + 1;
            if (i > aSketch.NumPoints)
                i = 0;
            while (i != n5)
            {
                mySk3.AddPoint(aSketch.Point(i));
                i = i + 1;
                if (i > aSketch.NumPoints)
                    i = 0;
            }

            i = n - 1;
            if (i < 0)
                i = aSketch.NumPoints;
            while (i != n5)
            {
                mySk4.AddPoint(aSketch.Point(i));
                i = i - 1;
                if (i < 0)
                    i = aSketch.NumPoints;
            }

            if (mySk2.AbsArea() > mySk1.AbsArea())
                mySk1 = mySk2.Copy();
            if (mySk3.AbsArea() > mySk1.AbsArea())
                mySk1 = mySk3.Copy();
            if (mySk4.AbsArea() > mySk1.AbsArea())
                mySk1 = mySk4.Copy();
            Clear();
            for (i = 0; i <= mySk1.NumPoints; i++)
            {
                AddPoint(mySk1.Point(i));
            }
            mySk1 = null;
            mySk2 = null;
            mySk3 = null;
            mySk4 = null;
        }

        public void CycleLeft()
        {
            int i;
            clsPoint3d p;

            p = Point(0).Copy();
            for (i = 1; i <= NumPoints; i++)
            {
                myPoints[i - 1] = Point(i).Copy();
            }
            myPoints[NumPoints] = p;
        }

        public void CycleRight()
        {
            int i;
            clsPoint3d p;

            p = Point(NumPoints).Copy();
            for (i = NumPoints; i >= 1; i += -1)
            {
                myPoints[i] = Point(i - 1).Copy();
            }
            myPoints[0] = p;
        }

        public void Reflect()
        {
            int i;
            int j;
            clsPoint3d p;

            i = 1;
            j = NumPoints;
            while (i < j)
            {
                p = Point(j).Copy();
                myPoints[j] = Point(i);
                myPoints[i] = p;
                i = i + 1;
                j = j - 1;
            }
        }

        public double AbsArea()
        {
            //Only works for non-intersecting polygons
            int i;
            int j;
            double a;

            a = 0;
            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                a = a + Point(i).X * Point(j).Y - Point(j).X * Point(i).Y;
            }
            return Abs(a / 2);
        }

        public double Area()
        {
            //Only works for non-intersecting polygons. -ve means clockwise
            int i;
            int j;
            double a;

            a = 0;
            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                a = a + Point(i).X * Point(j).Y - Point(j).X * Point(i).Y;
            }
            return a / 2;
        }

        public bool Overlaps(clsSketch3d aSketch, double aTol)
        {
            int i;
            int j;
            int k;
            clsLine l1;
            clsLine l2;
            clsPoint p1;
            bool isInside;

            //Disregard the case of when one sketch is contained in the other.
            isInside = true;
            for (i = 0; i <= NumPoints; i++)
            {
                if (aSketch.IsPointInside2(Point(i).Point2D()) == false)
                    isInside = false;
            }
            if (isInside)
                return false;
            isInside = true;
            for (i = 0; i <= aSketch.NumPoints; i++)
            {
                if (IsPointInside2(aSketch.Point(i).Point2D()) == false)
                    isInside = false;
            }
            if (isInside)
                return false;

            //Firstly see if a point of one sketch lies inside the other
            for (i = 0; i <= NumPoints; i++)
            {
                if (aSketch.IsPointInside2(Point(i).Point2D()))
                    return true;
            }
            for (i = 0; i <= aSketch.NumPoints; i++)
            {
                if (IsPointInside2(aSketch.Point(i).Point2D()))
                    return true;
            }

            //Next, do more than 3 points coincide?
            if (NumCommonPoints(aSketch, aTol) > 1)
                return true;

            //Is there a non-trivial intersection?
            for (i = 0; i <= NumPoints; i++)
            {
                if (i < NumPoints)
                {
                    k = i + 1;
                }
                else
                {
                    k = 0;
                }
                l1 = new clsLine(Point(i).Point2D(), Point(k).Point2D());

                //Is there a non-trivial intersection?
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (j < aSketch.NumPoints)
                    {
                        k = j + 1;
                    }
                    else
                    {
                        k = 0;
                    }
                    l2 = new clsLine(aSketch.Point(j).Point2D(), aSketch.Point(k).Point2D());
                    p1 = l1.Intersect(l2);
                    if (p1 != null)
                    {
                        if (l1.IsOnShortLine(p1, 0, true) & l2.IsOnShortLine(p1, 0, true))
                            return true;
                    }
                }
            }
            return false;
        }

        public clsPoint3d Intersect(clsLine l1)
        {
            int i;
            int j;
            clsLine l2;
            clsLine3d l3;
            clsPoint p1;
            double z;

            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                l2 = new clsLine(Point(i).Point2D(), Point(j).Point2D());
                p1 = l2.Intersect(l1);
                if (p1 != null)
                {
                    if (l1.IsOnShortLine(p1) & l2.IsOnShortLine(p1))
                    {
                        l3 = new clsLine3d(Point(i), Point(j));
                        z = l3.VerticalHeight(p1);
                        return new clsPoint3d(p1.X, p1.Y, z);
                    }
                }
            }
            return null;
        }

        public clsLine3d Line(int n)
        {
            int i;

            i = n + 1;
            if (i > NumPoints)
                i = 0;
            return new clsLine3d(myPoints[n], myPoints[i]);
        }

        public clsLine3d Line(int n1, int n2)
        {
            return new clsLine3d(myPoints[n1], myPoints[n2]);
        }

        public int SegmentNumber(clsPoint3d p3d)
        {
            int i;
            int j;
            clsLine l1;
            int n;

            n = NumPoints;
            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                l1 = new clsLine(Point(i).Point2D(), Point(j).Point2D());
                if (l1.IsOnShortLine(p3d.Point2D()))
                    return i;
            }
            return n;
        }

        //Only works for simple cases. Throws away smallest area
        public void CutBySketch(ref clsSketch3d aSketch, double aTol)
        {
            int i;
            int i1;
            int i2;
            int j;
            clsLine l1;
            clsPoint3d p3d;
            clsPoint3d p3d2;
            int n;

            //Firstly see if a point of one sketch lies inside the other
            for (i = 0; i <= NumPoints; i++)
            {
                if (aSketch.IsPointInside2(Point(i).Point2D()))
                {
                    //Track away from the point in both directions to hit an intersection
                    i1 = i;
                    j = i1 + 1;
                    if (j > NumPoints)
                        j = 0;
                    l1 = new clsLine(Point(i1).Point2D(), Point(j).Point2D());
                    p3d = aSketch.Intersect(l1);
                    while (p3d == null)
                    {
                        i1 = i1 + 1;
                        if (i1 > NumPoints)
                            i1 = 0;
                        j = i1 + 1;
                        if (j > NumPoints)
                            j = 0;
                        l1 = new clsLine(Point(i1).Point2D(), Point(j).Point2D());
                        p3d = aSketch.Intersect(l1);
                    }
                    i1 = j;

                    i2 = i;
                    j = i1 - 1;
                    if (j < 0)
                        j = NumPoints;
                    l1 = new clsLine(Point(i2).Point2D(), Point(j).Point2D());
                    p3d2 = aSketch.Intersect(l1);
                    while (p3d2 == null)
                    {
                        i2 = i2 - 1;
                        if (i2 < 0)
                            i2 = NumPoints;
                        j = i2 - 1;
                        if (j < 0)
                            j = NumPoints;
                        l1 = new clsLine(Point(i2).Point2D(), Point(j).Point2D());
                        p3d2 = aSketch.Intersect(l1);
                    }
                    i2 = j;

                    while (i != i1)
                    {
                        RemovePoint(i);
                        if (i < i1)
                            i1 = i1 - 1;
                        if (i < i2)
                            i2 = i2 - 1;
                        if (i > NumPoints)
                            i = 0;
                    }
                    while (i != i2)
                    {
                        RemovePoint(i);
                        if (i < i2)
                            i2 = i2 - 1;
                        if (i > NumPoints)
                            i = 0;
                    }

                    //Add the intersection points
                    if (!IsPointInSketch(p3d))
                    {
                        AddPoint(p3d, i);
                        i = i - 1;
                        if (i < 0)
                            i = NumPoints;
                    }
                    if (!IsPointInSketch(p3d2))
                    {
                        AddPoint(p3d2, i);
                    }

                    //Add to the other sketch as well
                    if (!aSketch.IsPointInSketch(p3d))
                    {
                        n = aSketch.SegmentNumber(p3d);
                        aSketch.AddPoint(p3d, n);
                    }
                    if (!aSketch.IsPointInSketch(p3d2))
                    {
                        n = aSketch.SegmentNumber(p3d2);
                        aSketch.AddPoint(p3d2, n);
                    }
                    return;
                }
            }

            for (i = 0; i <= aSketch.NumPoints; i++)
            {
                if (IsPointInside2(aSketch.Point(i).Point2D()))
                {
                    //Track away from the point in both directions to hit an intersection
                    i1 = i;
                    j = i1 + 1;
                    if (j > aSketch.NumPoints)
                        j = 0;
                    l1 = new clsLine(aSketch.Point(i1).Point2D(), aSketch.Point(j).Point2D());
                    p3d = Intersect(l1);
                    while (p3d == null)
                    {
                        i1 = i1 + 1;
                        if (i1 > aSketch.NumPoints)
                            i1 = 0;
                        j = i1 + 1;
                        if (j > aSketch.NumPoints)
                            j = 0;
                        l1 = new clsLine(aSketch.Point(i1).Point2D(), aSketch.Point(j).Point2D());
                        p3d = Intersect(l1);
                    }
                    i1 = j;

                    i2 = i;
                    j = i1 - 1;
                    if (j < 0)
                        j = aSketch.NumPoints;
                    l1 = new clsLine(aSketch.Point(i2).Point2D(), aSketch.Point(j).Point2D());
                    p3d2 = Intersect(l1);
                    while (p3d2 == null)
                    {
                        i2 = i2 - 1;
                        if (i2 < 0)
                            i2 = aSketch.NumPoints;
                        j = i2 - 1;
                        if (j < 0)
                            j = aSketch.NumPoints;
                        l1 = new clsLine(aSketch.Point(i2).Point2D(), aSketch.Point(j).Point2D());
                        p3d2 = Intersect(l1);
                    }
                    i2 = j;

                    while (i != i1)
                    {
                        aSketch.RemovePoint(i);
                        if (i < i1)
                            i1 = i1 - 1;
                        if (i < i2)
                            i2 = i2 - 1;
                        if (i > aSketch.NumPoints)
                            i = 0;
                    }
                    while (i != i2)
                    {
                        aSketch.RemovePoint(i);
                        if (i < i2)
                            i2 = i2 - 1;
                        if (i > aSketch.NumPoints)
                            i = 0;
                    }

                    //Add points to sketch
                    if (!aSketch.IsPointInSketch(p3d))
                    {
                        aSketch.AddPoint(p3d, i);
                        i = i - 1;
                        if (i < 0)
                            i = aSketch.NumPoints;
                    }
                    if (!aSketch.IsPointInSketch(p3d2))
                    {
                        aSketch.AddPoint(p3d2, i);
                    }

                    //Add to the other sketch as well
                    if (!IsPointInSketch(p3d))
                    {
                        n = SegmentNumber(p3d);
                        AddPoint(p3d, n);
                    }
                    if (!IsPointInSketch(p3d2))
                    {
                        n = SegmentNumber(p3d2);
                        AddPoint(p3d2, n);
                    }
                    return;
                }
            }

            //Next, do case by case number of common points
            if (NumCommonPoints(aSketch, aTol) <= 10)
            {
                for (i = 0; i <= NumPoints; i++)
                {
                    if (aSketch.IsPointInside(Point(i).Point2D()))
                    {
                        //Track away from the point in both directions to hit an intersection
                        i1 = i;
                        j = i1 + 1;
                        if (j > NumPoints)
                            j = 0;
                        l1 = new clsLine(Point(i1).Point2D(), Point(j).Point2D());
                        p3d = aSketch.Intersect(l1);
                        while (p3d == null)
                        {
                            i1 = i1 + 1;
                            if (i1 > NumPoints)
                                i1 = 0;
                            j = i1 + 1;
                            if (j > NumPoints)
                                j = 0;
                            l1 = new clsLine(Point(i1).Point2D(), Point(j).Point2D());
                            p3d = aSketch.Intersect(l1);
                        }
                        i1 = j;

                        i2 = i;
                        j = i1 - 1;
                        if (j < 0)
                            j = NumPoints;
                        l1 = new clsLine(Point(i2).Point2D(), Point(j).Point2D());
                        p3d2 = aSketch.Intersect(l1);
                        while (p3d2 == null)
                        {
                            i2 = i2 - 1;
                            if (i2 < 0)
                                i2 = NumPoints;
                            j = i2 - 1;
                            if (j < 0)
                                j = NumPoints;
                            l1 = new clsLine(Point(i2).Point2D(), Point(j).Point2D());
                            p3d2 = aSketch.Intersect(l1);
                        }
                        i2 = j;

                        while (i != i1)
                        {
                            RemovePoint(i);
                            if (i < i1)
                                i1 = i1 - 1;
                            if (i < i2)
                                i2 = i2 - 1;
                            if (i > NumPoints)
                                i = 0;
                        }
                        while (i != i2)
                        {
                            RemovePoint(i);
                            if (i < i2)
                                i2 = i2 - 1;
                            if (i > NumPoints)
                                i = 0;
                        }

                        //Add the intersection points
                        if (!IsPointInSketch(p3d))
                        {
                            AddPoint(p3d, i);
                            i = i - 1;
                            if (i < 0)
                                i = NumPoints;
                        }
                        if (!IsPointInSketch(p3d2))
                        {
                            AddPoint(p3d2, i);
                        }

                        //Add to the other sketch as well
                        if (!aSketch.IsPointInSketch(p3d))
                        {
                            n = aSketch.SegmentNumber(p3d);
                            aSketch.AddPoint(p3d, n);
                        }
                        if (!aSketch.IsPointInSketch(p3d2))
                        {
                            n = aSketch.SegmentNumber(p3d2);
                            aSketch.AddPoint(p3d2, n);
                        }
                        return;
                    }
                }

                for (i = 0; i <= aSketch.NumPoints; i++)
                {
                    if (IsPointInside(aSketch.Point(i).Point2D()))
                    {
                        //Track away from the point in both directions to hit an intersection
                        i1 = i;
                        j = i1 + 1;
                        if (j > aSketch.NumPoints)
                            j = 0;
                        l1 = new clsLine(aSketch.Point(i1).Point2D(), aSketch.Point(j).Point2D());
                        p3d = Intersect(l1);
                        while (p3d == null)
                        {
                            i1 = i1 + 1;
                            if (i1 > aSketch.NumPoints)
                                i1 = 0;
                            j = i1 + 1;
                            if (j > aSketch.NumPoints)
                                j = 0;
                            l1 = new clsLine(aSketch.Point(i1).Point2D(), aSketch.Point(j).Point2D());
                            p3d = Intersect(l1);
                        }
                        i1 = j;

                        i2 = i;
                        j = i1 - 1;
                        if (j < 0)
                            j = aSketch.NumPoints;
                        l1 = new clsLine(aSketch.Point(i2).Point2D(), aSketch.Point(j).Point2D());
                        p3d2 = Intersect(l1);
                        while (p3d2 == null)
                        {
                            i2 = i2 - 1;
                            if (i2 < 0)
                                i2 = aSketch.NumPoints;
                            j = i2 - 1;
                            if (j < 0)
                                j = aSketch.NumPoints;
                            l1 = new clsLine(aSketch.Point(i2).Point2D(), aSketch.Point(j).Point2D());
                            p3d2 = Intersect(l1);
                        }
                        i2 = j;

                        while (i != i1)
                        {
                            aSketch.RemovePoint(i);
                            if (i < i1)
                                i1 = i1 - 1;
                            if (i < i2)
                                i2 = i2 - 1;
                            if (i > aSketch.NumPoints)
                                i = 0;
                        }
                        while (i != i2)
                        {
                            aSketch.RemovePoint(i);
                            if (i < i2)
                                i2 = i2 - 1;
                            if (i > aSketch.NumPoints)
                                i = 0;
                        }

                        //Add points to sketch
                        if (!aSketch.IsPointInSketch(p3d))
                        {
                            aSketch.AddPoint(p3d, i);
                            i = i - 1;
                            if (i < 0)
                                i = aSketch.NumPoints;
                        }
                        if (!aSketch.IsPointInSketch(p3d2))
                        {
                            aSketch.AddPoint(p3d2, i);
                        }

                        //Add to the other sketch as well
                        if (!IsPointInSketch(p3d))
                        {
                            n = SegmentNumber(p3d);
                            AddPoint(p3d, n);
                        }
                        if (!IsPointInSketch(p3d2))
                        {
                            n = SegmentNumber(p3d2);
                            AddPoint(p3d2, n);
                        }
                        return;
                    }
                }
            }

        }

        public clsPoint3d NormalVector()
        {
            double x1;
            double y1;
            double z1;
            clsPoint3d p1;
            clsPoint3d p2;
            clsLine l1;
            clsPoint3d v;

            x1 = Point(0).X;
            y1 = Point(0).Y;
            z1 = Point(0).Z;
            p1 = new clsPoint3d(Point(1).X - x1, Point(1).Y - y1, Point(1).Z - z1);
            p2 = new clsPoint3d(Point(2).X - x1, Point(2).Y - y1, Point(2).Z - z1);
            l1 = new clsLine(Point(0).Point2D(), Point(1).Point2D());
            if (l1.IsOnLine(Point(2).Point2D()) & NumPoints >= 3)
                p2 = new clsPoint3d(Point(3).X - x1, Point(3).Y - y1, Point(3).Z - z1);
            v = p1.Cross(p2);
            if (v.Z < 0)
                v.Scale(-1);
            v.Normalise();
            return v;
        }

        private clsPoint3d NormalVector(bool useRiser)
        {
            clsPoint3d v1;
            clsPoint3d v2;
            clsPoint3d v3;
            clsPoint3d v3a;
            clsPoint3d p1;
            clsLine l1;
            int i;
            int j;
            double maxZ;

            if (NumPoints == 0)
                return new clsPoint3d(0, 0, 1);
            v1 = new clsPoint3d(Point(1).x - Point(0).x, Point(1).y - Point(0).y, Point(1).z - Point(0).z);
            if (NumPoints >= 2 & useRiser == false)
            {
                v2 = new clsPoint3d(Point(2).x - Point(0).x, Point(2).y - Point(0).y, Point(2).z - Point(0).z);
                v3 = v1.Cross(v2);
                i = 2;
                while (mdlGeometry.IsSameDbl(v3.Length, 0) & i < NumPoints)
                {
                    i = i + 1;
                    v2 = new clsPoint3d(Point(i).x - Point(0).x, Point(i).y - Point(0).y, Point(i).z - Point(0).z);
                    v3 = v1.Cross(v2);
                }
            }
            else
            {
                l1 = new clsLine(Point(0).Point2D(), Point(1).Point2D());
                l1 = l1.Normal();
                p1 = new clsPoint3d(Point(0).x + l1.DX(), Point(0).y + l1.DY(), Point(0).z);
                v2 = new clsPoint3d(l1.DX(), l1.DY(), 0);
                v3 = v1.Cross(v2);
                v3.Normalise();
                if (v3.z < 0)
                    v3.Scale(-1);
                return v3;
            }
            v3 = v1.Cross(v2);
            if (v3.Length > 0)
                v3.Normalise();

            maxZ = Abs(v3.z);
            j = i + 2;
            while (NumPoints >= j)
            {
                v1 = new clsPoint3d(Point(j).x - Point(0).x, Point(j).y - Point(0).y, Point(j).z - Point(0).z);
                v3a = v1.Cross(v2);
                if (v3a.Length > 0)
                    v3a.Normalise();
                if (Abs(v3a.z) > maxZ)
                {
                    maxZ = v3a.z;
                    v3 = v3a.Copy();
                }
                j = j + 1;
            }

            if (v3.z < 0)
                v3.Scale(-1);
            return v3;
        }

        public bool IsPointInside(clsPoint p1)
        {
            //Uses the "winding number" to check if a point lies inside or outside the sketch
            int i;
            double a;
            double a1;
            clsLine l1;
            clsLine l2;

            a = 0;
            l1 = new clsLine(p1, Point(0).Point2D());
            for (i = 1; i <= myPoints.Count - 1; i++)
            {
                l2 = new clsLine(p1, Point(i).Point2D());
                a1 = mdlGeometry.Angle(l1, l2);
                a = a + a1;
                l1 = l2.Copy();
            }
            l2 = new clsLine(p1, Point(0).Point2D());
            a1 = mdlGeometry.Angle(l1, l2);
            a = a + a1;
            if (mdlGeometry.IsSameDbl(a, 0))
                return false;
            return true;
        }

        public bool IsPointInside2(clsPoint p1)
        {
            //Avoids the case when one of the points is exactly on one of the lines
            if (IsPointTouching(p1))
                return false;
            return IsPointInside(p1);
        }

        public bool IsPointTouching(clsPoint p1)
        {
            //Avoids the case when the point is exactly on one of the lines
            int i;
            int j;
            clsLine l1;
            bool retB;

            retB = false;
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                j = i + 1;
                if (j > myPoints.Count - 1)
                    j = 0;
                l1 = new clsLine(Point(i).Point2D(), Point(j).Point2D());
                if (l1.IsOnShortLine(p1))
                    retB = true;
            }
            return retB;
        }

        public bool IsPointInSketch(clsPoint3d p1)
        {
            //Avoids the case when one the point is exactly one of the points
            int i;
            bool retB;

            retB = false;
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                if (Point(i) == p1)
                    retB = true;
            }
            return retB;
        }

        public List<clsPoint3d> IntersectWithLine(clsLine l2, bool forceShortLines)
        {
            //Returns all the intersection points of this sketch and a 2d line underneath it
            int i;
            int i1;
            clsLine l1;
            clsLine3d l3d;
            List<clsPoint3d> p = new List<clsPoint3d>();
            clsPoint p1;
            
            for (i = 0; i <= NumPoints; i++)
            {
                i1 = i + 1;
                if (i1 > NumPoints)
                    i1 = 0;
                l3d = new clsLine3d(Point(i), Point(i1));
                l1 = l3d.Line2D();

                p1 = l1.Intersect(l2);
                if (p1 != null)
                {
                    if (l1.IsOnShortLine(p1, 0, true) & (forceShortLines == false || l2.IsOnShortLine(p1, 0, false)))
                    {
                        p.Add(new clsPoint3d(p1, l3d.VerticalHeight(p1)));
                    }
                }
            }

            return p;
        }
        
        private clsSketch3d RemoveEar()
        {
            int i;
            int j;
            int k;
            clsSketch3d mySketch;

            // Find an ear.
            FindEar(out i, out j, out k);

            // Create a new triangle for the ear.
            mySketch = new clsSketch3d();
            mySketch.AddPoint(Point(i).Copy());
            mySketch.AddPoint(Point(j).Copy());
            mySketch.AddPoint(Point(k).Copy());

            // Remove the ear from the polygon.
            RemovePoint(j);

            // Repeat the first two points.
            myPoints[NumPoints - 1] = Point(0).Copy();
            myPoints[NumPoints] = Point(1).Copy();
            return mySketch;
        }

        private void FindEar(out int i, out int j, out int k)
        {
            i = 0;
            j = 0;
            k = 0;
            for (i = 0; i <= NumPoints - 2; i++)
            {
                if (FormsEar(i, i + 1, i + 2))
                {
                    j = i + 1;
                    if (j > NumPoints - 2)
                        j = 0;
                    k = j + 1;
                    if (k > NumPoints - 2)
                        k = 0;
                    return;
                }
            }
        }

        private bool FormsEar(int i, int j, int k)
        {
            int n;
            clsSketch aSketch;
            bool retB;

            // Assume the points form an ear.
            retB = true;

            // See if the angle ABC is concave.
            if (mdlGeometry.GetAngle(Point(i).X, Point(i).Y, Point(j).X, Point(j).Y, Point(k).X, Point(k).Y) > 0)
            {
                // This is a concave corner so the triangle
                // cannot be an ear.
                return false;
            }

            // Make the triangle A, B, C.
            aSketch = new clsSketch();
            aSketch.AddPoint(Point(i).Point2D());
            aSketch.AddPoint(Point(j).Point2D());
            aSketch.AddPoint(Point(k).Point2D());

            // Check the other points to see if they lie in the
            // triangle A, B, C.
            for (n = 0; n <= NumPoints - 2; n++)
            {
                if (n != i & n != j & n != k)
                {
                    if (aSketch.IsPointInside(Point(n).Point2D()))
                    {
                        // This point is in the triangle so
                        // this is not an ear.
                        return false;
                    }
                }
            }
            return retB;
        }

        public clsSketch3d Triangulate()
        {
            clsSketch3d aSketch = new clsSketch3d();
            int i;
            List<clsSketch3d> myTriangles = new List<clsSketch3d>();
            clsSketch3d aTriangle;

            // Copy the points into a new array.
            aSketch = Copy();

            // Orient the polygon.
            if (aSketch.Area() < 0)
                aSketch.Mirror();

            // Repeat the first two points.
            aSketch.AddPoint(Point(0));
            aSketch.AddPoint(Point(1));
            
            // While the copy of the polygon has more than
            // three points, remove an ear.
            while (aSketch.NumPoints > 4)
            {
                aTriangle = aSketch.RemoveEar();
                myTriangles.Add(aTriangle.Copy());
            }

            // Copy the last three points into their own triangle.
            myTriangles.Add(new clsSketch3d());
            for (i = 0; i <= 2; i++)
            {
                myTriangles[myTriangles.Count - 1].AddPoint(aSketch.Point(i));
            }

            //Join all the triangles together!
            clsSketch3d myTriangulate = new clsSketch3d();
            for (i = 0; i <= myTriangles.Count - 1; i++)
            {
                myTriangulate.AddPoint(myTriangles[i].Point(0));
                myTriangulate.AddPoint(myTriangles[i].Point(1));
                myTriangulate.AddPoint(myTriangles[i].Point(2));
            }
            return myTriangulate;
        }
        public double AverageHeight()
        {
            int i;
            double d = 0;
            
            for (i = 0; i <= NumPoints; i++)
            {
                d = d + Point(i).z;
            }
            d = d / (NumPoints + 1);
            return d;
        }

        public void InsertPoint(clsPoint3d aPoint, int anIndex)
        {
            if (anIndex == -1 | anIndex > myPoints.Count)
            {
                anIndex = myPoints.Count;
            }
            myPoints.Insert(anIndex, aPoint.Copy());
        }

        public bool IsPointInSketch2d(clsPoint p1, double aTol = 0)
        {
            //Does the point belong to the sketch?
            int i;
            bool ret = false;

            if (aTol == 0)
                aTol = mdlGeometry.myTol;
            ret = false;
            for (i = 0; i <= NumPoints; i++)
            {
                if (Point(i).Point2D().IsSameTol(p1, aTol))
                    ret = true;
            }
            return ret;
        }
        public double Length()
        {
            int i;
            int j;
            double d = 0;
            
            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                d = d + Point(i).Dist(Point(j));
            }

            return d;
        }
        public double OpenLength()
        {
            int i;
            double d = 0;

            for (i = 0; i <= NumPoints - 1; i++)
            {
                d = d + Point(i).Dist(Point(i + 1));
            }

            return d;
        }

        public double Length2D()
        {
            int i;
            int j;
            double d = 0;

            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                d = Length() + Point(i).Point2D().Dist(Point(j).Point2D());
            }

            return d;
        }

        public void MinimiseLength(bool suppressWarnings, clsPoint pt1 = null, clsPoint pt2 = null, clsPoint pt3 = null, clsPoint pt4 = null)
        {
            clsSketch aSk = new clsSketch();
            clsSketch3d aSk3d;
            int n;
            int n2;
            short i;
            short j;
            short k;
            short m;
            double myMinLength;
            double myLength;
            List<short> myIndex1 = new List<short>();
            List<short> myIndex2 = new List<short>();
            List<short> myIndex3 = new List<short>();
            bool isOK;
            short j1;
            short j2;
            int i1;
            System.DateTime t1;
            TimeSpan t2;
            List<clsPoint> myPts = new List<clsPoint>();

            t1 = DateTime.Now;
            n = NumPoints;
            if (n > 9)
            {
                MinimiseLength3(suppressWarnings, false, pt1, pt2, pt3, pt4);
                if (Sketch2d().SelfIntersects())
                    MinimiseLength3(suppressWarnings, true, pt1, pt2, pt3, pt4);
                FixUpMinimise(pt1, pt2, pt3, pt4);
                return;
            }
            if (n <= 1)
                return;

            n2 = MathFactorial(n);
            myMinLength = Length2D();

            for (i = 0; i <= n; i++)
            {
                myIndex1.Add(i);
                myIndex2.Add(i);
                myPts.Add(Point(i).Point2D());
            }

            //Find the combination of points with the minimum flat length
            for (i1 = 1; i1 <= n2; i1++)
            {
                aSk.Clear();
                j1 = -1;
                j2 = -1;
                for (j = 0; j <= n; j++)
                {
                    aSk.AddPoint(myPts[myIndex1[j]]);
                    if (pt1 != null && myPts[myIndex1[j]] == pt1)
                        j1 = j;
                    if (pt2 != null && myPts[myIndex1[j]] == pt2)
                        j2 = j;
                }

                //Disallow points being separated
                isOK = true;
                if (j1 > -1 & j2 > -1)
                {
                    isOK = false;
                    if (Abs(j1 - j2) == 1 | (j1 == 0 & j2 == n) | (j1 == 0 & j2 == n))
                        isOK = true;
                }

                //Disallow self-intersecting lines
                if (isOK && aSk.SelfIntersects() == false)
                {
                    myLength = aSk.Length();
                    if (myLength < myMinLength)
                    {
                        myMinLength = myLength;
                        myIndex2.Clear();
                        myIndex2.AddRange(myIndex1.ToArray());
                    }
                }
                if (i1 == n2)
                    break; // TODO: might not be correct. Was : Exit For

                j = (short)n;
                isOK = false;
                while (true)
                {
                    myIndex3.Clear();
                    for (k = 0; k <= j - 1; k++)
                    {
                        myIndex3.Add(myIndex1[k]);
                    }

                    myIndex1[j] = (short)(myIndex1[j] + 1);
                    while (myIndex1[j] <= n && myIndex3.Contains(myIndex1[j]))
                    {
                        myIndex1[j] = (short)(myIndex1[j] + 1);
                    }

                    if (myIndex1[j] > n)
                    {
                        j = (short)(j - 1);
                    }
                    else
                    {
                        break; // TODO: might not be correct. Was : Exit While
                    }
                }

                for (k = (short)(j + 1); k <= n; k++)
                {
                    myIndex3.Clear();
                    for (m = 0; m <= k - 1; m++)
                    {
                        myIndex3.Add(myIndex1[m]);
                    }
                    for (m = 0; m <= n; m++)
                    {
                        if (myIndex3.Contains(m) == false)
                            break; // TODO: might not be correct. Was : Exit For
                    }
                    myIndex1[k] = m;
                }
            }

            //Now set this sketch to that combination
            aSk3d = new clsSketch3d();
            for (i = 0; i <= n; i++)
            {
                aSk3d.AddPoint(Point(myIndex2[i]));
            }
            Clear();
            for (i = 0; i <= n; i++)
            {
                AddPoint(aSk3d.Point(i));
            }
            aSk3d.Clear();
            aSk3d = null;

            //Always make Anti-clockwise
            if ((pt1 != null) & NumPoints > 1 && Area() < 0)
                Mirror();

            if (pt1 != null & pt2 != null)
            {
                for (i = 0; i <= NumPoints; i++)
                {
                    if (Point(0).Point2D() == pt1 & Point(1).Point2D() == pt2)
                        break; // TODO: might not be correct. Was : Exit For
                    if (Point(1).Point2D() == pt1 & Point(0).Point2D() == pt2)
                        break; // TODO: might not be correct. Was : Exit For
                    RotatePoints();
                }
            }
            t2 = DateTime.Now - t1;

            System.Diagnostics.Debug.Print(Convert.ToString(t2.TotalMilliseconds / 1000));
            //FixUpMinimise(pt1, pt2, pt3, pt4)
        }

        public int MathFactorial(int n)
        {
            int ret = n;
            n = n - 1;
            while (n > 1)
            {
                ret = ret * n;
                n = n - 1;
            }

            return ret;
        }

        //Try again!
        public void MinimiseLength3(bool suppressWarnings, bool alternativeMethod, clsPoint pt1 = null, clsPoint pt2 = null, clsPoint pt3 = null, clsPoint pt4 = null)
        {
            clsSketch3d aSk3d;
            clsSketch3d aSk3d2;
            clsSketch aSk;
            clsSketch aSk2;
            clsSketch aSk3;
            clsPoint p1;
            clsPoint p2;
            clsPoint p3;
            clsPoint p4;
            clsPoint endPt;
            int i;
            int j = -1;
            int k;
            int k1 = -1;
            int k2 = -1;
            int k3 = -1;
            double d;
            double maxD;
            double minD;
            double a;
            double a1;
            double minA;
            int avoidPt1;
            int avoidPt2;
            int avoidPt3;
            int avoidPt4;

            try
            {
                aSk3d = Copy();
                aSk = Sketch2d();

                p1 = aSk.Centre();
                maxD = 0;
                k = 0;
                for (i = 0; i <= NumPoints; i++)
                {
                    d = p1.Dist(aSk.Point(i));
                    if (d > maxD)
                    {
                        maxD = d;
                        k = i;
                    }
                }

                aSk3d2 = new clsSketch3d();
                aSk2 = new clsSketch();

                aSk3d2.AddPoint(myPoints[k].Copy());
                aSk2.AddPoint(myPoints[k].Point2D());
                aSk3d.RemovePoint(k);
                aSk.RemovePoint(k);
                p2 = myPoints[k].Point2D();
                p3 = new clsPoint(p2.x - p1.x, p2.y - p1.y);
                p3.Rotate(PI / 2);
                endPt = p2.Copy();

                p1 = new clsPoint(10000, 10000);

                while (p1 != endPt)
                {
                    a = p3.Angle();
                    j = k;
                    k = -1;
                    minA = 10 * PI;
                    for (i = 0; i <= NumPoints; i++)
                    {
                        if (i != j)
                        {
                            p4 = new clsPoint(myPoints[i].x - p2.x, myPoints[i].y - p2.y);
                            a1 = p4.Angle();
                            if (a1 < a)
                                a1 = a1 + 2 * PI;
                            a1 = a1 - a;
                            if (a1 < minA)
                            {
                                minA = a1;
                                k = i;
                            }
                        }
                    }
                    if (k == -1)
                        break; // TODO: might not be correct. Was : Exit While
                    p1 = myPoints[k].Point2D();
                    if (p1 != endPt)
                    {
                        aSk3d2.AddPoint(myPoints[k].Copy());
                        aSk2.AddPoint(myPoints[k].Point2D());
                        k1 = aSk.GetPoint(myPoints[k].Point2D());
                        if (k1 == -1)
                        {
                            k1 = -(-k1);
                        }
                        aSk3d.RemovePoint(k1);
                        aSk.RemovePoint(k1);
                        p3 = new clsPoint(p1.x - p2.x, p1.y - p2.y);
                        p2 = p1.Copy();
                    }
                }

                if (pt1 != null && aSk2.GetPoint(pt1) == -1)
                {
                    k = aSk.GetPoint(pt1);
                    if (pt2 != null && aSk2.GetPoint(pt2) != -1)
                    {
                        d = aSk2.DistanceToNearestSegment(pt1, ref k1, ref k2, includePt: aSk2.GetPoint(pt2));
                    }
                    else
                    {
                        d = aSk2.DistanceToNearestSegment(pt1, ref k1, ref k2);
                    }
                    aSk3d2.InsertPoint(aSk3d.Point(k), k1 + 1);
                    aSk2.InsertPoint(aSk.Point(k), k1 + 1);
                    aSk3d.RemovePoint(k);
                    aSk.RemovePoint(k);
                }

                if (pt2 != null && aSk2.GetPoint(pt2) == -1)
                {
                    k = aSk.GetPoint(pt2);
                    if (pt1 != null && aSk2.GetPoint(pt1) != -1)
                    {
                        d = aSk2.DistanceToNearestSegment(pt2, ref k1, ref k2, includePt: aSk2.GetPoint(pt1));
                    }
                    else
                    {
                        d = aSk2.DistanceToNearestSegment(pt2, ref k1, ref k2);
                    }
                    aSk3d2.InsertPoint(aSk3d.Point(k), k1 + 1);
                    aSk2.InsertPoint(aSk.Point(k), k1 + 1);
                    aSk3d.RemovePoint(k);
                    aSk.RemovePoint(k);
                }

                if (pt3 != null && aSk2.GetPoint(pt3) == -1)
                {
                    k = aSk.GetPoint(pt3);
                    if (k > -1)
                    {
                        avoidPt1 = -1;
                        avoidPt2 = -1;
                        if (pt1 != null & pt2 != null)
                        {
                            avoidPt1 = aSk2.GetPoint(pt1);
                            avoidPt2 = aSk2.GetPoint(pt2);
                            if (avoidPt1 > avoidPt2 & (avoidPt2 > 0 || avoidPt1 != aSk2.NumPoints))
                                avoidPt1 = avoidPt2;
                        }

                        if (pt4 != null && aSk2.GetPoint(pt4) != -1)
                        {
                            d = aSk2.DistanceToNearestSegment(pt3, ref k1, ref k2, avoidPt1, includePt: aSk2.GetPoint(pt4));
                        }
                        else
                        {
                            d = aSk2.DistanceToNearestSegment(pt3, ref k1, ref k2, avoidPt1);
                        }
                        aSk3d2.InsertPoint(aSk3d.Point(k), k1 + 1);
                        aSk2.InsertPoint(aSk.Point(k), k1 + 1);
                        aSk3d.RemovePoint(k);
                        aSk.RemovePoint(k);
                    }
                }

                if (pt4 != null && aSk2.GetPoint(pt4) == -1)
                {
                    k = aSk.GetPoint(pt4);
                    if (k > -1)
                    {
                        avoidPt1 = -1;
                        avoidPt2 = -1;
                        if (pt1 != null & pt2 != null)
                        {
                            avoidPt1 = aSk2.GetPoint(pt1);
                            avoidPt2 = aSk2.GetPoint(pt2);
                            if (avoidPt1 > avoidPt2 & (avoidPt2 > 0 || avoidPt1 != aSk2.NumPoints))
                                avoidPt1 = avoidPt2;
                        }

                        if (pt3 != null && aSk2.GetPoint(pt3) != -1)
                        {
                            d = aSk2.DistanceToNearestSegment(pt4, ref k1, ref k2, avoidPt1, includePt: aSk2.GetPoint(pt3));
                        }
                        else
                        {
                            d = aSk2.DistanceToNearestSegment(pt4, ref k1, ref k2, avoidPt1);
                        }
                        aSk3d2.InsertPoint(aSk3d.Point(k), k1 + 1);
                        aSk2.InsertPoint(aSk.Point(k), k1 + 1);
                        aSk3d.RemovePoint(k);
                        aSk.RemovePoint(k);
                    }
                }

                i = 0;
                //dummy line for breaking
                while (aSk.NumPoints > -1)
                {
                    minD = 100000;
                    maxD = 0;
                    k = -1;
                    k1 = -1;

                    avoidPt1 = -1;
                    avoidPt2 = -1;
                    if (pt1 != null & pt2 != null)
                    {
                        avoidPt1 = aSk2.GetPoint(pt1);
                        avoidPt2 = aSk2.GetPoint(pt2);
                        if (avoidPt1 > avoidPt2 & (avoidPt2 > 0 || avoidPt1 != aSk2.NumPoints))
                            avoidPt1 = avoidPt2;
                    }
                    avoidPt3 = -1;
                    avoidPt4 = -1;
                    if (pt3 != null & pt4 != null)
                    {
                        avoidPt3 = aSk2.GetPoint(pt3);
                        avoidPt4 = aSk2.GetPoint(pt4);
                        if (avoidPt3 > avoidPt4 & (avoidPt4 > 0 || avoidPt3 != aSk2.NumPoints))
                            avoidPt3 = avoidPt4;
                    }

                    if (alternativeMethod == false)
                    {
                        for (i = 0; i <= aSk.NumPoints; i++)
                        {
                            d = aSk2.DistanceToNearestSegment(aSk.Point(i), ref j, ref k3, avoidPt1, avoidPt3);
                            if (d > maxD)
                            {
                                maxD = d;
                                k = i;
                                k1 = j;
                                k2 = k3;
                            }
                        }
                    }
                    else
                    {
                        for (i = 0; i <= aSk.NumPoints; i++)
                        {
                            d = aSk2.DistanceToNearestSegment(aSk.Point(i), ref j, ref k3, avoidPt1, avoidPt3);
                            if (d < minD)
                            {
                                minD = d;
                                k = i;
                                k1 = j;
                                k2 = k3;
                            }
                        }
                    }
                    if (k == -1)
                        return;

                    aSk3 = new clsSketch();
                    for (i = 0; i <= aSk.NumPoints; i++)
                    {
                        if (aSk2.Point(k2).Dist(aSk.Point(i)) < aSk2.Point(k2).Dist(aSk.Point(k)) + mdlGeometry.myTol)
                        {
                            aSk3.AddPoint(aSk.Point(i).Copy());
                        }
                    }

                    if (aSk3.NumPoints == 0)
                    {
                        aSk3d2.InsertPoint(aSk3d.Point(k), k1 + 1);
                        aSk2.InsertPoint(aSk.Point(k), k1 + 1);
                        aSk3d.RemovePoint(k);
                        aSk.RemovePoint(k);
                        //newell posts fix
                    }
                    else
                    {
                        while (aSk3.NumPoints > -1)
                        {
                            minD = 100000;
                            k = -1;
                            k1 = -1;
                            for (i = 0; i <= aSk3.NumPoints; i++)
                            {
                                d = aSk2.DistanceToNearestSegment(aSk3.Point(i), ref j, ref k2, avoidPt1, avoidPt3);
                                if (d < minD)
                                {
                                    minD = d;
                                    k = i;
                                    k1 = j;
                                }
                            }
                            k3 = aSk.GetPoint(aSk3.Point(k));
                            aSk3d2.InsertPoint(aSk3d.Point(k3), k1 + 1);
                            aSk2.InsertPoint(aSk.Point(k3), k1 + 1);
                            if (k1 < avoidPt1)
                                avoidPt1 = avoidPt1 + 1;
                            if (k1 < avoidPt3)
                                avoidPt3 = avoidPt3 + 1;
                            aSk3d.RemovePoint(k3);
                            aSk.RemovePoint(k3);
                            aSk3.RemovePoint(k);
                        }
                    }
                }

                Clear();
                for (i = 0; i <= aSk3d2.NumPoints; i++)
                {
                    AddPoint(aSk3d2.Point(i));
                }
                aSk3d2.Clear();
                aSk3d2 = null;
                aSk2.Clear();
                aSk2 = null;

            }
            catch 
            {
                //if (suppressWarnings == false)
                    //System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        public void FixUpMinimise(clsPoint pt1, clsPoint pt2, clsPoint pt3, clsPoint pt4)
        {
            int j1;
            int j2;
            bool isOK;
            int n;
            int nLoop;

            n = NumPoints;
            nLoop = 0;
            if (((pt1 != null) & (pt2 != null)) && pt1 != pt2)
            {
                j1 = Sketch2d().GetPoint(pt1);
                j2 = Sketch2d().GetPoint(pt2);

                isOK = false;
                if (Abs(j1 - j2) == 1 | (j1 == 0 & j2 == n) | (j1 == n & j2 == 0))
                    isOK = true;
                while (isOK == false)
                {
                    if (j1 == 0 & j2 == n - 1 | j2 == 0 & j1 == n - 1)
                    {
                        SwapPoints(0, n);
                        if (Sketch2d().SelfIntersects())
                        {
                            SwapPoints(0, n);
                            SwapPoints(0, n - 1);
                        }
                    }
                    else if (j1 > j2)
                    {
                        SwapPoints(j1, j1 - 1);
                    }
                    else
                    {
                        if (j1 < n)
                        {
                            SwapPoints(j1, j1 + 1);
                        }
                        else
                        {
                            SwapPoints(j1, 0);
                        }
                    }
                    j1 = Sketch2d().GetPoint(pt1);
                    j2 = Sketch2d().GetPoint(pt2);
                    isOK = false;
                    if (Abs(j1 - j2) == 1 | (j1 == 0 & j2 == n) | (j1 == 0 & j2 == n))
                        isOK = true;
                    nLoop = nLoop + 1;
                    if (nLoop > 1000)
                        return;
                }
            }
            nLoop = 0;
            if ((pt3 != null) & (pt4 != null))
            {
                j1 = Sketch2d().GetPoint(pt3);
                j2 = Sketch2d().GetPoint(pt4);

                isOK = false;
                if (j1 == -1 | j2 == -1 | Abs(j1 - j2) == 1 | (j1 == 0 & j2 == n) | (j1 == n & j2 == 0))
                    isOK = true;
                while (isOK == false)
                {
                    if (j1 == 0 & j2 == n - 1 | j2 == 0 & j1 == n - 1)
                    {
                        SwapPoints(0, n);
                        if (Sketch2d().SelfIntersects())
                        {
                            SwapPoints(0, n);
                            SwapPoints(0, n - 1);
                        }
                    }
                    else if (j1 > j2)
                    {
                        SwapPoints(j1, j1 - 1);
                    }
                    else
                    {
                        SwapPoints(j1, j1 + 1);
                    }
                    j1 = Sketch2d().GetPoint(pt3);
                    j2 = Sketch2d().GetPoint(pt4);
                    isOK = false;
                    if (Abs(j1 - j2) == 1 | (j1 == 0 & j2 == n) | (j1 == 0 & j2 == n))
                        isOK = true;
                    nLoop = nLoop + 1;
                    if (nLoop > 1000)
                        return;
                }
            }
        }

        public void SwapPoints(int i1, int i2)
        {
            clsPoint3d p1;

            p1 = myPoints[i1];
            myPoints[i1] = myPoints[i2];
            myPoints[i2] = p1;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BatchProcess
{
    public class clsMeasurement
    {
        public int MeasurementNumber { get; set; }
        public float minISO { get; set; }
        public float currentISO { get; set; }
        public float maxISO { get; set; }
        public float currentExposureDuration { get; set; }

        public List<int> MarkerUIDs { get; } = new List<int>();
        public List<double[]> Matrixes { get; } = new List<double[]>(); // Model View Matrices as recorded by ARToolkit
        public List<List<clsPoint>> Corners { get; } = new List<List<clsPoint>>(); // Image corner points (in Ideal coordinates)
        public List<List<clsPoint>> Circles { get; } = new List<List<clsPoint>>(); // Circle Centres points (in Ideal coordinates)        

        public void Load(StreamReader sr) {
            var myLine = sr.ReadLine();
            var NumCircles = 0;

            if (myLine == "SETTINGS") {
                myLine = sr.ReadLine();
                while (myLine != "END_SETTINGS") {
                    var mySplit = myLine.Split(',');
                    if (mySplit.GetUpperBound(0) == 1) {
                        if (mySplit[0] == "MeasurementNumber") MeasurementNumber = Convert.ToInt32(mySplit[1]);
                        if (mySplit[0] == "MinISO") minISO = Convert.ToSingle(mySplit[1]);
                        if (mySplit[0] == "CurrentISO") currentISO = Convert.ToSingle(mySplit[1]);
                        if (mySplit[0] == "MaxISO") maxISO = Convert.ToSingle(mySplit[1]);
                        if (mySplit[0] == "CurrentExposureDuration") currentExposureDuration = Convert.ToSingle(mySplit[1]);
                        if (mySplit[0] == "NumCircles") NumCircles = Convert.ToInt32(mySplit[1]);
                    }
                    myLine = sr.ReadLine();
                }
            } else {
                MeasurementNumber = Convert.ToInt32(myLine);
            }

            var n = Convert.ToInt32(sr.ReadLine());
            for (int i = 0; i < n; i++) {
                MarkerUIDs.Add(Convert.ToInt32(sr.ReadLine()));
            }
            n = Convert.ToInt32(sr.ReadLine());
            for (int i = 0; i < n; i++) {
                Matrixes.Add(new double[16]);
                for (int j = 0; j < 16; j++) {
                    Matrixes.Last()[j] = Convert.ToDouble(sr.ReadLine());
                }
            }
            n = Convert.ToInt32(sr.ReadLine());
            for (int i = 1; i <= n; i++) {
                var n1 = Convert.ToInt32(sr.ReadLine());
                Corners.Add(new List<clsPoint>());
                for (int j = 1; j <= n1; j++) {
                    var p2d = new clsPoint();
                    p2d.Load(sr);
                    Corners.Last().Add(p2d);
                }
            }
            if (NumCircles > 0) {
                for (int i = 1; i <= n; i++) {
                    var n1 = Convert.ToInt32(sr.ReadLine());
                    Circles.Add(new List<clsPoint>());
                    for (int j = 1; j <= n1; j++) {
                        var p2d = new clsPoint();
                        p2d.Load(sr);
                        Circles.Last().Add(p2d);
                    }
                }
            }
        }

        public void Save(StreamWriter sw) {
            sw.WriteLine(MeasurementNumber);
            sw.WriteLine(MarkerUIDs.Count);
            for (int i = 0; i < MarkerUIDs.Count; i++) {
                sw.WriteLine(MarkerUIDs[i]);
            }
            sw.WriteLine(Matrixes.Count);
            for (int i = 0; i < Matrixes.Count; i++) {
                for (int j = 0; j < 16; j++) {
                    sw.WriteLine(Matrixes[i][j].ToString());
                }
            }
            sw.WriteLine(Corners.Count);
            for (int i = 0; i < Corners.Count; i++) {
                sw.WriteLine(Corners[i].Count);
                foreach (clsPoint p1 in Corners[i]) {
                    p1.Save(sw);
                }
            }
            if (Circles.Count > 0) {
                for (int i = 0; i < Circles.Count; i++) {
                    sw.WriteLine(Circles[i].Count);
                    foreach (clsPoint p1 in Circles[i]) {
                        p1.Save(sw);
                    }
                }
            }
        }

        public double[] Trans() {
            var trans = new double[Matrixes.Count * 12];
            for (int i = 0; i < Matrixes.Count; i++) {
                trans[i * 12] = Matrixes[i][0];
                trans[i * 12 + 1] = Matrixes[i][4];
                trans[i * 12 + 2] = Matrixes[i][8];
                trans[i * 12 + 3] = Matrixes[i][12];
                trans[i * 12 + 4] = -Matrixes[i][1];
                trans[i * 12 + 5] = -Matrixes[i][5];
                trans[i * 12 + 6] = -Matrixes[i][9];
                trans[i * 12 + 7] = -Matrixes[i][13];
                trans[i * 12 + 8] = -Matrixes[i][2];
                trans[i * 12 + 9] = -Matrixes[i][6];
                trans[i * 12 + 10] = -Matrixes[i][10];
                trans[i * 12 + 11] = -Matrixes[i][14];
            }
            return trans;
        }

        public void SaveCorners() {
            var sw = new StreamWriter("C:\\Temp\\Corners.txt");
            for (int i = 0; i < Corners.Count; i++) {
                foreach (clsPoint p1 in Corners[i]) {
                    sw.WriteLine(p1.x + "\t" + p1.y);
                }
            }
            sw.Close();
        }

    }
}
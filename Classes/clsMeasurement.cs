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

        public List<int> MarkerUIDs { get; } = new List<int>();
        public List<double[]> Matrixes { get; } = new List<double[]>(); // Model View Matrices as recorded by ARToolkit
        public List<List<clsPoint>> Corners { get; } = new List<List<clsPoint>>(); // Image corner points (in Ideal coordinates)

        public void Load(StreamReader sr) {
            MeasurementNumber = Convert.ToInt32(sr.ReadLine());
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
        }

    }
}
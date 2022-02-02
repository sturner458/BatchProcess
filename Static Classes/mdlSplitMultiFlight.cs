using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BatchProcess.mdlRecognise;

namespace BatchProcess
{
    public static class mdlSplitMultiFlight
    {

        private static clsPoint3d myVerticalVector;
        private static clsPoint3d myCorrectionVector;
        private static clsPoint3d myUncorrectedVerticalVector;
        private static string myPGLoadedVersion;
        private static List<DateTime> surveyStartTimes = new List<DateTime>();
        private static List<DateTime> surveyEndTimes = new List<DateTime>();
        private static string CalibrationFile;
        private static double CalibrationLastRMSError;
        private static string CameraSetupAutoFocus;
        private static double CameraSetupFocalDistance;
        private static string ThresholdMode;
        private static int MinimumNumberOfImages;
        private static double MinimumAngle1;
        private static double MinimumAngle2;
        private static double GTSAMTolerance;
        private static string useDatums;
        private static int arToolkitMarkerType;
        private static int circlesToUse;
        private static string timeTaken;


        public static void SplitMultiFlight(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("filePath was null or whitespace");
            if (!File.Exists(filePath)) return;

            ReadFromFile(filePath);

            var _stitchingVectors = stitchingVectors.Select(s => s.Copy()).ToList();
            var _stitchingMeasurements = stitchingMeasurements.ToList();
            var _measurements = myMeasurements.Select(m => m.Clone()).ToList();
            var _confirmedMarkers = ConfirmedMarkers.Select(m => m.Copy(true)).ToList();


            for (int i = -1; i < _stitchingMeasurements.Count; i++) {
                stitchingVectors.Clear();
                stitchingMeasurements.Clear();
                myMeasurements.Clear();
                ConfirmedMarkers.Clear();

                if (i > -1) myVerticalVector = _stitchingVectors[i];
                var n1 = 0;
                if (i > -1) n1 = _stitchingMeasurements[i] + 1;
                var n2 = _measurements.Count - 1;
                if (i < _stitchingMeasurements.Count - 1) n2 = _stitchingMeasurements[i + 1];
                for (int j = n1; j <= n2; j++) {
                    myMeasurements.Add(_measurements[j].Clone());
                    myMeasurements.Last().MeasurementNumber = j - n1;
                }
                for (int j = 0; j < _confirmedMarkers.Count; j++) {
                    if (_confirmedMarkers[j].ConfirmedImageNumber >= n1 && _confirmedMarkers[j].ConfirmedImageNumber <= n2) {
                        ConfirmedMarkers.Add(_confirmedMarkers[j]);
                        ConfirmedMarkers.Last().ConfirmedImageNumber = ConfirmedMarkers.Last().ConfirmedImageNumber - n1;
                    }
                }
                if (i % 2 == 0) { // Reverse the 100 and 101 markers
                    for (int j = 0; j < ConfirmedMarkers.Count; j++) {
                        if (ConfirmedMarkers[j].MarkerID == 100) {
                            ConfirmedMarkers[j].MarkerID = 101;
                        } else if (ConfirmedMarkers[j].MarkerID == 101) {
                            ConfirmedMarkers[j].MarkerID = 100;
                        }
                        if (ConfirmedMarkers[j].ActualMarkerID == 100) {
                            ConfirmedMarkers[j].ActualMarkerID = 101;
                        } else if (ConfirmedMarkers[j].ActualMarkerID == 101) {
                            ConfirmedMarkers[j].ActualMarkerID = 100;
                        }
                    }
                    for (int j = 0; j < myMeasurements.Count; j++) { // Reverse the barcode Ids for markers 100 and 101
                        for (int k = 0; k < myMeasurements[j].MarkerUIDs.Count; k++) {
                            if (myMeasurements[j].MarkerUIDs[k] == 121) {
                                myMeasurements[j].MarkerUIDs[k] = 125;
                            } else if (myMeasurements[j].MarkerUIDs[k] == 125) {
                                myMeasurements[j].MarkerUIDs[k] = 121;
                            }
                            if (myMeasurements[j].MarkerUIDs[k] == 122) {
                                myMeasurements[j].MarkerUIDs[k] = 126;
                            } else if (myMeasurements[j].MarkerUIDs[k] == 126) {
                                myMeasurements[j].MarkerUIDs[k] = 122;
                            }
                            if (myMeasurements[j].MarkerUIDs[k] == 123) {
                                myMeasurements[j].MarkerUIDs[k] = 127;
                            } else if (myMeasurements[j].MarkerUIDs[k] == 127) {
                                myMeasurements[j].MarkerUIDs[k] = 123;
                            }
                            if (myMeasurements[j].MarkerUIDs[k] == 124) {
                                myMeasurements[j].MarkerUIDs[k] = 128;
                            } else if (myMeasurements[j].MarkerUIDs[k] == 128) {
                                myMeasurements[j].MarkerUIDs[k] = 124;
                            }
                        }
                    }
                }
                stitchingVectors.Add(_stitchingVectors[i + 1]);

                var s = SaveToString();
                var file = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "_" + (i + 2).ToString() + Path.GetExtension(filePath);
                if (File.Exists(file)) {
                    try {
                        File.Delete(file);
                    } catch {

                    }
                }
                if (!File.Exists(file)) {
                    using (var sw = new StreamWriter(file)) {
                        sw.WriteLine(s);
                    }
                }
            }
        }

        private static void ReadFromFile(string filePath) {
            using (var sr = new StreamReader(filePath)) {
                
                ResetMeasurements();

                stitchingMeasurements.Clear();
                stitchingVectors.Clear();
                myPGLoadedVersion = "1.1";

                var myLine = sr.ReadLine();
                var mySplit = myLine.Split(',');
                if (mySplit.GetUpperBound(0) >= 1) {
                    myPGLoadedVersion = mySplit[1];
                } else {
                    myPGLoadedVersion = "1.1";
                }
                myLine = sr.ReadLine();
                if (myLine.Contains(",")) {
                    mySplit = myLine.Split(',');
                    if (mySplit.GetUpperBound(0) >= 1) myPGLoadedVersion = mySplit[1];
                    myLine = sr.ReadLine();
                }

                myMeasurements.Clear();
                ConfirmedMarkers.Clear();
                myBulkheadMarkers.Clear();
                myDoorMarkers.Clear();
                myObstructMarkers.Clear();
                myWallMarkers.Clear();
                mySuspectedMarkers.Clear();
                stitchingMeasurements.Clear();
                surveyStartTimes.Clear();
                surveyEndTimes.Clear();
                numImagesProcessed = 0;

                if (myLine == "SETTINGS") {
                    myLine = sr.ReadLine();
                    while (myLine != "END_SETTINGS") {
                        mySplit = myLine.Split(',');
                        if (mySplit.GetUpperBound(0) == 1) {

                            if (mySplit[0] == "CalibrationFile") CalibrationFile = mySplit[1];
                            if (mySplit[0] == "CalibrationScore") CalibrationLastRMSError = Convert.ToDouble(mySplit[1]);
                            if (mySplit[0] == "AutoFocus") CameraSetupAutoFocus = mySplit[1];
                            if (mySplit[0] == "FocalDistance") CameraSetupFocalDistance = Convert.ToDouble(mySplit[1]);
                            if (mySplit[0] == "ThresholdMode") ThresholdMode = mySplit[1];
                            if (mySplit[0] == "MinimumNumberOfImages") MinimumNumberOfImages = Convert.ToInt32(mySplit[1]);
                            if (mySplit[0] == "MinimumAngle1") MinimumAngle1 = Convert.ToDouble(mySplit[1]);
                            if (mySplit[0] == "MinimumAngle2") MinimumAngle2 = Convert.ToDouble(mySplit[1]);
                            if (mySplit[0] == "GTSAMTolerance") GTSAMTolerance = Convert.ToDouble(mySplit[1]);
                            if (mySplit[0] == "UseDatumMarkers") useDatums = mySplit[1];
                            if (mySplit[0] == "MarkerType") arToolkitMarkerType = Convert.ToInt32(mySplit[1]);
                            if (mySplit[0] == "NumCircles") circlesToUse = Convert.ToInt32(mySplit[1]);
                            if (mySplit[0] == "AverageImageCaptureTime") timeTaken = mySplit[1];

                            if (mySplit[0] == "VerticalVectorX") {
                                if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
                                myVerticalVector.X = Convert.ToDouble(mySplit[1]);
                            }
                            if (mySplit[0] == "VerticalVectorY") {
                                if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
                                myVerticalVector.Y = Convert.ToDouble(mySplit[1]);
                            }
                            if (mySplit[0] == "VerticalVectorZ") {
                                if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
                                myVerticalVector.Z = Convert.ToDouble(mySplit[1]);
                            }
                            if (mySplit[0] == "UncorrectedVerticalVectorX") {
                                if (myUncorrectedVerticalVector == null) myUncorrectedVerticalVector = new clsPoint3d(0, 0, 0);
                                myUncorrectedVerticalVector.X = Convert.ToDouble(mySplit[1]);
                            }
                            if (mySplit[0] == "UncorrectedVerticalVectorY") {
                                if (myUncorrectedVerticalVector == null) myUncorrectedVerticalVector = new clsPoint3d(0, 0, 0);
                                myUncorrectedVerticalVector.Y = Convert.ToDouble(mySplit[1]);
                            }
                            if (mySplit[0] == "UncorrectedVerticalVectorZ") {
                                if (myUncorrectedVerticalVector == null) myUncorrectedVerticalVector = new clsPoint3d(0, 0, 0);
                                myUncorrectedVerticalVector.Z = Convert.ToDouble(mySplit[1]);
                            }
                            if (mySplit[0] == "StitchingMeasurement") {
                                stitchingMeasurements.Add(Convert.ToInt32(mySplit[1]));
                            }
                            if (mySplit[0] == "SurveyStartTime") {
                                surveyStartTimes.Add(Convert.ToDateTime(mySplit[1]));
                            }
                            if (mySplit[0] == "SurveyEndTime") {
                                surveyEndTimes.Add(Convert.ToDateTime(mySplit[1]));
                            }
                            if (mySplit[0] == "NumImagesProcessed") {
                                numImagesProcessed = Convert.ToInt32(mySplit[1]);
                            }
                            if (mySplit[0] == "StepMarkerLevelled") {
                                StepMarker.Levelled = mySplit[1] == "1";
                            }
                            if (mySplit[0] == "StepMarkerStitched") {
                                StepMarker.Stitched = mySplit[1] == "1";
                            }
                            if (mySplit[0] == "StepMarkerConfirmed") {
                                StepMarker.Confirmed = mySplit[1] == "1";
                            }
                            if (mySplit[0] == "LastDatumID") {
                                myLastDatumId = Convert.ToInt32(mySplit[1]);
                            }
                        }
                        myLine = sr.ReadLine();
                    }
                    myLine = sr.ReadLine();
                }

                if (sr.Peek() == -1) return;
                var n = Convert.ToInt32(myLine);
                clsMeasurement myMeasurement;
                for (var i = 1; i <= n; i++) {
                    myMeasurement = new clsMeasurement();
                    myMeasurement.Load(sr);
                    myMeasurements.Add(myMeasurement);
                }

                if (sr.Peek() == -1) return;
                myLine = sr.ReadLine();
                n = Convert.ToInt32(myLine);
                clsMarkerPoint myMarkerPoint;
                for (var i = 1; i <= n; i++) {
                    myMarkerPoint = new clsMarkerPoint();
                    myMarkerPoint.Load(sr);
                    ConfirmedMarkers.Add(myMarkerPoint);
                }
                ConfirmedMarkers.Sort((c1, c2) => c1.ConfirmedImageNumber.CompareTo(c2.ConfirmedImageNumber));
                ConfirmedMarkers.ForEach(c => {
                    if (c.VerticalVect != null && (c.ActualMarkerID == myGFMarkerID || c.ActualMarkerID == myStepMarkerID)) {
                        stitchingVectors.Add(c.VerticalVect);
                    }
                });

                if (sr.Peek() == -1) return;
                myLine = sr.ReadLine();
                n = Convert.ToInt32(myLine);
                for (var i = 1; i <= n; i++) {
                    myMarkerPoint = new clsMarkerPoint();
                    myMarkerPoint.Load(sr);
                    myBulkheadMarkers.Add(myMarkerPoint);
                }

                if (sr.Peek() == -1) return;
                myLine = sr.ReadLine();
                n = Convert.ToInt32(myLine);
                for (var i = 1; i <= n; i++) {
                    myMarkerPoint = new clsMarkerPoint();
                    myMarkerPoint.Load(sr);
                    myDoorMarkers.Add(myMarkerPoint);
                }

                if (sr.Peek() == -1) return;
                myLine = sr.ReadLine();
                n = Convert.ToInt32(myLine);
                for (var i = 1; i <= n; i++) {
                    myMarkerPoint = new clsMarkerPoint();
                    myMarkerPoint.Load(sr);
                    myObstructMarkers.Add(myMarkerPoint);
                }

                if (sr.Peek() == -1) return;
                myLine = sr.ReadLine();
                n = Convert.ToInt32(myLine);
                for (var i = 1; i <= n; i++) {
                    myMarkerPoint = new clsMarkerPoint();
                    myMarkerPoint.Load(sr);
                    myWallMarkers.Add(myMarkerPoint);
                }

                if (sr.Peek() == -1) return;
                myLine = sr.ReadLine();
                if (!int.TryParse(myLine, out n)) return;
                for (var i = 1; i <= n; i++) {
                    myMarkerPoint = new clsMarkerPoint();
                    myMarkerPoint.Load(sr);
                    mySuspectedMarkers.Add(myMarkerPoint);
                }

            }
        }

        private static string SaveToString() {
            if (myCorrectionVector == null) myCorrectionVector = new clsPoint3d(0, 0, 0);
            if (myVerticalVector == null) myVerticalVector = new clsPoint3d(0, 0, 0);
            if (myUncorrectedVerticalVector == null) myUncorrectedVerticalVector = myVerticalVector.Copy();

            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms) { AutoFlush = true }) {
                    sw.WriteLine("#VERSION," + myPGLoadedVersion.ToString());
                    sw.WriteLine("#ENGAGEVERSION," + myPGLoadedVersion.ToString());
                    sw.WriteLine("SETTINGS");
                    sw.WriteLine("AppVersion," + myPGLoadedVersion);
                    sw.WriteLine("CalibrationFile," + CalibrationFile);
                    sw.WriteLine("CalibrationScore," + CalibrationLastRMSError);
                    sw.WriteLine("AutoFocus," + CameraSetupAutoFocus);
                    sw.WriteLine("FocalDistance," + CameraSetupFocalDistance);
                    sw.WriteLine("ThresholdMode," + ThresholdMode);
                    sw.WriteLine("MinimumNumberOfImages," + MinimumNumberOfImages);
                    sw.WriteLine("MinimumAngle1," + MinimumAngle1);
                    sw.WriteLine("MinimumAngle2," + MinimumAngle2);
                    sw.WriteLine("GTSAMTolerance," + GTSAMTolerance);
                    sw.WriteLine("UncorrectedVerticalVectorX," + myUncorrectedVerticalVector.X);
                    sw.WriteLine("UncorrectedVerticalVectorY," + myUncorrectedVerticalVector.Y);
                    sw.WriteLine("UncorrectedVerticalVectorZ," + myUncorrectedVerticalVector.Z);
                    sw.WriteLine("CorrectionVectorX," + myCorrectionVector.X);
                    sw.WriteLine("CorrectionVectorY," + myCorrectionVector.Y);
                    sw.WriteLine("CorrectionVectorZ," + myCorrectionVector.Z);
                    sw.WriteLine("VerticalVectorX," + myVerticalVector.X);
                    sw.WriteLine("VerticalVectorY," + myVerticalVector.Y);
                    sw.WriteLine("VerticalVectorZ," + myVerticalVector.Z);
                    sw.WriteLine("UseDatumMarkers," + useDatums);
                    sw.WriteLine("MarkerType," + arToolkitMarkerType);
                    sw.WriteLine("NumCircles," + circlesToUse);
                    sw.WriteLine("GFMarkerID," + myGFMarkerID);
                    sw.WriteLine("StepMarkerID," + myStepMarkerID);
                    sw.WriteLine("LastDatumID," + myLastDatumId);
                    sw.WriteLine("StepMarkerLevelled," + (StepMarker.Levelled ? "1" : "0"));
                    sw.WriteLine("StepMarkerStitched," + (StepMarker.Stitched ? "1" : "0"));
                    sw.WriteLine("StepMarkerConfirmed," + (StepMarker.Confirmed ? "1" : "0"));
                    foreach (var id in stitchingMeasurements) sw.WriteLine("StitchingMeasurement," + id);
                    foreach (var date in surveyStartTimes) sw.WriteLine("SurveyStartTime," + date.ToString());
                    foreach (var date in surveyEndTimes) sw.WriteLine("SurveyEndTime," + date.ToString());
                    sw.WriteLine("NumImagesProcessed," + numImagesProcessed);
                    sw.WriteLine("AverageImageCaptureTime," + timeTaken);
                    for (var i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                        sw.WriteLine("Marker " + ConfirmedMarkers[i].MarkerID + "," + ConfirmedMarkers[i].GTSAMMatrixes.Count);
                    }
                    sw.WriteLine("END_SETTINGS");

                    sw.WriteLine(myMeasurements.Count);
                    for (var i = 0; i <= myMeasurements.Count - 1; i++) {
                        myMeasurements[i].Save(sw);
                    }
                    sw.WriteLine(ConfirmedMarkers.Count);
                    for (var i = 0; i <= ConfirmedMarkers.Count - 1; i++) {
                        ConfirmedMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myBulkheadMarkers.Count);
                    for (var i = 0; i <= myBulkheadMarkers.Count - 1; i++) {
                        myBulkheadMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myDoorMarkers.Count);
                    for (var i = 0; i <= myDoorMarkers.Count - 1; i++) {
                        myDoorMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myObstructMarkers.Count);
                    for (var i = 0; i <= myObstructMarkers.Count - 1; i++) {
                        myObstructMarkers[i].Save(sw);
                    }
                    sw.WriteLine(myWallMarkers.Count);
                    for (var i = 0; i <= myWallMarkers.Count - 1; i++) {
                        myWallMarkers[i].Save(sw);
                    }

                    ms.Position = 0;
                    using (var sr = new StreamReader(ms)) {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

    }
}

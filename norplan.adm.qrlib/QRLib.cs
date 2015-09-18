using DotSpatial.Data;
using DotSpatial.Projections;
using DotSpatial.Topology;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace norplan.adm.qrlib
{
    public static class QRLib
    {
        public static Shapefile Districts = null;

        public static List<string> QRCodes = null;

        public static StreamWriter LogFile = null;

        public static void ResetDistrict()
        {
            Districts = null;
        }

        public static void ResetQRCodes()
        {
            QRCodes = null;
        }

        public static bool HasInternetConnection()
        {
            Ping myPing = new Ping();
            String host = "google.com";
            byte[] buffer = new byte[32];
            int timeout = 1000;
            PingOptions pingOptions = new PingOptions();
            try
            {
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }

            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Loads the existing QR-codes from myabudhabi.net
        /// </summary>
        /// <returns>A retobj where the data attribute is set to a <MyAbuDhabiNetCodesResponse/> object</returns>
        public static RetObj DownloadCodes()
        {
            var mRetObj = new RetObj();
            var mClient = new WebClient();
            try
            {
                mClient.Headers.Add(HttpRequestHeader.Accept, "text/json");
                var mJson = mClient.DownloadString("http://myabudhabi.net/getcodes.php");
                mRetObj.Data = JsonConvert.DeserializeObject<MyAbuDhabiNetCodesResponse>(mJson);
                mRetObj.SetSuccess();
            }
            catch (System.Net.WebException ex)
            {
                mRetObj.SetError();
                mRetObj.AddMessage("Error: " + ex.Status.ToString());
            }
            catch (Exception ex)
            {
                mRetObj.SetError();
                mRetObj.AddMessage("Error: " + ex.Message);
            }
            return mRetObj;
        }

        public static RetObj DownloadLandingPage(this string pQRCode)
        {
            var mRetObj = new RetObj();
            var mClient = new WebClient();
            try
            {
                mClient.Headers.Add(HttpRequestHeader.Accept, "text/json");
                var mJson = mClient.DownloadString(pQRCode);
                mRetObj.Data = JsonConvert.DeserializeObject<MyAbuDhabiNetResponse>(mJson);
                mRetObj.SetSuccess();
            }
            catch (System.Net.WebException ex)
            {
                mRetObj.SetError();
                mRetObj.AddMessage("Error: " + ex.Status.ToString());
            }
            catch (Exception ex)
            {
                mRetObj.SetError();
                mRetObj.AddMessage("Error: " + ex.Message);
            }
            return mRetObj;
        }

        public static QrTestResult TestQRCode(this string qrCode, string districtsShapefile, bool checkOnline = true, double xCoord = double.NaN, double yCoord = double.NaN, bool checkForDuplicates = false)
        {
            var qrTestResult = new QrTestResult();

            if (QRCodes == null && checkForDuplicates == true)
            {
                QRCodes = new List<string>();
            }

            if (String.IsNullOrEmpty(qrCode))
            {
                qrTestResult.AddMessage("No QR-code to test: " + qrCode);
                return qrTestResult;
            }

            if (Districts == null)
            {
                qrTestResult.AddMessage("Loading districts from Shapefile (once per session): " + districtsShapefile);
                Districts = Shapefile.OpenFile(districtsShapefile);
            }

            string mBaseURL, mMunicipalityAbbreviation, mAreaAbbreviation = "", mRoadID, mSignType, mAUSNumber;

            qrTestResult.AddMessage("Decoded QR code content: " + qrCode);

            qrTestResult.QrCode = qrCode;

            var mRegex = Regex.Match(qrCode, @"http://([a-z\.]*)/([A-Za-z]*)/([A-Za-z]*)/([0-9]*)/([a-zA-Z0-9]*)/([0-9]*)$");

            if (mRegex.Groups.Count != 7)
            {
                qrTestResult.AddMessage("Error: QR code has wrong structure for ANS sign: " + (mRegex.Groups.Count - 1) + " parts, should be 6");
                qrTestResult.StructureOk = false;
                qrTestResult.HasIssue = true;
                qrTestResult.SignType = QrTestResult.TypeOfSign.Unknown;
            }
            else
            {
                qrTestResult.AddMessage("Info: QR code has correct structure for ANS sign");
                qrTestResult.StructureOk = true;
                qrTestResult.SignType = QrTestResult.TypeOfSign.AddressUnitNumber;
                mBaseURL = mRegex.Groups[1].ToString();
                mMunicipalityAbbreviation = mRegex.Groups[2].ToString();
                mAreaAbbreviation = mRegex.Groups[3].ToString();
                mRoadID = mRegex.Groups[4].ToString();
                mSignType = mRegex.Groups[5].ToString();
                mAUSNumber = mRegex.Groups[6].ToString();
            }
            
            // Check for existence of QR-code in processed batch
            if (checkForDuplicates == true && QRCodes.Contains(qrCode.Trim().ToLower()))
            {
                qrTestResult.AddMessage("QR-code already exists (duplicate): " + qrCode);
                qrTestResult.IsDuplicate = true;
                qrTestResult.HasIssue = true;
            }
            else
            {
                QRCodes.Add(qrCode.Trim().ToLower());
                qrTestResult.IsDuplicate = false;
            }

            // Check for correct start of QR code
            if (!qrCode.StartsWith("http://myabudhabi.net/adm"))
            {
                qrTestResult.AddMessage("Error: base URL or municipality");
                qrTestResult.UriOk = false;
                qrTestResult.HasIssue = true;
            }
            else
            {
                qrTestResult.AddMessage("Info: URL part is ok");
                qrTestResult.UriOk = true;
            }

            // Check if codes contains spaces
            if (qrCode.Contains(" "))
            {
                qrTestResult.AddMessage("Error: QR Code contains spaces");
                qrTestResult.SpacesOk = false;
                qrTestResult.HasIssue = true;
            }
            else
            {
                qrTestResult.AddMessage("Info: Contains no spaces");
                qrTestResult.SpacesOk = true;
            }

            // Check if code exists on myabudhabi.net
            MyAbuDhabiNetResponse mResponse = null;
            if (checkOnline)
            {
                if (HasInternetConnection())
                {
                    mResponse = qrCode.DownloadLandingPage().Data as MyAbuDhabiNetResponse;
                    if (mResponse == null || mResponse.status != "success")
                    {
                        qrTestResult.AddMessage("Notice: The QR-code does not exist on myabudhabi.net");
                        qrTestResult.IsOnline = QrTestResult.OnlineStatus.Unavailable;
                        qrTestResult.HasIssue = true;
                    }
                    else
                    {
                        qrTestResult.AddMessage(String.Format("Info: Exists on myabudhabi.net (Longitude: {0}/Latitude: {1}){2}",
                            mResponse.x,
                            mResponse.y,
                            Environment.NewLine));
                        qrTestResult.IsOnline = QrTestResult.OnlineStatus.Available;
                    }

                }
                else
                {
                    qrTestResult.AddMessage("Either the computer is not connected to the Internet or there is a problem with the connection");
                    qrTestResult.IsOnline = QrTestResult.OnlineStatus.Unknown;
                }
            }
            else
            {
                qrTestResult.IsOnline = QrTestResult.OnlineStatus.Unknown;
            }

            // Check if inside district
            Point mPointGeom = null;

            if (xCoord != double.NaN || yCoord != double.NaN)
            {
                mPointGeom = new Point(xCoord, yCoord);
                qrTestResult.HasCoordinates = true;
            }
            else if (mResponse != null)
            {
                double mX, mY;

                if (!double.TryParse(mResponse.x, out mX) || !double.TryParse(mResponse.y, out mY))
                {
                    qrTestResult.AddMessage("Notice: Coordinate values could not be parsed to a number or records on myabudhabi.net does not contain coordinates");
                    qrTestResult.HasCoordinates = false;
                    qrTestResult.HasIssue = true;
                }
                else
                {
                    var mPoints = new double[] { mX, mY };
                    ProjectionInfo pSRSFrom = KnownCoordinateSystems.Geographic.World.WGS1984;
                    ProjectionInfo pSRSTo = KnownCoordinateSystems.Projected.World.WebMercator;
                    Reproject.ReprojectPoints(mPoints, new double[] { 0 }, pSRSFrom, pSRSTo, 0, 1);
                    mPointGeom = new Point(mPoints[0], mPoints[1]);
                    qrTestResult.HasCoordinates = true;
                }
            }

            if (mPointGeom != null)
            {
                foreach (Feature mFeature in Districts.Features)
                {
                    if (mFeature.Contains(mPointGeom))
                    {
                        if (mFeature.DataRow["DISTRICTABB"].ToString().ToLower().Trim() == mAreaAbbreviation.ToLower())
                        {
                            qrTestResult.AddMessage(String.Format("Info: District abbreviation is correct according to coordinates: {0} ({1}){2}",
                                mFeature.DataRow["NAMELATIN"].ToString(),
                                mFeature.DataRow["DISTRICTABB"].ToString().ToLower(),
                                Environment.NewLine));
                            qrTestResult.DistrictOk = true;
                        }
                        else
                        {
                            qrTestResult.AddMessage(String.Format("Error: District abbreviation is wrong according to coordinates; it is: {1} but should be {0}{2}",
                                mFeature.DataRow["DISTRICTABB"].ToString().ToLower(),
                                mAreaAbbreviation,
                                Environment.NewLine));
                            qrTestResult.DistrictOk = false;
                            qrTestResult.HasIssue = true;
                        }
                        break;
                    }
                }
            }

            return qrTestResult;
        }

        public static void setLogFile(string pLogFile)
        {
            closeLogFile();
            LogFile = new StreamWriter(pLogFile, true, Encoding.UTF8);
        }

        public static void closeLogFile()
        {
            if (LogFile != null)
            {
                LogFile.Dispose();
            }
        }
    }
}

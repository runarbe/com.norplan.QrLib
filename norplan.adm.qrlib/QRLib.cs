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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace norplan.adm.qrlib
{
    public static class QRLib
    {
        public static Shapefile Districts = null;

        public static StreamWriter LogFile = null;

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

        public static string TestQRCode(this string pQRCode, string pDistrictsShapefile, bool pCheckOnline = true, double pX = double.NaN, double pY = double.NaN)
        {
            var mSb = new StringBuilder();
            if (String.IsNullOrEmpty(pQRCode))
            {
                return null;
            }

            if (Districts == null)
            {

                mSb.AppendLine("Loading districts from Shapefile (once per session): " + pDistrictsShapefile);
                Districts = Shapefile.OpenFile(pDistrictsShapefile);
            }

            string mBaseURL, mMunicipalityAbbreviation, mAreaAbbreviation = "", mRoadID, mSignType, mAUSNumber;

            mSb.AppendLine("Decoded QR code content: " + pQRCode);

            var mRegex = Regex.Match(pQRCode, @"http://([a-z\.]*)/([A-Za-z]*)/([A-Za-z]*)/([0-9]*)/([a-zA-Z0-9]*)/([0-9]*)$");

            if (mRegex.Groups.Count != 7)
            {
                mSb.AppendLine("Error: QR code has wrong structure for ANS sign: " + (mRegex.Groups.Count - 1) + " parts, should be 6");
            }
            else
            {
                mSb.AppendLine("Info: QR code has correct structure for ANS sign");

                mBaseURL = mRegex.Groups[1].ToString();
                mMunicipalityAbbreviation = mRegex.Groups[2].ToString();
                mAreaAbbreviation = mRegex.Groups[3].ToString();
                mRoadID = mRegex.Groups[4].ToString();
                mSignType = mRegex.Groups[5].ToString();
                mAUSNumber = mRegex.Groups[6].ToString();
            }

            if (!pQRCode.StartsWith("http://myabudhabi.net/adm"))
            {
                mSb.AppendLine("Error: base URL or municipality");
            }
            else
            {
                mSb.AppendLine("Info: URL part is ok");
            }

            if (pQRCode.Contains(" "))
            {
                mSb.AppendLine("Error: QR Code contains spaces");
            }
            else
            {
                mSb.AppendLine("Info: Contains no spaces");
            }
            MyAbuDhabiNetResponse mResponse = null;
            if (pCheckOnline)
            {
                mResponse = pQRCode.DownloadLandingPage().Data as MyAbuDhabiNetResponse;
                if (mResponse == null || mResponse.status != "success")
                {
                    mSb.AppendLine("Notice: Either the QR-code does not exist on myabudhabi.net or there is a problem with the Internet connection");
                }
                else
                {
                    mSb.AppendFormat("Info: Exists on myabudhabi.net (Longitude: {0}/Latitude: {1}){2}",
                        mResponse.x,
                        mResponse.y,
                        Environment.NewLine);
                }
            }

            Point mPointGeom = null;

            if (pX != double.NaN || pY != double.NaN)
            {
                mPointGeom = new Point(pX, pY);
            }
            else if (mResponse != null)
            {
                double mX, mY;

                if (!double.TryParse(mResponse.x, out mX) || !double.TryParse(mResponse.y, out mY))
                {
                    mSb.AppendLine("Notice: Coordinate values could not be parsed to a number or records on myabudhabi.net does not contain coordinates");
                }
                else
                {
                    var mPoints = new double[] { mX, mY };
                    ProjectionInfo pSRSFrom = KnownCoordinateSystems.Geographic.World.WGS1984;
                    ProjectionInfo pSRSTo = KnownCoordinateSystems.Projected.World.WebMercator;
                    Reproject.ReprojectPoints(mPoints, new double[] { 0 }, pSRSFrom, pSRSTo, 0, 1);
                    mPointGeom = new Point(mPoints[0], mPoints[1]);
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
                            mSb.AppendFormat("Info: District abbreviation is correct according to coordinates: {0} ({1}){2}",
                                mFeature.DataRow["NAMELATIN"].ToString(),
                                mFeature.DataRow["DISTRICTABB"].ToString().ToLower(),
                                Environment.NewLine);
                        }
                        else
                        {
                            mSb.AppendFormat("Error: District abbreviation is wrong according to coordinates; it is: {1} but should be {0}{2}",
                                mFeature.DataRow["DISTRICTABB"].ToString().ToLower(),
                                mAreaAbbreviation,
                                Environment.NewLine);
                        }
                        break;
                    }
                }
            }

            if (LogFile != null)
            {
                LogFile.Write(mSb.ToString());
            }

            return mSb.ToString();
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

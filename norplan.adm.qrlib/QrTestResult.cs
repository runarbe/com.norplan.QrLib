using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace norplan.adm.qrlib
{
    public class QrTestResult
    {
        public string QrCode { get; set; }
        public bool HasIssue { get; set; }
        public TypeOfSign SignType { get; set; }
        public bool StructureOk { get; set; }
        public bool UriOk { get; set; }
        public bool SpacesOk { get; set; }
        public bool DistrictOk { get; set; }
        public OnlineStatus IsOnline { get; set; }
        public bool HasCoordinates { get; set; }
        public List<string> Messages { get; set; }
        public bool IsDuplicate { get; set; }

        public enum OnlineStatus {
            Available,
            Unavailable,
            Unknown
        }

        public enum TypeOfSign
        {
            AddressUnitNumber,
            StreetNameOrAddressGuide,
            Unknown
        }

        public QrTestResult()
        {
            this.Messages = new List<string>();
            this.HasIssue = false;
        }

        public void AddMessage(string pMessage) {
            this.Messages.Add(pMessage);
        }

        public override string ToString()
        {
            return String.Join(Environment.NewLine, this.Messages);
        }

        public static QrTestResult Create(
            string pQrCode,
            TypeOfSign pSignType,
            bool pStructureOk,
            bool pUriOk,
            bool pSpacesOk,
            bool pDistrictOk,
            OnlineStatus pIsOnline,
            bool pHasCoordinates,
            bool pIsDuplicate,
            string pMessage,
            bool pHasIssue)
        {
            var mRV = new QrTestResult();
            mRV.QrCode = pQrCode;
            mRV.SignType = pSignType;
            mRV.UriOk = pUriOk;
            mRV.StructureOk = pStructureOk;
            mRV.DistrictOk = pDistrictOk;
            mRV.SpacesOk = pSpacesOk;
            mRV.IsOnline = pIsOnline;
            mRV.HasCoordinates = pHasCoordinates;
            mRV.IsDuplicate = pIsDuplicate;
            mRV.HasIssue = pHasIssue;
            mRV.Messages.Add(pMessage);
            return mRV;
        }

    }
}

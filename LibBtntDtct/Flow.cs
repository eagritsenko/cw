using System;
using System.Net;
using System.Globalization;
using System.ComponentModel;
using System.Text;

namespace LibBtntDtct
{
    public class Flow
    {
        static readonly IFormatProvider invCulture = CultureInfo.InvariantCulture;
        public const string timePattern = "dd.MM.yyyy HH:mm:ss.ffffff";

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public IPAddress Source { get; set; }
        public ushort SPort { get; set; }
        public IPAddress Destination { get; set; }
        public ushort DPort { get; set; }
        public int ProtocolID { get; set; }
        public int PacketsCount { get; set; }
        public int OutgoingPackets { get; set; }
        public long OctetsCount { get; set; }
        public long PayloadOctetsCount { get; set; }

        public override string ToString()
        {
            const string separator = ", ";
            StringBuilder sb = new StringBuilder(21);
            sb.AppendFormat(Start.ToString(timePattern, invCulture));
            sb.Append(separator);
            sb.AppendFormat(End.ToString(timePattern, invCulture));
            sb.Append(separator);
            sb.Append(Source);
            sb.Append(separator);
            sb.Append(SPort.ToString(invCulture));
            sb.Append(separator);
            sb.Append(Destination);
            sb.Append(separator);
            sb.Append(DPort.ToString(invCulture));
            sb.Append(separator);
            sb.Append(ProtocolID.ToString(invCulture));
            sb.Append(separator);
            sb.Append(PacketsCount.ToString(invCulture));
            sb.Append(separator);
            sb.Append(OutgoingPackets.ToString(invCulture));
            sb.Append(separator);
            sb.Append(OctetsCount.ToString(invCulture));
            sb.Append(separator);
            sb.Append(PayloadOctetsCount.ToString(invCulture));
            return sb.ToString();
        }


        protected static int Parse(string[] arr, int i, Flow at)
        {
            at.Start = DateTime.ParseExact(arr[0].Trim(), timePattern, invCulture);
            at.End = DateTime.ParseExact(arr[1].Trim(), timePattern, invCulture);
            at.Source = IPAddress.Parse(arr[2].Trim());
            at.SPort = ushort.Parse(arr[3], invCulture);
            at.Destination = IPAddress.Parse(arr[4].Trim());
            at.DPort = ushort.Parse(arr[5], invCulture);
            at.ProtocolID = int.Parse(arr[6], invCulture);
            at.PacketsCount = int.Parse(arr[7], invCulture);
            at.OutgoingPackets = int.Parse(arr[8], invCulture);
            at.OctetsCount = long.Parse(arr[9], invCulture);
            at.PayloadOctetsCount = long.Parse(arr[10], invCulture);
            return 11;
        }

        public static void Parse(string s, Flow at)
        {
            string[] arr = s.Split(',');
            Parse(arr, 0, at);
        }

        public static Flow Parse(string s)
        {
            Flow f = new Flow();
            Parse(s, f);
            return f;
        }
    }
}

using System;
using SharpPcap.LibPcap;
using PacketDotNet;
using PacketDotNet.Utils;

namespace LibBtntDtct
{

    /// <summary>
    /// Used to meter flows from packets of a pcap file.
    /// </summary>
    public class PcapFlowAdapter : FlowAdapter, IDisposable
    {
        /// <summary>
        /// Error handling options.
        /// </summary>
        public enum Options
        {
            Strict,
            IgnoreMinorErrors,
            IgnoreMajorErrors,
        }

        /// <summary>
        /// Pcap dump reader device.
        /// </summary>
        /// <value>The reader device.</value>
        public CaptureFileReaderDevice ReaderDevice { get; private set; }

        /// <summary>
        /// Represents error handling policy of this adapter.
        /// </summary>
        public Options Option { get; set; }

        /// <summary>
        /// Reads the next pcap packet from the file.
        /// </summary>
        /// <returns>SharpPcap staus code.</returns>
        public int ReadNextPcapPacket()
        {
            int status = 0;
            SharpPcap.RawCapture p;
            switch (Option)
            {
                case Options.Strict:
                    status = ReaderDevice.GetNextPacket(out p);
                    if (status != 1)
                        return status;
                    ReadPacket(new EthernetPacket(new ByteArraySegment(p.Data)), p.Timeval.Date, false);
                    break;
                case Options.IgnoreMinorErrors:
                    ReaderDevice.GetNextPacket(out p);
                    if (status != 1)
                        return status;
                    ReadPacket(new EthernetPacket(new ByteArraySegment(p.Data)), p.Timeval.Date, true);
                    break;
                case Options.IgnoreMajorErrors:
                    try
                    {
                        ReaderDevice.GetNextPacket(out p);
                        if (status != 1)
                            return status;
                        ReadPacket(new EthernetPacket(new ByteArraySegment(p.Data)), p.Timeval.Date, true);
                        break;
                    }
                    catch
                    {

                    }
                    break;
            }
            return status;
        }

        public void Dispose()
        {
            try
            {
                ReaderDevice.Close();
            }
            catch
            {

            }
            ReaderDevice = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.PcapFlowAdapter"/> class.
        /// </summary>
        /// <param name="filePath">Pcap file path to initialise this adapter on.</param>
        public PcapFlowAdapter(string filePath) : base()
        {
            ReaderDevice = new CaptureFileReaderDevice(filePath);
        }
    }
}

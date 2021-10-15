using System;
using System.Net;
using System.Net.Sockets;
using PacketDotNet;
using ProtocolType = PacketDotNet.ProtocolType;

namespace LibBtntDtct
{
    /// <summary>
    /// Represents a flow packer.
    /// </summary>
    public class FlowPacket
    {
        /// <summary>
        /// Protocol ID to use for the ARP.
        /// </summary>
        public const int ARP = 470;

        /// <summary>
        /// Represents this packet's timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Represents this packet's source.
        /// </summary>
        public IPAddress Source { get; set; }
        /// <summary>
        /// Represents this packet's source port.
        /// If a protocol does not specify ports, this should be set to 0.
        /// </summary>
        public ushort SPort { get; set; }
        /// <summary>
        /// Represents this packet's destination IP address.
        /// </summary>
        public IPAddress Destination { get; set; }
        /// <summary>
        /// Represents this packet's destination port.
        /// If a protocol does not specify ports, this should be set to 0.
        /// </summary>
        public ushort DPort { get; set; }
        /// <summary>
        /// Represents this packets protocol ID.
        /// </summary>
        public int ProtocolID { get; set; }
        /// <summary>
        /// Represents the total octets count of this packet.
        /// </summary>
        public int OctetsCount { get; set; }
        /// <summary>
        /// Indicates whether this packet has payload.
        /// </summary>
        /// <value><c>true</c> if has payload; otherwise, <c>false</c>.</value>
        public bool HasPayload { get; set; }
        /// <summary>
        /// Represents the length of this packet's payload
        /// </summary>
        public int PayloadLength { get; set; }


        /// <summary>
        /// Tries to parse a payload of the parent packet given.
        /// </summary>
        private static void ParseEndpointPayload(Packet parent, FlowPacket at, bool ignoreMinorErrors)
        {
            bool hasPayload = false;
            try
            {
                hasPayload = parent.HasPayloadPacket;
            }
            catch (Exception inner)
            {
                if (!ignoreMinorErrors)
                    throw new Exception("Error reading payload for packet", inner);
            }
            at.HasPayload = hasPayload;
            if (hasPayload)
            {
                try
                {
                    at.PayloadLength = parent.PayloadPacket.BytesSegment.Length;
                }
                catch (Exception inner)
                {
                    if (ignoreMinorErrors)
                        at.PayloadLength = 16;
                    else
                        throw new Exception("Error reading payload length for ip's payload packet", inner);
                }
            }
            else
            {
                try
                {
                    hasPayload = parent.HasPayloadData;
                }
                catch (Exception inner)
                {
                    if (!ignoreMinorErrors)
                        throw new Exception("Error reading payload data for packet", inner);
                }
                at.HasPayload = hasPayload;
                if (hasPayload)
                {
                    try
                    {
                        at.PayloadLength = parent.PayloadDataSegment.Length;
                    }
                    catch (Exception inner)
                    {
                        if (ignoreMinorErrors)
                            at.PayloadLength = 16;
                        else
                            throw new Exception("Error reading payload length for payload packet", inner);
                    }
                }
            }
        }

        /// <summary>
        /// Parses the IP packet.
        /// </summary>
        /// <param name="what">The packet to parse.</param>
        /// <param name="at">Where to save parsed data.</param>
        /// <param name="ignoreMinorErrors">If set to <c>true</c> ignores minor errors.</param>
        private static void ParseIPPacket(IPPacket what, FlowPacket at, bool ignoreMinorErrors)
        {
            try
            {
                at.Source = what.SourceAddress;
                at.Destination = what.DestinationAddress;
            }
            catch (Exception inner)
            {
                throw new Exception("Unable to parse ip addresses", inner);
            }



            bool hasPayload;
            try
            {
                hasPayload = what.HasPayloadPacket;
            }
            catch (Exception inner)
            {
                throw new Exception("Error reading payload for IP packet.", inner);
            }




            if (hasPayload)
            {
                Packet payload;
                try
                {
                    payload = what.PayloadPacket;
                }
                catch (Exception inner)
                {
                    throw new Exception("Error reading payload for IP packet.", inner);
                }
                if (payload is TcpPacket tcp)
                {
                    try
                    {
                        at.SPort = tcp.SourcePort;
                        at.DPort = tcp.DestinationPort;
                    }
                    catch (Exception inner)
                    {
                        if (!ignoreMinorErrors)
                            throw new Exception("Error reading ports for TCP packet", inner);
                    }
                }
                else if (payload is UdpPacket udp)
                {
                    try
                    {
                        at.SPort = udp.SourcePort;
                        at.DPort = udp.DestinationPort;
                    }
                    catch (Exception inner)
                    {
                        if (!ignoreMinorErrors)
                            throw new Exception("Error reading ports for UDP packet", inner);
                    }
                }
                ParseEndpointPayload(payload, at, ignoreMinorErrors);
            }
            else
            {
                try
                {
                    hasPayload = what.HasPayloadData;
                }
                catch (Exception inner)
                {
                    throw new Exception("Error reading payload data for ip packet", inner);
                }
                at.HasPayload = hasPayload;
                if (hasPayload)
                {
                    try
                    {
                        at.PayloadLength = what.PayloadDataSegment.Length;
                    }
                    catch (Exception inner)
                    {
                        if (ignoreMinorErrors)
                            at.PayloadLength = 16;
                        else
                            throw new Exception("Error reading payload length for ip's payload packet", inner);
                    }
                }
            }



            try
            {
                at.ProtocolID = (int)what.Protocol;
            }
            catch (Exception inner)
            {
                if (ignoreMinorErrors)
                {
                    bool isIPv4 = at.Source.AddressFamily == AddressFamily.InterNetwork ||
                                  at.Destination.AddressFamily == AddressFamily.InterNetwork;
                    at.ProtocolID = (int)(isIPv4 ? ProtocolType.IPv4 : ProtocolType.IPv6);
                }
                else
                    throw new Exception("Error reading payload length for ip's payload packet", inner);
            }
        }

        /// <summary>
        /// Parses an arp packet.
        /// </summary>
        /// <param name="what">The packet to parse.</param>
        /// <param name="at">Where to save parsed data.</param>
        /// <param name="ignoreMinorErrors">If set to <c>true</c> ignores minor errors.</param>
        private static void ParseArpPacket(ArpPacket what, FlowPacket at, bool ignoreMinorErrors)
        {
            try
            {
                at.Source = what.SenderProtocolAddress;
                at.Destination = what.TargetProtocolAddress;
            }
            catch (Exception inner)
            {
                throw new Exception("Unable to parse ip addresses", inner);
            }
            ParseEndpointPayload(what, at, true);
            at.ProtocolID = ARP;
        }

        /// <summary>
        /// Parses an ethernet packet.
        /// </summary>
        /// <param name="what">The packet to parse.</param>
        /// <param name="at">Where to save parsed data.</param>
        /// <param name="ignoreMinorErrors">If set to <c>true</c> ignores minor errors.</param>
        public static void ParseEthernetPacket(EthernetPacket what, out FlowPacket at, bool ignoreMinorErrors = false)
        {
            at = null;
            if (what == null)
                return;
            FlowPacket current = new FlowPacket();
            at = current;
            bool hasPayload;
            try
            {
                hasPayload = what.HasPayloadPacket;
            }
            catch (Exception inner)
            {
                throw new Exception("Error reading payload for ethernet packet", inner);
            }

            if (!hasPayload)
                throw new Exception("Unable to parse a bare ethernet packet (it has no payload with ip data).");

            Action parsingSubmethod = null;
            try
            {
                if (what.PayloadPacket is IPPacket ipp)
                    parsingSubmethod = () => ParseIPPacket(ipp, current, ignoreMinorErrors);
                else
                {
                    if (what.PayloadPacket is ArpPacket arp)
                        parsingSubmethod = () => ParseArpPacket(arp, current, ignoreMinorErrors);
                }
                at.OctetsCount = what.PayloadPacket.TotalPacketLength;
            }
            catch
            {
                throw new Exception("Error determining payload type and length for the ethernetPacket.");
            }
            if (parsingSubmethod == null)
                throw new Exception("Unable to determine ip from the payload of ethernet packet.");

            parsingSubmethod();
        }

        /// <summary>
        /// Tries to parse an ethernet packet.
        /// </summary>
        /// <returns><c>true</c>, on success, <c>false</c> otherwise.</returns>
        /// <param name="what">The packet to parse.</param>
        /// <param name="at">Where to save parsed data..</param>
        /// <param name="ignoreMinorErrors">If set to <c>true</c> ignores minor errors.</param>
        public static bool TryParseEthernetPacket(EthernetPacket what, out FlowPacket at, bool ignoreMinorErrors = false)
        {
            FlowPacket fp = null;
            try
            {
                ParseEthernetPacket(what, out fp, ignoreMinorErrors);
                at = fp;
                return true;
            }
            catch
            {
                at = fp;
                return false;
            }
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using PacketDotNet;

namespace LibBtntDtct
{
    /// <summary>
    /// Used to meter flows from packets.
    /// </summary>
    public class FlowAdapter
    {
        /// <summary>
        /// Occurs when flow is spawned.
        /// </summary>
        public event Action<Flow> FlowSpawned;
        /// <summary>
        /// Occurs when packet a packet is appeneded.
        /// </summary>
        public event Action<Flow, FlowPacket> PacketAppened;
        /// <summary>
        /// Occurs when the flow is removed from the listing.
        /// </summary>
        public event Action<Flow> FlowDead;

        /// <summary>
        /// Gets or sets the time window to use.
        /// Should the time windows be exceded, all the flows are removed.
        /// </summary>
        public TimeSpan TimeWindow { get; set; }

        /// <summary>
        /// Gets or sets the date of the last processed packet.
        /// </summary>
        public DateTime LastDate { get; set; }

        /// <summary>
        /// Gets or sets the flows currently listed.
        /// </summary>
        public List<Flow> Flows { get; set; }

        /// <summary>
        /// Searches for a flow which owns the packet given.
        /// Throws exception if more then one owner is found.
        /// </summary>
        /// <returns>The flow found. Null if the packet does not belong to any.</returns>
        public Flow SearchFlow(FlowPacket fp)
        {
            bool Filter(Flow f)
            {
                return
                ((f.Source.Equals(fp.Source) && f.Destination.Equals(fp.Destination) && // outgoing direction
                               f.SPort == fp.SPort && f.DPort == fp.DPort) ||
                 (f.Source.Equals(fp.Destination) && f.Destination.Equals(fp.Source) && // incoming direction
                               f.SPort == fp.DPort && f.DPort == fp.SPort)) &&
                                        f.ProtocolID == fp.ProtocolID; // protocol
            }

            var found = Flows.Where(Filter).ToList();

            switch (found.Count)
            {
                case 0:
                    return null;
                case 1:
                    return found[0];
                default:
                    throw new Exception("Two non-equal flows found.");
            }

        }

        /// <summary>
        /// Removes all the flows from the list and calls corresponding events.
        /// </summary>
        public virtual void KillFlows()
        {
            if(FlowDead != null)
                Flows.ForEach(FlowDead.Invoke);
            Flows.Clear();
        }

        /// <summary>
        /// Creates a flow for a packet given.
        /// </summary>
        /// <param name="fp">Packet to create the flow from.</param>
        /// <param name="flow">Flow class to write flow data in.</param>
        public void ConvertToFlow(FlowPacket fp, Flow flow)
        {
            flow.Start = fp.Timestamp;
            flow.End = fp.Timestamp;
            flow.Source = fp.Source;
            flow.SPort = fp.SPort;
            flow.Destination = fp.Destination;
            flow.DPort = fp.DPort;
            flow.ProtocolID = fp.ProtocolID;
            flow.OctetsCount = fp.OctetsCount;
            flow.PacketsCount = 1;
            flow.OutgoingPackets = 1;
            flow.PayloadOctetsCount = fp.PayloadLength;
        }

        /// <summary>
        /// Reads an ethernet packet packet.
        /// </summary>
        /// <param name="ethPacket">Ethernet packet.</param>
        /// <param name="packetTime">Packet timestamp.</param>
        /// <param name="ignoreMinorErrors">If set to <c>true</c> ignores minor parsing errors.</param>
        public virtual void ReadPacket(EthernetPacket ethPacket, DateTime packetTime, bool ignoreMinorErrors = true)
        {

            if (packetTime - LastDate > TimeWindow || packetTime < LastDate)
            {
                KillFlows();
                LastDate = packetTime;
            }


            if (FlowPacket.TryParseEthernetPacket(ethPacket, out FlowPacket fp, ignoreMinorErrors))
            {
                fp.Timestamp = packetTime;

                if (fp.ProtocolID == FlowPacket.ARP)
                    return;

                Flow flow = SearchFlow(fp);
                if(flow == null)
                {
                    flow = new Flow(); 
                    ConvertToFlow(fp, flow);
                    Flows.Add(flow);
                    FlowSpawned?.Invoke(flow);
                    PacketAppened?.Invoke(flow, fp);
                }
                else
                {
                    flow.End = fp.Timestamp;
                    flow.OctetsCount += fp.OctetsCount;
                    flow.PayloadOctetsCount += fp.PayloadLength;
                    flow.PacketsCount++;
                    if (flow.Source.Equals(fp.Source))
                        flow.OutgoingPackets++;
                    PacketAppened?.Invoke(flow, fp);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.FlowAdapter"/> class.
        /// </summary>
        public FlowAdapter()
        {
            Flows = new List<Flow>();
            LastDate = DateTime.MaxValue;
            TimeWindow = new TimeSpan(0, 0, 60);
        }
    }
}

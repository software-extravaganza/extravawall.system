using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;

namespace ExtravaWall;
public class ExtravaMetrics
{

    private readonly Counter<int> _productSoldCounter;

    public ExtravaMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("ExtravaWall.Router");
        _productSoldCounter = meter.CreateCounter<int>("extrava.packets.inspected");
    }

    public void PacketInspected(string packetTcpProtocol, int quantity)
    {
        _productSoldCounter.Add(quantity,
            new KeyValuePair<string, object?>("extrava.packet.tcp.protocol", packetTcpProtocol));
    }

}

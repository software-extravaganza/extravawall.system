using System.Runtime.InteropServices;
using System.Text;
using ExtravaCore;
using ExtravaWall.Watch;
using System.Buffers.Binary;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("ExtavaWall Watch");

//NetListener4.Start();
//NfqnlTest.Main();
//await KernelClient.StartAsync();

const int intSize = sizeof(int);
var logger = new Logger(false);
SharedMemory2 sharedMemory = new SharedMemory2(logger, "/dev/ringbuffer_device");
RingBufferReader reader = new RingBufferReader(logger, sharedMemory);
DateTime lastTime = DateTime.Now;
bool processedPacketLastRound = false;


//SharedMemoryManager.open_shared_memory("/dev/ringbuffer_device");
while (true) {
    if (!processedPacketLastRound && DateTime.Now - lastTime < TimeSpan.FromMilliseconds(1)) {
        Thread.Sleep(1);
        continue;
    }

    //logger.Log($"Checking for data ({reader.SlotWrittenTimes.Count})");
    for (var slotIndex = 0; slotIndex < reader.SlotWrittenTimes.Count; slotIndex++) {
        var slot = reader.SlotWrittenTimes.ElementAt(slotIndex);
        if (DateTime.Now - slot.Value > TimeSpan.FromMilliseconds(100)) {
            sharedMemory.WriteUserRingBufferSlotStatus((uint)slot.Key, SlotStatus.EMPTY);
            reader.SlotWrittenTimes.Remove(slot.Key);
            logger.Log($"Removed stale response in slot {slot.Key} due to timeout");
        }
    }

    lastTime = DateTime.Now;
    var data = reader.Read();
    if (data == null) {
        //logger.Log("No data found");
        continue;
    }

    processedPacketLastRound = data.Length > 0;
    if (!processedPacketLastRound) {
        continue;
    }

    logger.Log($"Data found: {data.Length} bytes");
    var flags = BitConverter.ToInt32(data.Slice(0, intSize));
    var routingType = (RoutingType)BitConverter.ToInt32(data.Slice(intSize * 1, intSize));
    var version = BitConverter.ToInt32(data.Slice(intSize * 2, intSize));
    var dataLength = BitConverter.ToInt32(data.Slice(intSize * 3, intSize));
    var rawPacketId = data.Slice(intSize * 4, intSize);
    var rawPacketQueueNumber = data.Slice(intSize * 5, intSize);
    var packetQueueNumber = BitConverter.ToInt32(rawPacketQueueNumber);
    var packetId = BitConverter.ToInt32(rawPacketId);
    var payload = data.Slice(intSize * 6, dataLength);
    var routingDecision = RoutingDecision.ACCEPT;
    //var routingDecision = GetRoutingDecision(logger, routingType, payload);

    var dataToSend = new byte[intSize * 3].AsSpan();
    var dataToSendFirstIntBytes = dataToSend.Slice(0, intSize);
    var dataToSendSecondIntBytes = dataToSend.Slice(intSize, intSize);
    var dataToSendThirdIntBytes = dataToSend.Slice(intSize * 2, intSize);

    rawPacketId.Slice(0, intSize).CopyTo(dataToSendFirstIntBytes);
    rawPacketQueueNumber.Slice(0, intSize).CopyTo(dataToSendSecondIntBytes);
    if (BitConverter.IsLittleEndian) {
        BinaryPrimitives.WriteInt32LittleEndian(dataToSendThirdIntBytes, (int)routingDecision); // Or WriteInt32BigEndian
    } else {
        BinaryPrimitives.WriteInt32BigEndian(dataToSendThirdIntBytes, (int)routingDecision); // Or WriteInt32BigEndian
    }

    reader.SendResponse(dataToSend.ToArray());
}

static RoutingDecision GetRoutingDecision(Logger logger, RoutingType routingType, ReadOnlySpan<byte> payload) {
    var routingDecision = RoutingDecision.DROP;
    KernelClient.CreationResult<IPHeader> ipHeaderResult = KernelClient.ParseIPHeader(payload); //.Skip(14).ToArray());
    if (!ipHeaderResult.Success || ipHeaderResult.Value is null) {
        //logger.Log(ipHeaderResult.Error);
        return routingDecision;
    }

    IPHeader ipHeader = ipHeaderResult.Value;
    if (ipHeader.Protocol == IPProtocol.TCP) {
        TCPHeader tcpHeader = KernelClient.ParseTCPHeader(payload.Slice(14 + (ipHeader.VersionAndHeaderLength & 0xF) * 4).ToArray());
        // Continue with further processing.
    } else if (ipHeader.Protocol == IPProtocol.ICMP) {
        var routeString = routingType switch {
            RoutingType.NONE => "None",
            RoutingType.PRE_ROUTING => "Pre-Routing",
            RoutingType.POST_ROUTING => "Post-Routing",
            RoutingType.LOCAL_ROUTING => "Local-Routing",
            _ => "Unknown"
        };

        logger.Log($"Ping! Source {ipHeader.SourceAddressString}, Destination {ipHeader.DestinationAddressString}, Routing Type {routeString}");
        if ((ipHeader.DestinationAddressString == "1.1.1.2" && routingType != RoutingType.PRE_ROUTING) || routingType == RoutingType.PRE_ROUTING) {
            routingDecision = RoutingDecision.ACCEPT;
            logger.Log("Accepting");
        } else {
            routingDecision = RoutingDecision.DROP;
            logger.Log("Dropping");
        }
    }

    return routingDecision;
}



// RingBufferSlot slot = SharedMemoryManager.read_slot(0);
// //slot.ClearanceStartIndex = 100;
//Console.WriteLine("Data found:");
//Console.WriteLine(Encoding.ASCII.GetString(data));
// await Task.Delay(3000);



//SharedMemoryManager.close_shared_memory();

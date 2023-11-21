using System.Runtime.InteropServices;
using System.Text;
using ExtravaCore;
using ExtravaWall.Watch;
using System.Buffers.Binary;
using Microsoft.Extensions.Logging;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("ExtavaWall Watch");

//NetListener4.Start();
//NfqnlTest.Main();
//await KernelClient.StartAsync();

const int intSize = sizeof(int);
const int ushortSize = sizeof(ushort);
const int ulongSize = sizeof(ulong);
using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddConsole()
//.SetMinimumLevel(LogLevel.Information)
.SetMinimumLevel(LogLevel.Trace)
);
ILogger logger = factory.CreateLogger("Program");


//var logger = new Logger(true);
SharedMemory2 sharedMemory = new SharedMemory2(logger, "/dev/ringbuffer_device");
RingBufferReader reader = new RingBufferReader(logger, sharedMemory);
DateTime lastTime = DateTime.Now;
bool processedPacketLastRound = false;
ulong dataProcessed = 0;
ulong HandledPacketCounter = 0;
IDictionary<byte, ulong> protocolCounter = new Dictionary<byte, ulong>();

//SharedMemoryManager.open_shared_memory("/dev/ringbuffer_device");

Task handlePacketsTask = Task.Run(async () => {
    logger.LogInformation("Packet processing thread started");
    while (true) {
        // Thread.Sleep(1000);
        // continue;

        if (!processedPacketLastRound && DateTime.Now - lastTime < TimeSpan.FromMilliseconds(1)) {
            processedPacketLastRound = false;
            await Task.Delay(1);
            continue;
        }

        //logger.Log($"Checking for data ({reader.SlotWrittenTimes.Count})");
        for (var slotIndex = 0; slotIndex < reader.SlotWrittenTimes.Count; slotIndex++) {
            var slot = reader.SlotWrittenTimes.ElementAt(slotIndex);
            if (DateTime.Now - slot.Value > TimeSpan.FromMilliseconds(1000)) {
                sharedMemory.WriteUserRingBufferSlotStatus((uint)slot.Key, SlotStatus.EMPTY);
                reader.SlotWrittenTimes.Remove(slot.Key);
                //logger.LogWarning($"Removed stale response in slot {slot.Key} due to timeout");
            }
        }

        lastTime = DateTime.Now;
        processedPacketLastRound = ProcessNextPacket(intSize, ushortSize, ulongSize, logger, reader, ref dataProcessed, ref HandledPacketCounter, protocolCounter);
    }
});

List<Task> handlingTasks = new List<Task>{
    handlePacketsTask.ContinueWith((task) => {
        logger.LogError($"Task 1 failed: {task.Exception}");
    }, TaskContinuationOptions.OnlyOnFaulted),

    // handlePacketsTask.ContinueWith((task) => {
    //     logger.LogError($"Task 2 failed: {task.Exception}");
    // }, TaskContinuationOptions.OnlyOnFaulted),
};

Task.WaitAll(handlingTasks.ToArray());

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

static bool ProcessNextPacket(int intSize, int ushortSize, int ulongSize, ILogger logger, RingBufferReader reader, ref ulong dataProcessed, ref ulong HandledPacketCounter, IDictionary<byte, ulong> protocolCounter) {
    var data = reader.Read();
    if (data == null || data.Length <= 0) {
        //logger.LogWarning("No data found");
        return false;
    }

    //logger.Log($"Data found: {data.Length} bytes");
    if (data.Length < (intSize * 5) + ushortSize) {
        logger.LogError($"Data too small ({data.Length} bytes)");
        return false;
    }

    HandledPacketCounter++;
    if (HandledPacketCounter % 1_000 == 0 && HandledPacketCounter != 0) {
        var protocolResults = new StringBuilder();
        foreach (var protocol in protocolCounter) {
            protocolResults.Append($"{KernelClient.IpProtocolToString(protocol.Key)}: {protocol.Value}; ");
        }

        logger.LogInformation($"Handled {HandledPacketCounter} packets. Protocols: {protocolResults}");
    }

    var peeler = new DataPeeler(data);
    var flags = peeler.PeelBytesToInt32(); //BitConverter.ToInt32(data.Slice(0, intSize));
    var routingType = peeler.PeelBytesToEnum<RoutingType>();
    var version = peeler.PeelBytesToInt32();
    var dataLength = peeler.PeelBytesToInt32();
    var packetId = peeler.PeelBytesToUInt64();
    var packetQueueNumber = peeler.PeelBytesToUInt32();

    logger.LogTrace($"Flags: {flags}, Routing Type: {routingType}, Version: {version}, Data Length: {dataLength}, Packet ID: {packetId}, Packet Queue Number: {packetQueueNumber}");

    if (dataLength < 0) {
        logger.LogError($"Data length is invalid: {dataLength}");
        return false;
    }

    dataProcessed += (ulong)dataLength;
    if (dataProcessed % 1_000_000_000 == 0 && dataProcessed != 0) {
        logger.LogInformation($"Processed {dataProcessed / 1_000_000_000} Gb of data.");
    }

    if (peeler.Length < dataLength) {
        logger.LogError($"Data too small ({peeler.Length} bytes) for payload ({dataLength} bytes)");
        return false;
    }

    var payload = peeler.PeelBytes(dataLength);
    var routingDecision = (uint)RoutingDecision.ACCEPT;
    //var routingDecision = GetRoutingDecision(logger, routingType, payload);
    KernelClient.CreationResult<IPHeader> ipHeaderResult = KernelClient.ParseIPHeader(payload);
    if (ipHeaderResult.Success) {
        var protocol = (byte)ipHeaderResult.Value.Protocol;
        if (protocolCounter.TryGetValue(protocol, out var protocolCount)) {
            protocolCounter[protocol] = protocolCount + 1;
        } else {
            protocolCounter.Add(protocol, 1);
        }
    }

    var dataToSend = new byte[(intSize * 2) + ulongSize].AsSpan();
    var dataToSendFirstIntBytes = dataToSend.Slice(0, ulongSize);
    var dataToSendSecondIntBytes = dataToSend.Slice(ulongSize, intSize);
    var dataToSendThirdIntBytes = dataToSend.Slice(ulongSize + intSize, intSize);

    BitConverter.GetBytes(packetId).CopyTo(dataToSendFirstIntBytes);
    BitConverter.GetBytes(packetQueueNumber).CopyTo(dataToSendSecondIntBytes);
    if (BitConverter.IsLittleEndian) {
        BinaryPrimitives.WriteInt32LittleEndian(dataToSendThirdIntBytes, (int)routingDecision); // Or WriteInt32BigEndian
    } else {
        BinaryPrimitives.WriteInt32BigEndian(dataToSendThirdIntBytes, (int)routingDecision); // Or WriteInt32BigEndian
    }

    logger.LogTrace($"Sending response for packet {packetId} with routing decision {routingDecision}");
    reader.SendResponse(dataToSend.ToArray());
    return true;
}



// RingBufferSlot slot = SharedMemoryManager.read_slot(0);
// //slot.ClearanceStartIndex = 100;
//Console.WriteLine("Data found:");
//Console.WriteLine(Encoding.ASCII.GetString(data));
// await Task.Delay(3000);



//SharedMemoryManager.close_shared_memory();

using System.Runtime.InteropServices;
using System.Text;
using ExtravaCore;
using ExtravaWall.Watch;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("ExtavaWall Watch");

//NetListener4.Start();
//NfqnlTest.Main();
//await KernelClient.StartAsync();


SharedMemory2 sharedMemory = new SharedMemory2("/dev/ringbuffer_device");
RingBufferReader reader = new RingBufferReader(sharedMemory);
//SharedMemoryManager.open_shared_memory("/dev/ringbuffer_device");
while (true) {
    byte[] data = reader.Read();
    // RingBufferSlot slot = SharedMemoryManager.read_slot(0);
    // //slot.ClearanceStartIndex = 100;
    //Console.WriteLine("Data found:");
    //Console.WriteLine(Encoding.ASCII.GetString(data));
    // await Task.Delay(3000);
}


//SharedMemoryManager.close_shared_memory();

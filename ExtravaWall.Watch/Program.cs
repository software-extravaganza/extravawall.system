using System.Runtime.InteropServices;
using System.Text;
using ExtravaCore;
using ExtravaWall.Watch;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("ExtavaWall Watch");

//NetListener4.Start();
//NfqnlTest.Main();
//await KernelClient.StartAsync();

SharedMemoryManager.open_shared_memory("/dev/ringbuffer_device");

// Read from slot
RingBufferSlot slot = SharedMemoryManager.read_slot(5);

// Modify and write back to slot
slot.ClearanceStartIndex = 100; // Example modification
//SharedMemoryManager.write_slot(0, slot);


SharedMemoryManager.close_shared_memory();

// Print the read data (for demonstration purposes, assuming it's ASCII text)
Console.WriteLine("Data found:");
Console.WriteLine(Encoding.ASCII.GetString(slot.Data));
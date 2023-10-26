using System.Runtime.InteropServices;
using System.Text;
using ExtravaCore;
using ExtravaWall.Watch;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("ExtavaWall Watch");

//NetListener4.Start();
//NfqnlTest.Main();
//await KernelClient.StartAsync();

// Initialize shared memory with the device path
SharedMemory sharedMemory = new SharedMemory("/dev/ringbuffer_device", (uint)Marshal.SizeOf(typeof(RingBuffer)));

// Create a reader instance
RingBufferReader reader = new RingBufferReader(sharedMemory);

// Read data from the shared memory
byte[] data = reader.Read();

// Print the read data (for demonstration purposes, assuming it's ASCII text)
Console.WriteLine("Data found:");
Console.WriteLine(Encoding.ASCII.GetString(data));
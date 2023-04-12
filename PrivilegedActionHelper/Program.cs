// See https://aka.ms/new-console-template for more information

using PrivilegedActionHelper;
using Tmds.DBus;

var connection = new Connection(Address.Session);
await connection.ConnectAsync();

var helper = new PrivilegedHelper();
await connection.RegisterObjectAsync(helper);

if (args.Length > 0 && args[0] == "create-root-file")
{
    string filePath = "/root/test_privileged_file.txt";
    File.WriteAllText(filePath, "This is a test file created by the PrivilegedActionHelper.");
    Console.WriteLine($"File created at: {filePath}");
}
else
{
    Console.WriteLine("No action specified.");
}
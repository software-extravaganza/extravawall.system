using System.Diagnostics;
using System.Reflection;
using System.Text;
using PrivilegedActionHelper;
using Tmds.DBus;

namespace ExtravaWallSetup;

public class PriviledgeManager
{
  private static void CreatePolicyFile(string helperPath)
  {
    string policyContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE policyconfig PUBLIC
 ""-//freedesktop//DTD PolicyKit Policy Configuration 1.0//EN""
 ""http://www.freedesktop.org/standards/PolicyKit/1/policyconfig.dtd"">
<policyconfig>
  <vendor>Extravaganza Software</vendor>
  <vendor_url>https://www.example.com</vendor_url>
  <action id=""software.extravaganza.extravawall.privilegedaction"">
    <description>Perform a privileged action</description>
    <message>Authentication is required to perform the privileged action.</message>
    <defaults>
      <allow_any>auth_admin</allow_any>
      <allow_inactive>auth_admin</allow_inactive>
      <allow_active>auth_admin</allow_active>
    </defaults>
    <annotate key=""org.freedesktop.policykit.exec.path"">{helperPath}</annotate>
    <annotate key=""org.freedesktop.policykit.exec.allow_gui"">true</annotate>
  </action>
</policyconfig>";

    string policyDirectory = "/usr/share/polkit-1/actions";
    string policyFile = Path.Combine(policyDirectory, "software.extravaganza.extravawall.privilegedaction.policy");

    if (!Directory.Exists(policyDirectory))
    {
      Directory.CreateDirectory(policyDirectory);
    }

    using var fileStream = new FileStream(policyFile, FileMode.Create, FileAccess.Write);
    byte[] info = new UTF8Encoding(true).GetBytes(policyContent);
    fileStream.Write(info, 0, info.Length);
    fileStream.Close();
  }

  public async Task<string> ExtractHelper()
  {
    // Extract the PrivilegedHelper to a temporary location
    string tempDirectory = Path.Combine(Path.GetTempPath(), "PrivilegedActionHelper");
    Directory.CreateDirectory(tempDirectory);
    string resourceName = "PrivilegedActionHelper";
    string helperPath = Path.Combine(tempDirectory, resourceName);
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
    if (stream == null)
    {
      throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
    }

    using var fileStream = new FileStream(helperPath, FileMode.Create, FileAccess.Write);
    stream.CopyTo(fileStream);

    // Make the extracted helper executable
    Process.Start("chmod", $"+x \"{helperPath}\"").WaitForExit();

    // Create the PolicyKit policy file
    CreatePolicyFile(tempDirectory);

    // Perform the privileged action using D-Bus and PolicyKit
    var result = await CallPrivilegedHelperAsync();

    // Clean up
    File.Delete(helperPath);
    Directory.Delete(tempDirectory);

    return result;
  }

  async Task<string> CallPrivilegedHelperAsync()
  {
    var connection = new Connection(Address.Session);
    await connection.ConnectAsync();

    var privilegedHelperProxy = connection.CreateProxy<IPrivilegedHelper>("software.extravaganza.extravawall.PrivilegedHelper", "/software/extravaganza/extravawall/PrivilegedHelper");
    string result = await privilegedHelperProxy.PerformPrivilegedTask("test");

    Console.WriteLine("Result from privileged helper: " + result);
    return result;
  }
}
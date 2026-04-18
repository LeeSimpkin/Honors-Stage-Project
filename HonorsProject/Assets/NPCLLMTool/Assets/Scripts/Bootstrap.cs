using UnityEngine;
using System.Diagnostics;
using System.IO;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        string flagPath = Application.persistentDataPath + "/setup_done.txt";


    if (!File.Exists(flagPath))
        {
            RunSetup();
            File.WriteAllText(flagPath, "done");
        }
    }

    void RunSetup()
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "powershell.exe";
        psi.Arguments = "-ExecutionPolicy Bypass -File scripts/setup.ps1";
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;

        Process.Start(psi);
    }


}

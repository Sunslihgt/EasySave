using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public static class ProcessChecker
    {
        public static bool AreProcessesRunning(List<BannedSoftware> processes)
        {
            return processes.Any(process => IsProcessRunning(process.Software));
        }
        
        // Check if a process is running by name
        public static bool IsProcessRunning(string processName)
        {
            if (processName.EndsWith(".exe"))
            {
                processName = processName.Replace(".exe", "");
            }
            return Process.GetProcessesByName(processName).Length > 0; // At least one process found
        }
    }
}

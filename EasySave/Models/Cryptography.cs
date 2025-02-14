using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class Cryptography
    {
        // Encrypt a file using the CryptoSoft executable
        // Returns the exit code of the process (encryption duration in ms, or -1 if an error occurred)
        public static int Encrypt(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrEmpty(Settings.Instance.CryptoSoftPath))
            {
                Console.Error.WriteLine("CryptoSoft path not set! Cannot encrypt file.");
                return -1;
            }
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = Settings.Instance.CryptoSoftPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { sourcePath, destinationPath, Settings.Instance.CryptoKey }
                };
                var myProcess = Process.Start(processStartInfo);
                if (myProcess != null) myProcess.WaitForExit();

                return myProcess?.ExitCode ?? -1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return -1;
        }

        public static string GenerateCryptoKey(int length = 16)
        {
            if (length < 16) throw new ArgumentException("Key length must be at least 16 characters.");

            byte[] keyBytes = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }

            // Convert to a Base64 string and trim to the required length
            string key = Convert.ToBase64String(keyBytes).Substring(0, length);

            return key;
        }

        // Check if the file should be encrypted based on its extension
        public static bool ShouldEncrypt(string path)
        {
            return Settings.Instance.EncryptExtensions.Any(extension => path.EndsWith(extension));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class SaveType
    {
        public string realDirectoryPath, copyDirectoryPath;

        public SaveType(string realDirectoryPath, string copyDirectoryPath)
        {
            this.realDirectoryPath = realDirectoryPath;
            this.copyDirectoryPath = copyDirectoryPath;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mindshift.SC9.PackageInstaller.Models
{
    public class InstallPackageResult
    {
        public bool Successful { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class vmFile
    {
        [CheckFile(false, 500000000, "video/*")]
        public IFormFile VideoUrl { get; set; }
    }
}

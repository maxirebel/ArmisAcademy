using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmSelect
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public string Title { get; set; }
    }
    public class vmCheck
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public bool Checked { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmPayResult
    {
        public int OrderID { get; set; }
        public string RefID { get; set; }
        public string CourseName { get; set; }
        public string CourseLink { get; set; }
        public string Status { get; set; }
        public string Amount { get; set; }
        public int Level { get; set; }
    }
    public class VmOnlinePayResult
    {
        public int OrderID { get; set; }
        public string RefID { get; set; }
        public string CourseTitle { get; set; }
        public string Package { get; set; }
        public string Status { get; set; }
        public string Amount { get; set; }
        public string ClassLink { get; set; }
        public string ClassImage { get; set; }
    }
}

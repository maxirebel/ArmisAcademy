using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmSchedule
    {
        public int CourseID { get; set; }
        public int InvoiceID { get; set; }
        public int RemainingDay { get; set; }
        public int RemainingClassNum { get; set; }

        public int ClassNum { get; set; }

        public List<VmScheduleEvent> lstEvents { get; set; }
        public List<VmCanceledClass> lstCanceledClass { get; set; }
        public List<VmReservedClass> lstReservedClass { get; set; }


    }
    public class VmScheduleEvent
    {
        public string id { get; set; }
        public string title { get; set; }
        public int Status { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string groupId { get; set; }
        public string display { get; set; }
        public bool overlap { get; set; }
        public string color { get; set; }
        public string constraint { get; set; }
        public string classNames { get; set; }
    }
    public class VmCanceledClass
    {
        public int ID { get; set; }
        public string Title { get; set; }
    }
    public class VmReservedClass
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
    }
}

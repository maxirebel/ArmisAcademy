using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmEvidence
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string CourseTitle { get; set; }
        public string Date { get; set; }
        public int SectionID { get; set; }
        public int PrintStatus { get; set; }
    }
    public class VmPlacement
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Date { get; set; }
        public string SkypeID { get; set; }
        public string TelegramID { get; set; }
        public string TeacherDescription { get; set; }
        public string StudentDescription { get; set; }
        public string TrainingPeriod { get; set; }
        public int Status { get; set; }
        public int SheetLevel { get; set; }
        public int TheoreticalLevel { get; set; }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmOnlineCourse
    {
        public int ID { get; set; }
        public int GroupID { get; set; }
        public int SubGroupID { get; set; }
        public int Visit { get; set; }
        public int Type { get; set; }
        public string TeacherID { get; set; }
        public string TeacherUserName { get; set; }
        public string TeacherImg { get; set; }
        public string Link { get; set; }
        public string GroupName { get; set; }
        public string SubGroupName { get; set; }
        public int TotalPrice { get; set; }
        public int FinalPrice { get; set; }
        public int DiscountPrice { get; set; }
        public int TeacherPercent { get; set; }
        public bool CancelEnable { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Teacher { get; set; }
        public string Image { get; set; }
        public string Date { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ShortLink { get; set; }
        public string MetaTag { get; set; }
        public string Keywords { get; set; }
        public string PackageName { get; set; }
        public int SessionsCount { get; set; }
        public int Capacity { get; set; }
        public int Status { get; set; }
        public bool ConfirmedPrice { get; set; }

        public List<VmCourseScheduling> LstScheduling { get; set; }
        public VmCoursePackagesPrice PackagesPrice { get; set; }

    }
    public class VmCourseScheduling
    {
        public int ID { get; set; }
        public int Sort { get; set; }
        public string Title { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

    }
    public class VmCoursePackagesPrice
    {
        public int ID { get; set; }
        public int PkgPrice1 { get; set; }
        public int PkgPrice2 { get; set; }
        public int PkgPrice3 { get; set; }
        public int PkgPrice4 { get; set; }
        public int PkgPrice5 { get; set; }
        public int PkgDiscount1 { get; set; }
        public int PkgDiscount2 { get; set; }
        public int PkgDiscount3 { get; set; }
        public int PkgDiscount4 { get; set; }
        public int PkgDiscount5 { get; set; }
    }
    public class VmClassStudent
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Teacher { get; set; }
        public int Type { get; set; }
        public int InvoiceID { get; set; }
        public int Status { get; set; }
        public int RemainingDay { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string TotalPrice { get; set; }
        public string Image { get; set; }
        public string StuduntImage { get; set; }
        public int StuduntCount { get; set; }
        public string Package { get; set; }
        public string Date { get; set; }
        public bool ConfirmedPrice { get; set; }
        public bool CancelEnable { get; set; }
        public string Link { get; set; }

    }
}

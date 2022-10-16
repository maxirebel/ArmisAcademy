using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmEarning
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string UserID { get; set; }
        public string UserCode { get; set; }
        public int UserStepID { get; set; }
        public string ProfileImage { get; set; }
        public string Inventory { get; set; }
        public string Title { get; set; }
        public string Role { get; set; }
        public int TotalSales { get; set; }
        public string TotalEarn { get; set; }
        public int TotalVisit { get; set; }


        public int Status { get; set; }

        public List<VmStepEarn> ListStepEarn { get; set; }
        public List<VmEarnLinks> ListEarnLinks { get; set; }
        public List<VmEarnTransaction> ListEarnTransaction { get; set; }

    }
    public class VmStepEarn
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int Rate { get; set; }
        public int Value { get; set; }
        public bool Checked { get; set; }
    }
    public class VmEarnLinks
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string UserName { get; set; }
        public string Link { get; set; }
        public int Visit { get; set; }
        public int BuyCount { get; set; }
    }
    public class VmEarnTransaction
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Amount { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public int TrackingCode { get; set; }
        public int Status { get; set; }

    }
    public class VmEarnReagent
    {
        public int ID { get; set; }
        public string CourseName { get; set; }
        public int Amount { get; set; }
        public int Type { get; set; }
        public string Date { get; set; }

    }
}

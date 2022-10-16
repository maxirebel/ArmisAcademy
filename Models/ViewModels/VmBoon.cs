using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmBoon
    {
        public int ID { get; set; }

        public string Title { get; set; }

        public bool Private { get; set; }

        public string Count { get; set; }

        public string RemainingCount { get; set; }

        public string Limit { get; set; }

        public string GroupLimit { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public string Description { get; set; }
    }
    public class Group
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public bool Checked { get; set; }
    }
    public class UserBoon
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmChat
    {
        public int ID { get; set; }

        public string SenderID { get; set; }

        public string SenderName { get; set; }

        public string ReceiverID { get; set; }

        public string Text { get; set; }
        public string Date { get; set; }
        public string ProfileImage { get; set; }
    }
}

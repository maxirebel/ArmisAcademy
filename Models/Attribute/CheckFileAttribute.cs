using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models
{
    public class CheckFileAttribute : ValidationAttribute
    {/// <summary>
     /// 
     /// </summary>
     /// <param name="MaxSize">حداکثر سایز مجاز برای اپلود فایل برحسب کیلوبایت</param>
     /// <param name="ContentType">MIMEs Type Of Uploaded Document</param>
        public CheckFileAttribute(bool isRequired, int MaxSize, string ContentType)
        {
            _MaxSize = MaxSize;
            _ContentType = ContentType;
            _IsRequired = isRequired;
        }
        private long _MaxSize { get; set; }
        private string _ContentType { get; set; }
        private bool _IsRequired { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            IFormFile MyFile = (IFormFile)value;
            if (_IsRequired == true && MyFile == null)
            {
                return new ValidationResult("فایل را انتخاب کنید");
            }

            if (MyFile != null)
            {
                if (MyFile.Length > (_MaxSize * 1024))
                {
                    return new ValidationResult(" حجم فایل بیش از " + (_MaxSize) + " می باشد ");
                }

                if (!_ContentType.Contains(MyFile.ContentType))
                {
                    return new ValidationResult("قالب فایل اشتباه است");
                }
            }
            else
            {
                return null;
            }

            return null;
        }
    }
}

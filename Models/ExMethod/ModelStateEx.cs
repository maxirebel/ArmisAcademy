using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


    public static class ModelStateEx
    {
        public static string GetErrors(this ModelStateDictionary modelState)
        {
            return string.Join("<br />", (from item in modelState
                                          where item.Value.Errors.Any()
                                          select item.Value.Errors[0].ErrorMessage).ToList());
        }
    }

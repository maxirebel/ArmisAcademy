using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.AccountViewModels
{
    public class EmailTemplate
    {
        public const string Tamplate = "<html><head><style>/*! normalize.css v3.0.0 | MIT License | git.io/normalize *//*! normalize.css v3.0.0 | HTML5 Display Definitions | MIT License | git.io/normalize */@import url(http://fonts.googleapis.com/css?family=Open+Sans:400,300,600);" +
            "article,aside,details,figcaption,figure,footer,header,hgroup,main,nav,section,summary{display:block}" +
            "audio,canvas,progress,video{display:inline-block;vertical-align:baseline}audio:not([controls])" +
            "{display:none;height:0}[hidden],template{display:none}/*! normalize.css v3.0.0 | Base | MIT License | git.io/normalize */html" +
            "{font-family:Tahoma,sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%}body{margin:0}/*! normalize.css v3.0.0 |" +
            " Links | MIT License | git.io/normalize */a{background:0 0}a:active,a:hover{outline:0}/*! normalize.css v3.0.0 | Typography | MIT License | git.io/normalize */abbr[title]" +
            "{border-bottom:1px dotted}b,strong{font-weight:700}dfn{font-style:italic}h1{font-size:2em;margin:.67em 0}mark{background:#ff0;color:#000}small{font-size:80%}sub,sup" +
            "{font-size:75%;line-height:0;position:relative;vertical-align:baseline}sup{top:-.5em}sub{bottom:-.25em}/*! normalize.css v3.0.0 | Embedded Content | MIT License | git.io/normalize */img{border:0}svg:not(:root)" +
            "{overflow:hidden}/*! normalize.css v3.0.0 | Figures | MIT License | git.io/normalize */figure{margin:1em 40px}hr{-moz-box-sizing:content-box;box-sizing:content-box;height:0}pre{overflow:auto}code,kbd,pre,samp" +
            "{font-family:monospace,monospace;font-size:1em}/*! normalize.css v3.0.0 | Forms | MIT License | git.io/normalize */button,input,optgroup,select,textarea{color:inherit;font:inherit;margin:0}button{overflow:visible}button,select{text-transform:none}button,html input[type=button],input[type=reset],input[type=submit]" +
            "{-webkit-appearance:button;cursor:pointer}button[disabled],html input[disabled]{cursor:default}button::-moz-focus-inner,input::-moz-focus-inner{border:0;padding:0}legend{border:0;padding:0}textarea{overflow:auto}optgroup{font-weight:700}" +
            "/*! normalize.css v3.0.0 | Tables | MIT License | git.io/normalize */table{border-collapse:collapse;border-spacing:0}td,th{padding:0}.header{overflow:hidden;padding:.75em 0;background:#fca60c;color:#fff}.header a{color:#fff}.header a:hover{color:#fff}.header.home{padding:3em 0;text-align:center}" +
            ".header.home .logo{padding-bottom:.5em;display:block;margin:0 auto}.header.home h1{font-size:2em;margin:.5em 0}.header.home p{margin:0;font-weight:600}.header.home p+a{margin:2.5em 0 0 0}.header.home span{display:block;font-size:.8em;font-weight:300;color:#bec2c7;margin-top:1em}@media only screen and (min-width:40em){.header.home{padding:3.5em 0}.header.home .logo{width:9.25em}.header.home h1{font-size:3em}}" +
            "@media only screen and (min-width:60em){.header.home{font-size:1.2em}}.footer{overflow:hidden;padding:2em 0;background:#f5f5f5}.footer a{font-size:.8em}.footer a.brand{font-size:1em}.footer li{margin-top:1.5em}.footer li:first-child{margin-top:0}" +
            "@media only screen and (min-width:46em){.footer{text-align:center}.footer li{margin:1em 1em 0 1em;display:inline-block}.footer li:first-child{display:block}}@media only screen and (min-width:70em){.footer{padding:1.5em 0}.footer li{margin:0 1.5em}" +
            ".footer li:first-child{display:inline-block}}input,label{display:block}label{cursor:pointer;margin-top:1.5em}input[type=email],input[type=text],input[type=url],select{font-size:.8em;width:100%;padding:.75em;margin-top:.75em;color:#6a727d}" +
            ".button,button,input[type=submit]{display:inline-block;padding:.5em 1em;background:#3b4046;color:#fff;border:0}.button:hover,button:hover,input[type=submit]:hover{color:#fff;background:#24262a}@media only screen and (min-width:60em)" +
            "{.form h1{font-size:3em;padding:0 .65em}button,input[type=submit]{font-size:1.2em}input[type=email],input[type=text],input[type=url],select{font-size:.8em;width:100%;padding:.75em;margin-top:.75em;color:#6a727d}}" +
            "*{-moz-box-sizing:border-box;-webkit-box-sizing:border-box;box-sizing:border-box}body{font-size:1em;font-family:Tahoma,Arial,'Open Sans',sans-serif;font-weight:300;color:#6a727d;background:#f5f5f5;display:flex;min-height:100vh;flex-direction:column}h1,h2,h3,h4,h5,h6{font-weight:300}h2{border-bottom:1px solid #d6d9dd;margin:0;padding:0 0 .25em 0}a{color:#6a727d;text-decoration:none;transition:color .25s}" +
            "a:hover{color:#b61528}a.brand{color:#b61528;font-weight:600}a.brand:hover{color:#fca60c}ul{margin:0;padding:0;list-style-type:none}.wrap{max-width:60em;margin:0 auto;overflow:hidden}.confirmation .wrap,.index .wrap,.submit-pattern .wrap{max-width:70em}.main{padding:2em 0;background:#fff;flex:1}@media only screen and (min-width:60em){.main{padding:3em 0 4em 0}}.footer,.header,.header.home,.main" +
            "{padding-left:1.5em;padding-right:1.5em}@media only screen and (min-width:40em){.footer,.header.home,.pattern-support{direction:rtl;padding-left:2em;padding-right:2em}.header,.main{padding-left:0;padding-right:0}}.title{border-bottom:1px solid #d6d9dd;padding-bottom:5px;text-align:center}.content{padding:10px;text-align:center;direction:rtl}.action{padding:10px;text-align:center}</style></head><body><header class='header home'>" +
            "<div href='http://www.responsiveemailpatterns.com'><img class='logo' src='https://www.freepnglogos.com/uploads/gmail-email-logo-png-16.png'" +
            " alt='' width='90'></div><h1>آکادمی موسیق آرمیس</h1><p>توضیح کوتاه</p></header><section class='main'><div class='wrap'><div class='main-content'><div class='title'>حساب کاربری شما با موفقیت ایجاد گردید</div>" +
            "<div class='content'>با تشکر از شما بابت انتخاب ما . هم اکنون می توانید از طریق لینک زیر حساب خود را فعال نمایید</div><div class='action'><a class='button' href='submit-pattern.html'>تایید حساب</a></div></div>" +
            "</div></section><footer class='footer'><nav><ul><li><a class='brand' href='http://www.responsiveemailpatterns.com'>صفحه اصلی</a></li><li><a href='http://www.responsiveemailresources.com'>دوره های آموزشی</a></li><li>" +
            "<a href='http://www.responsiveemailresources.com'>محصولات</a></li><li><a href='http://www.responsiveemailpatterns.com/support.html'>وبلاگ</a></li><li><a href='https://github.com/briangraves/ResponsiveEmailPatterns'>تماس با ما</a>" +
            "</li><li><a href='http://www.twitter.com/briangraves'>درباره ما</a></li></ul></nav></footer></body></html>";
    }
}

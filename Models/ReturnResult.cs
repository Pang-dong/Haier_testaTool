using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Services
{
    public  class ReturnResult
    {
        public class BaseResult
        {
            public string status { get; set; }

            public string msg { get; set; }
            public bool IsSuccess => status == "1";
        }
    }
}

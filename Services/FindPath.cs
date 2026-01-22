using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Haier_E246_TestTool.Services
{
    public  class FindPath
    {
        public string GetFilePathByKeyword( string directory,string keyword,string DefaultFileName)
        {
            if(!Directory.Exists(directory))
            {
                return Path.Combine(directory,DefaultFileName);
            }
            try
            {
                var files =Directory.GetFiles(directory,"*"+keyword+"*");
                if (files.Length > 0) 
                {
                    return files[0];
                }
                else
                {
                    return Path.Combine(directory, DefaultFileName);
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine($"查找文件出错 (关键字: {keyword}): {ex.Message}");
                return Path.Combine(directory, DefaultFileName);
            }
        }
    }
}

using Newtonsoft.Json.Linq;
using Haier_E246_TestTool.Models;
using System;
using Haier_E246_TestTool.Services;

namespace Haier_E246_TestTool.Services
{
    public class WriteTestResultService
    {
        public string EnrichJsonData(string originalJson, AppConfig config, string testStation,
                              string deviceMac, string wifiVer, string cameraVer,string sn)
        {
            try
            {
                JObject jsonObject = JObject.Parse(originalJson);

                if (config != null)
                {
                    jsonObject["Operator"] = config.LastUser;
                }
                jsonObject["TestType"] = testStation;
                jsonObject["MAC"] = deviceMac;
                jsonObject["MAC1"] = wifiVer;
                jsonObject["MAC2"] = cameraVer;
                jsonObject["SN"] = sn;
                jsonObject["ZJ_Result"] = 1;

                return jsonObject.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                App.LogService?.WriteLog($"JSON数据处理异常: {ex.Message}", LogType.Error,true);
                return originalJson;
            }
        }
    }
}
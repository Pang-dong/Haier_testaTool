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
        public string WriteJsonResult(string originalJson, string Workstation,AppConfig appConfig,string Lisence,string sn)
        {
            try
            {
                JObject jsonObject = JObject.Parse(originalJson);

                if (appConfig != null)
                {
                    jsonObject["Operator"] =appConfig.LastUser;
                }
                jsonObject["TestType"] = Workstation;
                jsonObject["ZJ_Result"] = 1;
                jsonObject["UUID"] = Lisence;
                jsonObject["ComputerNo"] =appConfig.FtpIp;
                jsonObject["SN"] = sn;

                return jsonObject.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                App.LogService?.WriteLog($"JSON数据处理异常: {ex.Message}", LogType.Error, true);
                return originalJson;
            }
        }
        public string WriteJsonYHResult(string originalJson, string Workstation, AppConfig appConfig, string sn)
        {
            try
            {
                JObject jsonObject = JObject.Parse(originalJson);

                if (appConfig != null)
                {
                    jsonObject["Operator"] = appConfig.LastUser;
                }
                jsonObject["TestType"] = Workstation;
                jsonObject["ZJ_Result"] = 1;
                jsonObject["SN"] = sn;
                jsonObject["ComputerNo"] = appConfig.FtpIp;

                return jsonObject.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                App.LogService?.WriteLog($"JSON数据处理异常: {ex.Message}", LogType.Error, true);
                return originalJson;
            }
        }
    }
}
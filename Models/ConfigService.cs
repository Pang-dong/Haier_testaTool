using Haier_E246_TestTool.Models;
using Newtonsoft.Json; // 引用 Newtonsoft.Json
using System;
using System.IO;

namespace Haier_E246_TestTool.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly ILogService _logService;

        public ConfigService(ILogService logService)
        {
            _logService = logService;
            // 文件名改为 .json
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppConfig.json");
        }

        public  AppConfig Load()
        {
            if (!File.Exists(_configPath))
            {
                _logService.WriteLog("配置文件不存在，初始化默认配置。", LogType.Info);
                return new AppConfig();
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                // 反序列化 JSON
                var config = JsonConvert.DeserializeObject<AppConfig>(json);

                if (config == null) return new AppConfig();

                _logService.WriteLog("配置文件(JSON)加载成功。", LogType.Info);
                return config;
            }
            catch (Exception ex)
            {
                _logService.WriteLog($"加载配置失败: {ex.Message}，使用默认配置。", LogType.Error);
                return new AppConfig();
            }
        }

        public  void Save(AppConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                _logService.WriteLog($"保存配置失败: {ex.Message}", LogType.Error);
            }
        }
    }
}
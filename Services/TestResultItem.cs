using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Services
{
    public class TestResult
    {
        public byte CommandId { get; set; }
        public bool Success { get; set; }
        public string Data { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class TestResultManager
    {
        private readonly Dictionary<byte, TestResult> _testResults = new Dictionary<byte, TestResult>();
        private readonly object _lock = new object();
        public void RecordTestResult(byte commandId, bool success, string data = null, string errorMessage = null)
        {
            lock (_lock)
            {
                _testResults[commandId] = new TestResult
                {
                    CommandId = commandId,
                    Success = success,
                    Data = data,
                    ErrorMessage = errorMessage
                };
            }
        }
        public bool GetTestResult(byte commandId)
        {
            lock (_lock)
            {
                return _testResults.TryGetValue(commandId, out TestResult result)&& result.Success;
            }
        }
        public TestResult GetTestResultData(byte commandId)
        {
            lock (_lock)
            {
                _testResults.TryGetValue(commandId,out var result);
                return result;
            }
        }
        public bool AllTestPassed()
        {
            lock(_lock)
            {
                return _testResults.Count > 0&&_testResults.Values.All(x => x.Success);
            }
        }
        public bool AnyTestFailed()
        {
            lock (_lock)
            {
                return _testResults.Values.Any(r => !r.Success);
            }
        }

        public int GetPassCount()
        {
            lock (_lock)
            {
                return _testResults.Values.Count(r => r.Success);
            }
        }

        public int GetTotalCount()
        {
            lock (_lock)
            {
                return _testResults.Count;
            }
        }

        public Dictionary<byte, TestResult> GetAllResults()
        {
            lock (_lock)
            {
                return new Dictionary<byte, TestResult>(_testResults);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _testResults.Clear();
            }
        }
    }
}

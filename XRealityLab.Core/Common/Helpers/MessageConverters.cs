using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Common.Helpers
{
    public class MessageConverter
    {
        public static ResponseDTO GetResponse(string message, MethodResult result)
        {
            return new ResponseDTO { ResponseGUID = Guid.NewGuid().ToString(), Message = message, Result = Enum.GetName(typeof(MethodResult), result) };
        }
    }
    public class ResponseDTO
    {
        public string ResponseGUID { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }
        public int? Value { get; set; }
    }
    public enum MethodResult
    {
        [Description("Succeeded")]
        Succeeded,
        [Description("Failed")]
        Failed
    }
}

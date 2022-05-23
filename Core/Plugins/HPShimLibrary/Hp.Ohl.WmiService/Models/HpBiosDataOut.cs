namespace Hp.Ohl.WmiService.Models
{
    public class HpBiosDataOut
    {
        public HpBiosDataOut(string originalDataType, bool? active, byte[] data, string instanceName,
        uint rwReturnCode, byte[] sign)
        {
            OriginalDataType = originalDataType;
            Active = active;
            Data = data;
            InstanceName = instanceName;
            RwReturnCode = rwReturnCode;
            Sign = sign;
        }
        public string OriginalDataType { get; set; }
        public bool? Active { get; set; }
        public byte[] Data { get; set; }
        public string InstanceName { get; set; }
        public uint RwReturnCode { get; set; }
        public byte[] Sign { get; set; }

    }
}
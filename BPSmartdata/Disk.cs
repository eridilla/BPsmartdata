using System.Collections.Generic;

namespace BPSmartdata
{
    public class Disk
    {
        public Disk()
        {
            SmartAttributes = new AttributeCollection();
            DriveLetters = new List<string>();
        }

        public int Index { get; set; }

        public string DeviceID { get; set; }
        public string PnpDeviceID { get; set; }

        public List<string> DriveLetters { get; set; }
        public bool IsOK { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Serial { get; set; }
        public AttributeCollection SmartAttributes { get; set; }

        public bool IsSupported {get; set; }
    }
}
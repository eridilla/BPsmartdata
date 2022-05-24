using System;

namespace BPSmartdata
{
    public class DiskLayoutData
    {
        public string device { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string serialNum { get; set; }
        public string smartStatus { get; set; }
        public SmartData smartData { get; set; }

        public DiskLayoutData(Disk disk)
        {
            device = disk.Model;
            type = disk.Type;
            name = disk.DeviceID;
            serialNum = disk.Serial;
            smartStatus = disk.IsOK ? "drive OK" : "drive fail";
            smartData = new SmartData(disk.SmartAttributes);
        }
    }
    
    public class SmartData
    {
        public Ata_smart_attributes ata_smart_attributes { get; set; }
        
        public SmartData(AttributeCollection attributes)
        {
            ata_smart_attributes = new Ata_smart_attributes(attributes);
        }
    }

    public class Ata_smart_attributes
    {
        public Table[] table { get; set; }
        public Ata_smart_attributes(AttributeCollection attributes)
        {
            table = new Table[attributes.Count];

            for (int i = 0; i < attributes.Count; i++)
            {
                table[i] = new Table(attributes[i]);
            }
        }
    }

    public class Table
    {
        public int id { get; set; }
        public string name { get; set; }
        public int value { get; set; }
        public int worst { get; set; }
        public int thresh { get; set; }

        public Table(Attribute attribute)
        {
            id = attribute.Register;
            name = attribute.Name;
            value = attribute.Current;
            worst = attribute.Worst;
            thresh = attribute.Threshold;
        }
    }
    
    public class OsData
    {
        public string platform;
        
        public OsData(string platform)
        {
            this.platform = platform;
        }
    }

    public class Data
    {
        public OsData OsData { get; set; }
        public DiskLayoutData[] DiskLayoutData { get; set; }

        public Data(OsData os, DiskLayoutData[] disk)
        {
            OsData = os;
            DiskLayoutData = disk;
        }
    }
}
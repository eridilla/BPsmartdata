using System.Collections.Generic;

namespace BPSmartdata
{
    public sealed class Attribute
    {
        public Attribute(int register, string name)
        {
            this.Register = register;
            this.Name = name;
        }

        public int Register { get; set; }
        public string Name { get; set; }

        public int Current { get; set; }
        public int Worst { get; set; }
        public int Threshold { get; set; }
        public int Data { get; set; }
        public bool IsOK { get; set; }

        public bool IsEmpty
        {
            get
            {
                if (Current == 0 && Worst == 0 && Threshold == 0 && Data == 0)
                    return true;
                return false;
            }
        }
    }
    
    public class AttributeCollection :List<Attribute>
    {
        public AttributeCollection()
        {
        }

        public Attribute GetAttribute(int registerID)
        {
            foreach (var item in this)
            {
                if (item.Register == registerID)
                    return item;
            }

            return null;
        }
    }
}
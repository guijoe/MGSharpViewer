using System;

namespace MGSharp.Viewer
{
    public class PersistantNumbers
    {
        public int frame { get; set; }
        public int n1 { get; set; }
        public int n2 { get; set; }
        
        public PersistantNumbers() { }

        public PersistantNumbers(int frame, int n1, int n2)
        {
            this.frame = frame;
            this.n1 = n1;
            this.n2 = n2;
        }
    }
}

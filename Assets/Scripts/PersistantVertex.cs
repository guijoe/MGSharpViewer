using UnityEngine;


namespace MGSharp.Viewer
{
    public class PersistantVertex
    {
        public int frame { get; set; }
        public int id { get; set; }
        public int cellId { get; set; }
        public Vector3 v { get; set; }

        public PersistantVertex() { }

        public PersistantVertex(int frame, int id, int cellId, Vector3 v)
        {
            this.frame = frame;
            this.id = id;
            this.cellId = cellId;
            this.v = v;
        }

        public PersistantVertex Clone()
        {
            PersistantVertex v = new PersistantVertex();
            v.v = new Vector3(this.v.x, this.v.y, this.v.z);
            v.id = id;
            v.cellId = cellId;
            return v;
        }
    }
}

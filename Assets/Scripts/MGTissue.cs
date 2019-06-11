using UnityEngine;

namespace MGSharp.Viewer{
	public class MGTissue : MonoBehaviour {

        [HideInInspector] public int popSize;
		int[] indices;
        public bool hide = false;
        bool hideAction = true;
        public Color color;
        [HideInInspector] public MGCell[] cells;
       

		void Start(){
			
		}

		public void Hide(){
			for (int i = 0; i < popSize; i++) {
				cells[i].Hide ();
			}
		}

		public void UnHide(){
			for (int i = 0; i < popSize; i++) {
				cells[i].UnHide ();
			}
		}

        void Update()
        {
            if (hide && hideAction)
            {
                Hide();
                hideAction = false;
            }
            if(!hide && !hideAction)
            {
                UnHide();
                hideAction = true;
            }
        }
    }
}

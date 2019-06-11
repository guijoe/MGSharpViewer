using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MGSharp.Viewer
{
    public class MGCell : MonoBehaviour
    {
        //public Mesh mesh;
        public Color color;
        public Vector3[] nuclei;
        public MGParticle[] particles;

        MeshRenderer meshRenderer;

        void Start() {
            meshRenderer = GetComponent<MeshRenderer>();
			color = Color.blue;
        }

        public void Hide(){
            //meshRenderer.enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
            //Debug.Log("Hiding");
        }

        public void UnHide() {
            //meshRenderer.enabled = true;
            GetComponent<MeshRenderer>().enabled = true;

        }

        //DA82F3 //777777
    }
}
using UnityEngine;
using System.Collections;

public class ParticleSortingLayer : MonoBehaviour
{
    public string layer = "default";
    public int order = 0;

    public void Start()
    {
        // Set the sorting layer of the particle system.
        GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingLayerName = layer;
        GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingOrder = order;
    }


}

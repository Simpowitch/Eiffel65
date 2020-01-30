using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Renderer))]
public class ColorRandomizer : MonoBehaviour
{
    [SerializeField] Color[] colorChoices = null;

    Renderer r;

    private void Awake()
    {
        r = GetComponent<Renderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        AsignRandomColor();
    }


    private void AsignRandomColor()
    {
        if (colorChoices.Length == 0)
        {
            Debug.LogWarning("No colors set on " + this.gameObject.name);
            return;
        }
        r.material.SetColor("_BaseColor", colorChoices[Random.Range(0, colorChoices.Length)]);
    }
}

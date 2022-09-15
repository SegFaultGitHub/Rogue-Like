using System;
using UnityEngine;

public class TransitionMask : MonoBehaviour {
    public Func<int> OnComplete;

    [SerializeField] private bool AutoActivate;
    private SpriteMask SpriteMask;
    private Transform Camera;

    public void Start() {
        this.SpriteMask = this.GetComponentInChildren<SpriteMask>();
        this.Camera = GameObject.FindGameObjectWithTag("MainCamera").transform;

        if (this.AutoActivate) { this.Activate(); }
    }

    public void Update() {
        this.transform.position = new(this.Camera.position.x, this.Camera.position.y, this.transform.position.z);
    }

    public LTDescr Activate() {
        return LeanTween
            .value(this.gameObject, 0, 1, 0.33f)
            .setEaseInQuad()
            .setOnUpdate((float val) => {
                this.SpriteMask.alphaCutoff = val;
            });

    }
}

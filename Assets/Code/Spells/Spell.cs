using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Spell : MonoBehaviour {
    [HideInInspector] public string Layer;
    protected Rigidbody2D Rigidbody;
    protected Animator Animator;

    public void Start() {
        this.Rigidbody = this.GetComponentInChildren<Rigidbody2D>();
        this.Animator = this.GetComponentInChildren<Animator>();
        this.gameObject.layer = LayerMask.NameToLayer(this.Layer);
    }

    public abstract void CastTowards(Vector2 from, Vector2 to);
}

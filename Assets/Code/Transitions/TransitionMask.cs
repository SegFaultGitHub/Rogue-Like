using System;
using System.Collections.Generic;
using UnityEngine;

public class TransitionMask : MonoBehaviour {
    [Serializable]
    private struct MaskSprite {
        public Direction Direction;
        public Sprite Sprite;
    }

    public Func<int> OnComplete;
    private SpriteMask SpriteMask;
    private Transform Camera;
    [SerializeField] private List<MaskSprite> Sprites;

    public void Awake() {
        this.SpriteMask = this.GetComponentInChildren<SpriteMask>();
        this.Camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
    }

    public void Update() {
        this.transform.position = new(this.Camera.position.x, this.Camera.position.y, this.transform.position.z);
    }

    public LTDescr Activate(Direction direction) {
        this.SpriteMask.sprite = this.Sprites.Find(maskSprite => maskSprite.Direction == direction).Sprite;
        return LeanTween
            .value(this.gameObject, 0, 1, 0.33f)
            .setEaseInQuad()
            .setOnUpdate((float val) => {
                this.SpriteMask.alphaCutoff = val;
            });

    }
}

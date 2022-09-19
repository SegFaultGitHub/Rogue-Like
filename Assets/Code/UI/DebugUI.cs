using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour {
    private Canvas Canvas;

    private TMP_Text SeedText;
    private TMP_Text MapPositionText;

    public void Start() {
        this.Canvas = this.gameObject.GetComponentInChildren<Canvas>();
        this.Canvas.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        this.Canvas.sortingLayerName = "UI";

        this.SeedText = this.Canvas.transform.Find("Seed").GetComponent<TMP_Text>();
        this.MapPositionText = this.Canvas.transform.Find("Map Position").GetComponent<TMP_Text>();
    }

    public void SetSeed(int seed) {
        this.SeedText.text = seed.ToString();
    }

    public void SetMapPosition(Vector2Int position) {
        this.MapPositionText.text = "[" + position.x + "," + position.y + "]";
    }
}

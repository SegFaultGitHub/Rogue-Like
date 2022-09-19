using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour {
    private Player Player;
    private HUDInputs HUDInputs;

    // GameObjects
    private GameObject HPBox;
    private GameObject Hearts;
    private GameObject MapBox;
    private GameObject MapLayout;
    private GameObject CurrentMapFrame;

    [SerializeField] private GameObject Heart;
    [SerializeField] private Sprite FullHeart, HalfHeart, EmptyHeart;

    [Header("POSITIONING")]
    [SerializeField] private int HeartXOffset;
    [SerializeField] private int HeartYOffset;
    [SerializeField] private int HeartWidth;
    [SerializeField] private int HeartHeight;
    [SerializeField] private Vector2 MapOpenedFrameSize;
    [SerializeField] private Vector2 MapClosedFrameSize;

    private bool MapOpened = false;
    private bool MapToggling = false;
    private Vector2 RoomPosition;
    private Map._MapLayoutData MapLayoutData;

    #region Input
    private void OnEnable() {
        this.HUDInputs = new HUDInputs();
        this.HUDInputs.Actions.Enable();

        this.HUDInputs.Actions.ToggleMap.started += this.ToggleMapInput;
    }

    private void OnDisable() {
        this.HUDInputs.Actions.ToggleMap.started -= this.ToggleMapInput;

        this.HUDInputs.Actions.Disable();
    }

    private void ToggleMapInput(InputAction.CallbackContext context) {
        this.ToggleMap();
    }
    #endregion

    public void Start() {
        this.Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        this.HPBox = this.transform.Find("HP").gameObject;
        this.Hearts = this.HPBox.transform.Find("Hearts").gameObject;
        this.MapLayout = this.transform.Find("Map/Layout/MaskedLayout").gameObject;
        this.MapBox = this.transform.Find("Map").gameObject;
        this.CurrentMapFrame = this.transform.Find("Map/CurrentMapFrame").gameObject;

        this.MapClosedFrameSize = this.MapBox.GetComponent<RectTransform>().sizeDelta;
    }

    public void Update() {
        this.UpdateHP();
    }

    private void UpdateHP() {
        if (this.Player.HP.Max <= 0) {
            Debug.Log("Player Max HP == 0");
            return;
        }

        this.HPBox.GetComponent<RectTransform>().sizeDelta = new(
            this.GetHeartCount() * this.HeartWidth + this.HeartWidth + 1,
            this.HPBox.GetComponent<RectTransform>().sizeDelta.y
        );

        for (int i = this.Hearts.transform.childCount; i < this.GetHeartCount(); i++) {
            GameObject currentHeart = Instantiate(this.Heart, this.Hearts.transform);
            currentHeart.GetComponent<RectTransform>().anchoredPosition = new(this.HeartXOffset + i * this.HeartWidth, this.HeartYOffset);
        }

        for (int i = this.Hearts.transform.childCount; i > this.GetHeartCount(); i--) {
            Destroy(this.Hearts.transform.GetChild(i - 1).gameObject);
        }

        for (int i = 0; i < this.GetHeartCount(); i++) {
            Image currentHeart = this.Hearts.transform.GetChild(i).gameObject.GetComponent<Image>();
            if (this.Player.HP.Current < i * 2 + 1) {
                currentHeart.sprite = this.EmptyHeart;
            } else if (this.Player.HP.Current > i * 2 + 1) {
                currentHeart.sprite = this.FullHeart;
            } else {
                currentHeart.sprite = this.HalfHeart;
            }
        }
    }

    public void SetMapSprite(Sprite sprite) {
        this.MapLayout.GetComponent<Image>().sprite = sprite;
        this.MapLayout.GetComponent<Image>().SetNativeSize();
    }

    public void MoveMapLayout(Vector2Int position, Map._MapLayoutData mapLayoutData) {
        this.MapLayoutData = mapLayoutData;
        this.RoomPosition = position;

        this.MapLayout.GetComponent<RectTransform>().anchoredPosition = this.GetMapLayoutPosition();
        this.CurrentMapFrame.GetComponent<RectTransform>().sizeDelta = this.MapLayoutData.RoomSize + new Vector2(4, 4);
    }

    private void ToggleMap() {
        if (this.MapToggling) { return; }
        this.MapToggling = true;
        this.MapOpened = !this.MapOpened;

        Vector2 boxSize = this.MapOpened ? this.MapOpenedFrameSize : this.MapClosedFrameSize;
        RectTransform mapBoxTransform = this.MapBox.GetComponent<RectTransform>();
        RectTransform mapLayoutPosition = this.MapLayout.GetComponent<RectTransform>();
        Vector2 currentSize = mapBoxTransform.sizeDelta;


        LeanTween.value(currentSize.x, boxSize.x, 0.1f)
            .setEaseInExpo()
            .setOnUpdate((float val) => {
                mapBoxTransform.sizeDelta = new(val, mapBoxTransform.sizeDelta.y);
                mapLayoutPosition.anchoredPosition = this.GetMapLayoutPosition();
            }).setOnStart(() => {
                LeanTween.value(currentSize.y, boxSize.y, 0.1f)
                    .setEaseInExpo()
                    .setOnUpdate((float val) => {
                        mapBoxTransform.sizeDelta = new(mapBoxTransform.sizeDelta.x, val);
                        mapLayoutPosition.anchoredPosition = this.GetMapLayoutPosition();
                    }).setOnComplete(() => { this.MapToggling = false; });
            });
    }

    private Vector2 GetMapLayoutPosition() {
        Rect boxSize = this.MapLayout.transform.parent.gameObject.GetComponent<RectTransform>().rect;
        Vector2 topLeftPosition = new Vector2(boxSize.width - this.MapLayoutData.RoomSize.x, -(boxSize.height - this.MapLayoutData.RoomSize.y)) / 2f;
        Vector2 positionOffset = new(
            -(this.RoomPosition.x - this.MapLayoutData.MinI) * this.MapLayoutData.RoomSize.x,
            (this.RoomPosition.y - this.MapLayoutData.MinJ) * this.MapLayoutData.RoomSize.y
        );
        return topLeftPosition + positionOffset;
    }

    private int GetHeartCount() {
        return (int) Mathf.Ceil(this.Player.HP.Max / 2f);
    }
}

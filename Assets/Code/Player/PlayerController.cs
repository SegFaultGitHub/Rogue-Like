using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    private enum PlayerDirection {
        Left, Right
    }

    [SerializeField] private float MovementSpeed;
    private Vector2 MovementDirection;

    private Rigidbody2D Rigidbody;
    private PlayerInputs PlayerInputs;
    private Animator Animator;

    [SerializeField] private GameObject Sprites;

    #region Input
    private void OnEnable() {
        this.PlayerInputs = new PlayerInputs();
        this.PlayerInputs.Actions.Enable();

        this.PlayerInputs.Actions.Move.performed += this.MoveInput;
        this.PlayerInputs.Actions.Move.canceled += this.MoveInput;
    }

    private void OnDisable() {
        this.PlayerInputs.Actions.Move.performed -= this.MoveInput;
        this.PlayerInputs.Actions.Move.canceled -= this.MoveInput;

        this.PlayerInputs.Actions.Disable();
    }

    private void MoveInput(InputAction.CallbackContext context) {
        this.Move(context.ReadValue<Vector2>());
    }
    #endregion

    public void Start() {
        this.Animator = this.GetComponent<Animator>();
        this.Rigidbody = this.GetComponentInChildren<Rigidbody2D>();
    }

    public void FixedUpdate() {
        this.Rigidbody.velocity = this.MovementDirection * this.MovementSpeed;
    }

    private void Move(Vector2 direction) {
        this.MovementDirection = direction;

        if (this.MovementDirection.x > 0) {
            Vector3 scale = this.Sprites.transform.localScale;
            this.Sprites.transform.localScale = new(1, scale.y, scale.z);
        } else if (this.MovementDirection.x < 0) {
            Vector3 scale = this.Sprites.transform.localScale;
            this.Sprites.transform.localScale = new(-1, scale.y, scale.z);
        }

        this.Animator.SetBool("IsMoving", direction != Vector2.zero);
    }
}

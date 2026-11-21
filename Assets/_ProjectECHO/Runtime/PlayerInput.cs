using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectEcho
{
    public sealed class PlayerInput : MonoBehaviour
    {
        private InputActionMap gameplay;
        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Crouch { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Hide { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Ability { get; private set; }
        public InputAction Scanner { get; private set; }
        public InputAction UseItem { get; private set; }
        public InputAction SelectEmp { get; private set; }
        public InputAction SelectStun { get; private set; }
        public InputAction Pause { get; private set; }

        private void Awake()
        {
            gameplay = new InputActionMap("ECHO Gameplay");
            Move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Look = gameplay.AddAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            Sprint = Button("Sprint", "leftShift");
            Sprint.AddBinding("<Keyboard>/rightShift");
            Crouch = Button("Crouch", "leftCtrl");
            Crouch.AddBinding("<Keyboard>/rightCtrl");
            Jump = Button("Jump", "space");
            Hide = Button("Hide", "c");
            Interact = Button("Interact", "f");
            Ability = Button("Ability", "e");
            Scanner = Button("Scanner", "t");
            UseItem = Button("Use item", "g");
            UseItem.AddBinding("<Keyboard>/q");
            SelectEmp = Button("Select EMP", "1");
            SelectStun = Button("Select Stun", "2");
            Pause = Button("Pause", "escape");
        }

        private InputAction Button(string actionName, string key) =>
            gameplay.AddAction(actionName, InputActionType.Button, $"<Keyboard>/{key}");

        private void OnEnable() => gameplay.Enable();
        private void OnDisable() => gameplay.Disable();
        private void OnDestroy() => gameplay.Dispose();
    }
}

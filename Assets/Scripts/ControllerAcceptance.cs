using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace RestaurantCity {
    // Run the built player with --controller-test. SmokeMode keeps the real save untouched.
    public class ControllerAcceptance : MonoBehaviour {
        public CityGame Game;
        IEnumerator Start() {
            yield return null;
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();
            yield return Press(first, GamepadButton.Start);
            Check(Game.Player.AssignedGamepad == first, "first Start assigns Player 1");
            Check(Game.CoOp.SecondPlayer == null, "first Start does not split screen");
            yield return Press(second, GamepadButton.Start);
            Check(Game.CoOp.SecondPlayer && Game.CoOp.SecondPlayer.AssignedGamepad == second, "second Start joins Player 2");
            yield return Press(first, GamepadButton.South);
            Check(!Game.Paused && Game.Started, "Player 1 A starts game");
            Game.State.StandBuilt = true;
            Game.Restaurant.ShowPanel("Phone");
            yield return null;
            var module = EventSystem.current ? EventSystem.current.GetComponent<InputSystemUIInputModule>() : null;
            Check(module && module.actionsAsset && EventSystem.current.currentSelectedGameObject, "management UI has gamepad focus");
            bool firstInUi = false, secondInUi = false;
            if (module && module.actionsAsset && module.actionsAsset.devices.HasValue)
                foreach (var device in module.actionsAsset.devices.Value) { if (device == first) firstInUi = true; if (device == second) secondInUi = true; }
            Check(firstInUi && !secondInUi, "shared UI belongs to Player 1 controller");
            yield return Press(second, GamepadButton.Start);
            Check(!Game.Paused && Game.Restaurant.PanelOpen, "Player 2 Start cannot hide shared menu behind pause");
            yield return Press(first, GamepadButton.South);
            Check(Game.Restaurant.Panel == "Staff", "Player 1 A activates selected phone action");
            yield return Press(first, GamepadButton.East);
            Check(!Game.Restaurant.PanelOpen, "Player 1 B closes management UI");
            float yaw = Game.Player.transform.eulerAngles.y;
            yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(first, new GamepadState { rightStick = Vector2.right });
            for (int frame = 0; frame < 10; frame++) yield return null;
            InputSystem.QueueStateEvent(first, new GamepadState());
            yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw, Game.Player.transform.eulerAngles.y)) > 1f, "right stick turns Player 1 in gameplay");
            var combat = PlayerCombat.Of(Game.Player);
            yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(first, new GamepadState { rightTrigger = 1f });
            yield return new WaitForSecondsRealtime(.08f);
            Check(combat.Charge01 > 0, "right trigger charges punch");
            InputSystem.QueueStateEvent(first, new GamepadState());
            yield return null;
            Check(combat.Charge01 == 0, "releasing right trigger throws punch");
            Game.Player.LookAt(Game.Player.View.transform.position + Vector3.up * 5);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(first, new GamepadState().WithButton(GamepadButton.RightShoulder));
            yield return new WaitForSecondsRealtime(.08f);
            Check(combat.Charge01 == 0, "right shoulder does not start a punch away from grill");
            InputSystem.QueueStateEvent(first, new GamepadState());
            yield return null;
            Debug.Log("CONTROLLER_RUNTIME_PASS first=" + first.deviceId + " second=" + second.deviceId);
            InputSystem.RemoveDevice(second); InputSystem.RemoveDevice(first);
            Application.Quit(0);
        }
        static IEnumerator Press(Gamepad pad, GamepadButton button) {
            yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
        }
        static void Check(bool ok, string description) {
            if (ok) return;
            Debug.LogError("CONTROLLER_RUNTIME_FAIL " + description);
            Application.Quit(2);
            throw new System.Exception(description);
        }
    }
}

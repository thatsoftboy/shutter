using UnityEngine;

namespace HorrorEngine
{
    public enum ControlScheme
    {
        None,
        KeyboardAndMouse,
        Gamepad
    }

    public interface IPlayerInput
    {
        Vector2 GetPrimaryAxis();
        Vector2 GetSecondaryAxis();
        bool IsRunHeld();
        bool IsAimingHeld();
        bool IsAttackDown();
        bool IsAttackUp();
        bool IsInteractingDown();
        bool IsReloadDown();

        bool IsTurn180Down();

        bool IsChangeAimTargetDown();

        void Flush();

        ControlScheme GetControlScheme();
    }
}
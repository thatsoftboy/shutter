using UnityEngine;

namespace HorrorEngine
{
    public class FocusVirtualCameraOnCharacter : MonoBehaviour
    {
        public bool LookAt = true;
        public bool Follow = true;

        // --------------------------------------------------------------------

        void Start()
        {
            if (GameManager.Instance.Player)
            {
                SetPlayer(GameManager.Instance.Player.transform);
            }
            else
            {
                GameManager.Instance.OnPlayerRegistered.AddListener(OnPlayerRegistered);
            }
        }

        // --------------------------------------------------------------------

        void SetPlayer(Transform playerT)
        {
            var cam = GetComponent<Cinemachine.CinemachineVirtualCamera>();
            if (LookAt)
                cam.LookAt = playerT;
            if (Follow)
                cam.Follow = playerT;
        }

        // --------------------------------------------------------------------

        void OnPlayerRegistered(PlayerActor player)
        {
            SetPlayer(player.transform);
        }

        // --------------------------------------------------------------------

        private void OnDestroy()
        {
            if (GameManager.Exists)
                GameManager.Instance.OnPlayerRegistered.RemoveListener(OnPlayerRegistered);
        }
    }

}
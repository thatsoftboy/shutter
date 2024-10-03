using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorEngine
{
    public static class GameSaveUtils
    {
        public class SaveUtilsBehaviour : MonoBehaviour { }

        private static GameSaveData m_LoadedData;
        private static SaveUtilsBehaviour m_Behaviour;

        // --------------------------------------------------------------------

        private static SaveUtilsBehaviour Behaviour
        {
            get
            {
                if (!m_Behaviour)
                {
                    GameObject go = new GameObject("SaveUtils");
                    m_Behaviour = go.AddComponent<SaveUtilsBehaviour>();
                }
                return m_Behaviour;
            }
        }

        // --------------------------------------------------------------------

        public static int GetLastSavedSlot()
        {
            SaveDataManager<GameSaveData> saveMgr = SaveDataManager<GameSaveData>.Instance;
            int[] slots = saveMgr.GetExistingSlots();
            DateTime lastTime = DateTime.MinValue;
            int lastIndex = -1;
            foreach (var slotIndex in slots)
            {
                SaveDataManager<GameSaveData>.SaveData saveData = saveMgr.GetSaveData(slotIndex);
                DateTime time = string.IsNullOrEmpty(saveData.GameData.Date) ? DateTime.MinValue : DateTime.Parse(saveData.GameData.Date);
                if (lastIndex == -1 || time > lastTime)
                {
                    lastIndex = slotIndex;
                    lastTime = time;
                }
            }
            return lastIndex;
        }

        // --------------------------------------------------------------------


        public static void LoadSlot(int slot)
        {
            m_LoadedData = SaveDataManager<GameSaveData>.Instance.LoadSlot(slot);
            SceneManager.LoadScene(m_LoadedData.SceneName);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // --------------------------------------------------------------------

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Behaviour.InvokeActionNextFrame(() => // Give time to HECore to initialize everything
            {
                GameManager.Instance.Clear(); // Needs to happen before ResetComponents or some components will use old data
                GameManager.Instance.Player.GetComponent<GameObjectReset>().ResetComponents();
                GameManager.Instance.SetFromSavedData(m_LoadedData);
                
                ObjectStateManager.Instance.InstantiateSpawned(scene, GameManager.Instance.SpawnableDatabase);
                ObjectStateManager.Instance.ApplyStates();

                GameManager.Instance.StartGame();
            });
        }
    }
}
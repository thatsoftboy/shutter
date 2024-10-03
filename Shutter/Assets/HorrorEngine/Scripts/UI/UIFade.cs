using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorEngine
{
    public class UIFade : MonoBehaviour
    {
        [SerializeField] private Image m_Fade;

        // --------------------------------------------------------------------

        private void OnEnable()
        {
            m_Fade.color = new Color(0f, 0f, 0f, 1f);
            Fade(1, 0, 1);
        }

        public Coroutine Fade(float from, float to, float duration)
        {
            return StartCoroutine(FadeRoutine(from, to, duration));
        }

        // --------------------------------------------------------------------

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float timePassed = 0f;
            float t = 0f;
            Color c = new Color(0f, 0f, 0f, 0f);
            while (t < 1f)
            {
                timePassed += Time.unscaledDeltaTime;
                t = Mathf.Min(timePassed / duration, 1f);
                c.a = Mathf.Lerp(from, to, t);
                m_Fade.color = c;
                yield return null;
            }
            c.a = to;
            m_Fade.color = c;
        }

        // --------------------------------------------------------------------

        public void Set(float opacity)
        {
            Color c = new Color(0f, 0f, 0f, 0f);
            c.a = opacity;
            m_Fade.color = c;
        }

    }
}
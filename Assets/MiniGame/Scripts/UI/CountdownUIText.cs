using UnityEngine;
using TMPro;
using System.Collections;

namespace MiniGame
{
    public class CountdownUIText : MonoBehaviour
    {
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI bonusText;

        private CountdownController _timeController;

        void Start()
        {
            _timeController = GameObject.FindFirstObjectByType<CountdownController>();

            if (_timeController == null)
            {
                Debug.LogError("Không tìm thấy CountdownController trong Scene.");
                enabled = false;
                return;
            }

            // Ẩn timer lúc bắt đầu
            timeText.gameObject.SetActive(false);

            // Ẩn text +time
            if (bonusText != null)
                bonusText.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_timeController.IsCounting)
            {
                if (!timeText.gameObject.activeSelf)
                {
                    ShowTimer();
                }

                UpdateTimeText(_timeController.timeRemaining);
            }
        }

        void ShowTimer()
        {
            timeText.gameObject.SetActive(true);

            timeText.canvasRenderer.SetAlpha(0f);
            timeText.CrossFadeAlpha(1f, 1f, false);
        }

        void UpdateTimeText(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            int milliseconds = Mathf.FloorToInt((time * 100) % 100);

            timeText.text = $"{minutes}'{seconds:00}\"{milliseconds:00}";
        }

        public void ShowBonusTime(float timeAdded)
        {
            if (bonusText == null) return;

            StopAllCoroutines();
            StartCoroutine(FadeBonus(timeAdded));
        }

        IEnumerator FadeBonus(float timeAdded)
        {
            bonusText.text = "+" + timeAdded;
            bonusText.gameObject.SetActive(true);

            bonusText.canvasRenderer.SetAlpha(0f);
            bonusText.CrossFadeAlpha(1f, 0.2f, false);

            float timer = 0f;
            float duration = 1f;

            Vector3 startPos = bonusText.transform.localPosition;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                bonusText.transform.localPosition =
                    startPos + Vector3.up * 30f * timer;

                yield return null;
            }

            bonusText.CrossFadeAlpha(0f, 0.5f, false);

            yield return new WaitForSeconds(0.5f);

            bonusText.gameObject.SetActive(false);
            bonusText.transform.localPosition = startPos;
        }
    }
}
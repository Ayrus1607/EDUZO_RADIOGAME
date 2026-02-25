using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Eduzo.Games.Radio.Data;
using Eduzo.Games.Radio.UI;

namespace Eduzo.Games.Radio
{
    public class RadioUserResponse
    {
        public string question;
        public string correctAnswer;
        public string wrongAnswer;
        public string userChoice;
        public bool isCorrect;
        public float timeTaken;
    }

    public class RadioGameManager : MonoBehaviour
    {
        public static event System.Action<List<RadioUserResponse>> OnRadioGameCompleted;
        public static event System.Action<float> OnRadioScoreCalculated;

        [Header("Screens")]
        public GameObject modeSelectionScreen;
        public GameObject gameScreen;
        public GameObject scoreScreen;
        public GameObject practiceCompleteScreen;
        public GameObject gameOverScreen;
        public GameObject optionsMenuScreen;
        public GameObject formScreen;

        [Header("Game UI Elements")]
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI option1Text;
        public TextMeshProUGUI option2Text;
        public TextMeshProUGUI timerText;
        public TMP_InputField timerInput;
        public Transform timerIcon;

        public Image[] heartIcons;
        public Sprite heartOnSprite;
        public Sprite heartOffSprite;

        public TextMeshProUGUI scoreSummaryText;

        [Header("Audio")]
        public AudioSource correctSound;
        public AudioSource wrongSound;
        public AudioSource gameOverSound;
        public AudioSource normalTickSound;
        public AudioSource fastTickSound;
        public AudioSource buttonClickSound;

        public enum GameMode { None, Practice, Test }
        public GameMode currentMode = GameMode.None;

        [Header("VFX")]
        public GameObject correctVFXPrefab;
        public GameObject secondaryCorrectVFXPrefab;
        public GameObject[] tertiaryCorrectVFXPrefabs;

        public GameObject wrongVFXPrefab;
        public GameObject secondaryWrongVFXPrefab;
        public GameObject[] tertiaryWrongVFXPrefabs;

        public Transform vfxSpawnPoint;
        public Transform secondaryVFXSpawnPoint;

        [Range(0.1f, 10f)]
        public float vfxScale = 1f;
        [Range(0.1f, 10f)]
        public float tertiaryVfxScale = 1f;

        private List<RadioQuestionData> questionBank = new List<RadioQuestionData>();
        private int currentQuestionIndex = 0;
        private float timer = 60f;
        private int lives = 3;
        private bool isPlaying = false;
        private bool isPaused = false;
        private List<RadioUserResponse> responseHistory = new List<RadioUserResponse>();
        private float activeTimeTotal = 0f;
        private float timeOnCurrentQuestion = 0f;

        private Vector3 originalTimerScale = Vector3.one;
        private bool isFastTicking = false;

        private void Awake()
        {
            StopTimerAudio();
            if (timerIcon != null) originalTimerScale = timerIcon.localScale;

            if (gameScreen != null)
            {
                gameScreen.SetActive(true);
                CanvasGroup cg = gameScreen.GetComponent<CanvasGroup>();
                if (cg == null) cg = gameScreen.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        private void ToggleGameScreen(bool show)
        {
            if (gameScreen == null) return;
            CanvasGroup cg = gameScreen.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameScreen.AddComponent<CanvasGroup>();

            if (show)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        private void OnEnable() { RadioFormController.OnRadioFormSubmitted += OnFormSubmitted; }
        private void OnDisable() { RadioFormController.OnRadioFormSubmitted -= OnFormSubmitted; }

        private void OnFormSubmitted(List<RadioQuestionData> questions)
        {
            questionBank = questions;
            if (formScreen != null) formScreen.SetActive(false);
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(true);
        }

        private void PlayClickSound()
        {
            if (buttonClickSound != null) buttonClickSound.Play();
        }

        public void StartPracticeMode()
        {
            PlayClickSound();
            currentMode = GameMode.Practice;

            if (timerText != null) timerText.gameObject.SetActive(false);

            StopTimerAudio();
            if (timerIcon != null) timerIcon.localScale = originalTimerScale;

            if (heartIcons != null)
            {
                for (int i = 0; i < heartIcons.Length; i++)
                {
                    if (heartIcons[i] != null) heartIcons[i].gameObject.SetActive(false);
                }
            }

            StartGame();
        }

        public void StartTestMode()
        {
            PlayClickSound();
            currentMode = GameMode.Test;
            lives = 3;

            timer = 60f;
            if (timerInput != null && !string.IsNullOrEmpty(timerInput.text))
            {
                if (float.TryParse(timerInput.text, out float customTime) && customTime > 0)
                {
                    timer = customTime;
                }
            }

            if (timerText != null) timerText.gameObject.SetActive(true);

            UpdateLivesUI();

            isFastTicking = false;
            if (normalTickSound != null) normalTickSound.Play();
            if (fastTickSound != null) fastTickSound.Stop();

            StartGame();
        }

        private void StartGame()
        {
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            ToggleGameScreen(true);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);

            currentQuestionIndex = 0;
            isPlaying = true;
            isPaused = false;
            responseHistory.Clear();
            activeTimeTotal = 0f;

            StartCoroutine(DelayedLoadQuestion());
        }

        private IEnumerator DelayedLoadQuestion()
        {
            yield return null;
            LoadNextQuestion();
        }

        private void Update()
        {
            if (!isPlaying || isPaused) return;

            activeTimeTotal += Time.deltaTime;
            timeOnCurrentQuestion += Time.deltaTime;

            if (currentMode == GameMode.Test)
            {
                timer -= Time.deltaTime;

                if (timer <= 10f && !isFastTicking)
                {
                    isFastTicking = true;
                    if (normalTickSound != null) normalTickSound.Stop();
                    if (fastTickSound != null) fastTickSound.Play();
                }

                if (timerIcon != null)
                {
                    float pulseSpeed = isFastTicking ? 15f : 5f;
                    float scaleAmount = isFastTicking ? 0.2f : 0.05f;
                    float pulse = 1f + Mathf.Abs(Mathf.Sin(activeTimeTotal * pulseSpeed)) * scaleAmount;
                    timerIcon.localScale = originalTimerScale * pulse;
                }

                int minutes = Mathf.FloorToInt(timer / 60F);
                int seconds = Mathf.FloorToInt(timer - minutes * 60);

                if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

                if (timer <= 0)
                {
                    timer = 0;
                    if (gameOverSound != null) gameOverSound.Play();
                    GameOver("Time's Up!", true);
                }
            }
        }

        private void LoadNextQuestion()
        {
            if (currentQuestionIndex < questionBank.Count)
            {
                timeOnCurrentQuestion = 0f;
                RadioQuestionData q = questionBank[currentQuestionIndex];

                string safeQuestion = !string.IsNullOrEmpty(q.questionText) ? q.questionText.Replace("\r", "").Replace("\n", "").Trim() : "Empty";
                string safeCorrect = !string.IsNullOrEmpty(q.correctAnswer) ? q.correctAnswer.Replace("\r", "").Replace("\n", "").Trim() : "Empty";
                string safeWrong = !string.IsNullOrEmpty(q.wrongAnswer) ? q.wrongAnswer.Replace("\r", "").Replace("\n", "").Trim() : "Empty";

                if (questionText != null) questionText.text = safeQuestion;

                if (Random.value > 0.5f)
                {
                    if (option1Text != null) option1Text.text = safeCorrect;
                    if (option2Text != null) option2Text.text = safeWrong;
                }
                else
                {
                    if (option1Text != null) option1Text.text = safeWrong;
                    if (option2Text != null) option2Text.text = safeCorrect;
                }
            }
            else
            {
                GameOver("You finished all the questions!", false);
            }
        }

        public void CheckAnswer1() { if (option1Text != null) VerifyAnswer(option1Text.text); }
        public void CheckAnswer2() { if (option2Text != null) VerifyAnswer(option2Text.text); }

        private void VerifyAnswer(string selectedAnswer)
        {
            if (!isPlaying || isPaused) return;

            RadioQuestionData q = questionBank[currentQuestionIndex];
            bool correct = (selectedAnswer == q.correctAnswer);

            responseHistory.Add(new RadioUserResponse
            {
                question = q.questionText,
                correctAnswer = q.correctAnswer,
                wrongAnswer = q.wrongAnswer,
                userChoice = selectedAnswer,
                isCorrect = correct,
                timeTaken = timeOnCurrentQuestion
            });

            if (correct)
            {
                if (correctSound != null) correctSound.Play();

                SpawnVFX(correctVFXPrefab, vfxSpawnPoint, vfxScale);
                SpawnVFX(secondaryCorrectVFXPrefab, vfxSpawnPoint, vfxScale);

                if (tertiaryCorrectVFXPrefabs != null && tertiaryCorrectVFXPrefabs.Length > 0)
                {
                    int randomIndex = Random.Range(0, tertiaryCorrectVFXPrefabs.Length);
                    SpawnVFX(tertiaryCorrectVFXPrefabs[randomIndex], secondaryVFXSpawnPoint, tertiaryVfxScale);
                }

                currentQuestionIndex++;
            }
            else
            {
                if (wrongSound != null) wrongSound.Play();

                SpawnVFX(wrongVFXPrefab, vfxSpawnPoint, vfxScale);
                SpawnVFX(secondaryWrongVFXPrefab, vfxSpawnPoint, vfxScale);

                if (tertiaryWrongVFXPrefabs != null && tertiaryWrongVFXPrefabs.Length > 0)
                {
                    int randomIndex = Random.Range(0, tertiaryWrongVFXPrefabs.Length);
                    SpawnVFX(tertiaryWrongVFXPrefabs[randomIndex], secondaryVFXSpawnPoint, tertiaryVfxScale);
                }

                if (currentMode == GameMode.Test)
                {
                    lives--;
                    if (lives >= 0 && heartIcons != null && lives < heartIcons.Length && heartIcons[lives] != null)
                    {
                        StartCoroutine(ShakeAndSwapHeart(heartIcons[lives]));
                    }

                    currentQuestionIndex++;
                    if (lives <= 0)
                    {
                        if (gameOverSound != null) gameOverSound.Play();
                        GameOver("Game Over! Out of lives.", true);
                        return;
                    }
                }
            }
            LoadNextQuestion();
        }

        private void SpawnVFX(GameObject prefab, Transform spawnLocation, float specificScale)
        {
            if (prefab != null && spawnLocation != null)
            {
                GameObject vfx = Instantiate(prefab, spawnLocation.position, Quaternion.identity);
                ParticleSystemRenderer[] renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>();
                foreach (ParticleSystemRenderer r in renderers) { r.sortingOrder = 100; }

                vfx.transform.localScale = new Vector3(specificScale, specificScale, specificScale);
                Destroy(vfx, 2f);
            }
        }

        private void UpdateLivesUI()
        {
            if (heartIcons == null || heartIcons.Length == 0) return;

            for (int i = 0; i < heartIcons.Length; i++)
            {
                Image heart = heartIcons[i];

                if (heart == null) continue;

                heart.gameObject.SetActive(currentMode == GameMode.Test);

                if (heartOnSprite != null && heartOffSprite != null)
                {
                    heart.sprite = (i < lives) ? heartOnSprite : heartOffSprite;
                }

                heart.color = Color.white;
            }
        }

        private IEnumerator ShakeAndSwapHeart(Image heart)
        {
            if (heart == null) yield break;

            Vector3 originalPos = heart.transform.localPosition;
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * 6f;
                float y = Random.Range(-1f, 1f) * 6f;
                heart.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            heart.transform.localPosition = originalPos;
            heart.sprite = heartOffSprite;
        }

        private void StopTimerAudio()
        {
            if (normalTickSound != null) normalTickSound.Stop();
            if (fastTickSound != null) fastTickSound.Stop();
        }

        private void GameOver(string message, bool isLoss)
        {
            isPlaying = false;

            StopTimerAudio();
            if (timerIcon != null) timerIcon.localScale = originalTimerScale;

            if (currentMode == GameMode.Test)
            {
                if (isLoss && gameOverScreen != null)
                {
                    StartCoroutine(ShowGameOverSequence());
                }
                else
                {
                    Invoke("ShowScoreScreen", 2f);
                }
            }
            else if (currentMode == GameMode.Practice)
            {
                Invoke("ShowPracticeCompleteScreen", 2f);
            }
        }

        private IEnumerator ShowGameOverSequence()
        {
            yield return new WaitForSeconds(1.5f);
            ToggleGameScreen(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(true);

            yield return new WaitForSeconds(3f);

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            ShowScoreScreen();
        }

        private void ShowScoreScreen()
        {
            ToggleGameScreen(false);
            if (scoreScreen != null) scoreScreen.SetActive(true);
            GenerateScoreReport();
        }

        private void ShowPracticeCompleteScreen()
        {
            ToggleGameScreen(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(true);
        }

        private void GenerateScoreReport()
        {
            int total = responseHistory.Count;
            int correct = 0;
            foreach (var r in responseHistory) { if (r.isCorrect) correct++; }
            float percentage = total > 0 ? ((float)correct / total) * 100f : 0f;

            OnRadioScoreCalculated?.Invoke(percentage);
            OnRadioGameCompleted?.Invoke(responseHistory);

            string report = $"======== GAME SCORE SUMMARY ========\nScore: {Mathf.RoundToInt(percentage)}%\nActive Time: {activeTimeTotal:F2}s\nIdle Time: 0s\n\nTotal Responses: {total}\nCorrect: {correct}\nWrong: {total - correct}\n\n======== QUESTION BREAKDOWN ========\n\n";
            for (int i = 0; i < responseHistory.Count; i++)
            {
                var r = responseHistory[i];
                report += $"Q{i + 1}: {r.question}\nResult: {(r.isCorrect ? "CORRECT" : "INCORRECT")}\n\n";
            }

            if (scoreSummaryText != null) scoreSummaryText.text = report;
        }

        public void ReplayGame()
        {
            PlayClickSound();
            if (scoreScreen != null) scoreScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);

            if (currentMode == GameMode.Practice) StartPracticeMode();
            else StartTestMode();
        }

        public void OpenOptionsMenu()
        {
            PlayClickSound();
            if (!isPlaying) return;
            isPaused = true;

            if (normalTickSound != null && normalTickSound.isPlaying) normalTickSound.Pause();
            if (fastTickSound != null && fastTickSound.isPlaying) fastTickSound.Pause();

            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(true);
        }

        public void ResumeGame()
        {
            PlayClickSound();
            isPaused = false;

            if (currentMode == GameMode.Test)
            {
                if (isFastTicking) { if (fastTickSound != null) fastTickSound.UnPause(); }
                else { if (normalTickSound != null) normalTickSound.UnPause(); }
            }

            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
        }

        public void GoToHome()
        {
            PlayClickSound();
            isPlaying = false;
            isPaused = false;

            StopTimerAudio();
            if (timerIcon != null) timerIcon.localScale = originalTimerScale;

            if (scoreScreen != null) scoreScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
            ToggleGameScreen(false);

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);

            if (timerInput != null) timerInput.text = "";

            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(true);
        }

        public void GoToFormScreen()
        {
            PlayClickSound();
            isPlaying = false;
            isPaused = false;

            StopTimerAudio();
            if (timerIcon != null) timerIcon.localScale = originalTimerScale;

            if (scoreScreen != null) scoreScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
            ToggleGameScreen(false);

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);

            questionBank.Clear();

            if (formScreen != null) formScreen.SetActive(true);
        }

        public void QuitGame()
        {
            PlayClickSound();
            Debug.Log("Quitting Game...");
            Application.Quit();
        }
    }
}
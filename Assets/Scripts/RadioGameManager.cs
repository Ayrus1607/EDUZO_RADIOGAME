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
        public GameObject playerDataScreen;
        public GameObject gameScreen;
        public GameObject scoreScreen;
        public GameObject practiceCompleteScreen;
        public GameObject gameOverScreen;
        public GameObject optionsMenuScreen;
        public GameObject formScreen;

        [Header("Game UI Elements")]
        public TMP_InputField playerNameInput;
        public TextMeshProUGUI finalPlayerNameText;
        public TMP_InputField timerInput;

        public GameObject timerContainer;
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI option1Text;
        public TextMeshProUGUI option2Text;
        public TextMeshProUGUI timerText;
        public Transform timerIcon;

        // Array left here purely so the Inspector doesn't break
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
        [Header("Transition Settings")]
        [Tooltip("How long to wait before starting the fade (seconds)")]
        public float delayBeforeFade = 1.5f;
        [Tooltip("How fast the screen fades out and back in")]
        public float fadeSpeed = 3f;
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
        private bool isTransitioning = false; // Prevents clicking during fade
        private List<RadioUserResponse> responseHistory = new List<RadioUserResponse>();
        private float activeTimeTotal = 0f;
        private float timeOnCurrentQuestion = 0f;

        // --- CACHED SCALES TO FIX THE GIANT UI BUG ---
        private Vector3 originalTimerScale = Vector3.one;
        private Vector3 originalTimerContainerScale = Vector3.one;
        private Vector3 originalTimerInputScale = Vector3.one;
        private Vector3[] originalHeartScales = new Vector3[] { Vector3.one, Vector3.one, Vector3.one };

        private bool isFastTicking = false;
        private string currentPlayerName = "Player";

        private void Awake()
        {
            StopTimerAudio();
            if (timerIcon != null) originalTimerScale = timerIcon.localScale;

            // Cache all original scales before we mess with them
            if (timerContainer != null) originalTimerContainerScale = timerContainer.transform.localScale;
            if (timerInput != null) originalTimerInputScale = timerInput.transform.localScale;

            if (gameScreen != null)
            {
                Transform h1 = gameScreen.transform.Find("Heart1");
                Transform h2 = gameScreen.transform.Find("Heart2");
                Transform h3 = gameScreen.transform.Find("Heart3");

                if (h1 != null) originalHeartScales[0] = h1.localScale;
                if (h2 != null) originalHeartScales[1] = h2.localScale;
                if (h3 != null) originalHeartScales[2] = h3.localScale;

                gameScreen.SetActive(true);
                CanvasGroup cg = gameScreen.GetComponent<CanvasGroup>();
                if (cg == null) cg = gameScreen.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            if (playerDataScreen != null)
            {
                playerDataScreen.SetActive(true);
                CanvasGroup cg = playerDataScreen.GetComponent<CanvasGroup>();
                if (cg == null) cg = playerDataScreen.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        private void ToggleCanvas(GameObject obj, bool show)
        {
            if (obj == null) return;
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();

            cg.alpha = show ? 1f : 0f;
            cg.interactable = show;
            cg.blocksRaycasts = show;
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

        public void SelectPracticeMode()
        {
            PlayClickSound();
            currentMode = GameMode.Practice;
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            ToggleCanvas(playerDataScreen, true);

            if (timerInput != null) timerInput.transform.localScale = Vector3.zero;
        }

        public void SelectTestMode()
        {
            PlayClickSound();
            currentMode = GameMode.Test;
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            ToggleCanvas(playerDataScreen, true);

            // Restores to perfect original size
            if (timerInput != null) timerInput.transform.localScale = originalTimerInputScale;
        }

        public void LaunchGameFromDataScreen()
        {
            PlayClickSound();
            StartCoroutine(SafeTransitionToGame());
        }

        private IEnumerator SafeTransitionToGame()
        {
            if (playerNameInput != null) playerNameInput.DeactivateInputField();
            if (timerInput != null) timerInput.DeactivateInputField();

            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

            yield return null;

            currentPlayerName = string.IsNullOrEmpty(playerNameInput.text) ? "Player" : playerNameInput.text;
            ToggleCanvas(playerDataScreen, false);

            if (currentMode == GameMode.Practice) StartPracticeMode();
            else StartTestMode();
        }

        private void SetHeartsActiveState(bool show)
        {
            if (gameScreen == null) return;

            Transform h1 = gameScreen.transform.Find("Heart1");
            if (h1 != null) h1.localScale = show ? originalHeartScales[0] : Vector3.zero;

            Transform h2 = gameScreen.transform.Find("Heart2");
            if (h2 != null) h2.localScale = show ? originalHeartScales[1] : Vector3.zero;

            Transform h3 = gameScreen.transform.Find("Heart3");
            if (h3 != null) h3.localScale = show ? originalHeartScales[2] : Vector3.zero;
        }

        private void StartPracticeMode()
        {
            if (timerContainer != null) timerContainer.transform.localScale = Vector3.zero;

            StopTimerAudio();
            if (timerIcon != null) timerIcon.localScale = originalTimerScale;

            SetHeartsActiveState(false);

            StartGame();
        }

        private void StartTestMode()
        {
            lives = 3;
            timer = 60f;

            if (timerInput != null && !string.IsNullOrEmpty(timerInput.text))
            {
                if (float.TryParse(timerInput.text, out float customTime) && customTime > 0)
                {
                    timer = customTime;
                }
            }

            // Restores to perfect original size
            if (timerContainer != null) timerContainer.transform.localScale = originalTimerContainerScale;

            SetHeartsActiveState(true);
            UpdateLivesUI();

            isFastTicking = false;
            if (normalTickSound != null) normalTickSound.Play();
            if (fastTickSound != null) fastTickSound.Stop();

            StartGame();
        }

        private void UpdateLivesUI()
        {
            if (gameScreen == null) return;

            Transform[] dynamicHearts = new Transform[] {
                gameScreen.transform.Find("Heart1"),
                gameScreen.transform.Find("Heart2"),
                gameScreen.transform.Find("Heart3")
            };

            for (int i = 0; i < dynamicHearts.Length; i++)
            {
                if (dynamicHearts[i] != null)
                {
                    Image img = dynamicHearts[i].GetComponent<Image>();
                    if (img != null && heartOnSprite != null && heartOffSprite != null)
                    {
                        img.sprite = (i < lives) ? heartOnSprite : heartOffSprite;
                    }
                    if (img != null) img.color = Color.white;
                }
            }
        }

        private IEnumerator ShakeAndSwapHeart(int heartIndex)
        {
            if (gameScreen == null) yield break;

            string heartName = "Heart" + (heartIndex + 1);
            Transform heartTransform = gameScreen.transform.Find(heartName);

            if (heartTransform == null) yield break;

            Vector3 originalPos = heartTransform.localPosition;
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * 6f;
                float y = Random.Range(-1f, 1f) * 6f;
                heartTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            heartTransform.localPosition = originalPos;

            Image img = heartTransform.GetComponent<Image>();
            if (img != null && heartOffSprite != null) img.sprite = heartOffSprite;
        }

        private void StartGame()
        {
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            ToggleCanvas(gameScreen, true);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);

            currentQuestionIndex = 0;
            isPlaying = true;
            isPaused = false;
            isTransitioning = false;
            responseHistory.Clear();
            activeTimeTotal = 0f;

            // Ensure text is visible at the start of a new game
            SetTextAlpha(1f);

            StartCoroutine(DelayedLoadQuestion());
        }

        private IEnumerator DelayedLoadQuestion()
        {
            yield return null;
            LoadNextQuestionData();
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

        // Renamed to LoadNextQuestionData so the Coroutine can call it while text is invisible
        private void LoadNextQuestionData()
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
            // Block clicks if game is paused, over, or currently fading
            if (!isPlaying || isPaused || isTransitioning) return;

            // Instantly remove highlight from the clicked button
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

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
                    if (lives >= 0 && lives < 3)
                    {
                        StartCoroutine(ShakeAndSwapHeart(lives));
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

            // Start the fade and delay transition
            StartCoroutine(TransitionToNextQuestion());
        }

        private IEnumerator TransitionToNextQuestion()
        {
            isTransitioning = true;

            // 1. Wait using your new public variable!
            yield return new WaitForSeconds(delayBeforeFade);

            CanvasGroup cg = gameScreen.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameScreen.AddComponent<CanvasGroup>();

            // 2. Fade Out using your public fadeSpeed!
            while (cg.alpha > 0)
            {
                cg.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }

            // 3. Load the next question data while the screen is black
            LoadNextQuestionData();

            // 4. Fade the screen back In
            while (cg.alpha < 1)
            {
                cg.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }

            // Ensure it is perfectly fully visible at the end
            cg.alpha = 1f;
            isTransitioning = false;
        }

        private void SetTextAlpha(float alpha)
        {
            if (questionText != null) questionText.color = new Color(questionText.color.r, questionText.color.g, questionText.color.b, alpha);
            if (option1Text != null) option1Text.color = new Color(option1Text.color.r, option1Text.color.g, option1Text.color.b, alpha);
            if (option2Text != null) option2Text.color = new Color(option2Text.color.r, option2Text.color.g, option2Text.color.b, alpha);
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
            ToggleCanvas(gameScreen, false);
            if (gameOverScreen != null) gameOverScreen.SetActive(true);

            yield return new WaitForSeconds(3f);

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            ShowScoreScreen();
        }

        private void ShowScoreScreen()
        {
            ToggleCanvas(gameScreen, false);
            if (scoreScreen != null) scoreScreen.SetActive(true);
            GenerateScoreReport();
        }

        private void ShowPracticeCompleteScreen()
        {
            ToggleCanvas(gameScreen, false);
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

            if (finalPlayerNameText != null)
            {
                finalPlayerNameText.text = "PLAYER: " + currentPlayerName.ToUpper();
            }

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
            ToggleCanvas(gameScreen, false);

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
            ToggleCanvas(playerDataScreen, false);

            if (timerInput != null) timerInput.text = "";
            if (playerNameInput != null) playerNameInput.text = "";

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
            ToggleCanvas(gameScreen, false);

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            ToggleCanvas(playerDataScreen, false);

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
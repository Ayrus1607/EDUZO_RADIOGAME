using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public List<int> dataValues;
        public string correctAnswer;
        public int preferredMode = -1; // --- NEW: -1 = Random, 0=Bar, 1=Pie, 2=Tally, 3=Table ---
    }

    public class DataGameManager : MonoBehaviour
    {
        [Header("UI Screens")]
        public GameObject formScreen; // --- NEW: Form Screen Reference ---
        public GameObject modeSelectionScreen;
        public GameObject playerDataScreen;
        public GameObject gameScreen;
        public GameObject gameOverScreen;

        [Header("Player Data UI")]
        public TMP_InputField playerNameInput;
        public TMP_InputField timerInput;

        [Header("Game UI Elements")]
        public GameObject timerContainer;
        public TextMeshProUGUI timerText;
        public RectTransform timerIcon;
        public GameObject[] hearts;

        [Header("Audio Sources")]
        public AudioSource correctSound;
        public AudioSource wrongSound;
        public AudioSource normalTick;
        public AudioSource fastTick;
        public AudioSource buttonSelect;
        public AudioSource backgroundMusic;
        public AudioSource gameOverSound;

        [Header("System References")]
        public BarGraphManager graphManager;
        public PieChartManager pieManager;
        public TallyMarkManager tallyManager;
        public DataTableManager tableManager;

        [Header("Containers (For Auto-Hiding)")]
        public GameObject barGraphContainer;
        public GameObject pieChartContainer;
        public GameObject tallyContainer;
        public GameObject tableContainer;

        [Header("UI & Scanner")]
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI feedbackText;
        public QRCodeDecodeController qrScanner;

        [Header("Question Bank")]
        public List<QuestionData> questionBank;
        private string expectedAnswer = "";

        [Header("Game State")]
        [HideInInspector] public bool isPracticeMode = false;
        [HideInInspector] public string currentPlayerName = "";
        [HideInInspector] public int currentTimerValue = 60;

        private float timeRemaining;
        private bool isTimerRunning = false;
        private int lastTickSecond = 0;
        private bool isFastTicking = false;
        private int currentLives = 3;

        private Vector2 originalTimerPos;
        private Vector3 originalTimerScale;
        private float shakeIntensity = 5f;
        private float pulseSpeed = 4f;
        private float pulseAmount = 0.15f;

        void Start()
        {
            // --- UPDATED: Turn ON the Form Screen first! Turn off everything else ---
            if (formScreen != null) formScreen.SetActive(true);
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            if (playerDataScreen != null) playerDataScreen.SetActive(false);
            if (gameScreen != null) gameScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);

            if (timerIcon != null)
            {
                originalTimerPos = timerIcon.anchoredPosition;
                originalTimerScale = timerIcon.localScale;
            }
        }

        void Update()
        {
            if (isTimerRunning)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerUI();

                if (timeRemaining > 10f)
                {
                    if (timerIcon != null)
                    {
                        float scale = 1f + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
                        timerIcon.localScale = originalTimerScale * scale;
                    }
                }
                else
                {
                    if (!isFastTicking)
                    {
                        isFastTicking = true;
                        if (timerIcon != null) timerIcon.localScale = originalTimerScale;
                    }
                    if (timerIcon != null) timerIcon.anchoredPosition = originalTimerPos + Random.insideUnitCircle * shakeIntensity;
                }

                int currentSecond = Mathf.CeilToInt(timeRemaining);
                if (currentSecond < lastTickSecond && currentSecond > 0)
                {
                    lastTickSecond = currentSecond;
                    PlayTickSound();
                }

                if (timeRemaining <= 0)
                {
                    timeRemaining = 0;
                    UpdateTimerUI();
                    TriggerGameOver();
                }
            }
        }

        private void PlayTickSound()
        {
            if (isFastTicking && fastTick != null) fastTick.Play();
            else if (!isFastTicking && normalTick != null) normalTick.Play();
        }

        private void UpdateTimerUI()
        {
            if (timerText != null) timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
        }

        public void SelectPracticeMode()
        {
            if (buttonSelect != null) buttonSelect.Play();
            isPracticeMode = true;
            GoToPlayerDataScreen();
        }

        public void SelectTestMode()
        {
            if (buttonSelect != null) buttonSelect.Play();
            isPracticeMode = false;
            GoToPlayerDataScreen();
        }

        private void GoToPlayerDataScreen()
        {
            if (fastTick != null) fastTick.Stop();
            if (normalTick != null) normalTick.Stop();

            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            if (playerDataScreen != null) playerDataScreen.SetActive(true);

            if (timerInput != null) timerInput.gameObject.SetActive(!isPracticeMode);
        }

        public void SubmitPlayerDataAndStart()
        {
            if (buttonSelect != null) buttonSelect.Play();

            if (playerNameInput != null) currentPlayerName = playerNameInput.text;

            if (!isPracticeMode && timerInput != null)
            {
                int.TryParse(timerInput.text, out currentTimerValue);
                if (currentTimerValue <= 0) currentTimerValue = 60;
            }

            if (playerDataScreen != null) playerDataScreen.SetActive(false);
            if (gameScreen != null) gameScreen.SetActive(true);

            // BGM starts when actual gameplay starts!
            if (backgroundMusic != null && !backgroundMusic.isPlaying) backgroundMusic.Play();

            if (isPracticeMode)
            {
                if (timerContainer != null) timerContainer.SetActive(false);
                foreach (GameObject heart in hearts) if (heart != null) heart.SetActive(false);
                isTimerRunning = false;
            }
            else
            {
                if (timerContainer != null) timerContainer.SetActive(true);
                foreach (GameObject heart in hearts) if (heart != null) heart.SetActive(true);
                currentLives = hearts.Length;

                timeRemaining = currentTimerValue;
                lastTickSecond = currentTimerValue;
                isFastTicking = false;

                if (timerIcon != null)
                {
                    timerIcon.anchoredPosition = originalTimerPos;
                    timerIcon.localScale = originalTimerScale;
                }

                UpdateTimerUI();
                isTimerRunning = true;
            }

            StartTestQuestion();
        }

        public void StartTestQuestion()
        {
            if (questionBank.Count == 0) return;

            int randomQIndex = Random.Range(0, questionBank.Count);
            QuestionData currentQuestion = questionBank[randomQIndex];

            if (questionText != null) questionText.text = currentQuestion.questionText;
            expectedAnswer = currentQuestion.correctAnswer;

            if (barGraphContainer) barGraphContainer.SetActive(false);
            if (pieChartContainer) pieChartContainer.SetActive(false);
            if (tallyContainer) tallyContainer.SetActive(false);
            if (tableContainer) tableContainer.SetActive(false);

            // --- UPDATED: Check if they selected a specific mode! ---
            int targetMode = currentQuestion.preferredMode;
            if (targetMode == -1) targetMode = Random.Range(0, 4); // If "Random" was chosen, roll the dice!

            if (targetMode == 0 && graphManager != null) { barGraphContainer.SetActive(true); graphManager.GenerateGraph(currentQuestion.dataValues); }
            else if (targetMode == 1 && pieManager != null) { pieChartContainer.SetActive(true); pieManager.GeneratePieChart(currentQuestion.dataValues); }
            else if (targetMode == 2 && tallyManager != null) { tallyContainer.SetActive(true); tallyManager.GenerateTallyMarks(currentQuestion.dataValues); }
            else if (targetMode == 3 && tableManager != null) { tableContainer.SetActive(true); tableManager.GenerateTable(currentQuestion.dataValues); }

            if (feedbackText != null) feedbackText.text = "Warming up scanner...";
            Invoke("TurnOnScanner", 1.5f);
        }

        private void TurnOnScanner()
        {
            if (qrScanner != null)
            {
                qrScanner.Reset();
                qrScanner.StartWork();
                if (feedbackText != null) feedbackText.text = "Scan your answer...";
            }
        }

        public void OnFlashcardScanned(string scannedData)
        {
            string cleanData = scannedData.Trim();

            if (cleanData == expectedAnswer)
            {
                if (correctSound != null) correctSound.Play();
                if (feedbackText != null) { feedbackText.color = Color.green; feedbackText.text = "CORRECT! Great job!"; }
                if (qrScanner != null) qrScanner.StopWork();
                Invoke("StartTestQuestion", 3f);
            }
            else
            {
                if (wrongSound != null) wrongSound.Play();
                if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Oops! You scanned " + cleanData + ". Try again!"; }
                if (qrScanner != null) qrScanner.Reset();

                if (!isPracticeMode)
                {
                    currentLives--;
                    if (currentLives >= 0 && currentLives < hearts.Length) hearts[currentLives].SetActive(false);
                    if (currentLives <= 0) TriggerGameOver();
                }
            }
        }

        private void TriggerGameOver()
        {
            isTimerRunning = false;
            if (backgroundMusic != null) backgroundMusic.Stop();
            if (normalTick != null) normalTick.Stop();
            if (fastTick != null) fastTick.Stop();
            if (gameOverSound != null) gameOverSound.Play();

            if (timerIcon != null)
            {
                timerIcon.anchoredPosition = originalTimerPos;
                timerIcon.localScale = originalTimerScale;
            }
            if (qrScanner != null) qrScanner.StopWork();

            if (gameScreen != null) gameScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(true);
        }
    }
}
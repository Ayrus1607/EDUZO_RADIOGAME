using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public List<string> categoryNames;
        public List<int> dataValues;
        public string correctAnswer;
        public int preferredMode = -1;
        public Sprite questionImage;
        public int targetIndex = 0;
    }

    public class DataGameManager : MonoBehaviour
    {
        [Header("Developer Debug Mode")]
        public bool quickStartDebug = false;
        public int forceGameMode = 0;
        public List<int> debugDataValues = new List<int> { 35, 15, 50 };

        [Header("Clipboard UI")]
        public GameObject clipboardRowPrefab;
        public Transform clipboardListContainer;

        [Header("UI Screens")]
        public GameObject formScreen;
        public GameObject modeSelectionScreen;
        public GameObject playerDataScreen;
        public GameObject gameScreen;
        public GameObject gameOverScreen;
        public GameObject optionsMenuScreen;
        public GameObject practiceCompleteScreen; // <-- NEW!
        public GameObject scoreScreen;            // <-- NEW!

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

        [Header("Containers (For Auto-Hiding)")]
        public GameObject barGraphContainer;
        public GameObject pieChartContainer;
        public GameObject tallyContainer;
        public GameObject lookAndCountContainer;
        public UnityEngine.UI.Image lookAndCountImageUI;

        [Header("UI & Scanner")]
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI feedbackText;
        public QRCodeDecodeController qrScanner;

        [Header("Question Bank")]
        public List<QuestionData> questionBank;
        private string expectedAnswer = "";
        private string inputBuffer = "";

        // --- NEW: Tracking Progress ---
        private List<int> unaskedQuestions = new List<int>();
        private int currentQuestionBankIndex = -1;

        private List<string> defaultCategoryNames = new List<string> { "Oranges", "Apples", "Grapes", "Bananas", "Mangoes" };

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
            if (timerIcon != null)
            {
                originalTimerPos = timerIcon.anchoredPosition;
                originalTimerScale = timerIcon.localScale;
            }

            if (quickStartDebug)
            {
                if (formScreen != null) formScreen.SetActive(false);
                if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
                if (playerDataScreen != null) playerDataScreen.SetActive(false);
                if (gameOverScreen != null) gameOverScreen.SetActive(false);
                if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
                if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
                if (scoreScreen != null) scoreScreen.SetActive(false);
                if (gameScreen != null) gameScreen.SetActive(true);

                if (questionBank == null) questionBank = new List<QuestionData>();
                if (questionBank.Count == 0)
                {
                    QuestionData debugQ = new QuestionData();
                    debugQ.dataValues = debugDataValues;
                    debugQ.correctAnswer = debugDataValues[0].ToString();
                    debugQ.preferredMode = forceGameMode;
                    questionBank.Add(debugQ);
                }
                else
                {
                    questionBank[0].preferredMode = forceGameMode;
                    questionBank[0].dataValues = debugDataValues;
                }

                isPracticeMode = true;

                // Populate the tracker for debug mode
                unaskedQuestions.Clear();
                for (int i = 0; i < questionBank.Count; i++) unaskedQuestions.Add(i);

                StartTestQuestion();
                return;
            }

            if (formScreen != null) formScreen.SetActive(true);
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            if (playerDataScreen != null) playerDataScreen.SetActive(false);
            if (gameScreen != null) gameScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
            if (scoreScreen != null) scoreScreen.SetActive(false);
        }

        void Update()
        {
            if (isTimerRunning && !isPracticeMode)
            {
                timeRemaining -= Time.deltaTime;
                if (timerText != null) timerText.text = Mathf.CeilToInt(timeRemaining).ToString();

                if (timeRemaining > 10f)
                {
                    if (timerIcon != null) timerIcon.localScale = originalTimerScale * (1f + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount));
                }
                else
                {
                    if (!isFastTicking) { isFastTicking = true; if (timerIcon != null) timerIcon.localScale = originalTimerScale; }
                    if (timerIcon != null) timerIcon.anchoredPosition = originalTimerPos + Random.insideUnitCircle * shakeIntensity;
                }

                int currentSecond = Mathf.CeilToInt(timeRemaining);
                if (currentSecond < lastTickSecond && currentSecond > 0)
                {
                    lastTickSecond = currentSecond;
                    if (isFastTicking && fastTick != null) fastTick.Play();
                    else if (!isFastTicking && normalTick != null) normalTick.Play();
                }

                if (timeRemaining <= 0) TriggerGameOver();
            }
        }

        public void SelectPracticeMode()
        {
            if (buttonSelect != null) buttonSelect.Play();
            isPracticeMode = true;

            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            if (playerDataScreen != null) playerDataScreen.SetActive(true);

            if (timerInput != null) timerInput.gameObject.SetActive(false);
        }

        public void SelectTestMode()
        {
            if (buttonSelect != null) buttonSelect.Play();
            isPracticeMode = false;

            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);
            if (playerDataScreen != null) playerDataScreen.SetActive(true);

            if (timerInput != null) timerInput.gameObject.SetActive(true);
        }

        public void StartGameFromPlayerData()
        {
            if (buttonSelect != null) buttonSelect.Play();

            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            {
                currentPlayerName = playerNameInput.text;
            }

            // --- NEW: Setup the question tracker so it knows how many questions to ask! ---
            unaskedQuestions.Clear();
            for (int i = 0; i < questionBank.Count; i++)
            {
                unaskedQuestions.Add(i);
            }

            if (isPracticeMode)
            {
                if (timerContainer != null) timerContainer.SetActive(false);
                foreach (var heart in hearts) if (heart != null) heart.SetActive(false);
            }
            else
            {
                if (timerInput != null && int.TryParse(timerInput.text, out int parsedTime))
                {
                    currentTimerValue = parsedTime;
                }
                else
                {
                    currentTimerValue = 60;
                }

                timeRemaining = currentTimerValue;
                isFastTicking = false;

                if (timerContainer != null) timerContainer.SetActive(true);
                currentLives = 3;
                foreach (var heart in hearts) if (heart != null) heart.SetActive(true);
            }

            if (playerDataScreen != null) playerDataScreen.SetActive(false);
            if (gameScreen != null) gameScreen.SetActive(true);

            StartTestQuestion();
        }

        public void StartTestQuestion()
        {
            // --- NEW: Did we finish all the questions? ---
            if (unaskedQuestions.Count == 0)
            {
                if (isPracticeMode) TriggerPracticeComplete();
                else TriggerScoreScreen(); // In test mode, we go to the score screen!
                return;
            }

            // Grab a random question from the REMAINING unasked questions
            int randomListIndex = Random.Range(0, unaskedQuestions.Count);
            currentQuestionBankIndex = unaskedQuestions[randomListIndex];
            QuestionData currentQuestion = questionBank[currentQuestionBankIndex];

            expectedAnswer = currentQuestion.correctAnswer;
            inputBuffer = "";

            int targetMode = currentQuestion.preferredMode;
            if (targetMode == -1) targetMode = Random.Range(0, 4);

            if (questionText != null)
            {
                if (targetMode == 0) questionText.text = "Bar Graph";
                else if (targetMode == 1) questionText.text = "Pie Chart";
                else if (targetMode == 2) questionText.text = "Tally Chart";
                else if (targetMode == 3) questionText.text = "Look & Count";
            }

            List<string> activeCategories = (currentQuestion.categoryNames != null && currentQuestion.categoryNames.Count > 0)
                                            ? currentQuestion.categoryNames
                                            : defaultCategoryNames;

            if (barGraphContainer) barGraphContainer.SetActive(false);
            if (pieChartContainer) pieChartContainer.SetActive(false);
            if (tallyContainer) tallyContainer.SetActive(false);
            if (lookAndCountContainer) lookAndCountContainer.SetActive(false);

            if (targetMode == 0 && graphManager != null) { barGraphContainer.SetActive(true); graphManager.GenerateGraph(currentQuestion.dataValues, activeCategories); }
            else if (targetMode == 1 && pieManager != null) { pieChartContainer.SetActive(true); pieManager.GeneratePieChart(currentQuestion.dataValues, activeCategories, currentQuestion.targetIndex); }
            else if (targetMode == 2 && tallyManager != null) { tallyContainer.SetActive(true); tallyManager.GenerateTallyMarks(currentQuestion.dataValues, activeCategories); }
            else if (targetMode == 3)
            {
                if (lookAndCountContainer != null) lookAndCountContainer.SetActive(true);
                if (lookAndCountImageUI != null && currentQuestion.questionImage != null)
                {
                    lookAndCountImageUI.sprite = currentQuestion.questionImage;
                }
            }

            if (clipboardRowPrefab != null && clipboardListContainer != null)
            {
                foreach (Transform child in clipboardListContainer) Destroy(child.gameObject);

                for (int i = 0; i < currentQuestion.dataValues.Count; i++)
                {
                    GameObject newRow = Instantiate(clipboardRowPrefab, clipboardListContainer);

                    TextMeshProUGUI nameText = newRow.transform.Find("Item_Name")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI valueText = newRow.transform.Find("Number_Box/Item_Value")?.GetComponent<TextMeshProUGUI>();

                    if (nameText != null) nameText.text = (i < activeCategories.Count) ? activeCategories[i] : "Item";

                    if (valueText != null)
                    {
                        string rawValueString = currentQuestion.dataValues[i].ToString();

                        if (i == currentQuestion.targetIndex)
                        {
                            valueText.text = "?";
                            valueText.color = Color.red;
                        }
                        else
                        {
                            valueText.color = new Color(0.2f, 0.1f, 0.05f);

                            if (targetMode == 1)
                            {
                                float total = 0;
                                foreach (int val in currentQuestion.dataValues) total += val;
                                float percentage = ((float)currentQuestion.dataValues[i] / total) * 100f;
                                valueText.text = Mathf.RoundToInt(percentage) + "%";
                            }
                            else
                            {
                                valueText.text = rawValueString;
                            }
                        }
                    }
                }
            }

            if (feedbackText != null) feedbackText.text = "Warming up scanner...";
            Invoke("TurnOnScanner", 1.5f);
        }

        private void TurnOnScanner()
        {
            if (!isPracticeMode) isTimerRunning = true;
            if (qrScanner != null) { qrScanner.Reset(); qrScanner.StartWork(); if (feedbackText != null) feedbackText.text = "Scan your answer..."; }
        }

        public void OnFlashcardScanned(string scannedData)
        {
            inputBuffer += scannedData.Trim();
            if (feedbackText != null) { feedbackText.color = Color.white; feedbackText.text = "Scanned: " + inputBuffer + "..."; }

            if (inputBuffer.Length >= expectedAnswer.Length)
            {
                if (inputBuffer == expectedAnswer)
                {
                    if (correctSound != null) correctSound.Play();
                    if (feedbackText != null) { feedbackText.color = Color.green; feedbackText.text = "CORRECT! Great job!"; }
                    if (qrScanner != null) qrScanner.StopWork();
                    isTimerRunning = false;

                    // --- NEW: Remove this question from the tracker because they got it right! ---
                    if (unaskedQuestions.Contains(currentQuestionBankIndex))
                    {
                        unaskedQuestions.Remove(currentQuestionBankIndex);
                    }

                    Invoke("StartTestQuestion", 3f);
                }
                else
                {
                    if (wrongSound != null) wrongSound.Play();
                    if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Oops! You scanned " + inputBuffer + ". Try again!"; }
                    inputBuffer = "";
                    if (qrScanner != null) qrScanner.Reset();

                    if (!isPracticeMode)
                    {
                        currentLives--;

                        if (currentLives >= 0 && currentLives < hearts.Length && hearts[currentLives] != null)
                        {
                            hearts[currentLives].SetActive(false);
                        }

                        if (currentLives <= 0)
                        {
                            TriggerGameOver();
                        }
                    }
                }
            }
            else { if (qrScanner != null) qrScanner.Reset(); }
        }

        private void TriggerGameOver()
        {
            isTimerRunning = false;
            if (backgroundMusic != null) backgroundMusic.Stop();
            if (gameOverSound != null) gameOverSound.Play();
            if (qrScanner != null) qrScanner.StopWork();
            if (gameScreen != null) gameScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(true);
        }

        // --- NEW: Methods to trigger the end-game screens ---
        private void TriggerPracticeComplete()
        {
            isTimerRunning = false;
            if (qrScanner != null) qrScanner.StopWork();
            if (gameScreen != null) gameScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(true);
        }

        private void TriggerScoreScreen()
        {
            isTimerRunning = false;
            if (qrScanner != null) qrScanner.StopWork();
            if (gameScreen != null) gameScreen.SetActive(false);
            if (scoreScreen != null) scoreScreen.SetActive(true);
        }

        public void OpenOptionsMenu()
        {
            if (buttonSelect != null) buttonSelect.Play();
            isTimerRunning = false;
            if (qrScanner != null) qrScanner.StopWork();
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(true);
        }

        public void CloseOptionsMenu()
        {
            if (buttonSelect != null) buttonSelect.Play();
            if (!isPracticeMode) isTimerRunning = true;
            if (qrScanner != null) qrScanner.StartWork();
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
        }
    }
}
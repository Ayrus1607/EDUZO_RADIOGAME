using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace Eduzo.Games.DataHandling
{
    [System.Serializable]
    public class QuestionResult
    {
        public string questionText;
        public string correctAnswer;
        public string userResponse;
        public bool isCorrect;
        public float responseTime;
    }

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

        [Header("Transitions")]
        public CanvasGroup fadeGroup;
        public float fadeSpeed = 1.5f;

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
        public GameObject practiceCompleteScreen;
        public GameObject scoreScreen;

        [Header("Player Data UI")]
        public TMP_InputField playerNameInput;
        public TMP_InputField timerInput;

        [Header("Game UI Elements")]
        public GameObject timerContainer;
        public TextMeshProUGUI timerText;
        public RectTransform timerIcon;
        public GameObject[] hearts;

        [Header("Score Screen UI")]
        public TextMeshProUGUI scoreSummaryText;

        [Header("Audio Sources")]
        public AudioSource correctSound;
        public AudioSource wrongSound;
        public AudioSource normalTick;
        public AudioSource fastTick;
        public AudioSource buttonSelect;
        public AudioSource backgroundMusic;
        public AudioSource gameOverSound;

        [Header("VFX Setup")]
        public Transform vfxSpawnPoint;
        public Transform secondaryVfxSpawnPoint;
        public float vfxScale = 4f;
        public float tertiaryVfxScale = 2f;
        public float tertiaryOffset = 100f;

        [Header("Correct VFX")]
        public GameObject correctVfxPrefab;
        public GameObject secondaryCorrectVfxPrefab;
        public GameObject[] tertiaryCorrectVfxPrefabs;

        [Header("Wrong VFX")]
        public GameObject wrongVfxPrefab;
        public GameObject secondaryWrongVfxPrefab;
        public GameObject[] tertiaryWrongVfxPrefabs;

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

        [HideInInspector] public string inputBuffer = "";

        private List<int> unaskedQuestions = new List<int>();
        private int currentQuestionBankIndex = -1;

        private List<QuestionResult> sessionResults = new List<QuestionResult>();
        private float gameStartTime;
        private float currentQuestionStartTime;

        private bool isProcessingScan = false;
        private float lastScanTime = 0f;

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
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = false;
            }

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

                unaskedQuestions.Clear();
                for (int i = 0; i < questionBank.Count; i++) unaskedQuestions.Add(i);

                sessionResults.Clear();
                gameStartTime = Time.time;

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

            unaskedQuestions.Clear();
            for (int i = 0; i < questionBank.Count; i++)
            {
                unaskedQuestions.Add(i);
            }

            sessionResults.Clear();
            gameStartTime = Time.time;

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
                if (timerText != null) timerText.text = currentTimerValue.ToString();

                currentLives = 3;
                foreach (var heart in hearts) if (heart != null) heart.SetActive(true);
            }

            if (playerDataScreen != null) playerDataScreen.SetActive(false);
            if (gameScreen != null) gameScreen.SetActive(true);

            StartTestQuestion();
        }

        public void StartTestQuestion()
        {
            if (unaskedQuestions.Count == 0)
            {
                if (isPracticeMode) TriggerPracticeComplete();
                else TriggerScoreScreen();
                return;
            }

            currentQuestionStartTime = Time.time;

            int randomListIndex = Random.Range(0, unaskedQuestions.Count);
            currentQuestionBankIndex = unaskedQuestions[randomListIndex];
            QuestionData currentQuestion = questionBank[currentQuestionBankIndex];

            expectedAnswer = currentQuestion.correctAnswer;

            ClearScannedAnswer();

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

            if (feedbackText != null) { feedbackText.text = ""; }
            Invoke("TurnOnScanner", 1.5f);
        }

        private void TurnOnScanner()
        {
            isProcessingScan = false;
            if (!isPracticeMode) isTimerRunning = true;

            if (qrScanner != null) { qrScanner.Reset(); qrScanner.StartWork(); if (feedbackText != null) feedbackText.text = ""; }
        }

        public void OnFlashcardScanned(string scannedData)
        {
            if (isProcessingScan) return;

            if (Time.time - lastScanTime < 0.5f) return;

            lastScanTime = Time.time;

            if (inputBuffer.Length < 10)
            {
                inputBuffer += scannedData.Trim();

                if (feedbackText != null) { feedbackText.color = Color.yellow; feedbackText.text = inputBuffer; }

                if (buttonSelect != null) buttonSelect.Play();
            }

            if (qrScanner != null)
            {
                qrScanner.Reset();
                qrScanner.StartWork();
            }
        }

        public void SubmitScannedAnswer()
        {
            if (string.IsNullOrEmpty(inputBuffer)) return;

            if (inputBuffer.Length < expectedAnswer.Length)
            {
                isProcessingScan = false;
                if (qrScanner != null) qrScanner.Reset();
                return;
            }

            isProcessingScan = true;

            QuestionResult result = new QuestionResult();
            result.questionText = questionBank[currentQuestionBankIndex].questionText;
            result.correctAnswer = expectedAnswer;
            result.userResponse = inputBuffer;
            result.isCorrect = (inputBuffer == expectedAnswer);
            result.responseTime = Time.time - currentQuestionStartTime;
            sessionResults.Add(result);

            if (result.isCorrect)
            {
                if (correctSound != null) correctSound.Play();
                if (feedbackText != null) { feedbackText.color = Color.green; feedbackText.text = "CORRECT!"; }
                if (qrScanner != null) qrScanner.StopWork();
                isTimerRunning = false;

                PlayVFX(correctVfxPrefab, secondaryCorrectVfxPrefab, tertiaryCorrectVfxPrefabs);

                if (unaskedQuestions.Contains(currentQuestionBankIndex))
                {
                    unaskedQuestions.Remove(currentQuestionBankIndex);
                }

                StartCoroutine(TransitionToNextQuestion());
            }
            else
            {
                if (wrongSound != null) wrongSound.Play();
                if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Oops!"; }

                PlayVFX(wrongVfxPrefab, secondaryWrongVfxPrefab, tertiaryWrongVfxPrefabs);

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
                        return;
                    }
                }

                Invoke("ResetScannerCooldown", 2f);
            }
        }

        private void PlayVFX(GameObject primary, GameObject secondary, GameObject[] tertiaries)
        {
            if (primary != null && vfxSpawnPoint != null)
            {
                GameObject pVfx = Instantiate(primary, vfxSpawnPoint.position, Quaternion.identity);
                pVfx.transform.SetParent(vfxSpawnPoint, true);
                pVfx.transform.localScale = new Vector3(vfxScale, vfxScale, vfxScale);
                Destroy(pVfx, 3f);
            }

            if (secondary != null && secondaryVfxSpawnPoint != null)
            {
                GameObject sVfx = Instantiate(secondary, secondaryVfxSpawnPoint.position, Quaternion.identity);
                sVfx.transform.SetParent(secondaryVfxSpawnPoint, true);
                sVfx.transform.localScale = new Vector3(vfxScale, vfxScale, vfxScale);
                Destroy(sVfx, 3f);
            }

            if (tertiaries != null && tertiaries.Length > 0 && vfxSpawnPoint != null)
            {
                GameObject randomTertiary = tertiaries[Random.Range(0, tertiaries.Length)];
                if (randomTertiary != null)
                {
                    Vector3 offset = new Vector3(Random.Range(-tertiaryOffset, tertiaryOffset), Random.Range(-tertiaryOffset, tertiaryOffset), 0f);

                    GameObject tVfx = Instantiate(randomTertiary, vfxSpawnPoint.position + offset, Quaternion.identity);
                    tVfx.transform.SetParent(vfxSpawnPoint, true);
                    tVfx.transform.localScale = new Vector3(tertiaryVfxScale, tertiaryVfxScale, tertiaryVfxScale);
                    Destroy(tVfx, 3f);
                }
            }
        }

        public void ClearScannedAnswer()
        {
            if (buttonSelect != null) buttonSelect.Play();

            inputBuffer = "";
            isProcessingScan = false;

            if (feedbackText != null) { feedbackText.color = Color.yellow; feedbackText.text = ""; }

            if (qrScanner != null) qrScanner.Reset();
        }

        private void ResetScannerCooldown()
        {
            ClearScannedAnswer();
        }

        private IEnumerator TransitionToNextQuestion()
        {
            yield return new WaitForSeconds(1.5f);

            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = true;
                float alpha = 0f;
                while (alpha < 1f)
                {
                    alpha += Time.deltaTime * fadeSpeed;
                    fadeGroup.alpha = Mathf.Clamp01(alpha);
                    yield return null;
                }
            }

            StartTestQuestion();

            if (fadeGroup != null)
            {
                float alpha = 1f;
                while (alpha > 0f)
                {
                    alpha -= Time.deltaTime * fadeSpeed;
                    fadeGroup.alpha = Mathf.Clamp01(alpha);
                    yield return null;
                }
                fadeGroup.blocksRaycasts = false;
            }
        }

        private void TriggerGameOver()
        {
            if (fadeGroup != null) { fadeGroup.alpha = 0f; fadeGroup.blocksRaycasts = false; }
            isTimerRunning = false;
            if (backgroundMusic != null) backgroundMusic.Stop();
            if (gameOverSound != null) gameOverSound.Play();
            if (qrScanner != null) qrScanner.StopWork();
            if (gameScreen != null) gameScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(true);

            StartCoroutine(GameOverToScoreRoutine());
        }

        private IEnumerator GameOverToScoreRoutine()
        {
            yield return new WaitForSeconds(2f);

            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = true;
                float alpha = 0f;
                while (alpha < 1f)
                {
                    alpha += Time.deltaTime * fadeSpeed;
                    fadeGroup.alpha = Mathf.Clamp01(alpha);
                    yield return null;
                }
            }

            if (gameOverScreen != null) gameOverScreen.SetActive(false);

            GenerateScoreSummary();

            if (scoreScreen != null) scoreScreen.SetActive(true);

            if (fadeGroup != null)
            {
                float alpha = 1f;
                while (alpha > 0f)
                {
                    alpha -= Time.deltaTime * fadeSpeed;
                    fadeGroup.alpha = Mathf.Clamp01(alpha);
                    yield return null;
                }
                fadeGroup.blocksRaycasts = false;
            }
        }

        private void TriggerPracticeComplete()
        {
            if (fadeGroup != null) { fadeGroup.alpha = 0f; fadeGroup.blocksRaycasts = false; }
            isTimerRunning = false;
            if (qrScanner != null) qrScanner.StopWork();
            if (gameScreen != null) gameScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(true);
        }

        private void TriggerScoreScreen()
        {
            if (fadeGroup != null) { fadeGroup.alpha = 0f; fadeGroup.blocksRaycasts = false; }
            isTimerRunning = false;
            if (qrScanner != null) qrScanner.StopWork();
            if (gameScreen != null) gameScreen.SetActive(false);

            GenerateScoreSummary();

            if (scoreScreen != null) scoreScreen.SetActive(true);
        }

        private void GenerateScoreSummary()
        {
            if (scoreSummaryText == null) return;

            int correctCount = 0;
            int wrongCount = 0;

            foreach (var res in sessionResults)
            {
                if (res.isCorrect) correctCount++;
                else wrongCount++;
            }

            int totalResponses = sessionResults.Count;
            int score = totalResponses > 0 ? Mathf.RoundToInt(((float)correctCount / totalResponses) * 100f) : 0;
            float activeTime = Time.time - gameStartTime;

            string summary = "========== GAME SCORE SUMMARY ==========\n";
            summary += $"Score: {score}%\n";
            summary += $"Active Time: {activeTime:F1}s\n";
            summary += "Idle Time: 0s\n\n";
            summary += $"Total Responses: {totalResponses}\n";
            summary += $"Correct Answers: {correctCount}\n";
            summary += $"Wrong Answers: {wrongCount}\n\n";
            summary += "========== QUESTION BREAKDOWN ==========\n\n";

            for (int i = 0; i < sessionResults.Count; i++)
            {
                var res = sessionResults[i];
                string simpleQuestion = res.questionText.Replace("How many ", "").Replace(" are there?", "");

                summary += $"--- Question {i + 1} ---\n";
                summary += $"Item: {simpleQuestion}\n";
                summary += $"Correct Answer: {res.correctAnswer}\n";
                summary += $"User's Response: {res.userResponse}\n";
                summary += $"Result: {(res.isCorrect ? "CORRECT" : "INCORRECT")}\n";
                summary += $"Response Time: {res.responseTime:F1}s\n\n";
            }

            scoreSummaryText.text = summary;
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

        public void ReplayGame()
        {
            if (buttonSelect != null) buttonSelect.Play();

            StopAllCoroutines();
            if (fadeGroup != null) { fadeGroup.alpha = 0f; fadeGroup.blocksRaycasts = false; }

            unaskedQuestions.Clear();
            for (int i = 0; i < questionBank.Count; i++)
            {
                unaskedQuestions.Add(i);
            }

            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
            if (scoreScreen != null) scoreScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (gameScreen != null) gameScreen.SetActive(true);

            sessionResults.Clear();
            gameStartTime = Time.time;

            if (!isPracticeMode)
            {
                timeRemaining = currentTimerValue;
                isFastTicking = false;
                if (timerContainer != null) timerContainer.SetActive(true);

                if (timerText != null) timerText.text = currentTimerValue.ToString();

                currentLives = 3;
                foreach (var heart in hearts) if (heart != null) heart.SetActive(true);
            }

            StartTestQuestion();
        }

        public void ReturnToHome()
        {
            if (buttonSelect != null) buttonSelect.Play();

            StopAllCoroutines();
            if (fadeGroup != null) { fadeGroup.alpha = 0f; fadeGroup.blocksRaycasts = false; }

            isTimerRunning = false;
            if (qrScanner != null) qrScanner.StopWork();

            if (gameScreen != null) gameScreen.SetActive(false);
            if (practiceCompleteScreen != null) practiceCompleteScreen.SetActive(false);
            if (scoreScreen != null) scoreScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (optionsMenuScreen != null) optionsMenuScreen.SetActive(false);
            if (playerDataScreen != null) playerDataScreen.SetActive(false);
            if (modeSelectionScreen != null) modeSelectionScreen.SetActive(false);

            if (formScreen != null) formScreen.SetActive(true);
        }

        // --- NEW: THE METHOD TO QUIT THE GAME ---
        public void QuitGame()
        {
            if (buttonSelect != null) buttonSelect.Play();

            Debug.Log("Game is quitting...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
    }
}
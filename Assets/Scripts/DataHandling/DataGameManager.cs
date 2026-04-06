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
        public List<int> debugDataValues = new List<int> { 2, 12, 14, 14, 14, 14, 14 };

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

        // --- GAMEPLAY LOGIC VARIABLES ---
        private QuestionData currentQuestion;
        private int activeItemIndex = 0;
        [HideInInspector] public string inputBuffer = "";
        private bool isProcessingScan = false;

        // --- SMART SCANNING VARIABLES ---
        private float lastScanTime = 0f;
        private string lastScannedDigit = "";

        private List<int> unaskedQuestions = new List<int>();
        private int currentQuestionBankIndex = -1;

        private List<QuestionResult> sessionResults = new List<QuestionResult>();
        private float gameStartTime;
        private float currentQuestionStartTime;

        private List<string> defaultCategoryNames = new List<string> { "Oranges", "Apples", "Grapes", "Bananas", "Mangoes", "Pears", "Kiwis", "Lemons" };

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
                    debugQ.categoryNames = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H" };
                    debugQ.correctAnswer = debugDataValues[0].ToString();
                    debugQ.preferredMode = forceGameMode;
                    questionBank.Add(debugQ);
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

        public void StartGameFromPlayerData()
        {
            if (buttonSelect != null) buttonSelect.Play();
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text)) currentPlayerName = playerNameInput.text;

            unaskedQuestions.Clear();
            for (int i = 0; i < questionBank.Count; i++) unaskedQuestions.Add(i);

            sessionResults.Clear();
            gameStartTime = Time.time;

            if (isPracticeMode)
            {
                if (timerContainer != null) timerContainer.SetActive(false);
                foreach (var heart in hearts) if (heart != null) heart.SetActive(false);
            }
            else
            {
                if (timerInput != null && int.TryParse(timerInput.text, out int parsedTime)) currentTimerValue = parsedTime;
                else currentTimerValue = 60;

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
            currentQuestion = questionBank[currentQuestionBankIndex];

            activeItemIndex = 0;
            if (currentQuestion.preferredMode == 1) activeItemIndex = currentQuestion.targetIndex;

            inputBuffer = "";
            lastScannedDigit = ""; // Reset tracker
            if (feedbackText != null) feedbackText.text = "";

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
                                            ? currentQuestion.categoryNames : defaultCategoryNames;

            int maxValue = 0;
            foreach (int v in currentQuestion.dataValues) if (v > maxValue) maxValue = v;
            int dynamicYRange = Mathf.CeilToInt(maxValue / 10f) * 10;
            if (dynamicYRange == 0) dynamicYRange = 10;

            if (barGraphContainer) barGraphContainer.SetActive(false);
            if (pieChartContainer) pieChartContainer.SetActive(false);
            if (tallyContainer) tallyContainer.SetActive(false);
            if (lookAndCountContainer) lookAndCountContainer.SetActive(false);

            if (targetMode == 0 && graphManager != null) { barGraphContainer.SetActive(true); graphManager.GenerateGraph(currentQuestion.dataValues, activeCategories, dynamicYRange); }
            else if (targetMode == 1 && pieManager != null) { pieChartContainer.SetActive(true); pieManager.GeneratePieChart(currentQuestion.dataValues, activeCategories, currentQuestion.targetIndex); }
            else if (targetMode == 2 && tallyManager != null) { tallyContainer.SetActive(true); tallyManager.GenerateTallyMarks(currentQuestion.dataValues, activeCategories); }
            else if (targetMode == 3)
            {
                if (lookAndCountContainer != null) lookAndCountContainer.SetActive(true);
                if (lookAndCountImageUI != null && currentQuestion.questionImage != null) lookAndCountImageUI.sprite = currentQuestion.questionImage;
            }

            RefreshClipboardList();
            Invoke("TurnOnScanner", 1.5f);
        }

        private void RefreshClipboardList()
        {
            if (clipboardRowPrefab == null || clipboardListContainer == null) return;

            foreach (Transform child in clipboardListContainer) Destroy(child.gameObject);

            List<string> activeCategories = (currentQuestion.categoryNames != null && currentQuestion.categoryNames.Count > 0)
                                            ? currentQuestion.categoryNames : defaultCategoryNames;

            for (int i = 0; i < currentQuestion.dataValues.Count; i++)
            {
                GameObject newRow = Instantiate(clipboardRowPrefab, clipboardListContainer);
                TextMeshProUGUI nameText = newRow.transform.Find("Item_Name")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueText = newRow.transform.Find("Number_Box/Item_Value")?.GetComponent<TextMeshProUGUI>();

                if (nameText != null) nameText.text = (i < activeCategories.Count) ? activeCategories[i] : "Item";

                if (valueText != null)
                {
                    if (currentQuestion.preferredMode == 1)
                    {
                        if (i == activeItemIndex)
                        {
                            valueText.text = string.IsNullOrEmpty(inputBuffer) ? "?" : inputBuffer;
                            valueText.color = Color.red;
                        }
                        else
                        {
                            float total = 0; foreach (int val in currentQuestion.dataValues) total += val;
                            float percentage = ((float)currentQuestion.dataValues[i] / total) * 100f;
                            valueText.text = Mathf.RoundToInt(percentage) + "%";
                            valueText.color = new Color(0.2f, 0.1f, 0.05f);
                        }
                    }
                    else
                    {
                        if (i < activeItemIndex)
                        {
                            valueText.text = currentQuestion.dataValues[i].ToString();
                            valueText.color = new Color(0.1f, 0.5f, 0.1f);
                        }
                        else if (i == activeItemIndex)
                        {
                            valueText.text = string.IsNullOrEmpty(inputBuffer) ? "?" : inputBuffer;
                            valueText.color = Color.red;
                        }
                        else
                        {
                            valueText.text = "";
                        }
                    }
                }
            }
        }

        private void TurnOnScanner()
        {
            isProcessingScan = false;
            lastScannedDigit = ""; // Reset tracker for the new row!
            if (!isPracticeMode) isTimerRunning = true;

            if (qrScanner != null)
            {
                qrScanner.StopWork();
                qrScanner.Reset();
                qrScanner.StartWork();
                if (feedbackText != null) feedbackText.text = "";
            }
        }

        // --- THE UNJAMMABLE SMART SCAN LOGIC ---
        public void OnFlashcardScanned(string scannedData)
        {
            // CRITICAL FIX: ALWAYS clear the plugin cache the millisecond a scan fires
            // This guarantees the camera never locks up, no matter what happens below.
            if (qrScanner != null) qrScanner.Reset();

            if (isProcessingScan) return;

            scannedData = scannedData.Trim();

            // SMART COOLDOWN: 
            // If the camera reads the EXACT SAME digit super fast, ignore it for 1 second.
            // But if you scan a '1' and then instantly hold up a '2', it accepts it instantly!
            if (scannedData == lastScannedDigit && (Time.time - lastScanTime < 1.0f))
            {
                return;
            }

            // Accept the Scan!
            lastScanTime = Time.time;
            lastScannedDigit = scannedData;

            inputBuffer += scannedData;

            RefreshClipboardList();
            if (buttonSelect != null) buttonSelect.Play();
        }

        public void ClearScannedAnswer()
        {
            if (buttonSelect != null) buttonSelect.Play();
            inputBuffer = "";
            lastScannedDigit = ""; // Reset the tracker!
            RefreshClipboardList();

            if (qrScanner != null) qrScanner.Reset();
        }

        public void SubmitScannedAnswer()
        {
            if (string.IsNullOrEmpty(inputBuffer)) return;
            if (isProcessingScan) return;

            if (qrScanner != null) qrScanner.StopWork();

            string targetAnswer = currentQuestion.dataValues[activeItemIndex].ToString();

            if (inputBuffer == targetAnswer) HandleCorrectRow();
            else HandleWrongRow();
        }

        private void HandleCorrectRow()
        {
            isProcessingScan = true;
            if (correctSound != null) correctSound.Play();
            PlayVFX(correctVfxPrefab, secondaryCorrectVfxPrefab, tertiaryCorrectVfxPrefabs);

            string catName = "Item";
            if (currentQuestion.categoryNames != null && activeItemIndex < currentQuestion.categoryNames.Count)
                catName = currentQuestion.categoryNames[activeItemIndex];

            QuestionResult result = new QuestionResult();
            result.questionText = catName;
            result.correctAnswer = currentQuestion.dataValues[activeItemIndex].ToString();
            result.userResponse = inputBuffer;
            result.isCorrect = true;
            result.responseTime = Time.time - currentQuestionStartTime;
            sessionResults.Add(result);

            inputBuffer = "";

            if (currentQuestion.preferredMode == 1)
            {
                StartCoroutine(TransitionToNextQuestion());
            }
            else
            {
                activeItemIndex++;
                if (activeItemIndex >= currentQuestion.dataValues.Count)
                {
                    StartCoroutine(TransitionToNextQuestion());
                }
                else
                {
                    RefreshClipboardList();
                    Invoke("TurnOnScanner", 1.0f);
                }
            }
        }

        private void HandleWrongRow()
        {
            isProcessingScan = true;
            if (wrongSound != null) wrongSound.Play();
            PlayVFX(wrongVfxPrefab, secondaryWrongVfxPrefab, tertiaryWrongVfxPrefabs);

            string catName = "Item";
            if (currentQuestion.categoryNames != null && activeItemIndex < currentQuestion.categoryNames.Count)
                catName = currentQuestion.categoryNames[activeItemIndex];

            QuestionResult result = new QuestionResult();
            result.questionText = catName;
            result.correctAnswer = currentQuestion.dataValues[activeItemIndex].ToString();
            result.userResponse = inputBuffer;
            result.isCorrect = false;
            result.responseTime = Time.time - currentQuestionStartTime;
            sessionResults.Add(result);

            if (!isPracticeMode)
            {
                currentLives--;
                if (currentLives >= 0 && currentLives < hearts.Length && hearts[currentLives] != null) hearts[currentLives].SetActive(false);
                if (currentLives <= 0) { TriggerGameOver(); return; }
            }

            Invoke("RecoverFromWrongAnswer", 1.5f);
        }

        private void RecoverFromWrongAnswer()
        {
            inputBuffer = "";
            lastScannedDigit = ""; // Reset tracker
            RefreshClipboardList();
            TurnOnScanner();
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

        private IEnumerator TransitionToNextQuestion()
        {
            if (unaskedQuestions.Contains(currentQuestionBankIndex)) unaskedQuestions.Remove(currentQuestionBankIndex);
            if (qrScanner != null) qrScanner.StopWork();
            isTimerRunning = false;

            yield return new WaitForSeconds(1.5f);

            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = true;
                float alpha = 0f;
                while (alpha < 1f) { alpha += Time.deltaTime * fadeSpeed; fadeGroup.alpha = Mathf.Clamp01(alpha); yield return null; }
            }

            StartTestQuestion();

            if (fadeGroup != null)
            {
                float alpha = 1f;
                while (alpha > 0f) { alpha -= Time.deltaTime * fadeSpeed; fadeGroup.alpha = Mathf.Clamp01(alpha); yield return null; }
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
                while (alpha < 1f) { alpha += Time.deltaTime * fadeSpeed; fadeGroup.alpha = Mathf.Clamp01(alpha); yield return null; }
            }

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            GenerateScoreSummary();
            if (scoreScreen != null) scoreScreen.SetActive(true);

            if (fadeGroup != null)
            {
                float alpha = 1f;
                while (alpha > 0f) { alpha -= Time.deltaTime * fadeSpeed; fadeGroup.alpha = Mathf.Clamp01(alpha); yield return null; }
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

            int correctCount = 0; int wrongCount = 0;
            foreach (var res in sessionResults) { if (res.isCorrect) correctCount++; else wrongCount++; }
            int totalResponses = sessionResults.Count;
            int score = totalResponses > 0 ? Mathf.RoundToInt(((float)correctCount / totalResponses) * 100f) : 0;
            float activeTime = Time.time - gameStartTime;

            string summary = "========== GAME SCORE SUMMARY ==========\n";
            summary += $"Score: {score}%\nActive Time: {activeTime:F1}s\n\nTotal Responses: {totalResponses}\nCorrect: {correctCount} | Wrong: {wrongCount}\n\n========== QUESTION BREAKDOWN ==========\n\n";

            for (int i = 0; i < sessionResults.Count; i++)
            {
                var res = sessionResults[i];
                summary += $"--- Scan {i + 1} ---\nItem: {res.questionText}\nTarget: {res.correctAnswer} | Scanned: {res.userResponse}\nResult: {(res.isCorrect ? "CORRECT" : "INCORRECT")}\n\n";
            }
            scoreSummaryText.text = summary;
        }

        // --- ALL UI BUTTON METHODS FULLY RESTORED ---
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
            for (int i = 0; i < questionBank.Count; i++) unaskedQuestions.Add(i);

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
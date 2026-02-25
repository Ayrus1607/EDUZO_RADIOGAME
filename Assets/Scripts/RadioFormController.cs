using UnityEngine;
using TMPro; // Required for TextMeshPro
using System.Collections.Generic;
using System;
using Eduzo.Games.Radio.Data;

namespace Eduzo.Games.Radio.UI
{
    public class RadioFormController : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_InputField questionInput;
        public TMP_InputField rightAnswerInput;
        public TMP_InputField wrongAnswerInput;

        [Header("Audio")]
        public AudioSource successSound; // <-- NEW: Positive sound slot
        public AudioSource errorSound;   // <-- NEW: Negative sound slot

        // Stores all questions created
        private List<RadioQuestionData> questionBank = new List<RadioQuestionData>();

        // Safely passes data without using PlayerPrefs (Eduzo rule!)
        public static event Action<List<RadioQuestionData>> OnRadioFormSubmitted;

        public void AddQuestionToList()
        {
            // 1. Check if any box is empty
            if (string.IsNullOrEmpty(questionInput.text) ||
                string.IsNullOrEmpty(rightAnswerInput.text) ||
                string.IsNullOrEmpty(wrongAnswerInput.text))
            {
                Debug.LogWarning("Please fill in all the boxes!");

                // Play negative sound!
                if (errorSound != null) errorSound.Play();

                return;
            }

            // 2. If all boxes are filled, create the question
            RadioQuestionData newQuestion = new RadioQuestionData
            {
                questionText = questionInput.text,
                correctAnswer = rightAnswerInput.text,
                wrongAnswer = wrongAnswerInput.text
            };

            questionBank.Add(newQuestion);
            Debug.Log("Question added! Total questions ready: " + questionBank.Count);

            // Play positive sound!
            if (successSound != null) successSound.Play();

            // Clear boxes for the next question
            questionInput.text = "";
            rightAnswerInput.text = "";
            wrongAnswerInput.text = "";
        }

        public void SubmitFormAndStart()
        {
            // Prevent starting the game if they haven't added any questions
            if (questionBank.Count == 0)
            {
                Debug.LogWarning("You must add at least one question!");

                // Play negative sound!
                if (errorSound != null) errorSound.Play();

                return;
            }

            // Optional: Play a success sound right before leaving the screen
            if (successSound != null) successSound.Play();

            OnRadioFormSubmitted?.Invoke(questionBank);
            Debug.Log("Form submitted! Hiding form screen...");
            gameObject.SetActive(false);
        }
    }
}
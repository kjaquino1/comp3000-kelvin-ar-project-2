using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathQuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_Text feedbackText;
    public TMP_Text scoreText;

    public Button[] answerButtons;
    public TMP_Text[] answerButtonTexts;

    public int minimumNumber = 1;
    public int maximumNumber = 5;

    private int correctAnswer;
    private int score = 0;

    private void Start()
    {
        GenerateQuestion();
        UpdateScoreText();
    }

    public void GenerateQuestion()
    {
        int numberA = Random.Range(minimumNumber, maximumNumber + 1);
        int numberB = Random.Range(minimumNumber, maximumNumber + 1);

        correctAnswer = numberA + numberB;

        questionText.text = numberA + " + " + numberB + " = ?";
        feedbackText.text = "Choose an answer";

        List<int> answers = GenerateAnswerOptions(correctAnswer);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int answerValue = answers[i];

            answerButtonTexts[i].text = answerValue.ToString();

            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(answerValue));
        }
    }

    private List<int> GenerateAnswerOptions(int correct)
    {
        List<int> answers = new List<int>();
        answers.Add(correct);

        while (answers.Count < 3)
        {
            int wrongAnswer = Random.Range(2, 11);

            if (!answers.Contains(wrongAnswer))
            {
                answers.Add(wrongAnswer);
            }
        }

        for (int i = 0; i < answers.Count; i++)
        {
            int randomIndex = Random.Range(i, answers.Count);
            int temporary = answers[i];
            answers[i] = answers[randomIndex];
            answers[randomIndex] = temporary;
        }

        return answers;
    }

    public void CheckAnswer(int selectedAnswer)
    {
        if (selectedAnswer == correctAnswer)
        {
            score++;
            feedbackText.text = "Correct!";
        }
        else
        {
            feedbackText.text = "Incorrect. Answer: " + correctAnswer;
        }

        UpdateScoreText();
        Invoke(nameof(GenerateQuestion), 1.5f);
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }
}
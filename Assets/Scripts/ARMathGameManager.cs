using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARMathGameManager : MonoBehaviour
{
    [Header("AR")]
    public Camera arCamera;
    public ARRaycastManager arRaycastManager;

    [Header("Prefabs")]
    public RobotUnit robotPrefab;
    public AnswerEnemy enemyPrefab;

    [Header("Shooting")]
    public LayerMask enemyLayer;
    public float shootDistance = 30f;

    [Header("Gameplay")]
    public float questionTime = 10f;
    public int startMinNumber = 1;
    public int startMaxNumber = 10;

    private RobotUnit currentRobot;
    private readonly List<AnswerEnemy> activeEnemies = new List<AnswerEnemy>();
    private static readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();

    private bool robotPlaced = false;
    private bool gameActive = false;

    private int correctAnswer;
    private int questionsSolved = 0;

    private float timeRemaining;
    private float maxTime;

    private void Update()
    {
        if (!TapPressedThisFrame())
            return;

        if (!robotPlaced)
        {
            TryPlaceRobot();
        }
        else if (gameActive)
        {
            ShootFromCrosshair();
        }
    }

    private void LateUpdate()
    {
        if (!gameActive || currentRobot == null)
            return;

        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(timeRemaining, 0f);

        currentRobot.UpdateTimer(timeRemaining, maxTime);

        if (timeRemaining <= 0f)
        {
            GameOver();
        }
    }

    private bool TapPressedThisFrame()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonDown(0);
#else
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }

    private Vector2 GetScreenCenter()
    {
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void TryPlaceRobot()
    {
        Vector2 screenCenter = GetScreenCenter();

        if (arRaycastManager.Raycast(screenCenter, arHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = arHits[0].pose;

            currentRobot = Instantiate(robotPrefab, hitPose.position, hitPose.rotation);
            robotPlaced = true;

            StartNextQuestion();
        }
    }

    private void StartNextQuestion()
    {
        ClearEnemies();

        maxTime = questionTime;
        timeRemaining = maxTime;
        gameActive = true;

        string questionText;
        correctAnswer = GenerateQuestion(out questionText);

        if (currentRobot != null)
        {
            currentRobot.SetQuestion(questionText);
            currentRobot.SetStatus("Shoot the correct answer");
            currentRobot.UpdateTimer(timeRemaining, maxTime);
        }

        int optionCount = GetOptionCount();
        SpawnAnswerEnemies(optionCount, correctAnswer);
    }

    private int GetOptionCount()
    {
        if (questionsSolved >= 5)
            return 4;

        if (questionsSolved >= 3)
            return 3;

        return 2;
    }

    private int GenerateQuestion(out string question)
    {
        int a = Random.Range(startMinNumber, startMaxNumber + 1);
        int b = Random.Range(startMinNumber, startMaxNumber + 1);

        bool useSubtraction = Random.value > 0.5f;

        if (useSubtraction)
        {
            if (b > a)
            {
                int temp = a;
                a = b;
                b = temp;
            }

            int answer = a - b;
            question = a + " - " + b + " = ?";
            return answer;
        }

        int sum = a + b;
        question = a + " + " + b + " = ?";
        return sum;
    }

    private void SpawnAnswerEnemies(int count, int correct)
    {
        HashSet<int> answers = new HashSet<int>();
        answers.Add(correct);

        while (answers.Count < count)
        {
            int wrong = correct + Random.Range(-5, 6);

            if (wrong < 0 || wrong == correct)
                continue;

            answers.Add(wrong);
        }

        List<int> answerList = new List<int>(answers);
        Shuffle(answerList);

        Transform orbitCenter = currentRobot.orbitCenter != null ? currentRobot.orbitCenter : currentRobot.transform;

        for (int i = 0; i < answerList.Count; i++)
        {
            float angle = (360f / answerList.Count) * i;

            AnswerEnemy enemy = Instantiate(enemyPrefab, orbitCenter.position, Quaternion.identity);
            enemy.Initialise(answerList[i], orbitCenter, angle);

            activeEnemies.Add(enemy);
        }
    }

    private void ShootFromCrosshair()
    {
        Ray ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance, enemyLayer))
        {
            AnswerEnemy enemy = hit.collider.GetComponentInParent<AnswerEnemy>();

            if (enemy != null)
            {
                HandleEnemyHit(enemy);
            }
        }
    }

    private void HandleEnemyHit(AnswerEnemy enemy)
    {
        if (!gameActive)
            return;

        if (enemy.AnswerValue == correctAnswer)
        {
            questionsSolved++;

            if (currentRobot != null)
            {
                currentRobot.SetStatus("Correct!");
            }

            StartNextQuestion();
        }
        else
        {
            timeRemaining = Mathf.Max(0f, timeRemaining - 1f);

            if (currentRobot != null)
            {
                currentRobot.SetStatus("Wrong! -1 second");
                currentRobot.PlayDamageFlash();
                currentRobot.UpdateTimer(timeRemaining, maxTime);
            }

            activeEnemies.Remove(enemy);
            Destroy(enemy.gameObject);
        }
    }

    private void GameOver()
    {
        gameActive = false;
        ClearEnemies();

        if (currentRobot != null)
        {
            currentRobot.SetQuestion("Time up!");
            currentRobot.SetStatus("Game Over");
            currentRobot.UpdateTimer(0f, maxTime);
        }
    }

    private void ClearEnemies()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                Destroy(activeEnemies[i].gameObject);
            }
        }

        activeEnemies.Clear();
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
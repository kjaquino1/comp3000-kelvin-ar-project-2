using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARMathShooterGame : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager arRaycastManager;
    public ARPlaneManager arPlaneManager;
    public Camera arCamera;

    [Header("Prefabs")]
    public GameObject robotPrefab;
    public GameObject enemyPrefab;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI crosshairText;

    [Header("Game Settings")]
    public int totalQuestions = 10;
    public float questionTime = 10f;
    public float enemyRadius = 1.2f;
    public float enemyHeight = 0.8f;

    private enum GameState
    {
        Scanning,
        ReadyToPlace,
        Playing,
        GameOver
    }

    private GameState state = GameState.Scanning;

    private static readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();

    private GameObject activeRobot;
    private TextMeshPro robotQuestionText;
    private LineRenderer timerRing;

    private int currentQuestionNumber = 0;
    private int score = 0;
    private int correctAnswer = 0;
    private float timer = 0f;
    private bool waitingForNextQuestion = false;

    private readonly List<EnemyData> activeEnemies = new List<EnemyData>();

    private class EnemyData
    {
        public GameObject obj;
        public int answer;
        public bool isCorrect;
        public float angle;
        public float speed;
    }

    private void Start()
    {
        state = GameState.Scanning;

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = "Scanning for landing surface...";
        }

        if (questionText != null)
            questionText.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        if (scoreText != null)
            scoreText.text = "Score: 0";

        if (crosshairText != null)
        {
            crosshairText.gameObject.SetActive(true);
            crosshairText.text = "+";
        }
    }

    private void Update()
    {
        UpdateScanningState();

        if (state == GameState.ReadyToPlace)
        {
            HandleRobotPlacement();
        }
        else if (state == GameState.Playing)
        {
            UpdateTimer();
            UpdateEnemies();
            UpdateRobotQuestionFacingCamera();
            HandleShooting();
        }
    }

    private void UpdateScanningState()
    {
        if (state != GameState.Scanning)
            return;

        bool floorFound = arPlaneManager != null && arPlaneManager.trackables.count > 0;

        if (floorFound)
        {
            state = GameState.ReadyToPlace;

            if (instructionText != null)
                instructionText.text = "Scanning complete, tap to teleport";
        }
    }

    private void HandleRobotPlacement()
    {
        if (!TryGetTapPosition(out Vector2 tapPosition))
            return;

        if (arRaycastManager == null)
            return;

        if (arRaycastManager.Raycast(tapPosition, arHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = arHits[0].pose;

            activeRobot = Instantiate(robotPrefab, hitPose.position, hitPose.rotation);

            // Make robot smaller
            activeRobot.transform.localScale = Vector3.one * 0.35f; SetupRobotQuestionText();
            CreateTimerRing();

            if (instructionText != null)
                instructionText.gameObject.SetActive(false);

            state = GameState.Playing;
            StartNextQuestion();
        }
    }

    private void StartNextQuestion()
    {
        waitingForNextQuestion = false;
        ClearEnemies();

        currentQuestionNumber++;

        if (currentQuestionNumber > totalQuestions)
        {
            EndGame();
            return;
        }

        GenerateQuestion(out string question, out correctAnswer);

        // Do not show question on screen UI
        if (questionText != null)
        {
            questionText.gameObject.SetActive(false);
            questionText.text = "";
        }

        // Show question only above robot head
        if (robotQuestionText != null)
            robotQuestionText.text = question;

        if (feedbackText != null)
            feedbackText.text = "Shoot the correct answer";

        timer = questionTime;

        SpawnAnswerEnemies();
        UpdateScoreText();
    }

    private void GenerateQuestion(out string question, out int answer)
    {
        int operation = Random.Range(0, 4);

        int a;
        int b;

        switch (operation)
        {
            case 0:
                a = Random.Range(1, 11);
                b = Random.Range(1, 11);
                answer = a + b;
                question = $"{a} + {b} = ?";
                break;

            case 1:
                a = Random.Range(5, 21);
                b = Random.Range(1, a);
                answer = a - b;
                question = $"{a} - {b} = ?";
                break;

            case 2:
                a = Random.Range(2, 10);
                b = Random.Range(2, 10);
                answer = a * b;
                question = $"{a} × {b} = ?";
                break;

            default:
                answer = Random.Range(2, 10);
                b = Random.Range(2, 10);
                a = answer * b;
                question = $"{a} ÷ {b} = ?";
                break;
        }
    }

    private int GetEnemyCount()
    {
        if (currentQuestionNumber <= 2)
            return 2;

        if (currentQuestionNumber <= 5)
            return 3;

        if (currentQuestionNumber <= 7)
            return 4;

        return 5;
    }

    private void SpawnAnswerEnemies()
    {
        int enemyCount = GetEnemyCount();
        List<int> answers = GenerateAnswerOptions(enemyCount);

        float baseSpeed = 30f + currentQuestionNumber * 8f;

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemy = CreateEnemyObject(answers[i]);

            EnemyData data = new EnemyData
            {
                obj = enemy,
                answer = answers[i],
                isCorrect = answers[i] == correctAnswer,
                angle = (360f / enemyCount) * i,
                speed = baseSpeed + Random.Range(-8f, 8f)
            };

            activeEnemies.Add(data);
        }
    }

    private List<int> GenerateAnswerOptions(int count)
    {
        List<int> answers = new List<int>();
        answers.Add(correctAnswer);

        while (answers.Count < count)
        {
            int offset = Random.Range(-8, 9);
            int wrongAnswer = correctAnswer + offset;

            if (wrongAnswer < 0)
                wrongAnswer = Mathf.Abs(wrongAnswer) + 1;

            if (wrongAnswer != correctAnswer && !answers.Contains(wrongAnswer))
                answers.Add(wrongAnswer);
        }

        for (int i = 0; i < answers.Count; i++)
        {
            int randomIndex = Random.Range(i, answers.Count);
            int temp = answers[i];
            answers[i] = answers[randomIndex];
            answers[randomIndex] = temp;
        }

        return answers;
    }

    private GameObject CreateEnemyObject(int answer)
    {
        GameObject enemy;

        if (enemyPrefab != null)
        {
            enemy = Instantiate(enemyPrefab);
        }
        else
        {
            enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        }

        enemy.name = "AnswerEnemy_" + answer;

        Collider enemyCollider = enemy.GetComponent<Collider>();

        if (enemyCollider == null)
            enemy.AddComponent<SphereCollider>();

        // Remove old answer label if prefab already has one
        Transform oldLabel = FindChildRecursive(enemy.transform, "AnswerLabel");

        if (oldLabel != null)
            Destroy(oldLabel.gameObject);

        // Create clear 3D number above enemy
        GameObject labelObj = new GameObject("AnswerLabel");
        labelObj.transform.SetParent(enemy.transform);
        // Put answer on the enemy body, not above it
        labelObj.transform.localPosition = new Vector3(0f, 0.05f, -0.32f);
        labelObj.transform.localRotation = Quaternion.identity;
        labelObj.transform.localScale = Vector3.one * 0.32f;

        TextMeshPro label = labelObj.AddComponent<TextMeshPro>();
        label.text = answer.ToString();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 10;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.outlineColor = Color.black;
        label.outlineWidth = 0.25f;

        MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();

        if (labelRenderer != null)
            labelRenderer.sortingOrder = 10;

        Renderer renderer = enemy.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.7f, 0.1f, 1f);
            renderer.material = material;
        }

        return enemy;
    }

    private void UpdateEnemies()
    {
        if (activeRobot == null || arCamera == null)
            return;

        Vector3 center = activeRobot.transform.position;

        foreach (EnemyData enemy in activeEnemies)
        {
            if (enemy.obj == null)
                continue;

            enemy.angle += enemy.speed * Time.deltaTime;

            float radians = enemy.angle * Mathf.Deg2Rad;
            float floatOffset = Mathf.Sin(Time.time * 2f + radians) * 0.15f;

            Vector3 position = center + new Vector3(
                Mathf.Cos(radians) * enemyRadius,
                enemyHeight + floatOffset,
                Mathf.Sin(radians) * enemyRadius
            );

            enemy.obj.transform.position = position;
            enemy.obj.transform.LookAt(arCamera.transform);
            enemy.obj.transform.Rotate(0, 180f, 0);

            Transform label = FindChildRecursive(enemy.obj.transform, "AnswerLabel");

            if (label != null)
            {
                label.LookAt(arCamera.transform);
                label.Rotate(0, 180f, 0);
            }
        }
    }

    private void HandleShooting()
    {
        if (!TryGetTapPosition(out _))
            return;

        if (arCamera == null)
            return;

        Ray ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            EnemyData hitEnemy = GetHitEnemy(hit.collider.gameObject);

            if (hitEnemy != null)
                ProcessEnemyHit(hitEnemy);
        }
    }

    private EnemyData GetHitEnemy(GameObject hitObject)
    {
        foreach (EnemyData enemy in activeEnemies)
        {
            if (enemy.obj == null)
                continue;

            if (hitObject == enemy.obj || hitObject.transform.IsChildOf(enemy.obj.transform))
                return enemy;
        }

        return null;
    }

    private void ProcessEnemyHit(EnemyData enemy)
    {
        if (waitingForNextQuestion)
            return;

        if (enemy.isCorrect)
        {
            score++;

            if (feedbackText != null)
                feedbackText.text = "Correct!";

            Destroy(enemy.obj);
            activeEnemies.Remove(enemy);

            UpdateScoreText();
            StartCoroutine(NextQuestionAfterDelay(0.8f));
        }
        else
        {
            timer = Mathf.Max(0f, timer - 1f);

            if (feedbackText != null)
                feedbackText.text = "Wrong! -1 second";

            Destroy(enemy.obj);
            activeEnemies.Remove(enemy);
        }
    }

    private IEnumerator NextQuestionAfterDelay(float delay)
    {
        waitingForNextQuestion = true;
        yield return new WaitForSeconds(delay);
        StartNextQuestion();
    }

    private void UpdateTimer()
    {
        timer -= Time.deltaTime;

        UpdateTimerRing();

        if (timer <= 0f && !waitingForNextQuestion)
        {
            timer = 0f;

            if (feedbackText != null)
                feedbackText.text = "Time up!";

            StartCoroutine(NextQuestionAfterDelay(0.8f));
        }
    }

    private void CreateTimerRing()
    {
        if (activeRobot == null)
            return;

        GameObject ringObj = new GameObject("TimerRing");
        ringObj.transform.position = activeRobot.transform.position + Vector3.up * 0.02f;

        timerRing = ringObj.AddComponent<LineRenderer>();
        timerRing.useWorldSpace = true;
        timerRing.loop = false;
        timerRing.widthMultiplier = 0.06f;
        timerRing.positionCount = 64;
        timerRing.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void UpdateTimerRing()
    {
        if (timerRing == null || activeRobot == null)
            return;

        float percent = Mathf.Clamp01(timer / questionTime);
        int segments = Mathf.Max(2, Mathf.RoundToInt(64 * percent));

        timerRing.positionCount = segments;

        Color ringColor = Color.green;

        if (timer <= 5f && timer > 2f)
            ringColor = new Color(1f, 0.5f, 0f);

        if (timer <= 2f)
            ringColor = Color.red;

        timerRing.startColor = ringColor;
        timerRing.endColor = ringColor;

        Vector3 center = activeRobot.transform.position + Vector3.up * 0.03f;
        float radius = 0.65f;

        for (int i = 0; i < segments; i++)
        {
            float angle = ((float)i / 63f) * Mathf.PI * 2f * percent;
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            timerRing.SetPosition(i, point);
        }
    }

    private void SetupRobotQuestionText()
    {
        if (activeRobot == null)
            return;

        Transform existing = FindChildRecursive(activeRobot.transform, "QuestionText3D");

        if (existing != null)
        {
            robotQuestionText = existing.GetComponent<TextMeshPro>();
            return;
        }

        GameObject textObj = new GameObject("QuestionText3D");
        textObj.transform.SetParent(activeRobot.transform);

        // Above robot head
        textObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.35f;

        robotQuestionText = textObj.AddComponent<TextMeshPro>();
        robotQuestionText.alignment = TextAlignmentOptions.Center;
        robotQuestionText.fontSize = 7;
        robotQuestionText.color = Color.white;
        robotQuestionText.fontStyle = FontStyles.Bold;
        robotQuestionText.outlineColor = Color.black;
        robotQuestionText.outlineWidth = 0.25f;
        robotQuestionText.text = "";
    }

    private void UpdateRobotQuestionFacingCamera()
    {
        if (robotQuestionText == null || arCamera == null)
            return;

        robotQuestionText.transform.LookAt(arCamera.transform);
        robotQuestionText.transform.Rotate(0, 180f, 0);
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private bool TryGetTapPosition(out Vector2 position)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }

    private void ClearEnemies()
    {
        foreach (EnemyData enemy in activeEnemies)
        {
            if (enemy.obj != null)
                Destroy(enemy.obj);
        }

        activeEnemies.Clear();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    private void EndGame()
    {
        state = GameState.GameOver;
        ClearEnemies();

        if (timerRing != null)
            Destroy(timerRing.gameObject);

        if (questionText != null)
            questionText.text = "Game complete";

        if (robotQuestionText != null)
            robotQuestionText.text = "Game complete";

        if (feedbackText != null)
            feedbackText.text = "Final score: " + score + "/" + totalQuestions;
    }
}
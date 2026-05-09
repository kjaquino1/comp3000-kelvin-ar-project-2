using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

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
    public GameObject topNotchPanel;

    [Header("Title Screen")]
    public GameObject titlePanel;
    public Button additionButton;
    public Button subtractionButton;
    public Button multiplicationButton;
    public Button divisionButton;
    public Button backButton;

    [Header("End Screen")]
    public GameObject endPanel;
    public TextMeshProUGUI finalScoreText;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Game Settings")]
    public int totalQuestions = 10;
    public float questionTime = 10f;
    public float enemyRadius = 0.75f;
    public float enemyHeight = 0.55f;
    public float robotScale = 0.18f;
    public float enemyScale = 0.28f;

    [Header("Shooting Effect")]
    public float shootLineDuration = 0.15f;
    public float enemyDropDuration = 0.45f;
    public float enemyDropDistance = 1.2f;

    private enum GameState
    {
        TitleScreen,
        Scanning,
        ReadyToPlace,
        Playing,
        GameOver
    }

    private enum OperationMode
    {
        Addition,
        Subtraction,
        Multiplication,
        Division
    }

    private GameState state = GameState.TitleScreen;
    private OperationMode selectedMode = OperationMode.Addition;

    private static readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();

    private GameObject activeRobot;
    private TextMeshPro robotQuestionText;
    private GameObject questionBackgroundObj;
    private LineRenderer timerRing;

    private int currentQuestionNumber = 0;
    private int score = 0;
    private int correctAnswer = 0;
    private float timer = 0f;
    private bool waitingForNextQuestion = false;
    private bool placementInProgress = false;

    private readonly List<EnemyData> activeEnemies = new List<EnemyData>();

    private class EnemyData
    {
        public GameObject obj;
        public TextMeshPro label;
        public int answer;
        public bool isCorrect;
        public float angle;
        public float speed;
    }

    private void Start()
    {
        ApplyMobileUILayout();

        if (additionButton != null)
        {
            additionButton.onClick.RemoveAllListeners();
            additionButton.onClick.AddListener(StartAdditionMode);
        }

        if (subtractionButton != null)
        {
            subtractionButton.onClick.RemoveAllListeners();
            subtractionButton.onClick.AddListener(StartSubtractionMode);
        }

        if (multiplicationButton != null)
        {
            multiplicationButton.onClick.RemoveAllListeners();
            multiplicationButton.onClick.AddListener(StartMultiplicationMode);
        }

        if (divisionButton != null)
        {
            divisionButton.onClick.RemoveAllListeners();
            divisionButton.onClick.AddListener(StartDivisionMode);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        ShowTitleScreen();
    }

    private void ApplyMobileUILayout()
    {
        if (topNotchPanel != null)
        {
            RectTransform notch = topNotchPanel.GetComponent<RectTransform>();

            if (notch != null)
            {
                notch.anchorMin = new Vector2(0f, 1f);
                notch.anchorMax = new Vector2(1f, 1f);
                notch.pivot = new Vector2(0.5f, 1f);
                notch.anchoredPosition = new Vector2(0f, -95f);
                notch.sizeDelta = new Vector2(0f, 90f);
            }
        }

        if (instructionText != null)
        {
            RectTransform instructionRect = instructionText.GetComponent<RectTransform>();

            if (instructionRect != null)
            {
                instructionRect.anchorMin = new Vector2(0f, 0f);
                instructionRect.anchorMax = new Vector2(1f, 1f);
                instructionRect.pivot = new Vector2(0.5f, 0.5f);
                instructionRect.offsetMin = new Vector2(130f, 0f);
                instructionRect.offsetMax = new Vector2(-20f, 0f);
            }

            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.fontSize = 28;
            instructionText.color = Color.black;
            instructionText.enableWordWrapping = false;
        }

        if (backButton != null)
        {
            RectTransform backRect = backButton.GetComponent<RectTransform>();

            if (backRect != null)
            {
                backRect.anchorMin = new Vector2(0f, 0.5f);
                backRect.anchorMax = new Vector2(0f, 0.5f);
                backRect.pivot = new Vector2(0f, 0.5f);
                backRect.anchoredPosition = new Vector2(10f, 0f);
                backRect.sizeDelta = new Vector2(120f, 80f);
            }

            TextMeshProUGUI backText = backButton.GetComponentInChildren<TextMeshProUGUI>();

            if (backText != null)
            {
                backText.text = "<";
                backText.fontSize = 58;
                backText.fontStyle = FontStyles.Bold;
                backText.alignment = TextAlignmentOptions.Center;
                backText.color = Color.black;
            }
        }

        if (scoreText != null)
        {
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();

            if (scoreRect != null)
            {
                scoreRect.anchorMin = new Vector2(0f, 0f);
                scoreRect.anchorMax = new Vector2(0f, 0f);
                scoreRect.pivot = new Vector2(0f, 0f);
                scoreRect.anchoredPosition = new Vector2(30f, 30f);
                scoreRect.sizeDelta = new Vector2(350f, 80f);
            }

            scoreText.alignment = TextAlignmentOptions.Left;
            scoreText.fontSize = 36;
            scoreText.color = Color.white;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.outlineColor = Color.black;
            scoreText.outlineWidth = 0.35f;
        }
    }

    private void ShowTitleScreen()
    {
        state = GameState.TitleScreen;

        ResetRuntimeObjects();

        if (titlePanel != null)
            titlePanel.SetActive(true);

        if (endPanel != null)
            endPanel.SetActive(false);

        if (topNotchPanel != null)
            topNotchPanel.SetActive(false);

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
            instructionText.text = "";
        }

        if (questionText != null)
        {
            questionText.gameObject.SetActive(false);
            questionText.text = "";
        }

        if (feedbackText != null)
            feedbackText.text = "";

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
            scoreText.text = "Score: 0";
        }

        if (crosshairText != null)
        {
            crosshairText.gameObject.SetActive(false);
            crosshairText.text = "+";
        }
    }

    private void StartAdditionMode()
    {
        selectedMode = OperationMode.Addition;
        BeginScanning();
    }

    private void StartSubtractionMode()
    {
        selectedMode = OperationMode.Subtraction;
        BeginScanning();
    }

    private void StartMultiplicationMode()
    {
        selectedMode = OperationMode.Multiplication;
        BeginScanning();
    }

    private void StartDivisionMode()
    {
        selectedMode = OperationMode.Division;
        BeginScanning();
    }

    private void BeginScanning()
    {
        ResetRuntimeObjects();

        state = GameState.Scanning;
        placementInProgress = false;
        currentQuestionNumber = 0;
        score = 0;
        timer = 0f;
        waitingForNextQuestion = false;

        ApplyMobileUILayout();

        if (titlePanel != null)
            titlePanel.SetActive(false);

        if (endPanel != null)
            endPanel.SetActive(false);

        if (topNotchPanel != null)
            topNotchPanel.SetActive(true);

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = "Scanning for landing surface...";
        }

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = "Score: 0";
        }

        if (crosshairText != null)
        {
            crosshairText.gameObject.SetActive(true);
            crosshairText.text = "+";
        }

        if (feedbackText != null)
            feedbackText.text = "";
    }

    private void PlayAgain()
    {
        BeginScanning();
    }

    private void ReturnToMainMenu()
    {
        ShowTitleScreen();
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
                instructionText.text = "Aim crosshair at floor, then tap";
        }
    }

    private void HandleRobotPlacement()
    {
        if (state != GameState.ReadyToPlace)
            return;

        if (activeRobot != null || placementInProgress)
            return;

        if (!TryGetTapPosition(out _))
            return;

        if (arRaycastManager == null)
            return;

        Vector2 crosshairPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (!arRaycastManager.Raycast(crosshairPosition, arHits, TrackableType.PlaneWithinPolygon))
        {
            if (instructionText != null)
                instructionText.text = "Aim the crosshair at the floor";

            return;
        }

        placementInProgress = true;
        state = GameState.Playing;

        Pose hitPose = arHits[0].pose;

        activeRobot = Instantiate(robotPrefab, hitPose.position, hitPose.rotation);
        activeRobot.transform.localScale = Vector3.one * robotScale;

        SetupRobotQuestionText();
        CreateTimerRing();

        if (instructionText != null)
            instructionText.text = "Hit the correct answer";

        StartNextQuestion();
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

        if (questionText != null)
        {
            questionText.gameObject.SetActive(false);
            questionText.text = "";
        }

        if (robotQuestionText != null)
            robotQuestionText.text = question;

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = "Hit the correct answer";
        }

        timer = questionTime;

        SpawnAnswerEnemies();
        UpdateScoreText();
    }

    private void GenerateQuestion(out string question, out int answer)
    {
        int a;
        int b;

        switch (selectedMode)
        {
            case OperationMode.Addition:
                a = Random.Range(1, 11);
                b = Random.Range(1, 11);
                answer = a + b;
                question = $"{a} + {b} = ?";
                break;

            case OperationMode.Subtraction:
                a = Random.Range(5, 21);
                b = Random.Range(1, a);
                answer = a - b;
                question = $"{a} - {b} = ?";
                break;

            case OperationMode.Multiplication:
                a = Random.Range(2, 10);
                b = Random.Range(2, 10);
                answer = a * b;
                question = $"{a} × {b} = ?";
                break;

            case OperationMode.Division:
                answer = Random.Range(2, 10);
                b = Random.Range(2, 10);
                a = answer * b;
                question = $"{a} ÷ {b} = ?";
                break;

            default:
                a = Random.Range(1, 11);
                b = Random.Range(1, 11);
                answer = a + b;
                question = $"{a} + {b} = ?";
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
        if (activeRobot == null)
            return;

        int enemyCount = GetEnemyCount();
        List<int> answers = GenerateAnswerOptions(enemyCount);

        float baseSpeed = 25f + currentQuestionNumber * 6f;

        for (int i = 0; i < enemyCount; i++)
        {
            TextMeshPro label;
            GameObject enemy = CreateEnemyObject(answers[i], out label);

            EnemyData data = new EnemyData
            {
                obj = enemy,
                label = label,
                answer = answers[i],
                isCorrect = answers[i] == correctAnswer,
                angle = (360f / enemyCount) * i,
                speed = baseSpeed + Random.Range(-5f, 5f)
            };

            activeEnemies.Add(data);
        }

        UpdateEnemies();
    }

    private List<int> GenerateAnswerOptions(int count)
    {
        List<int> answers = new List<int>();
        answers.Add(correctAnswer);

        while (answers.Count < count)
        {
            int offset = Random.Range(-8, 9);

            if (offset == 0)
                continue;

            int wrongAnswer = correctAnswer + offset;

            if (wrongAnswer < 0)
                wrongAnswer = Mathf.Abs(wrongAnswer) + 1;

            if (!answers.Contains(wrongAnswer))
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

    private GameObject CreateEnemyObject(int answer, out TextMeshPro label)
    {
        GameObject enemy;

        if (enemyPrefab != null)
        {
            enemy = Instantiate(enemyPrefab);
        }
        else
        {
            enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        enemy.name = "AnswerEnemy_" + answer;
        enemy.SetActive(true);
        enemy.transform.localScale = Vector3.one * enemyScale;

        Collider enemyCollider = enemy.GetComponent<Collider>();

        if (enemyCollider == null)
            enemyCollider = enemy.AddComponent<SphereCollider>();

        enemyCollider.enabled = true;

        Renderer enemyRenderer = enemy.GetComponent<Renderer>();

        if (enemyRenderer == null)
            enemyRenderer = enemy.GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (material.shader == null)
                material = new Material(Shader.Find("Standard"));

            material.color = new Color(0.7f, 0.1f, 1f);
            enemyRenderer.material = material;
        }

        GameObject labelObj = new GameObject("AnswerLabel_" + answer);
        label = labelObj.AddComponent<TextMeshPro>();
        label.text = answer.ToString();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 8;
        label.color = Color.black;
        label.fontStyle = FontStyles.Bold;
        label.outlineColor = Color.white;
        label.outlineWidth = 0.25f;
        label.transform.localScale = Vector3.one * 0.09f;

        return enemy;
    }

    private void UpdateEnemies()
    {
        if (activeRobot == null || arCamera == null)
            return;

        Vector3 center = activeRobot.transform.position;

        float radius = Mathf.Clamp(enemyRadius, 0.45f, 0.85f);
        float height = Mathf.Clamp(enemyHeight, 0.35f, 0.65f);

        foreach (EnemyData enemy in activeEnemies)
        {
            if (enemy.obj == null)
                continue;

            enemy.angle += enemy.speed * Time.deltaTime;

            float radians = enemy.angle * Mathf.Deg2Rad;
            float floatOffset = Mathf.Sin(Time.time * 2f + radians) * 0.12f;

            Vector3 enemyPosition = center + new Vector3(
                Mathf.Cos(radians) * radius,
                height + floatOffset,
                Mathf.Sin(radians) * radius
            );

            enemy.obj.transform.position = enemyPosition;
            enemy.obj.transform.LookAt(arCamera.transform);
            enemy.obj.transform.Rotate(0f, 180f, 0f);

            if (enemy.label != null)
            {
                Vector3 directionToCamera = (arCamera.transform.position - enemyPosition).normalized;
                enemy.label.transform.position = enemyPosition + directionToCamera * 0.18f;
                enemy.label.transform.LookAt(arCamera.transform);
                enemy.label.transform.Rotate(0f, 180f, 0f);
            }
        }
    }

    private void HandleShooting()
    {
        if (waitingForNextQuestion)
            return;

        if (!TryGetTapPosition(out _))
            return;

        if (arCamera == null)
            return;

        Ray ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 lineStart = arCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.15f, 0.4f));
        Vector3 lineEnd = arCamera.transform.position + arCamera.transform.forward * 8f;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            lineEnd = hit.point;

            EnemyData hitEnemy = GetHitEnemy(hit.collider.gameObject);

            if (hitEnemy != null)
            {
                StartCoroutine(ShootLineEffect(lineStart, hit.point));
                ProcessEnemyHit(hitEnemy);
                return;
            }
        }

        StartCoroutine(ShootLineEffect(lineStart, lineEnd));
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

        activeEnemies.Remove(enemy);

        if (enemy.isCorrect)
        {
            waitingForNextQuestion = true;
            score++;

            if (instructionText != null)
                instructionText.text = "Correct!";

            UpdateScoreText();

            StartCoroutine(DropEnemyThenDestroy(enemy));
            StartCoroutine(NextQuestionAfterDelay(0.8f));
        }
        else
        {
            timer = Mathf.Max(0f, timer - 1f);

            if (instructionText != null)
                instructionText.text = "Wrong! -1 second";

            StartCoroutine(DropEnemyThenDestroy(enemy));
        }
    }

    private IEnumerator NextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextQuestion();
    }

    private void UpdateTimer()
    {
        if (waitingForNextQuestion)
            return;

        timer -= Time.deltaTime;

        UpdateTimerRing();

        if (timer <= 0f)
        {
            timer = 0f;
            waitingForNextQuestion = true;

            if (instructionText != null)
                instructionText.text = "Time's up!";

            StartCoroutine(NextQuestionAfterDelay(0.8f));
        }
    }

    private void CreateTimerRing()
    {
        if (activeRobot == null)
            return;

        if (timerRing != null)
            Destroy(timerRing.gameObject);

        GameObject ringObj = new GameObject("TimerRing");
        ringObj.transform.position = activeRobot.transform.position + Vector3.up * 0.02f;

        timerRing = ringObj.AddComponent<LineRenderer>();
        timerRing.useWorldSpace = true;
        timerRing.loop = false;
        timerRing.widthMultiplier = 0.04f;
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
        float radius = 0.38f;

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

        if (questionBackgroundObj != null)
            Destroy(questionBackgroundObj);

        questionBackgroundObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        questionBackgroundObj.name = "QuestionBackground3D";

        Collider bgCollider = questionBackgroundObj.GetComponent<Collider>();

        if (bgCollider != null)
            Destroy(bgCollider);

        Renderer bgRenderer = questionBackgroundObj.GetComponent<Renderer>();

        if (bgRenderer != null)
        {
            Shader bgShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (bgShader == null)
                bgShader = Shader.Find("Sprites/Default");

            Material bgMaterial = new Material(bgShader);
            bgMaterial.color = Color.white;
            bgRenderer.material = bgMaterial;
        }

        GameObject textObj = new GameObject("QuestionText3D");
        robotQuestionText = textObj.AddComponent<TextMeshPro>();
        robotQuestionText.alignment = TextAlignmentOptions.Center;
        robotQuestionText.fontSize = 8;
        robotQuestionText.color = Color.black;
        robotQuestionText.fontStyle = FontStyles.Bold;
        robotQuestionText.text = "";
        robotQuestionText.transform.localScale = Vector3.one * 0.09f;
    }

    private void UpdateRobotQuestionFacingCamera()
    {
        if (robotQuestionText == null || arCamera == null || activeRobot == null)
            return;

        Vector3 textPosition = activeRobot.transform.position + Vector3.up * 0.48f;
        Vector3 bgPosition = activeRobot.transform.position + Vector3.up * 0.47f;

        robotQuestionText.transform.position = textPosition;
        robotQuestionText.transform.LookAt(arCamera.transform);
        robotQuestionText.transform.Rotate(0f, 180f, 0f);

        if (questionBackgroundObj != null)
        {
            questionBackgroundObj.transform.position = bgPosition;
            questionBackgroundObj.transform.localScale = new Vector3(0.65f, 0.20f, 1f);
            questionBackgroundObj.transform.LookAt(arCamera.transform);
            questionBackgroundObj.transform.Rotate(0f, 180f, 0f);
        }
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

            if (enemy.label != null)
                Destroy(enemy.label.gameObject);
        }

        activeEnemies.Clear();
    }

    private void ResetRuntimeObjects()
    {
        ClearEnemies();

        if (activeRobot != null)
            Destroy(activeRobot);

        if (robotQuestionText != null)
            Destroy(robotQuestionText.gameObject);

        if (questionBackgroundObj != null)
            Destroy(questionBackgroundObj);

        if (timerRing != null)
            Destroy(timerRing.gameObject);

        activeRobot = null;
        robotQuestionText = null;
        questionBackgroundObj = null;
        timerRing = null;
        placementInProgress = false;
        waitingForNextQuestion = false;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    private IEnumerator ShootLineEffect(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("ShootLineEffect");

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = 0.08f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.yellow;
        line.endColor = Color.red;

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        GameObject impactObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impactObj.name = "ShootImpactEffect";
        impactObj.transform.position = end;
        impactObj.transform.localScale = Vector3.one * 0.10f;

        Collider impactCollider = impactObj.GetComponent<Collider>();

        if (impactCollider != null)
            Destroy(impactCollider);

        Renderer impactRenderer = impactObj.GetComponent<Renderer>();

        if (impactRenderer != null)
        {
            Material impactMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (impactMaterial.shader == null)
                impactMaterial = new Material(Shader.Find("Standard"));

            impactMaterial.color = Color.yellow;
            impactRenderer.material = impactMaterial;
        }

        yield return new WaitForSeconds(shootLineDuration);

        Destroy(lineObj);
        Destroy(impactObj);
    }

    private IEnumerator DropEnemyThenDestroy(EnemyData enemy)
    {
        if (enemy == null || enemy.obj == null)
            yield break;

        Vector3 startPosition = enemy.obj.transform.position;
        Vector3 endPosition = startPosition + Vector3.down * enemyDropDistance;

        float elapsed = 0f;

        while (elapsed < enemyDropDuration)
        {
            if (enemy.obj == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / enemyDropDuration;

            enemy.obj.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            enemy.obj.transform.Rotate(0f, 0f, 720f * Time.deltaTime);

            if (enemy.label != null)
            {
                enemy.label.transform.position = enemy.obj.transform.position + Vector3.up * 0.05f;
            }

            yield return null;
        }

        if (enemy.obj != null)
            Destroy(enemy.obj);

        if (enemy.label != null)
            Destroy(enemy.label.gameObject);
    }

    private void EndGame()
    {
        state = GameState.GameOver;
        ClearEnemies();

        if (timerRing != null)
            Destroy(timerRing.gameObject);

        if (robotQuestionText != null)
            robotQuestionText.text = "";

        if (questionBackgroundObj != null)
            Destroy(questionBackgroundObj);

        if (topNotchPanel != null)
            topNotchPanel.SetActive(false);

        if (instructionText != null)
            instructionText.gameObject.SetActive(false);

        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (crosshairText != null)
            crosshairText.gameObject.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";

        if (questionText != null)
        {
            questionText.gameObject.SetActive(false);
            questionText.text = "";
        }

        if (finalScoreText != null)
            finalScoreText.text = "Final Score: " + score + "/" + totalQuestions;

        if (endPanel != null)
            endPanel.SetActive(true);
    }
}
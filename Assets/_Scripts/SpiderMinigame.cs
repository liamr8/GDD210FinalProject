using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SpiderMinigame : MonoBehaviour
{
    public RectTransform spiderBook;
    public RectTransform spiderBookParent;
    public RectTransform spiderLocalParticleTransform;
    public RectTransform spiderWorldSpaceParticleTransform;


    [Header("Difficulty Settings")]
    public int baseMinimumAmountOfSpiders = 900;
    public int maximumAmountOfAdditionalSpidersFromDifficulty = 500;

    public float baseDifficultyTimeToLose = 10f;
    public float maximumDifficultyTimeToLose = 7f;
    public float baseDifficultyShakePower = 9.2f;
    public float maximumDifficultyShakePower = 5f;

    [Header("Timers")]
    [SerializeField]float lossTimer = 0;
    public float lossTimerLimit;

    public UnityEngine.UI.Image timerBarFill;

    [Header("Minigame Values")]

    public float shakePowerMultiplier = 10;
    [SerializeField] int originalNumberOfSpiders;
    [SerializeField] int numberOfSpiders = 900;

    [SerializeField]int amountOfSpidersToAffect;
    [SerializeField] int amountOfSpidersToAffectMin = 10;
    [SerializeField] int amountOfSpidersToAffectMax = 85;
    [SerializeField] float shakePowerSuccessThreshold;

    [SerializeField] float shakeCooldownTimer = 0;
    [SerializeField] float shakeCooldownTimerLimit;
    [SerializeField] Vector2 shakeVector;
    [SerializeField] float visibleBookShakeMultiplier;

    [Header("Audio")]
    public AudioSource shakeSoundSource;
    ParticleSystem localPs;
    ParticleSystem worldSpacePs;

    Vector2 originalBookParentLocalPosition;

    bool minigameEndStateDebounce = false;

    GameManager gm;
    MinigameManager mm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gm = GameManager.Instance;
        mm = gm.GetService<MinigameManager>();
        localPs = spiderLocalParticleTransform.GetComponent<ParticleSystem>();
        worldSpacePs = spiderWorldSpaceParticleTransform.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule localPsmain = localPs.main;
        localPsmain.maxParticles = baseMinimumAmountOfSpiders + (int)UnityEngine.Random.Range(0, Mathf.Lerp(0, maximumAmountOfAdditionalSpidersFromDifficulty, mm.GetCurrentDifficultyValue()));
        lossTimerLimit = Mathf.Lerp(baseDifficultyTimeToLose, maximumDifficultyTimeToLose, mm.GetCurrentDifficultyValue());
        shakePowerMultiplier = Mathf.Lerp(baseDifficultyShakePower, maximumDifficultyShakePower, mm.GetCurrentDifficultyValue());
        numberOfSpiders = localPs.main.maxParticles;
        originalNumberOfSpiders = numberOfSpiders;
        originalBookParentLocalPosition = spiderBookParent.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.LogWarning(Screen.orientation);
        if(!minigameEndStateDebounce) {
            GetPlayerInput();
            ShakeBook();
            //ApplySpiderGravity();
            if (lossTimer > lossTimerLimit)
                LoseGame();
            else if (numberOfSpiders <= 0)
            {
                WinGame();
                return;
            }
            AdvanceTimers();
        }
        timerBarFill.fillAmount = Mathf.InverseLerp(0, lossTimerLimit, lossTimerLimit-lossTimer);

    }
    

    Vector2 GetPlayerInput()
    {

        Vector2 phoneAccelVector = Input.gyro.userAcceleration;
        Debug.Log(phoneAccelVector);
        
        shakeVector = new Vector2(-phoneAccelVector.x, -phoneAccelVector.y); //MAGICAL NEGATIVE OOOOO FUCK MOBILE INPUTS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        return shakeVector;
    }



    bool particlesLoaded = false;

    [SerializeField] float spiderLaunchMaxAngle = 30f;
    Vector2 FilterDeadZone(Vector2 v, float threshold)
    {
        return v.sqrMagnitude < threshold * threshold ? Vector3.zero : v;
    }
    private void ShakeBook()
    {

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[localPs.main.maxParticles];
        int particleCount = localPs.GetParticles(particles);
        numberOfSpiders = particleCount;
        if (!particlesLoaded && particleCount == originalNumberOfSpiders)
        {
            Debug.Log("Particle count is at max original number");
            ParticleSystem.EmissionModule em = localPs.emission;
            em.rateOverTime = 0;
            ParticleSystem.MainModule main = localPs.main;
            main.loop = false;
            particlesLoaded = true;
        }
        else if(!particlesLoaded)
            return;

        if(numberOfSpiders > 0)
        {
            //visibly shaking book according to shake vector
            spiderBookParent.localPosition = originalBookParentLocalPosition + FilterDeadZone(shakeVector, 0.03f) * visibleBookShakeMultiplier;
            
            if(shakeCooldownTimer > shakeCooldownTimerLimit 
            && shakeVector.magnitude * shakePowerMultiplier > shakePowerSuccessThreshold)
            {
                float _audioModifierValue = Mathf.InverseLerp(shakePowerSuccessThreshold, shakePowerSuccessThreshold * 1.5f, shakeVector.magnitude * shakePowerMultiplier);
                shakeSoundSource.pitch = UnityEngine.Random.Range(0.4f + Mathf.Lerp(0, 0.2f, _audioModifierValue), 0.5f + Mathf.Lerp(0, 0.6f, _audioModifierValue));
                shakeSoundSource.volume = UnityEngine.Random.Range(0.65f + Mathf.Lerp(0, 0.35f, _audioModifierValue), 0.9f + Mathf.Lerp(0, 0.3f, _audioModifierValue));
                shakeSoundSource.Play();

                amountOfSpidersToAffect = Mathf.Clamp(UnityEngine.Random.Range(amountOfSpidersToAffectMin, amountOfSpidersToAffectMax + 1), 0, numberOfSpiders);

                shakeCooldownTimer = 0;

                int startIndex = originalNumberOfSpiders - numberOfSpiders;
                int endIndex = startIndex + amountOfSpidersToAffect;

                endIndex = amountOfSpidersToAffect;
                endIndex = Mathf.Clamp(endIndex, 0, particleCount); //to prevent OutOfBoundsException

                for (int i = 0; i < endIndex; i++)
                {
                    // 1) Local position on the book system
                    Vector3 localPos = particles[i].position;
                    Debug.Log("particle local pos"+ i+" "+localPos);

                    // 2) Convert to world position
                    Vector3 worldPos = localPs.transform.TransformPoint(localPos);
                    Debug.Log("particle world pos" + i + " " + worldPos);
                    // 3) Launch velocity based on shake

                    // Random angle around Z (2D screen)
                    float angle = UnityEngine.Random.Range(-spiderLaunchMaxAngle, spiderLaunchMaxAngle);
                    Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);

                    Vector3 variedDir = rot * shakeVector.normalized;

                    // Optional: small random speed variation
                    float speedJitter = UnityEngine.Random.Range(0.8f, 1.2f);

                    Vector3 launchVelocity = variedDir * speedJitter * shakePowerMultiplier;

                    // 4) Emit a matching spider into the falling system
                    var emitParams = new ParticleSystem.EmitParams();
                    emitParams.position = worldPos;
                    emitParams.velocity = launchVelocity;
                    emitParams.startSize = particles[i].GetCurrentSize(localPs);
                    emitParams.startColor = particles[i].GetCurrentColor(localPs);

                    worldSpacePs.Emit(emitParams, 1);

                    // 5) Kill this spider in the book system
                    particles[i].remainingLifetime = 0f;

                }
                numberOfSpiders = particleCount;  // i paranoid
                localPs.SetParticles(particles, particleCount);
            }
        }
    }

    /*private void ApplySpiderGravity()
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
        int count = ps.GetParticles(particles);

        Vector3 gravity = new Vector3(0f, -9.81f, 0f); // world gravity or your own

        for (int i = 0; i < count; i++)
        {
            // If marked (alpha < 1.0), apply gravity
            if (particles[i].startColor.a < 0.99f)
            {
                particles[i].velocity += gravity * Time.deltaTime;
            }
        }

        ps.SetParticles(particles, count);
    }*/

    Coroutine minigameEndSequence = null;
    void WinGame()
    {
        minigameEndStateDebounce = true;
        Debug.LogWarning("player won");
        minigameEndSequence ??= StartCoroutine(WinGameCoroutine());
    }
    
    void LoseGame()
    {
        minigameEndStateDebounce = true;
        Debug.LogError("player lost");
        minigameEndSequence ??= StartCoroutine(LoseGameCoroutine());
    }
    
    System.Collections.IEnumerator WinGameCoroutine()
    {
        MinigameManager mm = GameManager.Instance.GetService<MinigameManager>();
        mm.OnPlayerWinMinigame();
        yield return new WaitForSeconds(3);
        mm.PlayerWinExitMinigame();
        yield return new WaitUntil(() => mm.IsScreenTransitionFinished());
        mm.OnMinigameDestroyedInvoke();
        Destroy(transform.parent.gameObject);
    }
    
    System.Collections.IEnumerator LoseGameCoroutine()
    {
        MinigameManager mm = GameManager.Instance.GetService<MinigameManager>();
        mm.OnPlayerLoseMinigame();
        yield return new WaitForSeconds(3);
        mm.PlayerLoseExitMinigame();
        yield return new WaitUntil(() => mm.IsScreenTransitionFinished());
        Debug.LogError("screen transition finished: "+ mm.IsScreenTransitionFinished());
        mm.OnMinigameDestroyedInvoke();
        Destroy(transform.parent.gameObject);
    }
    void AdvanceTimers()
    {
        lossTimer += Time.deltaTime;
        shakeCooldownTimer += Time.deltaTime;
    }
    
}

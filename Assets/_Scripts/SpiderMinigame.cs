using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SpiderMinigame : MonoBehaviour
{
    public RectTransform spiderBook;
    public RectTransform spiderLocalParticleTransform;
    public RectTransform spiderWorldSpaceParticleTransform;

    [Header("Timers")]
    [SerializeField]float lossTimer = 0;
    public float lossTimerLimit;

    public TMP_Text timerText;

    [Header("Minigame Values")]

    public float shakePowerMultiplier = 1;
    [SerializeField] int originalNumberOfSpiders;
    [SerializeField] int numberOfSpiders = 900;

    [SerializeField]int amountOfSpidersToAffect;
    [SerializeField] int amountOfSpidersToAffectMin = 10;
    [SerializeField] int amountOfSpidersToAffectMax = 85;
    [SerializeField] float shakePowerSuccessThreshold;

    [SerializeField] float shakeCooldownTimer = 0;
    [SerializeField] float shakeCooldownTimerLimit;
    [SerializeField] Vector2 shakeVector;

    ParticleSystem localPs;
    ParticleSystem worldSpacePs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        localPs = spiderLocalParticleTransform.GetComponent<ParticleSystem>();
        worldSpacePs = spiderWorldSpaceParticleTransform.GetComponent<ParticleSystem>();
        numberOfSpiders = localPs.main.maxParticles;
        originalNumberOfSpiders = numberOfSpiders;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.LogWarning(Screen.orientation);
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
        timerText.text = "SHAKE\n" + (lossTimerLimit - lossTimer).ToString();

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


        if (numberOfSpiders > 0
        && shakeCooldownTimer > shakeCooldownTimerLimit
        && shakeVector.magnitude * shakePowerMultiplier > shakePowerSuccessThreshold)
        {
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
        Debug.LogWarning("player won");
        minigameEndSequence ??= StartCoroutine(WinGameCoroutine());
    }
    IEnumerator WinGameCoroutine()
    {
        yield return new WaitForSeconds(3);
        GameManager.Instance.GetService<MinigameManager>().PlayerWinMinigame();
        Destroy(transform.parent.gameObject);
    }
    void LoseGame()
    {
        Debug.LogError("player lost");
        minigameEndSequence ??= StartCoroutine(LoseGameCoroutine());
    }
    IEnumerator LoseGameCoroutine()
    {
        yield return new WaitForSeconds(3);
        GameManager.Instance.GetService<MinigameManager>().PlayerLoseMinigame();
        Destroy(transform.parent.gameObject);
    }
    void AdvanceTimers()
    {
        lossTimer += Time.deltaTime;
        shakeCooldownTimer += Time.deltaTime;
    }
    
}

﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class controller : MonoBehaviour {
    public float m_movePower=1.0f;
    public Vector2 m_tiltMultiplier, m_tiltMax;
    public LineRenderer m_lineRenderer;
    public SpringJoint2D m_spring;
    public Transform m_face;
    public Transform m_tail;
    public Transform m_springConnector;
    public swing m_paddle1, m_paddle2;
    private int m_dirAnimHash, m_deadStateAnimHash;
    public Animator m_faceAnimation;
    public float m_flipSpeed = 10.0f;
    private float m_flipT=1.0f;
    private float m_maxAbsVelMp = 1.0f;
    float startY;
    float[] startYList;
    int startYidx = 0;
    float calibrationTime = 2.0f;

    public Vector2 m_outsideHDist;
    private Vector3 m_startPos;
    public float m_normalDrag = 5.0f, m_outsideDrag = 30.0f, m_veryOutsideDrag=35.0f;
	// Use this for initialization
    public bool m_handleInput=true;
    public SpriteRenderer m_spriteRendererHead;
    public Sprite m_deadHeadSprite;

    public Transform m_gameOverText;
    public ParticleSystem m_heart;

    public bool m_trigDeath;
    private bool m_dead;

    public int m_hp = 2;
    public float m_invulnTime = 2.0f;
    private float m_invulnTick;

    public GameObject m_hitFx;
    private static shake m_camShake;
    public AudioSource m_soundSource;
    public AudioClip m_hurtsound;
    public AudioClip[] m_barksounds;

    public SpriteRenderer m_ballSprite;
    private int m_ballComboCount;
    public float m_ballComboCoolDownTime;
    private float m_ballComboCooldownTick;
    public Image m_healthUI;
    public Sprite[] m_healthUISprites;

	void Start () {
	    m_dirAnimHash=Animator.StringToHash("facing");
        m_deadStateAnimHash = Animator.StringToHash("dead");
        startY=Input.acceleration.y;
        startYList = new float[10];
        m_startPos = transform.position;
        if (m_camShake == null) m_camShake = GameObject.FindGameObjectWithTag("camshaker").GetComponent<shake>();
	}

    public void registerEnemyHurtbyBall()
    {
        if (m_ballComboCooldownTick > 0.0f)
            m_ballComboCount++;
        m_ballComboCooldownTick = m_ballComboCoolDownTime;
        StartCoroutine("happyEffect");
    }
	
    IEnumerator happyEffect()
    {
        yield return new WaitForSeconds(0.6f);
        m_hp++;
        if (m_hp > 2) m_hp = 2;
        updateHealthUI();
        if (!m_soundSource.isPlaying)
        {
            m_soundSource.PlayOneShot(m_barksounds[Random.Range(0, m_barksounds.Length)]);
        }
        m_heart.Play();
    }

    void updateHealthUI()
    {
        int imgIdx=m_hp-1;
        if (imgIdx >= 0 && imgIdx < m_healthUISprites.Length)
        {
            m_healthUI.sprite = m_healthUISprites[m_hp - 1];
            m_healthUI.enabled = true;
        }
        else if (imgIdx < 0)
            m_healthUI.enabled = false;
    }

    void Update()
    {
        if (m_trigDeath) kill();
        if (m_ballComboCooldownTick>0.0f)
        {
            m_ballComboCooldownTick -= Time.deltaTime;
            m_ballSprite.color = Color.Lerp(Color.Lerp(Color.yellow,Color.magenta,Mathf.Clamp01((float)m_ballComboCount/3.0f)),
                                            Color.white,1.0f-(m_ballComboCooldownTick/m_ballComboCoolDownTime));
        }
        else
        {
            if (m_ballComboCount > 0) m_ballSprite.color = Color.white;
            m_ballComboCount = 0;         
        }

        Vector3 myLocalConnectorPoint = transform.InverseTransformPoint(m_springConnector.position);
        m_spring.anchor = new Vector2(myLocalConnectorPoint.x, myLocalConnectorPoint.y);
        m_lineRenderer.SetPosition(0, m_springConnector.position);
        m_lineRenderer.SetPosition(1, m_spring.connectedBody.transform.position);
        float dist = (m_springConnector.position - m_spring.connectedBody.transform.position).magnitude;
        m_lineRenderer.materials[0].mainTextureScale = new Vector2(dist/4.0f, 1.0f);
        if (!m_dead)
        {
            // anims
            Vector2 vel = rigidbody2D.velocity;
            float velMagnitude = vel.magnitude;
            if (Mathf.Abs(velMagnitude) > m_maxAbsVelMp) m_maxAbsVelMp = Mathf.Abs(velMagnitude);
            float dirSign = 1.0f;
            if (velMagnitude > 5.0f)
            {
                m_paddle1.m_speedMp = 1.0f + (velMagnitude - 0.5f) * 0.05f;
                m_paddle2.m_speedMp = 1.0f + (velMagnitude - 0.5f) * 0.05f;
                if (vel.y > 2.0f)
                    m_faceAnimation.SetInteger(m_dirAnimHash, 1); // backface
                else
                    m_faceAnimation.SetInteger(m_dirAnimHash, 0); // frontface
                float speedMp = Mathf.Max(1.0f, m_maxAbsVelMp);
                if (vel.x > 0.0f)
                {
                    m_flipT = Mathf.Lerp(m_flipT, -1.0f, -(m_flipT - 2.0f) * m_flipSpeed * speedMp * Time.deltaTime);
                    m_face.localScale = new Vector3(blorp(m_flipT, -vel.x), 1.0f, 1.0f);
                    dirSign = m_face.localScale.x;
                }
                else
                {
                    m_flipT = Mathf.Lerp(m_flipT, 1.0f, (m_flipT + 2.0f) * m_flipSpeed * speedMp * Time.deltaTime);
                    m_face.localScale = new Vector3(blorp(m_flipT, -vel.x), 1.0f, 1.0f);
                }
                //
            }
            else
            {
                if (m_face.localScale.x < 0.0f)
                    m_face.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
                else
                    m_face.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                dirSign = m_face.localScale.x;
            }
            if (velMagnitude > 0.5f)
                m_tail.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * Mathf.Atan2(-vel.y, -vel.x * dirSign));

            // if outside
            Vector3 deltaPosFromStart = transform.position - m_startPos;
            if (deltaPosFromStart.x > -m_outsideHDist.x && deltaPosFromStart.x < m_outsideHDist.x &&
                deltaPosFromStart.y > -m_outsideHDist.y && deltaPosFromStart.y < m_outsideHDist.y)
            {
                rigidbody2D.drag = m_normalDrag;
            }
            else
            {
                if (deltaPosFromStart.x > -m_outsideHDist.x - 8.0f && deltaPosFromStart.x < m_outsideHDist.x + 8.0f &&
                deltaPosFromStart.y > -m_outsideHDist.y - 8.0f && deltaPosFromStart.y < m_outsideHDist.y + 8.0f)
                {
                    rigidbody2D.drag = m_outsideDrag;
                }
                else
                    rigidbody2D.drag = m_veryOutsideDrag;
            }
        }

        handleInvuln();
    }

    void handleInvuln()
    {
        if (m_invulnTick > 0.0f)
        {
            if (m_invulnTick < m_invulnTime * 0.5f && m_invulnTick > m_invulnTime * 0.48f && !m_soundSource.isPlaying)
                m_soundSource.PlayOneShot(m_hurtsound);
            m_face.renderer.enabled = !m_face.renderer.enabled;
            m_paddle1.renderer.enabled = !m_paddle1.renderer.enabled;
            m_paddle2.renderer.enabled = !m_paddle2.renderer.enabled;
            m_tail.renderer.enabled = !m_tail.renderer.enabled;
        }
        else if (m_invulnTick>-10000.0f)
        {
            m_face.renderer.enabled = true;
            m_paddle1.renderer.enabled = true;
            m_paddle2.renderer.enabled = true;
            m_tail.renderer.enabled = true;
            m_invulnTick = -20000.0f;
        }

        m_invulnTick -= Time.deltaTime;
    }

    IEnumerator kill()
    {
        Debug.Log("die1");
        yield return new WaitForSeconds(2.0f);
        Debug.Log("die2");
        if (!m_dead && m_hp<=0)
        {
            m_handleInput = false;
            m_spriteRendererHead.sprite = m_deadHeadSprite;
            m_faceAnimation.SetBool(m_deadStateAnimHash, true);
            m_paddle1.enabled = false;
            m_paddle2.enabled = false;
            m_dead = true;
            GameObject goText = Instantiate(m_gameOverText, transform.position+new Vector3(0.0f,0.0f,10.0f), Quaternion.identity) as GameObject;
        }
    }

    float blorp(float p_t, float p_dir)
    {
        float half = 0.9f;
        float endhalf = 0.8f;
        float extra = 0.4f;
        if (p_dir > 0.0f && p_t > -half)
        {
            if (p_t < 0.9f)
                p_t = Mathf.Max(endhalf, p_t + extra);
            else if (p_t >= 0.9f)
                p_t = p_t + extra * (1.0f - p_t) * 10.0f;
            return p_t; 
        }
        if (p_dir <= 0.0f && p_t < half)
        {
            if (p_t > -0.9f)
                p_t = Mathf.Min(-endhalf, p_t - extra);
            else if (p_t <= -0.9f)
                p_t = p_t - extra * (1.0f + p_t) * 10.0f;
            return p_t; 
        }
        return p_t;
    }

	// Update is called once per frame
	void FixedUpdate () 
    {
        if (m_handleInput)
        {
            Vector2 dirInput = Vector2.zero;
#if (!UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID))
            Vector2 tilt = new Vector2(Input.acceleration.x, Input.acceleration.y/* - startY*/);
            tilt.x *= Mathf.Clamp(m_tiltMultiplier.x * tilt.magnitude, 1.0f, m_tiltMultiplier.x);
            tilt.y *= Mathf.Clamp(m_tiltMultiplier.y * tilt.magnitude, 1.0f, m_tiltMultiplier.y);
            tilt.x = Mathf.Clamp(tilt.x, -m_tiltMax.x, m_tiltMax.y);
            tilt.y = Mathf.Clamp(tilt.y, -m_tiltMax.y, m_tiltMax.y);
            Debug.Log(tilt);
            dirInput = tilt;
#else
            dirInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif
            Debug.DrawLine(transform.position, transform.position + new Vector3(dirInput.x, dirInput.y, 0.0f), Color.white);
            float dirInputSqrMagnitude = dirInput.sqrMagnitude;
            rigidbody2D.AddForce(dirInput * m_movePower);
            m_maxAbsVelMp *= 0.95f;
        }

	}

    void OnTriggerEnter2D(Collider2D p_other)
    {
        HandlePain(p_other);
    }

    void OnTriggerStay2D(Collider2D p_other)
    {
        HandlePain(p_other);
    }

    public bool isDead()
    {
        return m_dead;
    }

    void HandlePain(Collider2D p_other)
    {
        Debug.Log(p_other.gameObject.tag);
        if (p_other.gameObject.tag == "playerHurt" && m_invulnTick<=0.0f && !m_dead)
        {
            m_hp--; 
            if (m_hp <= 0)
                StartCoroutine("kill");
            updateHealthUI();
            Instantiate(m_hitFx, transform.position, Quaternion.identity);
            m_invulnTick = m_invulnTime;
            Vector3 hitDir = (transform.position - p_other.transform.position).normalized;
            rigidbody2D.AddForce(hitDir * 20000.0f);
            if (m_camShake) m_camShake.Activate(1.0f, hitDir * 5.0f, new Vector2(10.0f, 20.0f));
            
        }
    }
}

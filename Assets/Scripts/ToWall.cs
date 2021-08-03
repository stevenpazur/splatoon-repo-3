using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ToWall : MonoBehaviour
{
    public GameObject model, gun, outfit;
    public ParticleSystem swimParticle;
    public float Velocity, SwimVelocity;
    private float playerSpeed = 6; // move speed
    private float turnSpeed = 90; // turning speed (degrees/second)
    public float lerpSpeed = 10; // smoothing speed
    private float gravity = 0.15f; // gravity acceleration
    private bool isGrounded = false, canJump = false;
    public float deltaGround = -0.5f; // character is grounded up to this distance
    public float jumpRange = 0.1f; // range to detect target wall
    public float jumpHeight = 0.5f;
    [Range(0, 1)] public float similarColorsOffsetValue = 0.1f;
    private Vector3 surfaceNormal; // current surface normal
    private Vector3 myNormal; // character normal
    private float distGround; // distance from character position to ground
    private bool jumping = false; // flag &quot;I'm jumping to wall&quot;
    private bool blockRotation = false;
    private bool squidKid = false;
    public bool isRidingARail = false;
    private float inputMagnitude;
    private List<Material> modelMaterials, outfitMaterials;
    private List<Renderer> outfitRenderers;
    private List<Texture> origBaseMaps, origBumpMaps;
    private List<Color> origColors;

    private Transform myTransform;
    //public BoxCollider boxCollider; // drag BoxCollider ref in editor
    public Animator anim;
    public Color theColor;
    public Material inkMaterial;
    public Rig squidkidRigs;
    public GameObject squid;
    public ParticleSystem inkLaunchTrail;
    public MeshRenderer blob2Rend;
    public List<ParticleSystem> droplets;
    [HideInInspector] public LaunchPad launcher;

    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;
    [Range(0, 30f)]
    public float SquidNowKidNow = 10f;

    private void Start()
    {
        myNormal = transform.up; // normal starts as character up direction
        myTransform = transform;
        modelMaterials = model.GetComponent<Renderer>().materials.ToList();
        //outfitMaterials = outfit.Get
        origBaseMaps = new List<Texture>();
        origBumpMaps = new List<Texture>();
        origColors = new List<Color>();
        outfitRenderers = outfit.GetComponentsInChildren<Renderer>().ToList();

        for(int i = 0; i < modelMaterials.Count; i++)
        {
            origBaseMaps.Add(modelMaterials[i].GetTexture("_BaseMap"));
            origBumpMaps.Add(modelMaterials[i].GetTexture("_BumpMap"));
            origColors.Add(modelMaterials[i].GetColor("_BaseColor"));
        }

        outfitMaterials = new List<Material>();
        for (int j = 0; j < outfitRenderers.Count; j++)
        {
            if(outfitRenderers[j].materials.Length > 0)
                outfitMaterials.AddRange(outfitRenderers[j].materials.ToList());
        }

        // set color for particle effects
        var colLifetimeTrail = inkLaunchTrail.colorOverLifetime;
        colLifetimeTrail.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(theColor, 0.0f), new GradientColorKey(theColor, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) });
        colLifetimeTrail.color = gradient;

        ParticleSystem[] systems = inkLaunchTrail.GetComponentsInChildren<ParticleSystem>();
        for(int i = 0; i < systems.Length; i++)
        {
            var colLife = systems[i].colorOverLifetime;
            colLife.enabled = true;
            colLife.color = gradient;
        }
    }

    private void FixedUpdate()
    {
        if (launcher != null || isRidingARail || jumping)
        {
            return;
        }
        // apply constant weight force according to character normal:
        //GetComponent<Rigidbody>().AddForce(-gravity * GetComponent<Rigidbody>().mass * myNormal);
        GetComponent<CharacterController>().Move(-gravity * myNormal);
    }

    private void Update()
    {
        if(launcher != null)
        {
            anim.SetFloat("jumpLength", launcher.cart.m_Position);
            anim.SetFloat("FlightSpeedMultiplier", launcher.flightSpeed / 0.25f);
            if (launcher.cart.m_Position < 0.5f)
            {
                model.SetActive(false);
            }
            else
            {
                model.SetActive(true);
            }
            if(launcher.cart.m_Position > 0.5f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Vector3.up), 0.01f);
            }
            return;
        }

        // jump code - jump to wall
        if (jumping)
        {
            return;
        }

        if (isRidingARail)
        {
            print("yesyesyesyes");
            if (Input.GetButtonDown("Jump"))
            {
                print("hi");
            }
            return;
        }

        anim.SetBool("shooting", blockRotation);

        Ray ray;
        RaycastHit hit;

        squidKid = Input.GetButton("Fire2");
        if (Input.GetButton("Fire2"))
        {
            GetComponent<CharacterController>().height = 1.1f;
            if (squidkidRigs != null)
            {
                if (squidkidRigs.weight < 1)
                {
                    squidkidRigs.weight += SquidNowKidNow * Time.deltaTime;
                }
                else
                {
                    squidkidRigs.weight = 1;
                }
            }
            GetComponent<ShootingSystem>().canShoot = false;

            InkMeUpBaby(true);

            RaycastHit raycastHit;
            ray = new Ray(transform.position, -transform.up);
            if (Physics.Raycast(ray, out raycastHit, 1.1f))
            {
                if (raycastHit.transform == null || raycastHit.transform.tag != "Paintable")
                {
                    print("not paintable");
                    return;
                }

                Material mat = raycastHit.collider.gameObject.GetComponent<Renderer>().sharedMaterial;
                Shader shader = mat.shader;
                string shaderName = ShaderUtil.GetPropertyName(shader, 0);
                Texture texture = mat.GetTexture(shaderName);
                Texture2D t2d = TextureToTexture2D(texture);
                Vector2 pCoord = raycastHit.textureCoord;
                pCoord.x *= t2d.width;
                pCoord.y *= t2d.height;
                Vector2 tiling = mat.GetTextureScale(shaderName);
                int textureAtX = Mathf.FloorToInt(pCoord.x * tiling.x);
                int textureAtZ = Mathf.FloorToInt(pCoord.y * tiling.y);
                Color cool = t2d.GetPixel(textureAtX, textureAtZ);
                Color color = cool;

                Color color1 = new Color(theColor.r * (1f - similarColorsOffsetValue), theColor.g * (1f - similarColorsOffsetValue), theColor.b * (1f - similarColorsOffsetValue), theColor.a * (1f - similarColorsOffsetValue));
                Color color2 = new Color(theColor.r * (1f + similarColorsOffsetValue), theColor.g * (1f + similarColorsOffsetValue), theColor.b * (1f + similarColorsOffsetValue), theColor.a * (1f + similarColorsOffsetValue));
                if (ColorsSimilar(color1, color2, color) && inputMagnitude > 0.6f)
                {
                    StartCoroutine(SquidNowKidNowCoroutine(true, false, false));
                    playerSpeed = SwimVelocity;
                }
                else if(ColorsSimilar(color1, color2, color))
                {
                    StartCoroutine(SquidNowKidNowCoroutine(false, false, false));
                    playerSpeed = SwimVelocity;
                }
                else if(!ColorsSimilar(color1, color2, color))
                {
                    StartCoroutine(SquidNowKidNowCoroutine(false, false, true));
                    playerSpeed = SwimVelocity;
                    myNormal = Vector3.up;
                }
                else
                {
                    StartCoroutine(SquidNowKidNowCoroutine(false, true, false));
                    playerSpeed = Velocity;
                    myNormal = Vector3.up;
                }
            }
        }
        else
        {
            GetComponent<CharacterController>().height = 2;
            if (squidkidRigs != null)
            {
                if (squidkidRigs.weight > 0)
                {
                    squidkidRigs.weight -= SquidNowKidNow * Time.deltaTime;
                }
                else
                {
                    squidkidRigs.weight = 0;
                }
            }
            StartCoroutine(SquidNowKidNowCoroutine(false, true, false));
            playerSpeed = Velocity;
            myNormal = Vector3.up;

            InkMeUpBaby(false);
        }
        ray = new Ray(myTransform.position, myTransform.forward);

        if (Physics.Raycast(ray, out hit, jumpRange) && Input.GetButton("Fire2"))
        { // wall ahead?
            JumpToWall(hit.point, hit.normal); // yes: jump to the wall
        }
        else if (!Input.GetButton("Fire2"))
        {
            myNormal = Vector3.up;
            GetComponent<ShootingSystem>().canShoot = true;
        }

        // movement code:

        float InputX = Input.GetAxis("Horizontal");
        float InputZ = Input.GetAxis("Vertical");
        inputMagnitude = new Vector2(InputX, InputZ).sqrMagnitude;

        if (inputMagnitude > 0.1f)
        {
            anim.SetFloat("Blend", inputMagnitude, StartAnimTime, Time.deltaTime);
            anim.SetFloat("X", InputX, StartAnimTime / 3, Time.deltaTime);
            anim.SetFloat("Y", InputZ, StartAnimTime / 3, Time.deltaTime);
        }
        else
        {
            anim.SetFloat("Blend", inputMagnitude, StopAnimTime, Time.deltaTime);
            anim.SetFloat("X", InputX, StopAnimTime / 3, Time.deltaTime);
            anim.SetFloat("Y", InputZ, StopAnimTime / 3, Time.deltaTime);
        }

        var cam = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 desiredMoveDirection = forward * InputZ + right * InputX;

        //Camera
        if (desiredMoveDirection != Vector3.zero && !blockRotation)
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.LookRotation(desiredMoveDirection), 0.1f);
        GetComponent<CharacterController>().Move(desiredMoveDirection * Time.deltaTime * playerSpeed);

        if (Input.GetButton("Fire1"))
        {
            blockRotation = true;
            RotateToCamera(cam, desiredMoveDirection, 0.1f, transform);
        }
        else
        {
            blockRotation = false;
        }

        // update surface normal and isGrounded:
        ray = new Ray(myTransform.position, -myNormal); // cast ray downwards
        if (Physics.Raycast(ray, out hit, 2f))
        { // use it to update myNormal and isGrounded
            isGrounded = true;
            surfaceNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            // assume usual ground normal to avoid "falling forever"
            surfaceNormal = Vector3.up;
        }
        // find forward direction with new myNormal:
        Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
        // align character to the new myNormal while keeping the forward direction:
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, lerpSpeed * Time.deltaTime);
        // move the character forth/back with Vertical axis:
        if (myNormal != Vector3.up)
        {
            if (Input.GetAxis("Vertical") > 0)
                GetComponent<CharacterController>().Move(myTransform.forward * Input.GetAxis("Vertical") * playerSpeed * Time.deltaTime);
            if (Input.GetAxis("Vertical") < 0)
                GetComponent<CharacterController>().Move(-myTransform.forward * Input.GetAxis("Vertical") * playerSpeed * Time.deltaTime);
        }

        if(Input.GetButtonDown("Jump"))
        {
            if (isGrounded && canJump)
            {
                StartCoroutine(JumpUp(-jumpHeight));
            }
        }

        if (Input.GetButtonDown("Fire2") || Input.GetButtonUp("Fire2"))
        {
            droplets.ForEach(d => d.Play());
        }

        float surfaceMagnitude = Vector3.SqrMagnitude(new Vector2(Vector3.Cross(myNormal, surfaceNormal).x, Vector3.Cross(myNormal, surfaceNormal).z));
        canJump = model.activeInHierarchy && surfaceMagnitude < 0.3f;
    }

    private void RotateToCamera(Camera cam, Vector3 desiredMoveDirection, float desiredRotationSpeed, Transform t)
    {
        var forward = cam.transform.forward;

        desiredMoveDirection = forward;
        Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
        Quaternion lookAtRotationOnly_Y = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        t.rotation = Quaternion.Slerp(transform.rotation, lookAtRotationOnly_Y, desiredRotationSpeed);
    }

    public void InkMeUpBaby(bool inky)
    {
        for (int i = 0; i < modelMaterials.Count; i++)
        {
            if (inky)
            {
                modelMaterials[i].SetTexture("_BaseMap", inkMaterial.GetTexture("_BaseMap"));
                modelMaterials[i].SetColor("_BaseColor", inkMaterial.GetColor("_BaseColor"));
                modelMaterials[i].SetTexture("_BumpMap", inkMaterial.GetTexture("_BumpMap"));
            }
            else
            {
                modelMaterials[i].SetTexture("_BaseMap", origBaseMaps[i]);
                modelMaterials[i].SetColor("_BaseColor", origColors[i]);
                modelMaterials[i].SetTexture("_BumpMap", origBumpMaps[i]);
            }
        }
    }

    private bool ColorsSimilar(Color color1, Color color2, Color checkedColor)
    {
        bool isRedGood = checkedColor.r >= Mathf.Min(color1.r, color2.r) && checkedColor.r <= Mathf.Max(color1.r, color2.r);

        bool isGreenGood = checkedColor.g >= Mathf.Min(color1.g, color2.g) && checkedColor.g <= Mathf.Max(color1.g, color2.g);

        bool isBlueGood = checkedColor.b >= Mathf.Min(color1.b, color2.b) && checkedColor.b <= Mathf.Max(color1.b, color2.b);

        bool isAlphaGood = checkedColor.a >= Mathf.Min(color1.a, color2.a) && checkedColor.a <= Mathf.Max(color1.a, color2.a);// May not need this one if you don't have an alpha channel :P

        return isRedGood && isGreenGood && isBlueGood && isAlphaGood;
    }

    private Texture2D TextureToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);
        return texture2D;
    }

    private void JumpToWall(Vector3 point, Vector3 normal)
    {
        // jump to wall
        jumping = true; // signal it's jumping to wall
        Vector3 orgPos = myTransform.position;
        Quaternion orgRot = myTransform.rotation;
        Vector3 dstPos = point + normal * (distGround + 0.5f); // will jump to 0.5 above wall
        Vector3 myForward = Vector3.Cross(myTransform.right, normal);
        Quaternion dstRot = Quaternion.LookRotation(myForward, normal);

        StartCoroutine(jumpTime(orgPos, orgRot, dstPos, dstRot, normal));
        //jumptime
    }

    private IEnumerator jumpTime(Vector3 orgPos, Quaternion orgRot, Vector3 dstPos, Quaternion dstRot, Vector3 normal)
    {
        //for (float t = 0.0f; t < 0.2f;)
        //{
        //    t += Time.deltaTime;
        myTransform.position = Vector3.Lerp(orgPos, dstPos, 0.09f);
        //myTransform.position = dstPos;
        myTransform.rotation = Quaternion.Slerp(orgRot, dstRot, 0.09f);
        //myTransform.rotation = dstRot;
        yield return null; // return here next frame
        //}
        myNormal = normal; // update myNormal
        jumping = false; // jumping to wall finished

    }

    private IEnumerator JumpUp(float grav)
    {
        anim.Play("Jumping");
        float origGrav = gravity;
        gravity = grav;
        while (gravity < origGrav)
        {
            yield return new WaitForSeconds(0.2f);
            gravity += 0.1f;
            Ray jumpableRay = new Ray(transform.position, -transform.up);
            RaycastHit hit;
            if (Physics.Raycast(jumpableRay, out hit, 1.1f))
            {
                gravity = origGrav;
            }
        }
    }

    private IEnumerator SquidNowKidNowCoroutine(bool particlePlay, bool modelActive, bool squidActive)
    {
        for(int i = 0; i < 5; i++)
        {
            yield return null;
        }

        if (particlePlay)
            swimParticle.Play();
        else
            swimParticle.Stop();

        model.SetActive(modelActive);
        gun.SetActive(modelActive);
        outfit.SetActive(modelActive);
        squid.SetActive(squidActive);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{

    public float Velocity;
    public float SwimVelocity;
    private float playerSpeed;
    public ParticleSystem swimParticle;
    public ParticlesController particlesController;
    public GameObject model, gun;
    private bool grabbedTexture = false;
    private Texture2D tex;
    [Space]

    public Animator anim;
    private Camera cam;
    private CharacterController controller;
    private bool isGrounded;
    private Vector3 desiredMoveDirection;
    private float InputX;
    private float InputZ;
    [Range(0, 1f)]
    public float similarColorsOffsetValue = 0.05f;
    [SerializeField] private Color theColor;

    public bool blockRotationPlayer;
    public float desiredRotationSpeed = 0.1f;

    public float Speed;
    public float allowPlayerRotation = 0.1f;


    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    public float verticalVel;
    private Vector3 moveVector;

    void Start()
    {
        //anim = this.GetComponent<Animator>();
        cam = Camera.main;
        controller = this.GetComponent<CharacterController>();
        theColor = particlesController.paintColor;
        playerSpeed = Velocity;
    }

    void Update()
    {
        TryGettingPaintColor();
        InputMagnitude();

        isGrounded = controller.isGrounded;
        if (isGrounded)
            verticalVel -= 0;
        else
            verticalVel -= 1;

        moveVector = new Vector3(0, verticalVel * .2f * Time.deltaTime, 0);
        controller.Move(moveVector);
    }

    void PlayerMoveAndRotation()
    {
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");

        var camera = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        desiredMoveDirection = forward * InputZ + right * InputX;

        if (blockRotationPlayer == false)
        {
            //Camera
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
            controller.Move(desiredMoveDirection * Time.deltaTime * playerSpeed);
        }
        else
        {
            //Strafe
            controller.Move((transform.forward * InputZ + transform.right * InputX) * Time.deltaTime * playerSpeed);
        }
    }

    public void LookAt(Vector3 pos)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {
        var forward = cam.transform.forward;

        desiredMoveDirection = forward;
        Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
        Quaternion lookAtRotationOnly_Y = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        t.rotation = Quaternion.Slerp(transform.rotation, lookAtRotationOnly_Y, desiredRotationSpeed);
    }

    void InputMagnitude()
    {
        //Calculate Input Vectors
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");

        //Calculate the Input Magnitude
        Speed = new Vector2(InputX, InputZ).sqrMagnitude;

        //Change animation mode if rotation is blocked
        anim.SetBool("shooting", blockRotationPlayer);

        //Physically move player
        if (Speed > allowPlayerRotation)
        {
            anim.SetFloat("Blend", Speed, StartAnimTime, Time.deltaTime);
            anim.SetFloat("X", InputX, StartAnimTime / 3, Time.deltaTime);
            anim.SetFloat("Y", InputZ, StartAnimTime / 3, Time.deltaTime);
            PlayerMoveAndRotation();
        }
        else if (Speed < allowPlayerRotation)
        {
            anim.SetFloat("Blend", Speed, StopAnimTime, Time.deltaTime);
            anim.SetFloat("X", InputX, StopAnimTime / 3, Time.deltaTime);
            anim.SetFloat("Y", InputZ, StopAnimTime / 3, Time.deltaTime);
        }
    }

    private void TryGettingPaintColor()
    {
        if (Input.GetButton("Fire2"))
        {
            GetComponent<ShootingSystem>().canShoot = false;

            //Vector3 rayOffset = new Vector3(transform.up.x, transform.up.y + 0.1f, transform.up.z);
            RaycastHit raycastHit;
            Ray ray = new Ray(transform.position, -transform.up);
            Debug.DrawRay(transform.position, -transform.up, Color.red, 2f);
            if (Physics.Raycast(ray, out raycastHit))
            {
                if(raycastHit.transform.tag != "Paintable")
                {
                    return;
                }

                Color color = new Color();
                if (!grabbedTexture)
                {
                    //grabbedTexture = true;
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
                    color = cool;

                    Color color1 = new Color(theColor.r * (1f - similarColorsOffsetValue), theColor.g * (1f - similarColorsOffsetValue), theColor.b * (1f - similarColorsOffsetValue), theColor.a * (1f - similarColorsOffsetValue));
                    Color color2 = new Color(theColor.r * (1f + similarColorsOffsetValue), theColor.g * (1f + similarColorsOffsetValue), theColor.b * (1f + similarColorsOffsetValue), theColor.a * (1f + similarColorsOffsetValue));
                    if (ColorsSimilar(color1, color2, color))
                    {
                        swimParticle.Play();
                        model.SetActive(false);
                        gun.SetActive(false);
                        playerSpeed = SwimVelocity;
                    }
                    else
                    {
                        swimParticle.Stop();
                        model.SetActive(true);
                        gun.SetActive(true);
                        playerSpeed = Velocity;
                    }
                }
            }
        }
        else
        {
            GetComponent<ShootingSystem>().canShoot = true;

            grabbedTexture = false;
            swimParticle.Stop();
            model.SetActive(true);
            gun.SetActive(true);
            playerSpeed = Velocity;
        }
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

    private bool ColorsSimilar(Color color1, Color color2, Color checkedColor)
    {
        bool isRedGood = checkedColor.r >= Mathf.Min(color1.r, color2.r) && checkedColor.r <= Mathf.Max(color1.r, color2.r);

        bool isGreenGood = checkedColor.g >= Mathf.Min(color1.g, color2.g) && checkedColor.g <= Mathf.Max(color1.g, color2.g);

        bool isBlueGood = checkedColor.b >= Mathf.Min(color1.b, color2.b) && checkedColor.b <= Mathf.Max(color1.b, color2.b);

        bool isAlphaGood = checkedColor.a >= Mathf.Min(color1.a, color2.a) && checkedColor.a <= Mathf.Max(color1.a, color2.a);// May not need this one if you don't have an alpha channel :P

        return isRedGood && isGreenGood && isBlueGood && isAlphaGood;
    }
}

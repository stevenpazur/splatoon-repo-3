
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class MovementInputTest : MonoBehaviour
{

    public float Velocity;
    public float SwimVelocity;
    private float playerSpeed;
    private bool grabbedTexture = false;
    private Texture2D tex;
    [Space]

    private Camera cam;
    private CharacterController controller;
    private bool isGrounded;
    private Vector3 desiredMoveDirection;
    private float moveSpeed = 6; // turning speed (degrees/second)
    public float lerpSpeed = 10; // smoothing speed
    private float InputX;
    private float InputZ;
    private float distGround;
    private float deltaGround = 0.2f; // character is grounded up to this distance
    private Vector3 myNormal, surfaceNormal;
    private Transform myTransform;
    [Range(0, 1f)]
    public float similarColorsOffsetValue = 0.05f;
    [SerializeField] private Color theColor;

    public bool blockRotationPlayer;
    public float desiredRotationSpeed = 0.1f;

    public float Speed;
    public float allowPlayerRotation = 0.1f;
    public float wallDetectDistance = 3f;

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
        playerSpeed = Velocity;
        myNormal = -Vector3.up;
        myTransform = transform;
        distGround = GetComponent<CharacterController>().bounds.size.y - GetComponent<CharacterController>().center.y;
    }

    void Update()
    {
        TryGettingPaintColor();
        InputMagnitude();

        isGrounded = controller.isGrounded;
        if (isGrounded)
            verticalVel = 0;
        else
            verticalVel += 1;

        //moveVector = new Vector3(0, verticalVel * .2f * Time.deltaTime, 0);
        moveVector = myNormal * verticalVel * .2f * Time.deltaTime;
        controller.Move(moveVector);

        Ray ray = new Ray(myTransform.position, myTransform.forward);
        RaycastHit hitWall;
        if (Physics.Raycast(ray, out hitWall, wallDetectDistance))
        {
            print("wall detected");
            Color color = new Color();
            if (!grabbedTexture)
            {
                //grabbedTexture = true;
                Material mat = hitWall.collider.gameObject.GetComponent<Renderer>().sharedMaterial;
                Shader shader = mat.shader;
                string shaderName = ShaderUtil.GetPropertyName(shader, 0);
                Texture texture = mat.GetTexture(shaderName);
                Texture2D t2d = TextureToTexture2D(texture);
                Vector2 pCoord = hitWall.textureCoord;
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
                    print("yay");
                    print(color);
                    myNormal = -hitWall.normal;
                    myTransform.rotation = Quaternion.Euler(myNormal - new Vector3(90, 0, 0));
                }
                else
                {
                    print("nay");
                    print(color);
                }
            }
        }
    }

    void PlayerMoveAndRotation()
    {
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");

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
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
            controller.Move(desiredMoveDirection * Time.deltaTime * playerSpeed);
        }
        else
        {
            //Strafe
            controller.Move((myTransform.forward * InputZ + myTransform.right * InputX) * Time.deltaTime * playerSpeed);
        }

        // update surface normal and isGrounded:
        Ray ray = new Ray(myTransform.position, -myNormal); // cast ray downwards
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        { // use it to update myNormal and isGrounded
            isGrounded = hit.distance <= distGround + deltaGround;
            surfaceNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            // assume usual ground normal to avoid "falling forever"
            surfaceNormal = Vector3.up;
        }
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, lerpSpeed * Time.deltaTime);
        // find forward direction with new myNormal:
        Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
        // align character to the new myNormal while keeping the forward direction:
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, lerpSpeed * Time.deltaTime);
        // move the character forth/back with Vertical axis:
        //myTransform.Translate(0, 0, Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
        if (myNormal != Vector3.up)
        {
            if (Input.GetAxis("Vertical") > 0)
                GetComponent<CharacterController>().Move(myTransform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
            if (Input.GetAxis("Vertical") < 0)
                GetComponent<CharacterController>().Move(-myTransform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
        }
    }

    public void LookAt(Vector3 pos)
    {
        myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {
        var forward = cam.transform.forward;

        desiredMoveDirection = forward;
        Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
        Quaternion lookAtRotationOnly_Y = Quaternion.Euler(myTransform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, myTransform.rotation.eulerAngles.z);

        t.rotation = Quaternion.Slerp(myTransform.rotation, lookAtRotationOnly_Y, desiredRotationSpeed);
    }

    void InputMagnitude()
    {
        //Calculate Input Vectors
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");

        //Calculate the Input Magnitude
        Speed = new Vector2(InputX, InputZ).sqrMagnitude;

        //Change animation mode if rotation is blocked

        //Physically move player
        if (Speed > allowPlayerRotation)
        {
            PlayerMoveAndRotation();
        }
        else if (Speed < allowPlayerRotation)
        {
        }
    }

    private void TryGettingPaintColor()
    {
        if (Input.GetButton("Fire2"))
        {
            GetComponent<ShootingSystem>().canShoot = false;

            //Vector3 rayOffset = new Vector3(transform.up.x, transform.up.y + 0.1f, transform.up.z);
            RaycastHit raycastHit;
            Ray ray = new Ray(myTransform.position, -myTransform.up);
            Debug.DrawRay(myTransform.position, -myTransform.up, Color.red, 2f);
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.tag != "Paintable")
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
                        playerSpeed = SwimVelocity;
                    }
                    else
                    {
                        playerSpeed = Velocity;
                    }
                }
            }
        }
        else
        {
            //GetComponent<ShootingSystem>().canShoot = true;

            grabbedTexture = false;
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

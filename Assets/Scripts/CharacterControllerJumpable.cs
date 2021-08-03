using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CharacterControllerJumpable : MonoBehaviour
{
    public float WalkSpeed = 1f;
    public float gravity = 1f;
    public float jumpForce = -5f;
    public float jumpCheckDistance = 1.1f, wallCheckDistance = 2f;
    [Range(0, 1)] public float colorSimilarity = 0.1f;
    public CollisionPainter collisionPainter;
    public GameObject model;
    public ParticleSystem swimParticle;

    private float origGravity;
    private float playerSpeed;
    private Vector3 myNormal, downwardForce, desiredDirection;
    private CharacterController controller;
    private float InputX, InputZ;
    private Camera cam;
    private bool blockRotationPlayer = false, isGrounded = false;
    private Color teamColor;
    private bool canSink = false, drowning = false;

    private void Start()
    {
        origGravity = gravity;
        playerSpeed = WalkSpeed;
        myNormal = Vector3.up;
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
        teamColor = collisionPainter.paintColor;
    }

    private void FixedUpdate()
    {
        downwardForce = -myNormal * gravity;
        controller.Move(downwardForce * Time.deltaTime);
    }

    private void Update()
    {
        // Handle movement
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");
        float speed = new Vector2(InputX, InputZ).sqrMagnitude;

        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        if (speed > 0.1f)
        {
            if (!blockRotationPlayer)
            {
                desiredDirection = forward * InputZ + right * InputX;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDirection), 0.2f);
                controller.Move(desiredDirection * playerSpeed * Time.deltaTime);
            }
            else
            {
                controller.Move((transform.forward * InputZ + transform.right * InputX) * Time.deltaTime * playerSpeed);
            }
        }

        // Handle blocking rotation on shooting
        if (Input.GetButton("Fire1"))
        {
            blockRotationPlayer = true;
            Vector3 camForward = cam.transform.forward;
            camForward.Normalize();
            camForward.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camForward), 0.5f);
        }
        else
        {
            blockRotationPlayer = false;
        }

        // Handle jumping
        Ray jumpableRay = new Ray(transform.position, -transform.up);
        RaycastHit hit;
        isGrounded = Physics.Raycast(jumpableRay, out hit, jumpCheckDistance);

        if (isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                StartCoroutine(SetGravity(jumpForce));
            }
        }

        // Handle swimming and wall climbing
        if (Input.GetButton("Fire2"))
        {
            GetComponent<ShootingSystem>().canShoot = false;

            // Handle swimming
            Ray textureRay = new Ray(transform.position, -transform.up); // cast ray downward
            if (Physics.Raycast(textureRay, out hit, jumpCheckDistance))
            {
                if (hit.transform.tag != "Paintable")
                    return;

                // get color player is standing on
                StartCoroutine(CheckInkColor(hit));

                model.SetActive(!canSink);
                if (canSink)
                    swimParticle.Play();
                else
                    swimParticle.Stop();
            }

            // Handle wall climbing
            Ray wallRay = new Ray(transform.position, transform.forward); // cast ray forward
            if (Physics.Raycast(wallRay, out hit, wallCheckDistance))
            {
                if (hit.transform.tag != "Paintable")
                    return;

                StartCoroutine(CheckInkColor(hit));

                if (canSink)
                {
                    Vector3 orgPos = transform.position;
                    Quaternion orgRot = transform.rotation;
                    Vector3 dstPos = hit.point + hit.normal;
                    Vector3 myForward = Vector3.Cross(transform.right, hit.normal);
                    Quaternion dstRot = Quaternion.LookRotation(myForward, hit.normal);

                    StartCoroutine(RotatePlayerOntoWall(orgPos, orgRot, dstPos, dstRot, hit.normal));
                }
            }
        }
        else
        {
            GetComponent<ShootingSystem>().canShoot = true;
        }
    }

    private IEnumerator SetGravity(float grav)
    {
        gravity = grav;
        while(gravity < origGravity)
        {
            yield return new WaitForSeconds(0.1f);
            gravity += 0.5f;
            Ray jumpableRay = new Ray(transform.position, -transform.up);
            RaycastHit hit;
            Debug.DrawRay(transform.position, -transform.up * jumpCheckDistance, Color.green);
            if (Physics.Raycast(jumpableRay, out hit, jumpCheckDistance + 0.2f))
            {
                gravity = origGravity;
            }
        }
    }

    private IEnumerator RotatePlayerOntoWall(Vector3 orgPos, Quaternion orgRot, Vector3 dstPos, Quaternion dstRot, Vector3 normal)
    {
        transform.position = Vector3.Lerp(orgPos, dstPos, 0.5f);
        desiredDirection = dstPos;
        transform.rotation = Quaternion.Slerp(orgRot, dstRot, 0.5f);
        myNormal = normal;
        yield return null;
    }

    private IEnumerator CheckInkColor(RaycastHit hit)
    {
        Material mat = hit.collider.gameObject.GetComponent<Renderer>().sharedMaterial;
        Shader shader = mat.shader;
        string shaderName = ShaderUtil.GetPropertyName(shader, 0);
        Texture texture = mat.GetTexture(shaderName);
        Texture2D t2d = TextureToTexture2D(texture);
        Vector2 pCoord = hit.textureCoord;
        pCoord.x *= t2d.width;
        pCoord.y *= t2d.height;
        Vector2 tiling = mat.GetTextureScale(shaderName);
        int textureAtX = Mathf.FloorToInt(pCoord.x * tiling.x);
        int textureAtZ = Mathf.FloorToInt(pCoord.y * tiling.y);
        Color color = t2d.GetPixel(textureAtX, textureAtZ);

        // generate 2 colors similar to teamColor
        Color color1 = new Color(teamColor.r * (1 + colorSimilarity), teamColor.g * (1 + colorSimilarity), teamColor.b * (1 + colorSimilarity), teamColor.a * (1 + colorSimilarity));
        Color color2 = new Color(teamColor.r * (1 - colorSimilarity), teamColor.g * (1 - colorSimilarity), teamColor.b * (1 - colorSimilarity), teamColor.a * (1 - colorSimilarity));

        // check if player can sink
        if(ColorsSimilar(color1, color2, color))
        {
            // player is in his own ink
            canSink = true;
            drowning = false;
        }
        else if(color.a < 0.1f)
        {
            // player is not in any ink
            canSink = false;
            drowning = false;
        }
        else
        {
            // player is in enemy ink
            canSink = false;
            drowning = true;
        }
        yield return new WaitForSeconds(0.1f);
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

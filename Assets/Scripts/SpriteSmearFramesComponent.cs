using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSmearFramesComponent : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite sprite;

    private Renderer smearFramesRenderer;
    private Material internalMaterial;
    private MaterialPropertyBlock matProps;
    private RenderTexture renderTexture;
    
    private Vector3 lastPosition;
    private Vector3 newPosition;
    private Vector3 direction;
    private float t = 0;

    #region Public properties

    [Range(1e-2f, 16f)] public float timeSlice = 2f;
    [Range(1e-2f, 0.4f)] public float intensityRange = 0.26f;
    public bool Enable { get; set; }

    #endregion

    Mesh CreateInternalQuadMesh(float width = 1F, float height = 1F)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width/2F, -height/2F, 0),
            new Vector3( width/2F, -height/2F, 0),
            new Vector3(-width/2F,  height/2F, 0),
            new Vector3( width/2F,  height/2F, 0)
        };
        mesh.vertices = vertices;

        int[] tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;
        return mesh;
    }
   
    void Start()
    {
        // disable source sprite.
        spriteRenderer = GetComponentInParent<SpriteRenderer>();        
        spriteRenderer.enabled = false;
               
        // and using this component's renderer instead. 
        smearFramesRenderer = gameObject.AddComponent<MeshRenderer>();
        smearFramesRenderer.sortingOrder = spriteRenderer.sortingOrder;

        renderTexture = new RenderTexture(256, 256, 0);

        internalMaterial = new Material(Shader.Find("Sprites/SpriteSmearFramesRenderer"));
        internalMaterial.mainTexture = renderTexture;
        smearFramesRenderer.sharedMaterial = internalMaterial;

        matProps = new MaterialPropertyBlock();
        smearFramesRenderer.SetPropertyBlock(matProps);        
        smearFramesRenderer.enabled = true;

        gameObject.AddComponent<MeshFilter>().mesh = CreateInternalQuadMesh();        
    }

    void OnDestroy()
    {
        Destroy(internalMaterial);    
    }

    void FixedUpdate()
    {
        newPosition = transform.position;

        if ((newPosition - lastPosition).sqrMagnitude < 1e-1)
        {
            t = 0;

            direction = Vector3.zero;
        }
        else
        {
            t += Time.deltaTime;

            lastPosition = Vector3.Lerp(lastPosition, newPosition, t / timeSlice);

            direction = lastPosition - newPosition;
        }

        if (Enable)
        {
            direction.x = Mathf.Clamp(direction.x, -intensityRange, intensityRange);
        }
        else
        {
            direction.x = 0f;
        }
    }

    void UpdateInternalProp()
    {
        // Calculate TRS of the sprite clone
        var bounds = sprite.bounds;
        var localPos = new Vector3(transform.position.x + bounds.center.x,
                transform.position.y + bounds.center.y, 0);
        var scale = new Vector3(2F * transform.lossyScale.x * bounds.extents.x,
                2F * transform.lossyScale.y * bounds.extents.y, 1F);

        matProps.SetMatrix("_ObjectToWorld", Matrix4x4.TRS(localPos, Quaternion.identity, scale));
        matProps.SetFloat("_Intensity", direction.x);
        matProps.SetVector("_Flip", new Vector2(spriteRenderer.flipX ? -1.0F : 1.0F, spriteRenderer.flipY ? -1.0F : 1.0F));
        smearFramesRenderer.SetPropertyBlock(matProps);
    }

    // Update is called once per frame
    void Update()
    {        
        sprite = spriteRenderer.sprite;

        // Set the active render texture
        RenderTexture.active = renderTexture;

        // Clear the temporary render texture
        GL.Clear(true, true, Color.clear);

        // Draw the sprite
        Graphics.Blit(sprite.texture, renderTexture);

        // Reset the active render texture
        RenderTexture.active = null;
        
        UpdateInternalProp();
    }
}

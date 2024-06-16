using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSmearFramesRenderer : MonoBehaviour
{
    private Sprite sprite;
    private RenderTexture renderTexture;
    private SpriteRenderer spriteRenderer;

    const int commandCount = 1;
    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    
    private Mesh internalMesh;
    private Material internalMat;

    private Vector3 lastPosition;
    private Vector3 newPosition;
    private Vector3 direction;
    private float t = 0;

    [Range(0.01f, 16f)] public float timeSlice = 2f;
    [Range(0.01f, 0.4f)] public float intensityRange = 0.26f;
    
    public bool enable = true;

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
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;

        sprite = spriteRenderer.sprite;

        // Set up the render texture
        renderTexture = new RenderTexture(256, 256, 0);
        // Set up afterimage quad mesh
        internalMesh = CreateInternalQuadMesh();
        // Set up afterimage material        
        internalMat = new Material(Shader.Find("Sprites/SpriteSmearFramesRenderer"));        
        internalMat.mainTexture = renderTexture;

        // Set up command buffer
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

        // Init position
        lastPosition = newPosition = transform.position;
    }

    void OnDestroy()
    {
        Destroy(internalMat);
        
        commandBuffer?.Release();
        commandBuffer = null;
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

        if (enable)
        {
            direction.x = Mathf.Clamp(direction.x, -intensityRange, intensityRange);
        }
        else
        {
            direction.x = 0f;
        }
    }

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

        // Calculate TRS of the sprite clone
        var bounds = sprite.bounds;
        var localPos = new Vector3(transform.position.x + bounds.center.x, 
                transform.position.y + bounds.center.y, 0);
        var scale = new Vector3(2F * transform.localScale.x * bounds.extents.x, 
                2F * transform.localScale.y * bounds.extents.y, 1F);        
            
        // Setup RenderParams
        RenderParams rp = new RenderParams(internalMat);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.TRS(localPos, Quaternion.identity, scale));
        rp.matProps.SetFloat("_Intensity", direction.x);
        rp.matProps.SetVector("_Flip", new Vector2(spriteRenderer.flipX ? -1.0F : 1.0F, spriteRenderer.flipY ? -1.0F : 1.0F));
        
        commandData[0].indexCountPerInstance = internalMesh.GetIndexCount(0);
        commandData[0].instanceCount = 1;
        commandBuffer.SetData(commandData);

        Graphics.RenderMeshIndirect(rp, internalMesh, commandBuffer, commandCount);
    }
}

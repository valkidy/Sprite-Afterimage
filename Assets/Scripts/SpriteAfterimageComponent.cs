using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpriteAfterimageComponent : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite sprite;

    private Renderer afterimageRenderer;
    private Material afterimageMaterial;
    private Material imageCropMaterial;
    private MaterialPropertyBlock matProps;
    public RenderTexture renderTexture;
    
    const int SpriteAfterimageInstanceCount = 16;
    private int slice = (int)Mathf.Sqrt(SpriteAfterimageInstanceCount);
    private float step = 1F / Mathf.Sqrt(SpriteAfterimageInstanceCount);

    private Matrix4x4[] worldMatrices = new Matrix4x4[SpriteAfterimageInstanceCount];

    #region Public properties

    public bool Enable { get; set; }

    #endregion

    static T[] Repeat<T>(T[] pattern, int repeat)
    {
        var typeList = new List<T>();
        Enumerable.Range(0, repeat)
            .ToList()
            .ForEach(n => typeList.AddRange(pattern));
        return typeList.ToArray();
    }

    Mesh CreateInternalStaticMesh(float width = 1F, float height = 1F)
    {
        Mesh mesh = new Mesh();

        var vertices = new List<Vector3>();
        Enumerable.Range(0, SpriteAfterimageInstanceCount)
            .ToList()
            .ForEach(n => vertices.AddRange(new Vector3[4]{
                 new Vector3(-width/2F, -height/2F, n),
                 new Vector3( width/2F, -height/2F, n),
                 new Vector3(-width/2F,  height/2F, n),
                 new Vector3( width/2F,  height/2F, n)
            }));
        mesh.vertices = vertices.ToArray();

        var triangles = new List<int>();
        Enumerable.Range(0, SpriteAfterimageInstanceCount)
            .ToList()
            .ForEach(n => triangles.AddRange(new int[6]{
                0 + 4 * n, 
                2 + 4 * n, 
                1 + 4 * n,
                2 + 4 * n,
                3 + 4 * n,
                1 + 4 * n
            }));
        mesh.triangles = triangles.ToArray();

        mesh.normals = Repeat(new Vector3[4]{
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            }, SpriteAfterimageInstanceCount);

        var uv = new List<Vector2>();
        Enumerable.Range(0, SpriteAfterimageInstanceCount)
            .ToList()
            .ForEach(n => uv.AddRange(new Vector2[4]{
                new Vector2(0    + step * (n / slice), 0    + step * (n % slice)),
                new Vector2(step + step * (n / slice), 0    + step * (n % slice)),
                new Vector2(0    + step * (n / slice), step + step * (n % slice)),
                new Vector2(step + step * (n / slice), step + step * (n % slice))
            }));        
        mesh.uv = uv.ToArray();
        
        return mesh;
    }

    void Start()
    {        
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
        
        // and using this component's renderer instead. 
        afterimageRenderer = gameObject.AddComponent<MeshRenderer>();
        afterimageRenderer.sortingOrder = spriteRenderer.sortingOrder;

        renderTexture = new RenderTexture(1024, 1024, 0);
    
        afterimageMaterial = new Material(Shader.Find("Sprites/SpriteAfterimageRenderer"));
        afterimageMaterial.mainTexture = renderTexture;
        afterimageRenderer.sharedMaterial = afterimageMaterial;

        imageCropMaterial = new Material(Shader.Find("Sprites/SpriteAfterimageCrop"));

        matProps = new MaterialPropertyBlock();
        afterimageRenderer.SetPropertyBlock(matProps);
        afterimageRenderer.enabled = true;

        gameObject.AddComponent<MeshFilter>().mesh = CreateInternalStaticMesh();
                
        worldMatrices = Repeat(new Matrix4x4[1] {Matrix4x4.zero}, SpriteAfterimageInstanceCount);
    }

    void OnDestroy()
    {
        Destroy(afterimageMaterial);
        Destroy(imageCropMaterial);        
    }

    int frameCount = 0;
    int frameIndex = -1;
    int skipFrame = 0;

    void SaveObjectToWorldMatrix(Sprite sprite, int frameIndex)
    {
        // Calculate TRS of the sprite clone
        var bounds = sprite.bounds;
        var localPos = new Vector3(
              transform.position.x + bounds.center.x,
              transform.position.y + bounds.center.y, 
              0);
        var scale = new Vector3(
              2F * transform.lossyScale.x * bounds.extents.x,
              2F * transform.lossyScale.y * bounds.extents.y, 
              1F);

        worldMatrices[frameIndex] = Matrix4x4.TRS(localPos, Quaternion.identity, scale);
    }

    void UpdateInternalProp()
    {        
        matProps.SetMatrixArray("_ObjectToWorld", worldMatrices);
        matProps.SetFloat("_FrameIndex", frameIndex);
        afterimageRenderer.SetPropertyBlock(matProps);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (skipFrame++ > 30)
        {
            skipFrame = 0;

            frameIndex = (frameIndex + 1) % SpriteAfterimageInstanceCount;

            frameCount++;

            // Update current sprite
            sprite = spriteRenderer.sprite;

            // Set the active render texture.
            RenderTexture.active = renderTexture;

            // We don't need to clear the temporary render texture.
            // GL.Clear(true, true, Color.clear);

            imageCropMaterial.SetVector("_Flip", new Vector2(spriteRenderer.flipX ? -1.0F : 1.0F, spriteRenderer.flipY ? -1.0F : 1.0F));
            imageCropMaterial.SetInt("_Slice", slice); 
            imageCropMaterial.SetInt("_FrameIndex", frameIndex);

            // Draw the sprite with the crop image on renderTexture.
            Graphics.Blit(sprite.texture, renderTexture, imageCropMaterial);

            // Reset the active render texture.
            RenderTexture.active = null;

            // Save current objctToWorld matrix base on frameIndex
            SaveObjectToWorldMatrix(sprite, frameIndex);

            // Update material property
            UpdateInternalProp();
        }       
    }
}

using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class MainCamera : MonoBehaviour
{
    [SerializeField]
    private Texture2D _waterHeightMap;

    [SerializeField]
    private int _animationTilesX;

    [SerializeField]
    private int _animationTilesY;

    [SerializeField, Range(12, 100)]
    private float _animationFPS;

    [SerializeField, Range(0.25f, 5.0f)]
    private float _lightShaftIntensity;

    [SerializeField, Range(0.25f, 5.0f)]
    private float _ambientLightIntensity;

    [SerializeField]
    private Texture2D _blueNoise;

    [SerializeField]
    private Color _waterColor;


    [SerializeField]
    private float _waterHeight;
    public float WaterHeight => _waterHeight;
    
    [SerializeField, Range(0.0f, 1.0f)]
    private float _stepSize;

    [SerializeField, Range(-1.0f, 1.0f)]
    private float _anisotropy;

    [SerializeField, Range(4, 64)]
    private int _steps;

    [SerializeField, Range(0.0f, 1.0f)]
    private float _density;
    public float Density
    {
        get => _density;
        set => _density = value;
    }

    [SerializeField]
    private Shader _shader;

    public Camera _camera
    {
        get
        {
            if (!_cam)
            {
                _cam = GetComponent<Camera>();
            }
            return _cam;
        }
    }

    private Camera _cam;

    private Material _shaderMaterial;

    public void Start()
    {
        if(!_shaderMaterial)
        {
            Debug.Log("Updated material");
            _shaderMaterial = new Material(_shader);
        }
        _camera.depthTextureMode = _camera.depthTextureMode | DepthTextureMode.Depth;
        _shaderMaterial.SetTexture("_WaterHeightMap", _waterHeightMap);
        _shaderMaterial.SetTexture("_BlueNoise", _blueNoise);
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_shaderMaterial)
        {
            Debug.Log("/!\\ unassigned shader material /!\\");
            Graphics.Blit(source, destination);
            return;
        }

        _shaderMaterial.SetVectorArray("_CamFrustum", NormalizedCamFrustum(_camera));
        _shaderMaterial.SetFloat("_WaterHeight", WaterHeight);
        _shaderMaterial.SetVector("_WaterColor", _waterColor);
        _shaderMaterial.SetFloat("_Density", Density);
        _shaderMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _shaderMaterial.SetVector("_CamPosition", _camera.transform.position);
        _shaderMaterial.SetVector("_BlueNoiseToSceen", new Vector4((float) Screen.width / _blueNoise.width, (float) Screen.height / _blueNoise.height, 0, 0));
        _shaderMaterial.SetFloat("_Anisotropy", _anisotropy);
        _shaderMaterial.SetFloat("_StepSize", _stepSize);
        _shaderMaterial.SetInt("_Steps", _steps);
        _shaderMaterial.SetFloat("_LightShaftIntensity", _lightShaftIntensity);
        _shaderMaterial.SetFloat("_AmbientLightIntensity", _ambientLightIntensity);
        _shaderMaterial.SetVector("_SunColor", RenderSettings.sun.color);
        _shaderMaterial.SetVector("_SunDirection", RenderSettings.sun.transform.TransformVector(0, 0, 1));

        _shaderMaterial.SetFloat("_zNear", _camera.nearClipPlane);
        _shaderMaterial.SetFloat("_zFar", _camera.farClipPlane);

        // animation
        int frame = (int) (Time.time * _animationFPS) % (_animationTilesX * _animationTilesY);
        Vector2 scale = new Vector2(1.0f / _animationTilesX, 1.0f / _animationTilesY);
        Vector2 offset = new Vector2(frame % _animationTilesX, frame / _animationTilesX) * scale;
        offset.y = 1.0f - scale.y - offset.y; //inverting on the y axis
        _shaderMaterial.SetVector("_HeightMapTileSize", scale);
        _shaderMaterial.SetVector("_HeightMapTileOffset", offset);

        Graphics.Blit(source, destination, _shaderMaterial);
    }

    private Vector4[] NormalizedCamFrustum(Camera cam)
    {
        

        Vector3[] frustumCorners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        Vector4[] frustumCorners4 = new Vector4[4];
        for (int i = 0; i < 4; i++)
        {
            Vector4 v = frustumCorners[i];
            frustumCorners4[i] = new Vector4(v.x/v.z, v.y/v.z, -1.0f, 0.0f);
        }
        return frustumCorners4;
    }
}

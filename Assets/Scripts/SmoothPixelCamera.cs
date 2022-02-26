using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[ExecuteAlways]
public class SmoothPixelCamera : MonoBehaviour
{
    public class SortingLayerAttribute : PropertyAttribute
    {
        [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
        public class SortingLayerDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                string[] sortingLayerNames = new string[SortingLayer.layers.Length];
                for (int a = 0; a < SortingLayer.layers.Length; a++)
                    sortingLayerNames[a] = SortingLayer.layers[a].name;
                if (property.propertyType != SerializedPropertyType.String){
                    EditorGUI.HelpBox(position, property.name + "{0} is not an string but has [SortingLayer].", MessageType.Error);
                } else if (sortingLayerNames.Length == 0){
                    EditorGUI.HelpBox(position, "There is no Sorting Layers.", MessageType.Error);
                } else if (sortingLayerNames != null){
                    EditorGUI.BeginProperty(position, label, property);

                    // Look up the layer name using the current layer ID
                    string oldName = property.stringValue;

                    // Use the name to look up our array index into the names list
                    int oldLayerIndex = -1;
                    for (int a = 0; a < sortingLayerNames.Length; a++)
                        if (sortingLayerNames[a].Equals(oldName)) oldLayerIndex = a;

                    // Show the popup for the names
                    int newLayerIndex = EditorGUI.Popup(position, label.text, oldLayerIndex, sortingLayerNames);

                    // If the index changes, look up the ID for the new index to store as the new ID
                    if (newLayerIndex != oldLayerIndex){
                        property.stringValue = sortingLayerNames[newLayerIndex];
                    }

                    EditorGUI.EndProperty();
                }
            }
        }
    }

    bool StringCompare(string a, string b) {
        if(a == null || b == null)
            return false;
        int aLen = a.Length;
        int bLen = b.Length;
        int pointer = 0;
        if(aLen != bLen)
            return false;

        while (pointer < aLen && a [pointer] == b [pointer]) {
            pointer++;
        }

        return pointer == aLen;
    }
    
    public float PPU = 8;
    [SortingLayer] public string CanvasSortingLayer = "Default"; 
    public LayerMask CameraCullingMask = 0;

    Camera m_mainCamera, m_pixelCamera;

    RenderTexture m_renderTexture;
    float m_scaleMultiplier = 3;
    float m_lastAspectRatio;

    Canvas m_pixelCanvas;
    RectTransform m_pixelCanvasRect;
    RawImage m_pixelRawImage;
    Vector2 m_canvasSizeDelta = new Vector2(32,18);

    void Start() {
        m_mainCamera = Camera.main;

        m_pixelCamera = GetComponentInChildren<Camera>();

        m_pixelCanvas = GetComponentInChildren<Canvas>();
        m_pixelCanvasRect = m_pixelCanvas.GetComponent<RectTransform>();
        m_pixelCanvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_pixelCamera.orthographicSize*2);
        m_pixelCanvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_pixelCamera.orthographicSize*2*m_mainCamera.aspect);
        m_pixelRawImage = m_pixelCanvas.GetComponentInChildren<RawImage>();

        SetScaleMultiplier();
        
        SetRenderTexture((int)(m_pixelCamera.orthographicSize*2*PPU));
    }

    void Update() {
        //Canvas Sorting Layer update
        if(!StringCompare(CanvasSortingLayer, m_pixelCanvas.sortingLayerName))
            m_pixelCanvas.sortingLayerName = CanvasSortingLayer;
        //Camera Culling Mask update
        if(CameraCullingMask != m_pixelCamera.cullingMask)
            m_pixelCamera.cullingMask = CameraCullingMask;
        //Scale Multiplier Update
        if(m_lastAspectRatio != m_mainCamera.aspect)
            SetScaleMultiplier();

        //set Canvas Scale and Render Texture if orthographic size changes
        int orthographicSize = (int)((int)((m_mainCamera.orthographicSize+1+m_scaleMultiplier)/m_scaleMultiplier)*m_scaleMultiplier);
        if(m_pixelCamera.orthographicSize != orthographicSize){
            m_pixelCamera.orthographicSize = orthographicSize;

            m_pixelCanvasRect.SetSizeWithCurrentAnchors( RectTransform.Axis.Vertical, m_pixelCamera.orthographicSize*2);
            m_pixelCanvasRect.SetSizeWithCurrentAnchors( RectTransform.Axis.Horizontal, m_pixelCamera.orthographicSize*2*m_mainCamera.aspect);

            SetRenderTexture((int)(m_pixelCamera.orthographicSize*2*PPU));
        }
        //set camera positon
        float x = m_mainCamera.transform.position.x * PPU;
        float y = m_mainCamera.transform.position.y * PPU;
        float roundX = (int)(x+.5f);
        float roundY = (int)(y+.5f);
        m_pixelCamera.transform.position = new Vector3(roundX / PPU, roundY / PPU, -10);

        m_pixelCanvasRect.position = m_mainCamera.transform.position + new Vector3((roundX - x)  / PPU ,(roundY - y)  / PPU, 10);
    }

    void SetRenderTexture(int renderTextureSize) {
        if(renderTextureSize <= 0)
            renderTextureSize = 1;
        if(m_renderTexture){
            m_renderTexture.Release();
        }
        m_renderTexture = new RenderTexture((int)(renderTextureSize * m_mainCamera.aspect), renderTextureSize, 16);
        m_renderTexture.filterMode = FilterMode.Point;
        m_pixelCamera.targetTexture = m_renderTexture;
        m_pixelRawImage.texture = m_renderTexture;      
    }

    void SetScaleMultiplier() {
        m_lastAspectRatio = m_mainCamera.aspect;

        string ratio = m_mainCamera.aspect.ToString("0.00", CultureInfo.InvariantCulture);
        switch (ratio.Substring(0, 4))
        {
        //9:16
        case "0.56":
            m_scaleMultiplier = 16;
            break;
        //16:10
        case "1.60":
        case "1.56":
            m_scaleMultiplier = 10;
            break;
        //21:9
        case "2.37":
        case "2.39":
        //16:9
        case "1.67":
        case "1.78":
        case "1.77":
            m_scaleMultiplier = 9;
            break;
        //5:4
        case "1.25":
            m_scaleMultiplier = 4;
            break;
        //4:3
        case "1.33":
        //2:3
        case "0.67":
            m_scaleMultiplier = 3;
            break;
        //3:2
        case "1.50":
            m_scaleMultiplier = 2;
            break;
        //1:1
        case "1.00":
        //2:1
        case "2.00":
        //3:1
        case "3.00":
            m_scaleMultiplier = 1;
            break;
        default:
            m_scaleMultiplier = 12;
            break;
        }
    }
}
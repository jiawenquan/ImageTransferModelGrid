using System.IO;
using UnityEditor;
using UnityEngine;
using System;
/// <summary>
/// 就差机器学习训练出的高度权重算法了  https://github.com/AaronJackson/vrn
/// </summary>
public class PictureGridWindow : EditorWindow
{

    private enum ErrorMessage
    {
        None,
        TextureDoNotRead,
    };

    //private ErrorMessage errorMessage;//=ErrorMessage.None;

    public Texture heightMap;

    private Material diffuseMap;  //材质贴图

    #region 通过特定的方法获取 赋值
    private Vector3[] vertives;   //储存顶点数据的向量 计算后获取
    private Vector2[] uvs;  //储存UVS 坐标
    #endregion

    private Mesh mesh;      //用来 储存mesh网格组建
    private int[] triangles;//三角面


    //生成信息 
    private Vector2 size;  //长度


    //这里回头不用 这两个参数
    //private float minHeight = -10;
    //private float maxHeight = 10;

    private Vector2 segment;
    //private float unitH;        //地形的高度比例参数

    private GameObject terrain;  //地形


    [MenuItem("Window/PictureGrid/创建图片网格")]  //LoadCloud
    public static void SetTerrain()
    {
        //创建一个窗体
        EditorWindow window = GetWindow(typeof(PictureGridWindow), true, "Texture 转模型网格");
        window.maxSize = new Vector2(485f, 475f); //窗体的最大尺寸 385f, 375f
        window.minSize = window.maxSize;          //窗体的最小尺寸
    }
    
    private void OnGUI()
    {

        //switch (errorMessage)
        //{
        //    case ErrorMessage.None:
        //        break;

        //    case ErrorMessage.TextureDoNotRead:
        //        ShowNotification(new GUIContent("请勾选为图片Inspector 的\"Read/Write Enabled\""));
        //        errorMessage = ErrorMessage.None;
        //        break;
        //    default:
        //        break;
        //}

        heightMap = EditorGUILayout.ObjectField("添加图片", heightMap, typeof(Texture2D), true) as Texture2D;

        //创建一个按钮
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        buttonStyle.fontStyle = FontStyle.Bold;
        if (heightMap==null)
        {
            
        }
        if (GUILayout.Button("图片转模型网格", buttonStyle, GUILayout.Height(50))&&heightMap!=null)
        {
            //创建PointCloud目录
            if (!Directory.Exists(Application.dataPath + "/PictureGrids/"))
            {

                AssetDatabase.CreateFolder("Assets", "PictureGrids");
            }

            float AspectRatio = heightMap.width / heightMap.height; //图片的宽高比例
            SetTerrain(100,(int)100 * AspectRatio,  99, 99, -10, 10); //地形宽度，地形高度，宽度的段数100*100个点  太大会报错  后面两个数规定地形的最大高度最小高度 来确定高度的比例
            
        }
    }


    public void SetTerrain(float width,float height,uint segmentX,uint segmentY,int min,int max)
    {
        Init(width,height,segmentX,segmentY,min,max);
        GetVertives();
        DrawMesh();
    }

    //初始化计算的某些值
    private void Init(float width, float height, uint segmentX, uint segmentY, int min, int max)
    {
        size = new Vector2(width,height); //尺寸  这里 回头写成动态的 这里根据图片的尺寸长宽比例动态

        //maxHeight = max;
        //minHeight = min;

        //unitH = maxHeight - minHeight;
        segment = new Vector2(segmentX,segmentY); 
    }

    private void DrawMesh()
    {
        //if (mesh == null)
        //{
        //    mesh = new Mesh();
        //}
        terrain = new GameObject(heightMap.name); // 创建出物体
        terrain.AddComponent<MeshFilter>();  //


        //这一步很重要
        terrain.GetComponent<MeshFilter>().mesh = mesh = new Mesh(); ;  //添加 MeshFilter组建  并获取到 mesh组件

        if (!Directory.Exists(Application.dataPath + "/PictureGrids/" + heightMap.name + "Material" + ".mat"))
        {
            //Debug.LogWarning("No material,Create diffuse!!");
            diffuseMap = new Material(Shader.Find("Unlit/Texture")); //创建材质
            AssetDatabase.CreateAsset(diffuseMap, "Assets/PictureGrids/" + heightMap.name + "Material" + ".mat");
            diffuseMap.mainTexture = heightMap;
        }
        if (diffuseMap == null)
        {
            Debug.Log("No heightMap!!!");
        }
        terrain.AddComponent<MeshRenderer>();
        terrain.GetComponent<Renderer>().material = diffuseMap;

        //Debug.Log(mesh);
        mesh.Clear(); //网格初始化
        mesh.vertices = vertives; //给网格顶点赋值
        mesh.uv = uvs;            //赋值uv
        mesh.triangles = triangles; //赋值三角面片
                                    //重置法线
        mesh.RecalculateNormals();
        //重置范围
        mesh.RecalculateBounds();
        //Debug.Log("创建模型");


        if (!Directory.Exists(Application.dataPath + "/PictureGrids/" + heightMap.name + @"/"))
        {

            AssetDatabase.CreateFolder("Assets/PictureGrids", heightMap.name);
            UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/PictureGrids/" + heightMap.name + @"/" + heightMap.name + ".asset"); //创建.asset文件储存网格数据
            UnityEditor.AssetDatabase.SaveAssets(); //保存.asset网格文件
            UnityEditor.AssetDatabase.Refresh();



        }
        UnityEditor.PrefabUtility.CreatePrefab("Assets/PictureGrids/" + heightMap.name + ".prefab", terrain);
        //terrain = null;
    }

    private void GetVertives()
    {
        GetUV();              //回头放在外边
        GetTriangles();

        int sum = Mathf.FloorToInt((segment.x + 1) * (segment.y + 1));  //要创建的总点数

        float w = size.x / segment.x;
        float h = size.y / segment.y;

        int index = 0;

        vertives = new Vector3[sum];  //创建模型网格的顶点 坐标数组
        for (int i = 0; i < segment.x + 1; i++)
        {
            for (int j = 0; j < segment.x + 1; j++)
            {
                float tempHeight = 0;
                if (heightMap != null)
                {
                    tempHeight = GetHeight((Texture2D)heightMap, uvs[index]);  //给每个顶点数据的高度赋值
                }
                vertives[index] = new Vector3((j * h - 50)*0.01f, tempHeight * 0.01f, (i * w - 50)*0.01f);


                index++;
            }
        }
    }

    /// <summary>
    /// 生成UV信息
    /// </summary>
    /// <returns></returns>
    private void GetUV()  //Vector2[]
    {
        int sum = Mathf.FloorToInt((segment.x + 1) * (segment.y + 1));
        uvs = new Vector2[sum];
        float u = 1.0F / segment.x;
        float v = 1.0F / segment.y;
        uint index = 0;
        for (int i = 0; i < segment.y + 1; i++)
        {
            for (int j = 0; j < segment.x + 1; j++)
            {
                uvs[index] = new Vector2(j * u, i * v);
                index++;
            }
        }
        //return uvs;
    }

    /// <summary>
    /// 生成三角面片的索引信息
    /// </summary>
    private void GetTriangles()  //int[]
    {
        int sum = Mathf.FloorToInt(segment.x * segment.y * 6);
        triangles = new int[sum];
        uint index = 0;
        for (int i = 0; i < segment.x; i++)
        {
            for (int j = 0; j < segment.y; j++)
            {
                int role = Mathf.FloorToInt(segment.x) + 1;  //
                int self = j + (i * role);
                int next = j + ((i + 1) * role);
                triangles[index] = self;
                triangles[index + 1] = next + 1;
                triangles[index + 2] = self + 1;
                triangles[index + 3] = self;
                triangles[index + 4] = next;
                triangles[index + 5] = next + 1;
                index += 6;
            }
        }
       // return triangles;
    }


    private float GetHeight(Texture2D texture,Vector2 uv)
    {
        if (texture != null)
        {
            Color c = GetColor(texture, uv);
            float gray = c.grayscale; //得到像素点的灰度值
            float h = gray * 10;    //这里公开一个值用来 调节高度
            return h;
        }
        else
        {
            return 0;
        }
    }
    private Color GetColor(Texture2D texture,Vector2 uv)
    {
        //获取到每个 uv所对应的像素
        try
        {

            Color color = texture.GetPixel(Mathf.FloorToInt(texture.width * uv.x), Mathf.FloorToInt(texture.height * uv.y));

            return color;

        }
        catch (Exception)
        {
            this.ShowNotification(new GUIContent("请先勾选为图片Inspector 的\"Read/Write Enabled\""));
            //           
            return Color.white;
            
        }
        

        
    }
}

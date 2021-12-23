// -----------------------------------------------------------------------
// <file>World.cs</file>
// <copyright>Grupa za Grafiku, Interakciju i Multimediju 2013.</copyright>
// <author>Srđan Mihić</author>
// <author>Aleksandar Josić</author>
// <summary>Klasa koja enkapsulira OpenGL programski kod.</summary>
// -----------------------------------------------------------------------
using System;
using Assimp;
using System.IO;
using System.Reflection;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Quadrics;
using SharpGL.SceneGraph.Core;
using SharpGL;

namespace AssimpSample
{


    /// <summary>
    ///  Klasa enkapsulira OpenGL kod i omogucava njegovo iscrtavanje i azuriranje.
    /// </summary>
    public class World : IDisposable
    {
        #region Atributi

        /// <summary>
        ///	 Ugao rotacije sveta oko X ose.
        /// </summary>
        private float m_xRotation = 0.0f;

        /// <summary>
        ///	 Ugao rotacije sveta oko Y ose.
        /// </summary>
        private float m_yRotation = 0.0f;

        /// <summary>
        ///	 Udaljenost scene od kamere.
        /// </summary>
        private float m_sceneDistance = 2500.0f;

        /// <summary>
        ///	 Sirina OpenGL kontrole u pikselima.
        /// </summary>
        private int m_width;

        /// <summary>
        ///	 Visina OpenGL kontrole u pikselima.
        /// </summary>
        private int m_height;
        private readonly AssimpScene m_trafficLightScene;

        private readonly AssimpScene m_motorcycleScene;

        private readonly float m_motorcycleScale = 0.01f;

        private readonly float m_trafficLightScale = 15.0f;

        private readonly float m_groundScaleX = 2000.0f;
        
        private readonly float m_groundScaleZ = 1800.0f;
        
        private readonly float[] m_groundVertices = new float[] {
            1.0f, 0.0f,-1.0f,
           -1.0f, 0.0f,-1.0f,
           -1.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 1.0f,
        };

        private readonly Cylinder m_post;

        private readonly float m_spaceBetweenPosts = 400.0f;
        
        private readonly float m_roadWidth = 520.0f;

        private float[] m_yellowLightingPosition = new float[] { 300.0f, 2000.0f, 0f, 1.0f };

        private float[] m_yellowLightingAmbient = new float[] { 1.0f, 1.0f, 0.0f, 1.0f };

    #endregion Atributi

    #region Properties


    /// <summary>
    ///	 Ugao rotacije sveta oko X ose.
    /// </summary>
    public float RotationX
        {
            get { return m_xRotation; }
            set { m_xRotation = value; }
        }

        /// <summary>
        ///	 Ugao rotacije sveta oko Y ose.
        /// </summary>
        public float RotationY
        {
            get { return m_yRotation; }
            set { m_yRotation = value; }
        }

        /// <summary>
        ///	 Udaljenost scene od kamere.
        /// </summary>
        public float SceneDistance
        {
            get { return m_sceneDistance; }
            set { m_sceneDistance = value; }
        }

        /// <summary>
        ///	 Sirina OpenGL kontrole u pikselima.
        /// </summary>
        public int Width
        {
            get { return m_width; }
            set { m_width = value; }
        }

        /// <summary>
        ///	 Visina OpenGL kontrole u pikselima.
        /// </summary>
        public int Height
        {
            get { return m_height; }
            set { m_height = value; }
        }

        #endregion Properties

        #region Konstruktori

        /// <summary>
        ///  Konstruktor klase World.
        /// </summary>
        public World(int width, int height, OpenGL gl)
        {
            m_width = width;
            m_height = height;

            string motorcyclePath = CreatePath("Motorcycle");
            string motorcycleFileName = "DUC916_L.3DS";
            m_motorcycleScene = new AssimpScene(motorcyclePath, motorcycleFileName, gl);

            string trafficLightPath = CreatePath("TrafficLight");
            string trafficLightFileName = "trafficlight.obj";
            m_trafficLightScene = new AssimpScene(trafficLightPath, trafficLightFileName, gl);

            m_post = CreatePost(gl);
        }

        /// <summary>
        ///  Destruktor klase World.
        /// </summary>
        ~World()
        {
            Dispose(false);
        }

        #endregion Konstruktori

        #region Metode
        public static string CreatePath(string subDirectoryName)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"3D Models\\{subDirectoryName}");
        } 

        /// <summary>
        ///  Korisnicka inicijalizacija i podesavanje OpenGL parametara.
        /// </summary>
        public void Initialize(OpenGL gl)
        {
            gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            gl.ShadeModel(OpenGL.GL_SMOOTH);
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            SetScenes();

            // 2.1
            gl.ColorMaterial(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT_AND_DIFFUSE);
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);

            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_NORMALIZE);
        }

        private void SetScenes()
        {
            m_motorcycleScene.LoadScene();
            m_motorcycleScene.Initialize();
            m_trafficLightScene.LoadScene();
            m_trafficLightScene.Initialize();
        }

        /// <summary>
        ///  Iscrtavanje OpenGL kontrole.
        /// </summary>
        public void Draw(OpenGL gl)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            gl.PushMatrix();
            gl.Translate(0.0f, -50.0f, -m_sceneDistance);
            gl.Rotate(m_xRotation, 1.0f, 0.0f, 0.0f);
            gl.Rotate(m_yRotation, 0.0f, 1.0f, 0.0f);

            PositionGround(gl);
            PositionModels(gl);
            PositionBuildings(gl);
            PositionText(gl);
            PositionYellowLight(gl);

            gl.PopMatrix();
            // Oznaci kraj iscrtavanja
            gl.Flush();
        }

        private void PositionGround(OpenGL gl)
        {
            gl.Color(0.1f, 0.1f, 0.1f);
            gl.Begin(OpenGL.GL_QUADS);
            gl.Normal(0.0f, 1.0f, 0.0f);

            for (int i = 0; i < m_groundVertices.Length; i += 3)
            {
                gl.Vertex(
                    m_groundScaleX * m_groundVertices[i],
                              1.0f * m_groundVertices[i + 1],
                    m_groundScaleZ * m_groundVertices[i + 2]);
            }
            gl.End();
        }

        private void PositionModels(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Translate(-250.0f, 0.0f, 80.0f);
            PositionTrafficLight(gl);
            PositionMotorcycle(gl);
            PositionLampPosts(gl);
            gl.PopMatrix();
        }

        private void PositionTrafficLight(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Scale(m_trafficLightScale, m_trafficLightScale, m_trafficLightScale);
            m_trafficLightScene.Draw();
            gl.PopMatrix();
        }

        private void PositionMotorcycle(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Translate(100.0f, 10.0f, 100.0f);
            gl.Rotate(180.0f, 0.0f, 1.0f, 0.0f);
            gl.Scale(m_motorcycleScale, m_motorcycleScale, m_motorcycleScale);
            m_motorcycleScene.Draw();
            gl.PopMatrix();
        }

        private void PositionLampPosts(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Translate(0.0f, 0.0f, m_spaceBetweenPosts);
            CreateLampPost(gl);
            gl.Translate(0.0f, 0.0f, m_spaceBetweenPosts);
            CreateLampPost(gl);
            gl.Translate(m_roadWidth, 0.0f, 0.0f);
            CreateLampPost(gl);
            gl.Translate(0.0f, 0.0f, -m_spaceBetweenPosts);
            CreateLampPost(gl);
            gl.PopMatrix();
        }

        private Cylinder CreatePost(OpenGL gl)
        {
            Cylinder post = new Cylinder();
            float postRadius = 4.5f;
            post.BaseRadius = postRadius;
            post.TopRadius = postRadius;
            post.Height = 165;
            post.Slices = 100;
            post.Stacks = 20;
            post.CreateInContext(gl);
            post.NormalGeneration = Normals.Smooth;
            return post;
        }

        private void CreateLampPost(OpenGL gl)
        {
            Cube lamp = new Cube();
            float lampEdge = 7.5f;
            
            gl.PushMatrix();
            gl.Color(0.3f, 0.3f, 0.3f);
            gl.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);
            m_post.Render(gl, RenderMode.Render);
            gl.PopMatrix();
            
            gl.PushMatrix();
            gl.Color(0.6f, 0.6f, 0.6f);
            gl.Translate(0.0f, m_post.Height + (lampEdge / 2), 0.0f);
            gl.Scale(lampEdge, lampEdge, lampEdge);
            CreateRedLight(gl);
            lamp.Render(gl, RenderMode.Render);
            gl.PopMatrix();
        }

        private void CreateRedLight(OpenGL gl)
        {
            float[] light1pos = new float[] { 0.0f, 0.0f, 0.0f, 1.0f };
            float[] light1ambient = new float[] { 1.0f, 0.0f, 0.0f, 1.0f };
            float[] light1diffuse = new float[] { 0.3f, 0.3f, 0.3f, 1.0f };
            float[] light1specular = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };
            float[] spotDirection = new float[] { -1.0f, -1.0f, -1.0f, 0.0f };

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, light1pos);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_AMBIENT, light1ambient);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, light1diffuse);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPECULAR, light1specular);

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_CUTOFF, 40.0f);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_EXPONENT, 5.0f);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_DIRECTION, spotDirection);
            gl.Enable(OpenGL.GL_LIGHT1);
        }

        private void PositionBuildings(OpenGL gl)
        {
            Cube building = new Cube();
            gl.PushMatrix();
            gl.Color(0.3f, 0.3f, 0.3f);
            gl.Translate(-700.0f, 700.0f, 600.0f);
            gl.Scale(250.0f, 700.0f, 350.0f);
            building.Render(gl, RenderMode.Render);

            gl.Translate(5.6f, 0.0f, 0.0f);
            building.Render(gl, RenderMode.Render);

            gl.Translate(0.0f, 0.0f, -4.8f);
            building.Render(gl, RenderMode.Render);

            gl.Translate(-5.6f, 0.0f, 0.0f);
            building.Render(gl, RenderMode.Render);

            gl.PopMatrix();
        }

        private void PositionText(OpenGL gl)
        {
            string textSubject = "Predmet: Racunarska grafika";
            string textSchoolYear = "Sk.god: 2021 / 22.";
            string textName = "Ime: Zarko";
            string textSurname = "Prezime: Blagojevic";
            string textAssignment = "Sifra zad: 11.1";

            int offsetHeight = 32;
            int wpWidth = 150;
            gl.Viewport(m_width - wpWidth, 0, wpWidth , m_height / 3);
            gl.DrawText(0, wpWidth, 0.5294f, 0.8078f, 0.9216f, "Arial Italic", 10.0f, textSubject);
            gl.DrawText(0, wpWidth - 1 * offsetHeight, 0.5294f, 0.8078f, 0.9216f, "Arial Italic", 10.0f, textSchoolYear);
            gl.DrawText(0, wpWidth - 2 * offsetHeight, 0.5294f, 0.8078f, 0.9216f, "Arial Italic", 10.0f, textName);
            gl.DrawText(0, wpWidth - 3 * offsetHeight, 0.5294f, 0.8078f, 0.9216f, "Arial Italic", 10.0f, textSurname);
            gl.DrawText(0, wpWidth - 4 * offsetHeight, 0.5294f, 0.8078f, 0.9216f, "Arial Italic", 10.0f, textAssignment);
            gl.Viewport(0, 0, m_width, m_height);
        }

        private void PositionYellowLight(OpenGL gl)
        {
            float[] light0pos = m_yellowLightingPosition;
            float[] light0ambient = m_yellowLightingAmbient;
            float[] light0diffuse = new float[] { 0.3f, 0.3f, 0.3f, 1.0f };
            float[] light0specular = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };

            // ShowYellowLightPosition(gl, light0pos);

            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, light0pos);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, light0ambient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, light0diffuse);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, light0specular);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPOT_CUTOFF, 180.0f);

            gl.Enable(OpenGL.GL_LIGHT0);
        }

        private static void ShowYellowLightPosition(OpenGL gl, float[] light0pos)
        {
            Cube light = new Cube();
            gl.PushMatrix();
            gl.Translate(light0pos[0], light0pos[1], light0pos[2]);
            gl.Scale(30.0f, 30.0f, 30.0f);
            light.Render(gl, RenderMode.Render);
            gl.PopMatrix();
        }



        /// <summary>
        /// Podesava viewport i projekciju za OpenGL kontrolu.
        /// </summary>
        public void Resize(OpenGL gl, int width, int height)
        {
            m_width = width;
            m_height = height;
            gl.Viewport(0, 0, m_width, m_height);
            gl.MatrixMode(OpenGL.GL_PROJECTION);      // selektuj Projection Matrix
            gl.LoadIdentity();
            gl.Perspective(45f, (double)width / height, 0.5f, 10000f);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();                // resetuj ModelView Matrix
        }

        /// <summary>
        ///  Implementacija IDisposable interfejsa.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_motorcycleScene.Dispose();
                m_trafficLightScene.Dispose();
            }
        }

        #endregion Metode

        #region IDisposable metode

        /// <summary>
        ///  Dispose metoda.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable metode
    }
}

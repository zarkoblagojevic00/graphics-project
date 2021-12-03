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

        private readonly float m_groundScaleX = 1000.0f;
        
        private readonly float m_groundScaleZ = 1500.0f;
        
        private readonly float[] m_groundVertices = new float[] {
            1.0f, 0.0f,-1.0f,
           -1.0f, 0.0f,-1.0f,
           -1.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 1.0f,
        };

        private readonly Cylinder m_post;

        private readonly float m_spaceBetweenPosts = 400.0f;

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
            this.m_width = width;
            this.m_height = height;

            var motorcyclePath = CreatePath("Motorcycle");
            var motorcycleFileName = "DUC916_L.3DS";
            this.m_motorcycleScene = new AssimpScene(motorcyclePath, motorcycleFileName, gl);
            
            var trafficLightPath = CreatePath("TrafficLight");
            var trafficLightFileName = "trafficlight.obj";
            this.m_trafficLightScene = new AssimpScene(trafficLightPath, trafficLightFileName, gl);

            this.m_post = CreatePost(gl);
        }

        /// <summary>
        ///  Destruktor klase World.
        /// </summary>
        ~World()
        {
            this.Dispose(false);
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
            gl.Color(1f, 0f, 0f);
            // Model sencenja na flat (konstantno)

            gl.ShadeModel(OpenGL.GL_FLAT);
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            SetScenes();
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

            gl.PopMatrix();
            // Oznaci kraj iscrtavanja
            gl.Flush();
        }

        private void PositionGround(OpenGL gl)
        {
            gl.Color(0.1f, 0.1f, 0.1f);
            gl.Begin(OpenGL.GL_QUADS);

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
            CreateLampPost(gl, m_post);
            gl.Translate(0.0f, 0.0f, m_spaceBetweenPosts);
            CreateLampPost(gl, m_post);
            gl.PopMatrix();
        }

        private Cylinder CreatePost(OpenGL gl)
        {
            Cylinder post = new Cylinder();
            var postRadius = 4.5f;
            post.BaseRadius = postRadius;
            post.TopRadius = postRadius;
            post.Height = 165;
            post.Slices = 100;
            post.Stacks = 20;
            post.CreateInContext(gl);
            return post;
        }

        private static void CreateLampPost(OpenGL gl, Cylinder post)
        {
            Cube lamp = new Cube();
            var lampEdge = 7.5f;
            
            gl.PushMatrix();
            gl.Color(0.3f, 0.3f, 0.3f);
            gl.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);
            post.Render(gl, RenderMode.Render);
            gl.PopMatrix();
            
            gl.PushMatrix();
            gl.Color(0.65f, 0.35f, 0.0f);
            gl.Translate(0.0f, post.Height + lampEdge/2, 0.0f);
            gl.Scale(lampEdge, lampEdge, lampEdge);
            lamp.Render(gl, RenderMode.Render);
            gl.PopMatrix();
        }

        private void PositionBuildings(OpenGL gl)
        {
            Cube building = new Cube();
            gl.PushMatrix();
            gl.Color(0.22f, 0.28f, 0.31f);
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
            gl.Perspective(45f, (double)width / height, 0.5f, 20000f);
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
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable metode
    }
}

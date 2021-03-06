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
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace AssimpSample
{


    /// <summary>
    ///  Klasa enkapsulira OpenGL kod i omogucava njegovo iscrtavanje i azuriranje.
    /// </summary>
    public class World : IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

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

        private readonly float[] m_groundTextures = new float[] {
            0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f,
            1.0f, 1.0f, 0.0f,
            1.0f, 0.0f, 0.0f
        };


        private readonly float m_spaceBetweenPosts = 400.0f;

        private readonly float m_roadWidth = 520.0f;

        private float[] m_yellowLightingPosition = new float[] { 300.0f, 2000.0f, 0f, 1.0f };

        private readonly int m_textureCount = 0;

        private uint[] m_textureIds = null;

        private string[] m_textureFiles = null;

        private enum TextureObjects { Wood = 0, Concrete };

        private DispatcherTimer m_timer = new DispatcherTimer();

        private int m_stepsNumber = 230;
        private float[] m_translateMotorX; 
        private float[] m_translateMotorZ;
        private float[] m_rotateMotorY;
        private float[] m_rotateMotorZ;
        private int m_delay = 0;

        private int m_turnBegin = 30;
        private int m_turnEnd = 75;

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

        public IList<float> LampPostScaleValues { get; internal set; }
        public IList<float> ChannelValues { get; internal set; }
        public IList<float> VelocityValues { get; internal set; }
        public float LampPostScale { get; set; }
        public float RChannel { get; set; }
        public float GChannel { get; set; }
        public float BChannel { get; set; }

        private float motorVelocity;
        public float MotorVelocity
        {
            get { return motorVelocity; }
            set
            {
                motorVelocity = value;
                m_timer.Interval = TimeSpan.FromMilliseconds(40 / motorVelocity);
            }
        }

        private bool notInAnimation;
        public bool NotInAnimation
        {
            get { return notInAnimation; }
            set
            {
                if (notInAnimation != value)
                {
                    notInAnimation = value;
                    OnPropertyChanged("NotInAnimation");
                }
            }
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

            InitializeChoosableValues();
            InitializeAnimation();

            string motorcyclePath = CreatePath("Motorcycle");
            string motorcycleFileName = "DUC916_L.3DS";
            m_motorcycleScene = new AssimpScene(motorcyclePath, motorcycleFileName, gl);

            string trafficLightPath = CreatePath("TrafficLight");
            string trafficLightFileName = "trafficlight.obj";
            m_trafficLightScene = new AssimpScene(trafficLightPath, trafficLightFileName, gl);

            string basePath = CreatePath("Textures");
            m_textureFiles = new string[] { Path.Combine(basePath, "wood-texture.jpg"), Path.Combine(basePath, "concrete-texture.jpg") };
            m_textureCount = m_textureFiles.Length;
            m_textureIds = new uint[m_textureCount];
        }

        private void InitializeAnimation()
        {
            NotInAnimation = true;
            m_timer.Tick += new EventHandler(MotorAnimation);
            InitTranslateX();
            InitTranslateZ();
            InitRotateY();
            InitRotateZ();
        }

        private void MotorAnimation(object sender, EventArgs e)
        {
            m_delay++;
            if (m_delay > m_stepsNumber - 5)
                ResetAnimation();
        }

        private void ResetAnimation()
        {
            m_timer.Stop();
            NotInAnimation = true;
            m_delay = 0;
        }

        private void InitTranslateX()
        {
            m_translateMotorX = new float[m_stepsNumber];
            float val = 0.0f;

            for (int i = 0; i < m_stepsNumber; i++)
            {
                if (i > m_turnBegin + 3)
                    val += 5.0f;
                m_translateMotorX[i] = val;
            }
        }

        private void InitTranslateZ()
        {
            m_translateMotorZ = new float[m_stepsNumber];
            float val = 0.0f;

            for (int i = 0; i < m_stepsNumber; i++)
            {
                if (i < m_turnEnd - 8)
                {
                    val += 5.0f;
                }
                m_translateMotorZ[i] = val;

            }
        }

        private void InitRotateY()
        {
            m_rotateMotorY = new float[m_stepsNumber];
            float val = 0.0f;

            for (int i = 0; i < m_stepsNumber; i++)
            {
                if (i > m_turnBegin && i < m_turnEnd)
                    val += 2;
                
                m_rotateMotorY[i] = val;
            }
        }

        private void InitRotateZ()
        {
            m_rotateMotorZ = new float[m_stepsNumber];
            float val = 0.0f;
            float turnMiddle = (m_turnBegin + m_turnEnd) / 2.0f;
            for (int i = 0; i < m_stepsNumber; i++)
            {
                if (i > m_turnBegin - 5 && i < turnMiddle - 5)
                    val -= 2.0f;
                if (i >= turnMiddle + 5 && i < m_turnEnd + 10)
                    val += 1.8f;

                m_rotateMotorZ[i] = val;
            }
        }

        public void DoAnimation()
        {
            NotInAnimation = false;
            m_timer.Start();
            Console.WriteLine("Did Animation!");
        }

        private void InitializeChoosableValues()
        {
            InitializeLampScaleValues();
            InitializeChannelValues();
            InitializeVelocityValues();
        }

        private void InitializeLampScaleValues()
        {
            LampPostScaleValues = new List<float>() {
                0.25f, 0.5f, 0.75f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f,
            };
        }

        private void InitializeChannelValues()
        {
            ChannelValues = new List<float>();
            for (int i = 0; i <= 100; i++)
            {
                ChannelValues.Add(i / 100.0f);
            }
        }

        private void InitializeVelocityValues()
        {
            VelocityValues = new List<float>() {
                0.25f, 0.5f, 0.75f, 1.0f, 2.0f, 3.0f
            };
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
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "3D Models", subDirectoryName);
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

            // Lighting
            gl.ColorMaterial(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT_AND_DIFFUSE);
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);

            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_NORMALIZE);

            SetTextures(gl);
        }

        private void SetScenes()
        {
            m_motorcycleScene.LoadScene();
            m_motorcycleScene.Initialize();
            m_trafficLightScene.LoadScene();
            m_trafficLightScene.Initialize();
        }
        private void SetTextures(OpenGL gl)
        {
            gl.Enable(OpenGL.GL_TEXTURE_2D);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_ADD);
            gl.GenTextures(m_textureCount, m_textureIds);
            for (int i = 0; i < m_textureCount; ++i)
            {
                // Pridruzi teksturu odgovarajucem identifikatoru
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textureIds[i]);

                // Ucitaj sliku i podesi parametre teksture
                string filename = m_textureFiles[i];
                Bitmap image = new Bitmap(filename);

                // rotiramo sliku zbog koordinantog sistema opengl-a
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                // RGB format 
                BitmapData imageData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGB8, imageData.Width, imageData.Height, 0, OpenGL.GL_BGR, OpenGL.GL_UNSIGNED_BYTE, imageData.Scan0);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_REPEAT);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_REPEAT);

                image.UnlockBits(imageData);
                image.Dispose();
            }
        }


        /// <summary>
        ///  Iscrtavanje OpenGL kontrole.
        /// </summary>
        public void Draw(OpenGL gl)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            gl.PushMatrix();
            gl.Translate(0.0f, -50.0f, -m_sceneDistance);
            gl.LookAt(0.0f, 15.0f, 40.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f);
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
            gl.MatrixMode(OpenGL.GL_TEXTURE);
            gl.PushMatrix();
            gl.Scale(0.8f, 0.8f, 0.8f);

            gl.Normal(0.0f, 1.0f, 0.0f);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textureIds[(int)TextureObjects.Concrete]);

            gl.Begin(OpenGL.GL_QUADS);
            for (int i = 0; i < m_groundVertices.Length; i += 3)
            {
                gl.Vertex(m_groundScaleX * m_groundVertices[i], m_groundVertices[i + 1], m_groundScaleZ * m_groundVertices[i + 2]);
                gl.TexCoord(m_groundTextures[i], m_groundTextures[i + 1]);
            }
            gl.End();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
            gl.PopMatrix();
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

        }

        private void PositionModels(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Translate(-250.0f, 0.0f, 80.0f);
            PositionTrafficLight(gl);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_MODULATE);
            PositionMotorcycle(gl);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_ADD);
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
            gl.Translate(m_translateMotorX[m_delay], 0.0f, m_translateMotorZ[m_delay]);
            gl.Rotate(0.0f, m_rotateMotorY[m_delay], m_rotateMotorZ[m_delay]);
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
            CreateLampPost(gl, true);
            gl.PopMatrix();
        }

        private Cylinder CreatePost(OpenGL gl)
        {
            Cylinder post = new Cylinder();
            float postRadius = 4.5f;
            post.BaseRadius = postRadius;
            post.TopRadius = postRadius;
            post.Height = 165 * LampPostScale;
            post.Slices = 100;
            post.Stacks = 20;
            post.CreateInContext(gl);
            post.NormalGeneration = Normals.Smooth;
            post.TextureCoords = true;
            return post;
        }

        private void CreateLampPost(OpenGL gl, bool addLight = false)
        {
            Cylinder post = CreatePost(gl);
            Cube lamp = new Cube();
            float lampEdge = 7.5f;
            gl.PushMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textureIds[(int)TextureObjects.Wood]);
            gl.Color(0.29f, 0.16f, 0.10f);
            gl.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);
            post.Render(gl, RenderMode.Render);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
            gl.PopMatrix();
            
            gl.PushMatrix();
            gl.Color(1f, 0.35f, 0.0f);
            gl.Translate(0.0f, post.Height + (lampEdge / 2), 0.0f);
            gl.Scale(lampEdge, lampEdge, lampEdge);
            if (addLight)
            {
                CreateRedLight(gl);
            }
            lamp.Render(gl, RenderMode.Render);
            gl.PopMatrix();
        }

        private void CreateRedLight(OpenGL gl)
        {
            float[] light1pos = new float[] { 0.0f, 0.0f, 0.0f, 1.0f };
            float[] light1ambient = new float[] { 1.0f, 0.0f, 0.0f, 1.0f };
            float[] light1diffuse = new float[] { 0.3f, 0.3f, 0.3f, 1.0f };
            float[] light1specular = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };
            float[] spotDirection = new float[] { -1.0f, -1.0f, -1.0f };

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, light1pos);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_AMBIENT, light1ambient);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, light1diffuse);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPECULAR, light1specular);

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_CUTOFF, 40.0f);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_EXPONENT, 5f);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_DIRECTION, spotDirection);
            gl.Enable(OpenGL.GL_LIGHT1);
        }

        private void PositionBuildings(OpenGL gl)
        {
            Cube building = new Cube();

            gl.PushMatrix();

            gl.Color(0.6f, 0.6f, 0.6f);
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
            float[] light0ambient = { RChannel, GChannel, BChannel, 1.0f };
            float[] light0diffuse = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };
            float[] light0specular = new float[] { 0.1f, 0.1f, 0.1f, 1.0f };

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

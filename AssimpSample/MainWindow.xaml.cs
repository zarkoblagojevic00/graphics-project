using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using SharpGL.SceneGraph;
using SharpGL;
using Microsoft.Win32;
using System.ComponentModel;

namespace AssimpSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        ///	 Instanca OpenGL "sveta" - klase koja je zaduzena za iscrtavanje koriscenjem OpenGL-a.
        /// </summary>
        

        #region Atributi

        World m_world;
        public World World {
            get { return m_world; }
        }

        readonly float rotationXUpperBoundary = 55.0f;
        readonly float rotationXLowerBoundary = -15.0f;
        readonly float sceneDistanceLowerBoundary = 50.0f;


        #endregion Atributi

        #region Konstruktori

        public MainWindow()
        {
            // Inicijalizacija komponenti
            InitializeComponent();

            // Kreiranje OpenGL sveta
            try
            {
                m_world = new World((int)openGLControl.ActualWidth, (int)openGLControl.ActualHeight, openGLControl.OpenGL);
            }
            catch (Exception e)
            {
                MessageBox.Show("Neuspesno kreirana instanca OpenGL sveta. Poruka greške: " + e.Message, "Poruka", MessageBoxButton.OK);
                this.Close();
            }
            this.DataContext = this;
        }

        #endregion Konstruktori

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lampPostScaleComboBox.ItemsSource = m_world.LampPostScaleValues;
            rChannelComboBox.ItemsSource = m_world.ChannelValues;
            gChannelComboBox.ItemsSource = m_world.ChannelValues;
            bChannelComboBox.ItemsSource = m_world.ChannelValues;
            velocityComboBox.ItemsSource = m_world.VelocityValues;
        }

        /// <summary>
        /// Handles the OpenGLDraw event of the openGLControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            m_world.Draw(args.OpenGL);
        }

        /// <summary>
        /// Handles the OpenGLInitialized event of the openGLControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            m_world.Initialize(args.OpenGL);
        }

        /// <summary>
        /// Handles the Resized event of the openGLControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void openGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            m_world.Resize(args.OpenGL, (int)openGLControl.ActualWidth, (int)openGLControl.ActualHeight);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!m_world.NotInAnimation)
                return;

            switch (e.Key)
            {
                case Key.F4: this.Close(); break;
                case Key.E: m_world.RotationX = (m_world.RotationX > rotationXLowerBoundary) ? m_world.RotationX - 5.0f : rotationXLowerBoundary; break;
                case Key.D: m_world.RotationX = (m_world.RotationX < rotationXUpperBoundary) ? m_world.RotationX + 5.0f : rotationXUpperBoundary; ; break;
                case Key.S: m_world.RotationY -= 5.0f; break;
                case Key.F: m_world.RotationY += 5.0f; break;
                case Key.Add: m_world.SceneDistance = (m_world.SceneDistance > sceneDistanceLowerBoundary) ? m_world.SceneDistance - 125.0f : sceneDistanceLowerBoundary; break;
                //case Key.Add: m_world.SceneDistance -= 125.0f; break;
                case Key.Subtract: m_world.SceneDistance += 125.0f; break;
                case Key.V: m_world.DoAnimation(); break;
            }
        }

        private void lampPostScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!m_world.NotInAnimation)
                return;
            m_world.LampPostScale = (float)lampPostScaleComboBox.SelectedItem;
        }

        private void rChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!m_world.NotInAnimation)
                return;
            m_world.RChannel = (float)rChannelComboBox.SelectedItem;
        }

        private void gChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!m_world.NotInAnimation)
                return;
            m_world.GChannel = (float)gChannelComboBox.SelectedItem;
        }

        private void bChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!m_world.NotInAnimation)
                return;
            m_world.BChannel = (float)bChannelComboBox.SelectedItem;
        }

        private void velocityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!m_world.NotInAnimation)
                return;
            m_world.MotorVelocity = (float)velocityComboBox.SelectedItem;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace AplicacionGestures
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaPlayer mediaPlayer;
        MediaTimelineController mediaTimelineController;
        CompositeTransform ct = new CompositeTransform();
        CompositeTransform ctv = new CompositeTransform();
        Rect sourceRect = new Rect(0, 0, 1, 1);
        Boolean controls = true;



        public MainPage()
        {
            this.InitializeComponent();
            this.image.ManipulationMode = ManipulationModes.Scale | ManipulationModes.Rotate;
            this.volume.ManipulationMode = ManipulationModes.TranslateX;
            this.mediaPlayerElement.ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateInertia | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            //ctv.ScaleY = 1.5;
            //this.volume.RenderTransform = ctv;
            mediaPlayer = new MediaPlayer();
            mediaTimelineController = new MediaTimelineController();
            mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/sample.mp4"));
            mediaPlayer = mediaPlayerElement.MediaPlayer;
            mediaPlayer.CommandManager.IsEnabled = false;
            mediaPlayer.TimelineController = mediaTimelineController;
            mediaTimelineController.IsLoopingEnabled = true;
        }

   
    
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.PlaybackSession.PlaybackRate = 1.0;
        }

        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            FrameworkElement source = (FrameworkElement) e.OriginalSource;
            
            ct.CenterX = source.ActualWidth / 2.0;
            ct.CenterY = source.ActualHeight / 2.0;

            ct.ScaleX *= e.Delta.Scale;
            ct.ScaleY *= e.Delta.Scale;
            
            double rotation = Math.Max(-60, Math.Min(120, ct.Rotation + e.Delta.Rotation));
            double rotationAux = rotation + 60;
            double clockRate = rotationAux / 60;
            mediaTimelineController.ClockRate = clockRate;
            ct.Rotation = rotation;

            source.RenderTransform = ct;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (mediaTimelineController.State != MediaTimelineControllerState.Running)
            {
                mediaTimelineController.Resume();
            }
        }

        private void _mediaPlayerElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (this.commandBar.Visibility.Equals(Visibility.Collapsed))
            {
                this.commandBar.Visibility = Visibility.Visible;
            } else
            {
                this.commandBar.Visibility = Visibility.Collapsed;
            }
        }

        /**
         * Escalado y movimiento del elemento de vídeo
         * Basado en el código disponible en los documentos de la librería del elemento MediaPlayer
         */ 
        private void _mediaPlayerElement_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //Diferente de 1 -> Escalando
            if (e.Delta.Scale != 1)
            {
                double halfWidth = sourceRect.Width / 2.0;
                double halfHeight = sourceRect.Height / 2.0;

                double centerX = sourceRect.X + halfWidth;
                double centerY = sourceRect.Y + halfHeight;

                float scale = e.Delta.Scale;
                double newHalfWidth = (sourceRect.Width * scale) / 2.0;
                double newHalfHeight = (sourceRect.Height * scale) / 2.0;

                if (centerX - newHalfWidth > 0 && centerX + newHalfWidth <= 1.0 &&
                    centerY - newHalfHeight > 0 && centerY + newHalfHeight <= 1.0)
                {
                    sourceRect.X = centerX - newHalfWidth;
                    sourceRect.Y = centerY - newHalfHeight;
                    sourceRect.Width *= e.Delta.Scale;
                    sourceRect.Height *= e.Delta.Scale;
                }
            }
            //Igual a 1 -> Movimiento
            else
            {
                //Translation X y Y se divide entre el Tamaño actual del Rect para adaptar la translación al tamaño
                double translateX = -1 * e.Delta.Translation.X / mediaPlayerElement.ActualWidth;
                double translateY = -1 * e.Delta.Translation.Y / mediaPlayerElement.ActualHeight;
                //Comprobar límites de translación
                if (sourceRect.X + translateX >= 0 && sourceRect.X + sourceRect.Width + translateX <= 1.0 &&
                    sourceRect.Y + translateY >= 0 && sourceRect.Y + sourceRect.Height + translateY <= 1.0)
                {
                    sourceRect.X += translateX;
                    sourceRect.Y += translateY;
                }
            }
            //NormalizedSourceRect es el Rect normalizado en la fuente de vídeo. Se utiliza para el zoom y movimiento
            mediaPlayer.PlaybackSession.NormalizedSourceRect = sourceRect;
        }

        private void MediaPlayerElement_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //Reset del tamaño del fotograma de vídeo
            sourceRect = new Rect(0, 0, 1, 1);
            mediaPlayer.PlaybackSession.NormalizedSourceRect = sourceRect;
        }

        private void MediaPlayerElement_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (mediaTimelineController.State == MediaTimelineControllerState.Running)
            {
                mediaTimelineController.Pause();
            }
            else
            {
                mediaTimelineController.Resume();
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaTimelineController.State == MediaTimelineControllerState.Running)
            {
                mediaTimelineController.Pause();
            }
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            mediaTimelineController.Start();
        }

        private void Volume_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)e.OriginalSource;
            
            GeneralTransform generalTransform = source.TransformToVisual(Window.Current.Content);
            Point screenCoords = generalTransform.TransformPoint(new Point(0, 0));
            double low = Window.Current.Content.RenderSize.Width * 0.2;
            double high = Window.Current.Content.RenderSize.Width * 0.8;
            double medium = (high + low) / 2 - low;
            if (screenCoords.X + e.Delta.Translation.X > low && screenCoords.X + source.ActualWidth + e.Delta.Translation.X < high)
            {
                ctv.TranslateX += e.Delta.Translation.X;
                source.RenderTransform = ctv;
                double current = (screenCoords.X + source.ActualWidth / 2 - low) / medium;
                mediaPlayer.Volume = current;
            }
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace WpfServerAdmin.Controls
{
    public class ProgressBarSmooth
    {
        public static double GetSmoothValue(DependencyObject obj)
        {
            return (double)obj.GetValue(SmoothValueProperty);
        }

        public static void SetSmoothValue(DependencyObject obj, double value)
        {
            obj.SetValue(SmoothValueProperty, value);
        }

        public static readonly DependencyProperty SmoothValueProperty = DependencyProperty.RegisterAttached("SmoothValue", typeof(double), typeof(ProgressBarSmooth), new PropertyMetadata(0.0, Changing));

        private static void Changing(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var animation = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, (double)e.NewValue != 100.0 ? new TimeSpan(0, 0, 0, 0, 100) : new TimeSpan(0, 0, 0, 0));
            ((ProgressBar)dependencyObject).BeginAnimation(RangeBase.ValueProperty, animation, HandoffBehavior.Compose);
        }
    }
}

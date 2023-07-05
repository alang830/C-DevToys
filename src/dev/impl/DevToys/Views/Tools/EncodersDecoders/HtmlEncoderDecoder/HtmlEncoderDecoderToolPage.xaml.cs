﻿#nullable enable

using DevToys.Api.Core.Navigation;
using DevToys.Shared.Core;
using DevToys.ViewModels.Tools.HtmlEncoderDecoder;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DevToys.Views.Tools.HtmlEncoderDecoder
{
    public sealed partial class HtmlEncoderDecoderToolPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty
            = DependencyProperty.Register(
                nameof(ViewModel),
                typeof(HtmlEncoderDecoderToolViewModel),
                typeof(HtmlEncoderDecoderToolPage),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        public HtmlEncoderDecoderToolViewModel ViewModel
        {
            get => (HtmlEncoderDecoderToolViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public HtmlEncoderDecoderToolPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var parameters = (NavigationParameter)e.Parameter;

            if (ViewModel is null)
            {
                // Set the view model
                Assumes.NotNull(parameters.ViewModel, nameof(parameters.ViewModel));
                ViewModel = (HtmlEncoderDecoderToolViewModel)parameters.ViewModel!;
                DataContext = ViewModel;
            }

            if (!string.IsNullOrWhiteSpace(parameters.ClipBoardContent))
            {
                ViewModel.IsEncodeMode = false;
                ViewModel.InputValue = parameters.ClipBoardContent;
            }

            base.OnNavigatedTo(e);
        }

        private void OutputTextBox_ExpandedChanged(object sender, System.EventArgs e)
        {
            if (OutputTextBox.IsExpanded)
            {
                MainGrid.Children.Remove(OutputTextBox);
                MainGrid.Visibility = Visibility.Collapsed;
                ExpandedGrid.Children.Add(OutputTextBox);
            }
            else
            {
                ExpandedGrid.Children.Remove(OutputTextBox);
                MainGrid.Children.Add(OutputTextBox);
                MainGrid.Visibility = Visibility.Visible;
            }
        }
    }
}

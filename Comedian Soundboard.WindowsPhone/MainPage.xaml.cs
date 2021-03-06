﻿using Comedian_Soundboard.Common;
using Comedian_Soundboard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.Devices.Notification;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Comedian_Soundboard.Helper;
using System.Collections.ObjectModel;
using Comedian_Soundboard.DataModel;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Comedian_Soundboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private ObservableCollection<Category> groups;
        private ObservableCollection<Category> filteredGroups;
        private Random random = new Random();

        public MainPage()
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            string param = e.NavigationParameter.ToString();

            if (param == "Search Online")
            {
                groups = await SoundDataSource.GetOnlineCategoriesAsync();
                filteredGroups = new IncrementalLoadingCollection<MyInstantsDataSource, Category>(groups);
            }
            else
            {
                groups = new ObservableCollection<Category>(await SoundDataSource.GetCategoriesAsync());
                filteredGroups = new ObservableCollection<Category>(groups);
            }

            this.DefaultViewModel["Groups"] = groups;
            LoadingPanel.Visibility = Visibility.Collapsed;

            Category category = await SoundDataSource.GetCategoryAsync(param);
            if (category != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    CategoryListView.ScrollIntoView(category);
                }
                );
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Audio.Source = null;
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void Group_Click(object sender, TappedRoutedEventArgs e)
        {
            string comedian = ((e.OriginalSource as FrameworkElement).DataContext as Category).UniqueId;
            if (comedian == "Search Online")
            {
                if (await AppHelper.IsInternetAvailable())
                {
                    Frame.Navigate(typeof(MainPage), comedian);
                }
            }
            else
            {
                Frame.Navigate(typeof(AudioPage), comedian);
            }
        }

        private void Lucky_Click(object sender, RoutedEventArgs e)
        {
            Category randComedian = groups.ElementAt(random.Next(0, groups.Count()));
            SoundItem randSound = randComedian.SoundItems.ElementAt(random.Next(0, randComedian.SoundItems.Count()));
            if (randSound.isOnline)  // Check whether url is online or in assets folder
            {
                Audio.Source = new Uri(randSound.SoundPath);
            }
            else
            {
                Audio.Source = new Uri("ms-appx:///" + randSound.SoundPath, UriKind.RelativeOrAbsolute);
            }

        }

        private void Audio_MediaOpened(object sender, RoutedEventArgs e)
        {
            Audio.Play();
        }

        private void Pointer_Pressed(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement image = (sender as FrameworkElement);
            Ellipse border = image.FindName("ImageBorder") as Ellipse;
            border.Width = 235;
            border.Height = 235;
            border.StrokeThickness = 8;
        }

        private void Pointer_Released(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement image = (sender as FrameworkElement);
            Ellipse border = image.FindName("ImageBorder") as Ellipse;
            border.Width = 228;
            border.Height = 228;
            border.StrokeThickness = 4;
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            Color color = Color.FromArgb(255, Convert.ToByte(random.Next(0, 256)), Convert.ToByte(random.Next(0, 256)), Convert.ToByte(random.Next(0, 256)));
            (sender as Ellipse).Stroke = new SolidColorBrush(color);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchTextBlock.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ? Visibility.Visible : Visibility.Collapsed; 
            if (groups != null)
            {
                foreach (Category item in groups)
                {
                    if (item.Title.ToLower().Contains(SearchTextBox.Text.ToLower()))
                    {
                        if (!filteredGroups.Contains(item))
                        {
                            filteredGroups.Add(item);
                        }
                    }
                    else
                    {
                        filteredGroups.Remove(item);
                    }
                }
                if (filteredGroups.Count() == groups.Count())
                {
                    filteredGroups = new ObservableCollection<Category>(groups);
                }

                this.DefaultViewModel["Groups"] = filteredGroups;
            }
        }
    }
}

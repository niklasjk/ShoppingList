using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Android.OS;
using System.Collections.Generic;
using Android.Provider;
using Java.IO;
using Android.Graphics;
using Uri = Android.Net.Uri;
using Android.Content.PM;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Android.Support.V7.Widget;
using Android.Animation;
using Android.Views.Animations;
using Com.Bumptech.Glide;
using Android.Support.V7.App;
using Android.Support.V4.View;
using Android.Support.V4.App;
using Android;
using Android.Support.Design.Widget;

public static class App
{
    public static File _file;
    public static File _dir;
    public static Bitmap bitmap;
}
namespace ShoppingList
{

    [Activity(Label = "@string/applicationName", MainLauncher = false,
        Icon = "@drawable/ic_launcher", ConfigurationChanges = ConfigChanges.Locale | ConfigChanges.Orientation | ConfigChanges.ScreenSize,
        Theme = "@style/Light")]
    public class MainActivity : AppCompatActivity
    {
        private Animator _currentAnimator;
        private int _shortAnimationDuration;

        ImageView expandedImageView;

        // RecyclerView instance that displays the photo album:
        RecyclerView mRecyclerView;

        // Layout manager that lays out each card in the RecyclerView:
        RecyclerView.LayoutManager mLayoutManager;

        // Adapter that accesses the data set (a photo album):
        ShoppingListAdapter mAdapter;

        List<ListItem> listItems;
        public static readonly int pickImageId = 1000;
        ListItem currentItem;
        View layout;
        static readonly int REQUEST_CAMERA = 0;
        static readonly int REQUEST_READ = 1;

        static string[] PERMISSIONS_CAMERA = {
            Manifest.Permission.Camera,
            Manifest.Permission.WriteExternalStorage
        };

        private int requestImageCapture = 0;
        public static String PREFS_NAME = "MyPrefsFile";
        public static String PREFS_THEME = "ThemePrefsFile";
        ISharedPreferences prefs;
        ISharedPreferences themePrefs;
        private int prefsThemeId = -1;
        private int mThemeId = -1;
        private int mThemechange = -1;
        private Android.Support.V7.Widget.ShareActionProvider shareActionProvider;

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if ((requestCode == pickImageId) && (resultCode == Result.Ok) && (data != null))
            {
                Uri uri = data.Data;

                currentItem.uri = uri;

                currentItem.path = uri.ToString();

                BitmapFactory.Options options = await GetBitmapOptionsOfImageAsync(Uri.Parse(currentItem.path));
                currentItem.bm = await LoadScaledDownBitmapForDisplayAsync((Uri.Parse(currentItem.path)), options, 32, 32);

                mAdapter.NotifyDataSetChanged();

            }
            if ((requestCode == requestImageCapture) && (resultCode == Result.Ok))
            {
                base.OnActivityResult(requestCode, resultCode, data);

                // Make it available in the gallery

                Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                Uri contentUri = Uri.FromFile(App._file);
                mediaScanIntent.SetData(contentUri);
                SendBroadcast(mediaScanIntent);

                currentItem.path = contentUri.ToString();

                if (App._file != null)
                {
                    BitmapFactory.Options options = await GetBitmapOptionsOfImageAsync(Uri.Parse(currentItem.path));
                    currentItem.bm = await LoadScaledDownBitmapForDisplayAsync((Uri.Parse(currentItem.path)), options, 32, 32);
                    App.bitmap = null;
                    mAdapter.NotifyDataSetChanged();

                }
                // Dispose of the Java side bitmap.
                GC.Collect();
            }
        }

        private void ZoomImageFromThumb(object sender, EventArgs eventArgs)
        {
            View thumbView = (View)sender;
            // If there's an animation in progress, cancel it immediately and proceed with this one.
            if (_currentAnimator != null)
            {
                _currentAnimator.Cancel();
            }

            // Load the high-resolution "zoomed-in" image.
            // ImageView expandedImageView = (ImageView)FindViewById(Resource.Id.expanded_image);
            // expandedImageView.SetImageResource(imageResId);

            expandedImageView = this.FindViewById<ImageView>(Resource.Id.expanded_image);

            Glide.With(this).Load(Uri.Parse((thumbView.Tag).ToString()))
                .Into(expandedImageView);

            // Calculate the starting and ending bounds for the zoomed-in image. 
            Rect startBounds = new Rect();
            Rect finalBounds = new Rect();
            Point globalOffset = new Point();

            // The start bounds are the global visible rectangle of the thumbnail, and the
            // final bounds are the global visible rectangle of the container view. Also
            // set the container view's offset as the origin for the bounds, since that's
            // the origin for the positioning animation properties (X, Y).
            thumbView.GetGlobalVisibleRect(startBounds);
            FindViewById(Resource.Id.container).GetGlobalVisibleRect(finalBounds, globalOffset);
            startBounds.Offset(-globalOffset.X, -globalOffset.Y);
            finalBounds.Offset(-globalOffset.X, -globalOffset.Y);

            // Adjust the start bounds to be the same aspect ratio as the final bounds using the
            // "center crop" technique. This prevents undesirable stretching during the animation.
            // Also calculate the start scaling factor (the end scaling factor is always 1.0).
            float startScale;
            if ((float)finalBounds.Width() / finalBounds.Height()
                    > (float)startBounds.Width() / startBounds.Height())
            {
                // Extend start bounds horizontally
                startScale = (float)startBounds.Height() / finalBounds.Height();
                float startWidth = startScale * finalBounds.Width();
                float deltaWidth = (startWidth - startBounds.Width()) / 2;
                startBounds.Left -= (int)deltaWidth;
                startBounds.Right += (int)deltaWidth;
            }
            else
            {
                // Extend start bounds vertically
                startScale = (float)startBounds.Width() / finalBounds.Width();
                float startHeight = startScale * finalBounds.Height();
                float deltaHeight = (startHeight - startBounds.Height()) / 2;
                startBounds.Top -= (int)deltaHeight;
                startBounds.Bottom += (int)deltaHeight;
            }

            // Hide the thumbnail and show the zoomed-in view. When the animation begins,
            // it will position the zoomed-in view in the place of the thumbnail.
            thumbView.Alpha = 0f;
            expandedImageView.Visibility = ViewStates.Visible;

            // Set the pivot point for SCALE_X and SCALE_Y transformations to the top-left corner of
            // the zoomed-in view (the default is the center of the view).
            expandedImageView.PivotX = 0f;
            expandedImageView.PivotY = 0f;

            AnimatorSet expandSet = new AnimatorSet();
            expandSet.Play(ObjectAnimator.OfFloat(expandedImageView, View.X, startBounds.Left, finalBounds.Left))
                     .With(ObjectAnimator.OfFloat(expandedImageView, View.Y, startBounds.Top, finalBounds.Top))
                     .With(ObjectAnimator.OfFloat(expandedImageView, View.ScaleXs, startScale, 1f))
                     .With(ObjectAnimator.OfFloat(expandedImageView, View.ScaleYs, startScale, 1f));
            expandSet.SetDuration(_shortAnimationDuration);
            expandSet.SetInterpolator(new DecelerateInterpolator());
            // expandSet.AnimationEnd += NullOutCurrentAnimator;
            // expandSet.AnimationCancel += NullOutCurrentAnimator;

            expandSet.AnimationEnd += (sender1, args1) =>
            {
                _currentAnimator = null;
            };
            expandSet.AnimationCancel += (sender1, args1) =>
            {
                _currentAnimator = null;
            };

            expandSet.Start();
            _currentAnimator = expandSet;

            // Upon clicking the zoomed-in image, it should zoom back down to the original bounds
            // and show the thumbnail instead of the expanded image.
            float startScaleFinal = startScale;

            // Create a custom clickListener so that click listeners won't be set multiple times
            var local = new LocalOnclickListener();

            expandedImageView.SetOnClickListener(local);

            local.HandleOnClick = () =>
            {

                if (_currentAnimator != null)
                {
                    _currentAnimator.Cancel();

                }

                AnimatorSet shrinkSet = new AnimatorSet();
                shrinkSet.Play(ObjectAnimator.OfFloat(expandedImageView, View.X, startBounds.Left))
                         .With(ObjectAnimator.OfFloat(expandedImageView, View.Y, startBounds.Top))
                         .With(ObjectAnimator.OfFloat(expandedImageView, View.ScaleXs, startScaleFinal))
                         .With(ObjectAnimator.OfFloat(expandedImageView, View.ScaleYs, startScaleFinal));
                shrinkSet.SetDuration(_shortAnimationDuration);
                shrinkSet.SetInterpolator(new DecelerateInterpolator());

                shrinkSet.AnimationEnd += (sender1, args1) =>
                {
                    thumbView.Alpha = 1.0f;
                    expandedImageView.Visibility = ViewStates.Gone;
                    _currentAnimator = null;
                };
                shrinkSet.AnimationCancel += (sender1, args1) =>
                {
                    thumbView.Alpha = 1.0f;
                    expandedImageView.Visibility = ViewStates.Gone;
                    _currentAnimator = null;
                };
                shrinkSet.Start();
                _currentAnimator = shrinkSet;
            };

        }
        public class LocalOnclickListener : Java.Lang.Object, View.IOnClickListener
        {
            public void OnClick(View v)
            {
                HandleOnClick();
            }
            public System.Action HandleOnClick { get; set; }
        }
        public async Task<Bitmap> LoadScaledDownBitmapForDisplayAsync(Uri uri, BitmapFactory.Options options, int reqWidth, int reqHeight)
        {

            // Calculate inSampleSize
            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

            // Decode bitmap with inSampleSize set
            options.InJustDecodeBounds = false;

            var bm = await LoadImageAsync(uri, options);

            return bm;
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (bundle != null && bundle.GetInt("theme", -1) != -1)
            {
                mThemeId = bundle.GetInt("theme");
                this.SetTheme(mThemeId);
            }
            if (bundle != null && bundle.GetInt("themeChange", -1) != -1)
            {
                mThemechange = bundle.GetInt("themeChange");
            }

            themePrefs = GetSharedPreferences(PREFS_THEME, 0);

            if (themePrefs.GetInt("0", 0) != null && themePrefs.GetInt("0", 0) != 0)
            {
                prefsThemeId = themePrefs.GetInt("0", 0);
                this.SetTheme(prefsThemeId);
                mThemeId = prefsThemeId;

            }

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            listItems = new List<ListItem>();

            layout = FindViewById(Resource.Id.container);


            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();
            }

            _shortAnimationDuration = this.Resources.GetInteger(Android.Resource.Integer.ConfigShortAnimTime);

            // Get our RecyclerView layout:
            mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            //............................................................
            // Layout Manager Setup:

            // Use the built-in linear layout manager:
            mLayoutManager = new LinearLayoutManager(this);


            // Plug the layout manager into the RecyclerView:
            mRecyclerView.SetLayoutManager(mLayoutManager);

            //............................................................
            // Adapter Setup:

            // Create an adapter for the RecyclerView, and pass it the
            // data set (the shopping list) to manage:
            mAdapter = new ShoppingListAdapter(listItems);

            // Register the item click handlers with the adapter:
            mAdapter.ItemClick += OnItemClick;
            mAdapter.ChkBoxClick += OnChkBoxClick;
            mAdapter.ItemLongClick += OnItemLongClick;
            mAdapter.ImageClick += ZoomImageFromThumb;

            // Plug the adapter into the RecyclerView:
            mRecyclerView.SetAdapter(mAdapter);

            prefs = GetSharedPreferences(PREFS_NAME, 0);

            // Get elements from the layout resource,
            // and attach events to them
            EditText itemName = FindViewById<EditText>(Resource.Id.itemName);
            Button addItem = FindViewById<Button>(Resource.Id.addItem);

            // Check if there is an existing shopping list when the app was last closed
            if (prefs.GetString("0", "null") != null && prefs.GetString("0", "null") != "null" && mThemechange == -1)
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                var continueTitleText = Resources.GetText(Resource.String.continueTitleText);
                var continueMessageText = Resources.GetText(Resource.String.continueMessageText);
                var cancelText = Resources.GetText(Resource.String.cancelText);

                builder.SetTitle(continueTitleText);
                builder.SetMessage(continueMessageText);
                builder.SetPositiveButton("Ok", async (senderAlert, args) =>
                {
                    string stringList = prefs.GetString("0", "null");
                    IList<string> list = new List<string>();
                    list = JsonConvert.DeserializeObject<List<string>>(stringList);
                    foreach (var myimage in list)
                    {
                        var convert = JsonConvert.DeserializeObject<ListItem>(myimage);

                        ListItem testi = convert;
                        if (testi.path != "test")
                        {
                            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
                            {

                                // Camera permission has not been granted
                                RequestReadPermission();
                            }
                            else
                            {

                                BitmapFactory.Options options = await GetBitmapOptionsOfImageAsync(Uri.Parse(testi.path));
                                testi.bm = await LoadScaledDownBitmapForDisplayAsync((Uri.Parse(testi.path)), options, 32, 32);
                            }
                        }
                        listItems.Add(convert);
                    }
                    mAdapter.NotifyDataSetChanged();
                    ISharedPreferencesEditor editor = prefs.Edit();
                    editor.Commit();
                    if (shareActionProvider != null)
                    {
                        shareActionProvider.SetShareIntent(CreateShareIntent());
                    }
                });

                builder.SetNegativeButton(cancelText, (senderAlert, args) =>
                {
                    ISharedPreferencesEditor editor = prefs.Edit();
                    listItems.Clear();
                    editor.Clear();
                    editor.Commit();
                });

                Dialog dialog = builder.Create();
                dialog.Show();

            }
            // if application was recreated via theme change, use the old list without asking
            else if (mThemechange == 0)
            {
                GetOldList();
            }

            // Plug the adapter into the RecycleViewer
            mRecyclerView.SetAdapter(mAdapter);

            addItem.Click += (sender, e) =>
            {
                ListItem item = new ListItem();
                item.title = itemName.Text;
                listItems.Add(item);
                mAdapter.NotifyDataSetChanged();
                itemName.Text = ("");
                if (shareActionProvider != null)
                {
                    shareActionProvider.SetShareIntent(CreateShareIntent());
                }
            };
        }

        private void RequestReadPermission()
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadExternalStorage))
            {
                // Provide an additional rationale to the user if the permission was not granted
                // and the user would benefit from additional context for the use of the permission.
                // For example if the user has previously denied the permission.

                Snackbar.Make(layout, Resource.String.permission_read_rationale,
                    Snackbar.LengthIndefinite).SetAction(Resource.String.ok, new Action<View>(delegate (View obj)
                    {
                        ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadExternalStorage }, REQUEST_READ);
                    })).Show();
            }
            else
            {
                // Read permission has not been granted yet. Request it directly.
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadExternalStorage }, REQUEST_READ);
            }
        }

        private async Task GetOldList()
        {
            string stringList = prefs.GetString("0", "null");
            IList<string> list = new List<string>();
            list = JsonConvert.DeserializeObject<List<string>>(stringList);
            foreach (var myimage in list)
            {
                var convert = JsonConvert.DeserializeObject<ListItem>(myimage);

                ListItem testi = convert;
                if (testi.path != "test")
                {
                    BitmapFactory.Options options = await GetBitmapOptionsOfImageAsync(Uri.Parse(testi.path));
                    testi.bm = await LoadScaledDownBitmapForDisplayAsync((Uri.Parse(testi.path)), options, 32, 32);
                }
                listItems.Add(convert);
            }
            mAdapter.NotifyDataSetChanged();
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.Commit();
            if (shareActionProvider != null)
            {
                shareActionProvider.SetShareIntent(CreateShareIntent());
            }
        }

        private void OnChkBoxClick(object sender, int position)
        {
            if (listItems[position].check == 0)
            {
                listItems[position].check = 1;
                mAdapter.NotifyDataSetChanged();
            }
            else
            {
                listItems[position].check = 0;
                mAdapter.NotifyDataSetChanged();
            }
        }
        private void OnItemClick(object sender, int position)
        {
            var toastText = Resources.GetText(Resource.String.holdForOptions);

            Toast.MakeText(this, toastText, ToastLength.Short).Show();
        }
        void OnItemLongClick(object sender, int position)
        {
            var selected = listItems[position];

            Android.Support.V7.Widget.PopupMenu menu = new Android.Support.V7.Widget.PopupMenu(this, mRecyclerView.FindViewHolderForAdapterPosition(position).ItemView);

            currentItem = selected;

            var currentIndex = position;

            menu.Inflate(Resource.Menu.PopupMenu);


            var toastText = Resources.GetText(Resource.String.onDeletion);

            menu.MenuItemClick += (s1, arg1) =>
            {
                switch (arg1.Item.TitleFormatted.ToString())
                {
                    case "Delete item":

                        listItems.Remove(selected);
                        mAdapter.NotifyDataSetChanged();

                        if (shareActionProvider != null)
                        {
                            shareActionProvider.SetShareIntent(CreateShareIntent());
                        }
                        Toast.MakeText(this, selected.title + toastText, ToastLength.Short).Show();

                        break;
                    case "Poista ostos":

                        listItems.Remove(selected);
                        mAdapter.NotifyDataSetChanged();

                        if (shareActionProvider != null)
                        {
                            shareActionProvider.SetShareIntent(CreateShareIntent());
                        }
                        Toast.MakeText(this, selected.title + toastText, ToastLength.Short).Show();

                        break;
                    case "Modify item":

                        Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
                        LayoutInflater inflater = this.LayoutInflater;
                        View dialogView = inflater.Inflate(Resource.Layout.editItemDialog, null);
                        builder.SetView(dialogView);
                        var titleText = Resources.GetText(Resource.String.modifyItem);
                        var cancelText = Resources.GetText(Resource.String.cancelText);

                        EditText edt = (EditText)dialogView.FindViewById(Resource.Id.edit1);
                        edt.Text = currentItem.title;

                        builder.SetTitle(titleText + " " + currentItem.title);
                        builder.SetPositiveButton("Ok", (senderAlert, args) =>
                        {

                            listItems[currentIndex].title = edt.Text;
                            mAdapter.NotifyDataSetChanged();
                        });

                        builder.SetNegativeButton(cancelText, (senderAlert, args) =>
                        {
                        });

                        Dialog dialog = builder.Create();
                        dialog.Show();
                        break;
                    case "Muokkaa ostosta":

                        Android.Support.V7.App.AlertDialog.Builder builderSuomi = new Android.Support.V7.App.AlertDialog.Builder(this);
                        LayoutInflater inflaterSuomi = this.LayoutInflater;
                        View dialogViewSuomi = inflaterSuomi.Inflate(Resource.Layout.editItemDialog, null);
                        builderSuomi.SetView(dialogViewSuomi);
                        var titleTextSuomi = Resources.GetText(Resource.String.modifyItem);
                        var cancelTextSuomi = Resources.GetText(Resource.String.cancelText);

                        EditText edtSuomi = (EditText)dialogViewSuomi.FindViewById(Resource.Id.edit1);
                        edtSuomi.Text = currentItem.title;

                        builderSuomi.SetTitle(titleTextSuomi + " " + currentItem.title);
                        builderSuomi.SetPositiveButton("Ok", (senderAlert, args) =>
                        {

                            listItems[currentIndex].title = edtSuomi.Text;
                            mAdapter.NotifyDataSetChanged();
                        });

                        builderSuomi.SetNegativeButton(cancelTextSuomi, (senderAlert, args) =>
                        {
                        });

                        Dialog dialogSuomi = builderSuomi.Create();
                        dialogSuomi.Show();
                        break;
                    case "Take photo":
                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted
                            || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
                        {

                            // Camera permission has not been granted
                            RequestPhotoPermissions();
                        }
                        else
                        {

                            Intent intent = new Intent(MediaStore.ActionImageCapture);

                            App._file = new File(App._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));

                            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file));

                            StartActivityForResult(intent, requestImageCapture);
                        }
                        break;
                    case "Ota valokuva":
                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted
                            || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
                        {

                            // Camera permission has not been granted
                            RequestPhotoPermissions();
                        }
                        else
                        {
                            Intent intentSecond = new Intent(MediaStore.ActionImageCapture);

                            App._file = new File(App._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));

                            intentSecond.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file));

                            StartActivityForResult(intentSecond, requestImageCapture);
                        }
                        break;
                    case "Get image from gallery":
                        var galleryIntent = new Intent(Intent.ActionOpenDocument);
                        galleryIntent.AddCategory(Intent.CategoryOpenable);
                        galleryIntent.SetType("image/*");
                        StartActivityForResult(galleryIntent, pickImageId);
                        break;
                    case "Hae kuva kuvagalleriasta":
                        var galleryIntentSecond = new Intent(Intent.ActionOpenDocument);
                        galleryIntentSecond.AddCategory(Intent.CategoryOpenable);
                        galleryIntentSecond.SetType("image/*");
                        StartActivityForResult(galleryIntentSecond, pickImageId);
                        break;
                    case "Cancel":
                        break;
                    case "Peruuta":
                        break;
                }
            };

            // Android 4 now has the DismissEvent
            menu.DismissEvent += (s2, arg2) =>
            {
                System.Console.WriteLine("menu dismissed");
            };
            menu.Show();
        }
        void RequestPhotoPermissions()
        {

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera)
                || ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.WriteExternalStorage))
            {

                Snackbar.Make(layout, Resource.String.permission_camera_rationale,
                    Snackbar.LengthIndefinite).SetAction(Resource.String.ok, new Action<View>(delegate (View obj)
                    {
                        ActivityCompat.RequestPermissions(this, PERMISSIONS_CAMERA, REQUEST_CAMERA);

                    })).Show();
            }
            else
            {
                // Camera or storage permissions have not been granted yet. Request them directly.
                ActivityCompat.RequestPermissions(this, PERMISSIONS_CAMERA, REQUEST_CAMERA);

            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == REQUEST_READ)
            {
                // Check if the only required permission has been granted
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    Snackbar.Make(layout, Resource.String.permission_available_camera, Snackbar.LengthShort).Show();
                }
                else
                {
                    Snackbar.Make(layout, Resource.String.permissions_not_granted, Snackbar.LengthShort).Show();
                }
            }
            else if (requestCode == REQUEST_CAMERA)
            {

                // We have requested multiple permissions for contacts, so all of them need to be
                // checked.
                if (PermissionUtil.VerifyPermissions(grantResults))
                {
                    // All required permissions have been granted, display contacts fragment.
                    Snackbar.Make(layout, Resource.String.permission_available_camera, Snackbar.LengthShort).Show();
                }
                else
                {
                    Snackbar.Make(layout, Resource.String.permissions_not_granted, Snackbar.LengthShort).Show();
                }

            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            }
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.ActionBarMenu, menu);

            var overflow_item = menu.FindItem(Resource.Id.overflowMenuShare);
            IMenuItem checkItem = menu.FindItem(Resource.Id.overFlowMenuDarkTheme);
            if (mThemeId != Resource.Style.Black)
            {
                checkItem.SetChecked(checkItem.IsChecked);
            }
            else
            {
                checkItem.SetChecked(!checkItem.IsChecked);
            }

            var actionprov = new Android.Support.V7.Widget.ShareActionProvider(this);
            MenuItemCompat.SetActionProvider(overflow_item, actionprov);
            var test = MenuItemCompat.GetActionProvider(overflow_item);
            shareActionProvider = test.JavaCast<Android.Support.V7.Widget.ShareActionProvider>();

            shareActionProvider.SetShareIntent(CreateShareIntent());

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.overflowMenuShare:
                    shareActionProvider.SetShareIntent(CreateShareIntent());
                    break;
                // action with ID overflowMenuClear was selected
                case Resource.Id.overflowMenuClear:
                    Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
                    var titleText = Resources.GetText(Resource.String.titleText);
                    var messageText = Resources.GetText(Resource.String.messageText);
                    var cancelText = Resources.GetText(Resource.String.cancelText);


                    builder.SetTitle(titleText);
                    builder.SetMessage(messageText);
                    builder.SetPositiveButton("Ok", (senderAlert, args) =>
                    {
                        ISharedPreferencesEditor editor = prefs.Edit();
                        listItems.Clear();
                        editor.Clear();
                        editor.Commit();
                        mAdapter.NotifyDataSetChanged();
                        if (shareActionProvider != null)
                        {
                            shareActionProvider.SetShareIntent(CreateShareIntent());
                        }
                    });

                    builder.SetNegativeButton(cancelText, (senderAlert, args) =>
                    {
                    });

                    Dialog dialog = builder.Create();
                    dialog.Show();
                    break;
                case Resource.Id.overflowMenuClearChecked:
                    Android.Support.V7.App.AlertDialog.Builder builderClearChecked = new Android.Support.V7.App.AlertDialog.Builder(this);
                    var titleTextClearChecked = Resources.GetText(Resource.String.titleTextClearChecked);
                    var messageTextClearChecked = Resources.GetText(Resource.String.messageTextClearChecked);
                    var cancelTextClearChecked = Resources.GetText(Resource.String.cancelText);


                    builderClearChecked.SetTitle(titleTextClearChecked);
                    builderClearChecked.SetMessage(messageTextClearChecked);
                    builderClearChecked.SetPositiveButton("Ok", (senderAlert, args) =>
                    {
                        listItems.RemoveAll(listItem => listItem.check == 1);

                        mAdapter.NotifyDataSetChanged();
                        if (shareActionProvider != null)
                        {
                            shareActionProvider.SetShareIntent(CreateShareIntent());
                        }
                    });

                    builderClearChecked.SetNegativeButton(cancelTextClearChecked, (senderAlert, args) =>
                    {
                    });

                    Dialog dialogClearChecked = builderClearChecked.Create();
                    dialogClearChecked.Show();
                    break;
                case Resource.Id.overFlowMenuDarkTheme:
                    if (mThemeId == Resource.Style.Black)
                    {
                        mThemeId = Resource.Style.Light;
                        prefsThemeId = Resource.Style.Light;
                    }
                    else
                    {
                        mThemeId = Resource.Style.Black;
                        prefsThemeId = Resource.Style.Black;
                    }
                    mThemechange = 0;
                    Recreate();
                    break;
                case Resource.Id.overflowMenuPrivacyPolicy:
                    var uri = Uri.Parse("http://www.sites.google.com/view/nkshoppinglist/home");
                    var intent = new Intent(Intent.ActionView, uri);
                    StartActivity(intent);
                    break;
                default:
                    break;
            }
            return true;
        }
        Intent CreateShareIntent()
        {
            var sharingIntent = new Intent(Intent.ActionSend);
            sharingIntent.SetType("text/plain");
            sharingIntent.PutExtra(Intent.ExtraText, MakePlainList());

            return sharingIntent;
        }

        private void CreateDirectoryForPictures()
        {
            App._dir = new File(
                Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryPictures), "Shopping list");
            if (!App._dir.Exists())
            {
                App._dir.Mkdirs();
            }
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }
        protected override void OnSaveInstanceState(Bundle outState)
        {
            ISharedPreferences prefs = GetSharedPreferences(PREFS_NAME, 0);
            ISharedPreferencesEditor editor = prefs.Edit();

            ISharedPreferences theme = GetSharedPreferences(PREFS_THEME, 0);
            ISharedPreferencesEditor themeEditor = theme.Edit();

            List<String> list = new List<string>();
            for (var i = 0; i < listItems.Count; i++)
            {
                var convert = JsonConvert.SerializeObject(listItems[i]);
                list.Add(convert);
            }
            string stringList = JsonConvert.SerializeObject(list);

            if (listItems.Count > 0)
            {
                editor.PutString("" + 0, stringList);
                editor.Commit();
            }

            themeEditor.PutInt("" + 0, prefsThemeId);
            themeEditor.Commit();
            outState.PutInt("theme", mThemeId);
            outState.PutInt("themeChange", mThemechange);
            // always call the base implementation!
            base.OnSaveInstanceState(outState);
        }

        public string MakePlainList()
        {
            string plainList = "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (var i = 0; i < listItems.Count; i++)
            {
                if (i < listItems.Count - 1)
                {
                    sb.Append(listItems[i].title + ", ");
                }
                else
                {
                    sb.Append(listItems[i].title);
                }
            }
            plainList = sb.ToString();
            return plainList;
        }

        async Task<BitmapFactory.Options> GetBitmapOptionsOfImageAsync(Uri uri)
        {
            BitmapFactory.Options options = new BitmapFactory.Options
            {
                InJustDecodeBounds = true
            };

            // The result will be null because InJustDecodeBounds == true.

            ParcelFileDescriptor parcelFileDescriptor = null;
            try
            {
                parcelFileDescriptor = this.ContentResolver.OpenFileDescriptor(uri, "r");
                var fileDescriptor = parcelFileDescriptor.FileDescriptor;
                Bitmap result = await BitmapFactory.DecodeFileDescriptorAsync(fileDescriptor, null, options).ConfigureAwait(false);
                parcelFileDescriptor.Close();
            }
            catch (Java.Lang.Exception e)
            {
                // Log.Error(TAG, "Failed to load image.", e);
                e.PrintStackTrace();
                return null;
            }
            finally
            {
                try
                {
                    if (parcelFileDescriptor != null)
                    {
                        parcelFileDescriptor.Close();
                    }
                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                    // Log.Error(TAG, "Error closing ParcelFile Descriptor");
                }
            }

            int imageHeight = options.OutHeight;
            int imageWidth = options.OutWidth;

            return options;
        }
        public async Task<Bitmap> GetBitmapFromUriAsync(Android.Net.Uri uri, BitmapFactory.Options options)
        {
            ParcelFileDescriptor parcelFileDescriptor = null;
            try
            {
                parcelFileDescriptor = this.ContentResolver.OpenFileDescriptor(uri, "r");
                var fileDescriptor = parcelFileDescriptor.FileDescriptor;
                var image = await BitmapFactory.DecodeFileDescriptorAsync(fileDescriptor, null, options).ConfigureAwait(false);
                parcelFileDescriptor.Close();
                // Log.Info(TAG, "Asynchronous Bitmap Decoding Complete!");
                return image;
            }
            catch (Java.Lang.Exception e)
            {
                // Log.Error(TAG, "Failed to load image.", e);
                e.PrintStackTrace();
                return null;
            }
            finally
            {
                try
                {
                    if (parcelFileDescriptor != null)
                    {
                        parcelFileDescriptor.Close();
                    }
                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                    // Log.Error(TAG, "Error closing ParcelFile Descriptor");
                }
            }
        }
        public async Task<Bitmap> LoadImageAsync(Uri mUri, BitmapFactory.Options options)
        {
            // Start decoding our Bitmap from its file descriptor.
            var bitmapFromUri = GetBitmapFromUriAsync(mUri, options).ConfigureAwait(false);

            return await bitmapFromUri;
        }

        public static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            float height = options.OutHeight;
            float width = options.OutWidth;
            double inSampleSize = 1D;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = (int)(height / 2);
                int halfWidth = (int)(width / 2);

                // Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
                while ((halfHeight / inSampleSize) > reqHeight && (halfWidth / inSampleSize) > reqWidth)
                {
                    inSampleSize *= 2;
                }
            }
            return (int)inSampleSize;
        }
    }
    // VIEW HOLDER

    // Implement the ViewHolder pattern: each ViewHolder holds references
    // to the UI components (ImageView and TextView) within the CardView 
    // that is displayed in a row of the RecyclerView:
    public class ListitemViewHolder : RecyclerView.ViewHolder
    {
        public TextView Description { get; set; }
        public ImageView Image { get; set; }
        public ImageView ChkBox { get; set; }

        // Get references to the views defined in the CardView layout.
        public ListitemViewHolder(View itemView, Action<int> listener, Action<int> llistener, Action<int> alistener, Action<object, EventArgs> ilistener)
            : base(itemView)
        {
            // Locate and cache view references:
            Description = itemView.FindViewById<TextView>(Resource.Id.item_infor);
            Image = itemView.FindViewById<ImageView>(Resource.Id.item_img);
            ChkBox = itemView.FindViewById<ImageView>(Resource.Id.imageButton);

            // Detect user clicks on the item view and report which item
            // was clicked (by position) to the listener:
            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => llistener(AdapterPosition);
            ChkBox.Click += (s, e) => alistener(AdapterPosition);
            Image.Click += new EventHandler(ilistener);
        }
    }
    public class ShoppingListAdapter : RecyclerView.Adapter
    {
        // Event handler for item clicks:
        public event EventHandler<int> ItemClick;

        public event EventHandler<int> ItemLongClick;

        public event EventHandler<int> ChkBoxClick;

        public event EventHandler<EventArgs> ImageClick;

        // Underlying data set (a photo album):
        public List<ListItem> mShoppingList;

        // Load the adapter with the data set (photo album) at construction time:
        public ShoppingListAdapter(List<ListItem> shoppingList)
        {
            mShoppingList = shoppingList;
        }

        // Create a new photo CardView (invoked by the layout manager): 
        public override RecyclerView.ViewHolder
            OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate the CardView for the photo:
            View itemView = LayoutInflater.From(parent.Context).
                        Inflate(Resource.Layout.ListItem, parent, false);

            View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ListItem, parent, false);

            // Create a ViewHolder to find and hold these view references, and 
            // register OnClick with the view holder:
            ListitemViewHolder vh = new ListitemViewHolder(itemView, OnClick, OnLongClick, OnChkBoxClick, OnImageClick);
            return vh;
        }

        // Fill in the contents of the photo card (invoked by the layout manager):
        public override void
            OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            ListitemViewHolder vh = holder as ListitemViewHolder;

            int checkeRresourceId = (int)typeof(Resource.Drawable).GetField("ic_check_box_white_24dp").GetValue(null);
            int resourceId = (int)typeof(Resource.Drawable).GetField("ic_check_box_outline_blank_white_24dp").GetValue(null);


            // Set the ImageView and TextView in this ViewHolder's CardView 
            // from this position in the photo album:
            vh.Image.SetImageBitmap(mShoppingList[position].bm);
            vh.Description.Text = mShoppingList[position].title;
            if (mShoppingList[vh.AdapterPosition].check == 1)
            {
                vh.ChkBox.SetImageResource(checkeRresourceId);
            }
            else
            {
                vh.ChkBox.SetImageResource(resourceId);
            }
            vh.Image.Tag = mShoppingList[position].path;
        }

        // Return the number of photos available in the photo album:
        public override int ItemCount
        {
            get { return mShoppingList.Count; }
        }

        // Raise an event when the item-click takes place:
        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }
        void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
        }
        void OnChkBoxClick(int position)
        {
            ChkBoxClick(this, position);
        }
        void OnImageClick(object sender, EventArgs e)
        {
            ImageClick(sender, e);
        }
    }
}
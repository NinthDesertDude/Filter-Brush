using BrushFilter.Properties;
using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrushFilter
{
    /// <summary>
    /// The dialog used for working with the effect.
    /// </summary>
    public class winBrushFilter : EffectConfigDialog
    {
        #region Fields
        /// <summary>
        /// Causes the dialog to load when ready. The first time the dialog
        /// is called it will apply the filter twice, then PDN will stop
        /// calling DialogLoad and it will be applied once.
        /// </summary>
        private bool hasLoaded = false;

        /// <summary>
        /// When true, the effect surface has 192 alpha as a preview.
        /// </summary>
        private bool doPreview = false;

        /// <summary>
        /// Creates the list of brushes used by the brush selector.
        /// </summary>
        private BindingList<BrushSelectorItem> loadedBrushes;

        /// <summary>
        /// Stores the user's custom brushes by file and path until it can
        /// be copied to persistent settings, or ignored.
        /// </summary>
        private List<string> loadedBrushPaths = new List<string>();

        /// <summary>
        /// Whether the user is drawing on the image.
        /// </summary>
        private bool isUserDrawing = false;

        /// <summary>
        /// Whether the user is panning the image.
        /// </summary>
        private bool isUserPanning = false;

        /// <summary>
        /// Stores the current mouse location.
        /// </summary>
        private Point mouseLoc = new Point();

        /// <summary>
        /// Stores the mouse location at the last place a brush stroke was
        /// successfully applied. Used exclusively by minimum draw distance.
        /// </summary>
        private Point? mouseLocBrush;

        /// <summary>
        /// Stores the previous mouse location.
        /// </summary>
        private Point mouseLocPrev = new Point();

        /// <summary>
        /// Sets up a randomizer for brush dynamics.
        /// </summary>
        private Random random = new Random();

        /// <summary>
        ///Stores a list of temporary files by name, to be used by redo. Files
        ///will be reloaded to redo changes.
        /// </summary>
        private Stack<string> redoHistory = new Stack<string>();

        /// <summary>
        ///Stores a list of temporary files by name, to be used by undo. Files
        ///will be reloaded to undo changes.
        /// </summary>
        private Stack<string> undoHistory = new Stack<string>();

        /// <summary>
        /// Contains the current brush.
        /// </summary>
        private Bitmap bmpBrush;

        /// <summary>
        /// Stores the current drawing in full.
        /// </summary>
        private Bitmap bmpCurrentDrawing = new Bitmap(1, 1);

        /// <summary>
        /// Stores the bitmap copy with the effect applied to it. When the user
        /// begins a brush stroke, the drawing is copied and an effect like
        /// blur is applied across it, then alpha is set to 0. 
        /// </summary>
        private Bitmap bmpEffectDrawing = new Bitmap(1, 1);

        /// <summary>
        /// Stores the original alpha values of each pixel during the effect.
        /// </summary>
        private byte[,] bmpEffectAlpha;

        /// <summary>
        /// All non-GUI controls must register the components container as the
        /// parent so they can be disposed when the form exits.
        /// </summary>
        private IContainer components;

        /// <summary>
        /// Contains the current image being drawn on.
        /// </summary>
        internal PictureBox displayCanvas;

        /// <summary>
        /// Draws a checkerboard background behind the drawing region.
        /// </summary>
        private Panel displayCanvasBG;

        /// <summary>
        /// Stores the zoom percentage for the drawing region.
        /// </summary>
        private float displayCanvasZoom = 1;

        /// <summary>
        /// Determines the direction of intensity shifting, which can be growing
        /// (true) or shrinking (false). Used by the intensity shift slider.
        /// </summary>
        private bool isGrowingIntensity = true;

        /// <summary>
        /// Determines the direction of size shifting, which can be growing
        /// (true) or shrinking (false). Used by the size shift slider.
        /// </summary>
        private bool isGrowingSize = true;

        /// <summary>
        /// The outline of the user's selection.
        /// </summary>
        private PdnRegion selectionOutline;

        /// <summary>
        /// Contains the list of all symmetry options for using brush strokes.
        /// </summary>
        BindingList<Tuple<string, string>> symmetryOptions;

        /// <summary>
        /// Contains the list of all effect choices.
        /// </summary>
        BindingList<Tuple<string, CmbxEffectOptions>> effectOptions;

        /// <summary>
        /// Tracks when the user draws out-of-bounds and moves the canvas to
        /// accomodate them.
        /// </summary>
        private Timer timerRepositionUpdate;

        /// <summary>
        /// Displays a list of brushes to choose from.
        /// </summary>
        private ComboBox bttnBrushSelector;

        /// <summary>
        /// Allows the user to cancel and exit without applying the effect.
        /// </summary>
        private Button bttnCancel;

        /// <summary>
        /// Removes all custom brushes imported by the user.
        /// </summary>
        private Button bttnClearBrushes;

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        private Button bttnClearSettings;

        /// <summary>
        /// Sets permanent directories to browse for brushes on load.
        /// </summary>
        private Button bttnCustomBrushLocations;

        /// <summary>
        /// Allows the user to accept and apply the effect.
        /// </summary>
        private Button bttnOk;

        /// <summary>
        /// Allows the user to redo a previously undone change.
        /// </summary>
        private Button bttnRedo;

        /// <summary>
        /// The user can enable symmetry to draw mirrored brush strokes.
        /// </summary>
        private ComboBox cmbxSymmetry;

        /// <summary>
        /// Allows the user to undo a committed change.
        /// </summary>
        private Button bttnUndo;

        /// <summary>
        /// When active, the brush will be affected by the mouse angle. By
        /// default, brushes "facing" to the right (like an 'arrow brush')
        /// will point in the same direction as the mouse, while brushes that
        /// don't already point to the right will seem to be offset by some
        /// amount. The brush rotation can be used as a relative offset to fix
        /// this.
        /// </summary>
        private CheckBox chkbxOrientToMouse;

        /// <summary>
        /// Labels the miscellaneous brush options area.
        /// </summary>
        private GroupBox grpbxBrushOptions;

        /// <summary>
        /// Controls the intensity of the effect (strength).
        /// </summary>
        private TrackBar sliderBrushIntensity;

        /// <summary>
        /// Controls the brush orientation.
        /// </summary>
        private TrackBar sliderBrushRotation;

        /// <summary>
        /// Controls the size of the drawing brush.
        /// </summary>
        private TrackBar sliderBrushSize;

        /// <summary>
        /// Controls the zooming factor for the drawing region.
        /// </summary>
        private TrackBar sliderCanvasZoom;

        /// <summary>
        /// The mouse must be at least this far away from its last successful
        /// brush stroke position to create another brush stroke. Used for
        /// spacing between strokes.
        /// </summary>
        private TrackBar sliderMinDrawDistance;

        /// <summary>
        /// Randomly repositions the brush left or right while drawing.
        /// </summary>
        private TrackBar sliderRandHorzShift;

        /// <summary>
        /// Controls the maximum effect's intensity amount.
        /// </summary>
        private TrackBar sliderRandMaxIntensity;

        /// <summary>
        /// Controls the maximum brush size range.
        /// </summary>
        private TrackBar sliderRandMaxSize;

        /// <summary>
        /// Controls the minimum effect's intensity amount.
        /// </summary>
        private TrackBar sliderRandMinIntensity;

        /// <summary>
        /// Controls the minimum brush size range.
        /// </summary>
        private TrackBar sliderRandMinSize;

        /// <summary>
        /// Controls the minimum brush rotation range, which is negative.
        /// </summary>
        private TrackBar sliderRandRotLeft;

        /// <summary>
        /// Controls the maximum brush rotation range, which is positive.
        /// </summary>
        private TrackBar sliderRandRotRight;

        /// <summary>
        /// Randomly repositions the brush up or down while drawing.
        /// </summary>
        private TrackBar sliderRandVertShift;

        /// <summary>
        /// Allows the effect intensity to change by adding this value,
        /// which may be negative, on each successful brush stroke.
        /// </summary>
        private TrackBar sliderShiftIntensity;

        /// <summary>
        /// Draws the name of the shift rotation slider.
        /// </summary>
        private TrackBar sliderShiftRotation;

        /// <summary>
        /// Allows the brush size to change by adding this value, which may be
        /// negative, on each successful brush stroke.
        /// </summary>
        private TrackBar sliderShiftSize;

        /// <summary>
        /// Contains all tab pages.
        /// </summary>
        private TabControl tabBar;

        /// <summary>
        /// Contains the main, important controls.
        /// </summary>
        private TabPage tabControls;

        /// <summary>
        /// Contains the different possible effects to choose from.
        /// </summary>
        private TabPage tabEffect;

        /// <summary>
        /// Contains controls for randomly changing brush settings without
        /// regard to the previous randomly-selected settings.
        /// </summary>
        private TabPage tabJitter;

        /// <summary>
        /// Contains controls for incrementing brush settings a specified
        /// amount on each successful brush stroke.
        /// </summary>
        private TabPage tabOther;

        /// <summary>
        /// Draws the name of the brush intensity slider.
        /// </summary>
        private Label txtBrushIntensity;

        /// <summary>
        /// Draws the name of the brush rotation slider.
        /// </summary>
        private Label txtBrushRotation;

        /// <summary>
        /// Draws the name of the brush size slider.
        /// </summary>
        private Label txtBrushSize;

        /// <summary>
        /// Draws the name of the canvas zoom slider.
        /// </summary>
        private Label txtCanvasZoom;

        /// <summary>
        /// Draws the name of the minimum drawing distance slider.
        /// </summary>
        private Label txtMinDrawDistance;

        /// <summary>
        /// Draws the name of the random horizontal shift slider.
        /// </summary>
        private Label txtRandHorzShift;

        /// <summary>
        /// Draws the name of the random max intensity slider.
        /// </summary>
        private Label txtRandMaxIntensity;

        /// <summary>
        /// Draws the name of the random max size slider.
        /// </summary>
        private Label txtRandMaxSize;

        /// <summary>
        /// Draws the name of the random min intensity slider.
        /// </summary>
        private Label txtRandMinIntensity;

        /// <summary>
        /// Draws the name of the random min size slider.
        /// </summary>
        private Label txtRandMinSize;

        /// <summary>
        /// Draws the name of the random rotation to the left slider.
        /// </summary>
        private Label txtRandRotLeft;

        /// <summary>
        /// Draws the name of the random rotation to the right slider.
        /// </summary>
        private Label txtRandRotRight;

        /// <summary>
        /// Draws the name of the random vertical shift slider.
        /// </summary>
        private Label txtRandVertShift;

        /// <summary>
        /// Draws the name of the shift intensity slider.
        /// </summary>
        private Label txtShiftIntensity;

        /// <summary>
        /// Draws the name of the shift rotation slider.
        /// </summary>
        private Label txtShiftRotation;

        /// <summary>
        /// Draws the name of the shift size slider.
        /// </summary>
        private Label txtShiftSize;
        private TrackBar sliderEffectProperty2;
        private Label txtEffectProperty2;
        private TrackBar sliderEffectProperty1;
        private Label txtEffectProperty1;
        private TrackBar sliderEffectProperty4;
        private Label txtEffectProperty4;
        private TrackBar sliderEffectProperty3;
        private Label txtEffectProperty3;
        private Label txtEffectType;
        private ComboBox cmbxEffectType;

        /// <summary>
        /// Provides useful messages when hovering over controls.
        /// </summary>
        private Label txtTooltip;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes components and brushes.
        /// </summary>
        public winBrushFilter()
        {
            InitializeComponent();
            InitBrushes();

            //Configures items for the symmetry options combobox.
            symmetryOptions = new BindingList<Tuple<string, string>>();
            symmetryOptions.Add(new Tuple<string, string>(
                Globalization.GlobalStrings.CmbxSymmetryNone, "None"));
            symmetryOptions.Add(new Tuple<string, string>(
                Globalization.GlobalStrings.CmbxSymmetryHorz, "Horizontal"));
            symmetryOptions.Add(new Tuple<string, string>(
                Globalization.GlobalStrings.CmbxSymmetryVert, "Vertical"));
            symmetryOptions.Add(new Tuple<string, string>(
                Globalization.GlobalStrings.CmbxSymmetryBoth, "Both"));
            cmbxSymmetry.DataSource = symmetryOptions;
            cmbxSymmetry.DisplayMember = "Item1";
            cmbxSymmetry.ValueMember = "Item2";

            //Configures items for the effect combobox.
            cmbxEffectType.SelectedValueChanged -= cmbxEffectType_SelectedValueChanged;
            effectOptions = new BindingList<Tuple<string, CmbxEffectOptions>>();
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectDodgeBurn, CmbxEffectOptions.DodgeBurn));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectBlackWhite, CmbxEffectOptions.BlackAndWhite));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectBrightnessContrast, CmbxEffectOptions.BrightnessContrast));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectHueSaturation, CmbxEffectOptions.HueSaturation));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectInvertColors, CmbxEffectOptions.InvertColors));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectPosterize, CmbxEffectOptions.Posterize));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectSepia, CmbxEffectOptions.Sepia));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectRgbTint, CmbxEffectOptions.RgbTint));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectFlipHorizontal, CmbxEffectOptions.FlipHorizontal));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectFlipVertical, CmbxEffectOptions.FlipVertical));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectInkSketch, CmbxEffectOptions.InkSketch));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectOilPainting, CmbxEffectOptions.OilPainting));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectPencilSketch, CmbxEffectOptions.PencilSketch));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectFragment, CmbxEffectOptions.Fragment));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectBlur, CmbxEffectOptions.Blur));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectMotionBlur, CmbxEffectOptions.MotionBlur));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectSurfaceBlur, CmbxEffectOptions.SurfaceBlur));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectUnfocus, CmbxEffectOptions.Unfocus));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectZoomBlur, CmbxEffectOptions.ZoomBlur));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectBulge, CmbxEffectOptions.Bulge));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectCrystalize, CmbxEffectOptions.Crystalize));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectDents, CmbxEffectOptions.Dents));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectFrostedGlass, CmbxEffectOptions.FrostedGlass));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectPixelate, CmbxEffectOptions.Pixelate));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectTileReflection, CmbxEffectOptions.TileReflection));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectTwist, CmbxEffectOptions.Twist));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectAddNoise, CmbxEffectOptions.AddNoise));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectMedian, CmbxEffectOptions.Median));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectReduceNoise, CmbxEffectOptions.ReduceNoise));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectGlow, CmbxEffectOptions.Glow));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectSharpen, CmbxEffectOptions.Sharpen));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectSoftenPortrait, CmbxEffectOptions.SoftenPortrait));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectVignette, CmbxEffectOptions.Vignette));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectClouds, CmbxEffectOptions.Clouds));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectEdgeDetect, CmbxEffectOptions.EdgeDetect));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectEmboss, CmbxEffectOptions.Emboss));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectOutline, CmbxEffectOptions.Outline));
            effectOptions.Add(new Tuple<string, CmbxEffectOptions>(
                Globalization.GlobalStrings.CmbxEffectRelief, CmbxEffectOptions.Relief));
            cmbxEffectType.DataSource = effectOptions;
            cmbxEffectType.DisplayMember = "Item1";
            cmbxEffectType.ValueMember = "Item2";
            cmbxEffectType.SelectedValueChanged += cmbxEffectType_SelectedValueChanged;

            //Forces the window to cover the screen without being maximized.
            Left = Top = 0;
            Width = Screen.PrimaryScreen.WorkingArea.Width;
            Height = Screen.PrimaryScreen.WorkingArea.Height;
        }
        #endregion

        #region Methods (overridden)
        /// <summary>
        /// Configures settings so they can be stored between consecutive
        /// calls of the effect.
        /// </summary>
        protected override void InitialInitToken()
        {
            theEffectToken = new PersistentSettings(20, "", 0, 100, 0, 0, 0, 0,
                0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, new List<string>());
        }

        /// <summary>
        /// Sets up the GUI to reflect the previously-used settings; i.e. this
        /// loads the settings. Called twice by a quirk of Paint.NET.
        /// </summary>
        protected override void InitDialogFromToken(EffectConfigToken effectToken)
        {
            //Copies GUI values from the settings.
            PersistentSettings token = (PersistentSettings)effectToken;
            sliderBrushSize.Value = token.BrushSize;

            //Loads custom brushes if possible, but skips duplicates. This
            //method is called twice by Paint.NET for some reason, so this
            //ensures there are no duplicates. Brush names are unique.
            if (token.CustomBrushLocations.Count > 0)
            {
                for (int i = 0; i < token.CustomBrushLocations.Count; i++)
                {
                    if (!loadedBrushPaths.Contains(token.CustomBrushLocations[i]))
                    {
                        //Ensures the brush location is preserved, then loads
                        //the brush.
                        loadedBrushPaths.Add(token.CustomBrushLocations[i]);
                        ImportBrushes(
                            new string[] { token.CustomBrushLocations[i] },
                            false,
                            false);
                    }
                }
            }

            //Attempts to find the brush's index in the current list of
            //brushes, by name. If it doesn't exist, it's set to default: "".
            int brushIndex = loadedBrushes.ToList()
                .FindIndex(o => o.Name.Equals(token.BrushName));

            //Doesn't copy custom brushes and brushes that weren't found.
            if (token.BrushName.Equals("") ||
                token.BrushName.Equals(BrushSelectorItem.CustomBrush.Name) ||
                brushIndex == -1)
            {
                bttnBrushSelector.SelectedIndex = 0;
            }
            else
            {
                bttnBrushSelector.SelectedIndex = brushIndex;
            }

            //Sets all other fields.
            sliderBrushRotation.Value = token.BrushRotation;
            sliderBrushIntensity.Value = token.BrushIntensity;
            sliderRandHorzShift.Value = token.RandHorzShift;
            sliderRandMaxIntensity.Value = token.RandMaxIntensity;
            sliderRandMaxSize.Value = token.RandMaxSize;
            sliderRandMinIntensity.Value = token.RandMinIntensity;
            sliderRandMinSize.Value = token.RandMinSize;
            sliderRandRotLeft.Value = token.RandRotLeft;
            sliderRandRotRight.Value = token.RandRotRight;
            sliderRandVertShift.Value = token.RandVertShift;
            chkbxOrientToMouse.Checked = token.DoRotateWithMouse;
            sliderMinDrawDistance.Value = token.MinDrawDistance;
            sliderShiftSize.Value = token.SizeChange;
            sliderShiftRotation.Value = token.RotChange;
            sliderShiftIntensity.Value = token.IntensityChange;
            cmbxSymmetry.SelectedIndex = token.SymmetryMode;
            cmbxEffectType.SelectedIndex = token.EffectMode;
            sliderEffectProperty1.Value = Utils.Clamp(token.EffectProperty1,
                sliderEffectProperty1.Minimum, sliderEffectProperty1.Maximum);
            sliderEffectProperty2.Value = Utils.Clamp(token.EffectProperty2,
                sliderEffectProperty2.Minimum, sliderEffectProperty2.Maximum);
            sliderEffectProperty3.Value = Utils.Clamp(token.EffectProperty3,
                sliderEffectProperty3.Minimum, sliderEffectProperty3.Maximum);
            sliderEffectProperty4.Value = Utils.Clamp(token.EffectProperty4,
                sliderEffectProperty4.Minimum, sliderEffectProperty4.Maximum);
        }

        /// <summary>
        /// Overwrites the settings with the dialog's current settings so they
        /// can be reused later; i.e. this saves the settings.
        /// </summary>
        protected override void InitTokenFromDialog()
        {
            var token = (PersistentSettings)EffectToken;

            token.BrushSize = sliderBrushSize.Value;
            token.BrushName = (bttnBrushSelector.SelectedItem as BrushSelectorItem).Name;
            token.BrushRotation = sliderBrushRotation.Value;
            token.BrushIntensity = sliderBrushIntensity.Value;
            token.RandHorzShift = sliderRandHorzShift.Value;
            token.RandMaxIntensity = sliderRandMaxIntensity.Value;
            token.RandMaxSize = sliderRandMaxSize.Value;
            token.RandMinIntensity = sliderRandMinIntensity.Value;
            token.RandMinSize = sliderRandMinSize.Value;
            token.RandRotLeft = sliderRandRotLeft.Value;
            token.RandRotRight = sliderRandRotRight.Value;
            token.RandVertShift = sliderRandVertShift.Value;
            token.DoRotateWithMouse = chkbxOrientToMouse.Checked;
            token.MinDrawDistance = sliderMinDrawDistance.Value;
            token.SizeChange = sliderShiftSize.Value;
            token.RotChange = sliderShiftRotation.Value;
            token.IntensityChange = sliderShiftIntensity.Value;
            token.SymmetryMode = cmbxSymmetry.SelectedIndex;
            token.EffectMode = cmbxEffectType.SelectedIndex;
            token.EffectProperty1 = sliderEffectProperty1.Value;
            token.EffectProperty2 = sliderEffectProperty2.Value;
            token.EffectProperty3 = sliderEffectProperty3.Value;
            token.EffectProperty4 = sliderEffectProperty4.Value;
            token.CustomBrushLocations = loadedBrushPaths;
        }

        /// <summary>
        /// Repaints only the visible areas of the drawing region.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            SolidBrush colorBrush = new SolidBrush(BackColor);

            e.Graphics.FillRectangle(colorBrush,
                new Rectangle(0, 0, ClientRectangle.Width, displayCanvasBG.Top));

            e.Graphics.FillRectangle(colorBrush,
                new Rectangle(0, displayCanvasBG.Bottom, ClientRectangle.Width, ClientRectangle.Bottom));

            e.Graphics.FillRectangle(colorBrush,
                new Rectangle(0, 0, displayCanvasBG.Left, ClientRectangle.Height));

            e.Graphics.FillRectangle(colorBrush,
                new Rectangle(displayCanvasBG.Right, 0, ClientRectangle.Width - displayCanvasBG.Right,
                this.ClientRectangle.Height));
        }
        #endregion

        #region Methods (not event handlers)
        /// <summary>
        /// Sets the brushes to be used, clearing any that already exist and
        /// removing all custom brushes as a result.
        /// </summary>
        private void InitBrushes()
        {
            bmpBrush = new Bitmap(Resources.BrCircle);

            //Configures the default list of brushes for the brush selector.
            loadedBrushes = new BindingList<BrushSelectorItem>();

            // Retrieves values from the registry for the gui.
            Microsoft.Win32.RegistryKey key =
                Microsoft.Win32.Registry.CurrentUser
                .CreateSubKey("software", true)
                .CreateSubKey("paint.net_brushfilter", true);

            //Gets whether default brushes should be used.
            bool useDefaultBrushes = true;
            string value = (string)key.GetValue("useDefaultBrushes");
            if (value != null)
            {
                Boolean.TryParse(value, out useDefaultBrushes);
            }

            //Gets the desired locations to load custom brushes from.
            string[] customBrushDirectories = { };
            value = (string)key.GetValue("customBrushLocations");
            if (value != null)
            {
                customBrushDirectories = value.Split('\n');
            }

            key.Close();

            //Loads stored brushes.
            loadedBrushes.Add(new BrushSelectorItem("Circle 1", Resources.BrCircle));

            if (useDefaultBrushes)
            {
                loadedBrushes.Add(new BrushSelectorItem("Circle 2", Resources.BrCircleMedium));
                loadedBrushes.Add(new BrushSelectorItem("Circle 3", Resources.BrCircleHard));
                loadedBrushes.Add(new BrushSelectorItem("Rough", Resources.BrCircleRough));
                loadedBrushes.Add(new BrushSelectorItem("Sketchy", Resources.BrCircleSketchy));
                loadedBrushes.Add(new BrushSelectorItem("Segments", Resources.BrCircleSegmented));
                loadedBrushes.Add(new BrushSelectorItem("Spiral", Resources.BrSpiral));
                loadedBrushes.Add(new BrushSelectorItem("Cracks", Resources.BrCracks));
                loadedBrushes.Add(new BrushSelectorItem("Dirt 1", Resources.BrDirt));
                loadedBrushes.Add(new BrushSelectorItem("Dirt 2", Resources.BrDirt2));
                loadedBrushes.Add(new BrushSelectorItem("Dirt 3", Resources.BrDirt3));
                loadedBrushes.Add(new BrushSelectorItem("Dirt 4", Resources.BrFractalDirt));
                loadedBrushes.Add(new BrushSelectorItem("Scales", Resources.BrScales));
                loadedBrushes.Add(new BrushSelectorItem("Smoke", Resources.BrSmoke));
                loadedBrushes.Add(new BrushSelectorItem("Grass", Resources.BrGrass));
                loadedBrushes.Add(new BrushSelectorItem("Rain", Resources.BrRain));
                loadedBrushes.Add(new BrushSelectorItem("Gravel", Resources.BrGravel));
                loadedBrushes.Add(new BrushSelectorItem("Spark", Resources.BrSpark));
                loadedBrushes.Add(new BrushSelectorItem("Big Dots", Resources.BrDotsBig));
                loadedBrushes.Add(new BrushSelectorItem("Tiny Dots", Resources.BrDotsTiny));
                loadedBrushes.Add(new BrushSelectorItem("Line", Resources.BrLine));
            }

            loadedBrushes.Add(BrushSelectorItem.CustomBrush);

            //Enables dynamic binding and sets the list.
            bttnBrushSelector.DataSource = loadedBrushes;
            bttnBrushSelector.DisplayMember = "Name";
            bttnBrushSelector.ValueMember = "Brush";

            //Loads any custom brushes.
            ImportBrushes(FilesInDirectory(customBrushDirectories), true, false);
        }

        /// <summary>
        /// Sets/resets all persistent settings in the dialog to their default
        /// values.
        /// </summary>
        private void InitSettings()
        {
            InitialInitToken();
            InitDialogFromToken();
        }

        /// <summary>
        /// Updates the effect properties' labels and slider values to
        /// reflect the current effect choice.
        /// </summary>
        private void SetEffectProperties(bool resetSliders)
        {
            switch (((Tuple<string, CmbxEffectOptions>)cmbxEffectType.SelectedItem).Item2)
            {
                case CmbxEffectOptions.BlackAndWhite:
                case CmbxEffectOptions.InvertColors:
                case CmbxEffectOptions.Sepia:
                case CmbxEffectOptions.FlipHorizontal:
                case CmbxEffectOptions.FlipVertical:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = false;
                    sliderEffectProperty1.Enabled = false;
                    txtEffectProperty1.Visible = false;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;
                    break;
                case CmbxEffectOptions.BrightnessContrast:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = -100;
                    sliderEffectProperty1.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty1.Value = 0; }

                    sliderEffectProperty2.Minimum = -100;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 0; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectBrightnessContrastProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectBrightnessContrastProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectBrightnessContrastProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectBrightnessContrastProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.HueSaturation:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = -180;
                    sliderEffectProperty1.Maximum = 180;
                    if (resetSliders) { sliderEffectProperty1.Value = 0; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty2.Value = 100; }

                    sliderEffectProperty3.Minimum = -100;
                    sliderEffectProperty3.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty3.Value = 0; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectHueSaturationProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectHueSaturationProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectHueSaturationProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectHueSaturationProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectHueSaturationProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectHueSaturationProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Posterize:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 2;
                    sliderEffectProperty1.Maximum = 64;
                    if (resetSliders) { sliderEffectProperty1.Value = 16; }

                    sliderEffectProperty2.Minimum = 2;
                    sliderEffectProperty2.Maximum = 64;
                    if (resetSliders) { sliderEffectProperty2.Value = 16; }

                    sliderEffectProperty3.Minimum = 2;
                    sliderEffectProperty3.Maximum = 64;
                    if (resetSliders) { sliderEffectProperty3.Value = 16; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectPosterizeProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectPosterizeProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectPosterizeProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectPosterizeProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectPosterizeProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectPosterizeProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.InkSketch:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 99;
                    if (resetSliders) { sliderEffectProperty1.Value = 50; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 50; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectInkSketchProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectInkSketchProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectInkSketchProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectInkSketchProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.OilPainting:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 8;
                    if (resetSliders) { sliderEffectProperty1.Value = 3; }

                    sliderEffectProperty2.Minimum = 2;
                    sliderEffectProperty2.Maximum = 255;
                    if (resetSliders) { sliderEffectProperty2.Value = 50; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectOilPaintingProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectOilPaintingProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectOilPaintingProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectOilPaintingProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.PencilSketch:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 20;
                    if (resetSliders) { sliderEffectProperty1.Value = 2; }

                    sliderEffectProperty2.Minimum = -20;
                    sliderEffectProperty2.Maximum = 20;
                    if (resetSliders) { sliderEffectProperty2.Value = 0; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectPencilSketchProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectPencilSketchProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectPencilSketchProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectPencilSketchProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Fragment:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 2;
                    sliderEffectProperty1.Maximum = 50;
                    if (resetSliders) { sliderEffectProperty1.Value = 4; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 8; }

                    sliderEffectProperty3.Minimum = 0;
                    sliderEffectProperty3.Maximum = 359;
                    if (resetSliders) { sliderEffectProperty3.Value = 0; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectFragmentProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectFragmentProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectFragmentProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectFragmentProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectFragmentProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectFragmentProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Blur:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 2;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectBlurProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectBlurProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.MotionBlur:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 359;
                    if (resetSliders) { sliderEffectProperty1.Value = 25; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty2.Value = 10; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectMotionBlurProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectMotionBlurProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectMotionBlurProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectMotionBlurProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.SurfaceBlur:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty1.Value = 6; }

                    sliderEffectProperty2.Minimum = 1;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 15; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectSurfaceBlurProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectSurfaceBlurProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectSurfaceBlurProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectSurfaceBlurProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Unfocus:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 4; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectUnfocusProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectUnfocusProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.ZoomBlur:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty1.Value = 10; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectZoomBlurProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectZoomBlurProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Bulge:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = -200;
                    sliderEffectProperty1.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty1.Value = 45; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectBulgeProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectBulgeProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Crystalize:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 2;
                    sliderEffectProperty1.Maximum = 250;
                    if (resetSliders) { sliderEffectProperty1.Value = 8; }

                    sliderEffectProperty2.Minimum = 1;
                    sliderEffectProperty2.Maximum = 5;
                    if (resetSliders) { sliderEffectProperty2.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectCrystalizeProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectCrystalizeProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectCrystalizeProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectCrystalizeProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Dents:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = true;
                    sliderEffectProperty4.Enabled = true;
                    txtEffectProperty4.Visible = true;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 25; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty2.Value = 50; }

                    sliderEffectProperty3.Minimum = 0;
                    sliderEffectProperty3.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty3.Value = 10; }

                    sliderEffectProperty4.Minimum = 0;
                    sliderEffectProperty4.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty4.Value = 10; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectDentsProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectDentsProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectDentsProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectDentsProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectDentsProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectDentsProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);

                    txtEffectProperty4.Tag = Globalization.GlobalStrings.EffectDentsProperty4;
                    sliderEffectProperty4.Tag = Globalization.GlobalStrings.EffectDentsProperty4Tip;
                    sliderEffectProperty4_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.FrostedGlass:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 3; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty2.Value = 0; }

                    sliderEffectProperty3.Minimum = 1;
                    sliderEffectProperty3.Maximum = 8;
                    if (resetSliders) { sliderEffectProperty3.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectFrostedGlassProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectFrostedGlassProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectFrostedGlassProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectFrostedGlassProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectFrostedGlassProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectFrostedGlassProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Pixelate:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty1.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectPixelateProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectPixelateProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.TileReflection:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = true;
                    sliderEffectProperty4.Enabled = true;
                    txtEffectProperty4.Visible = true;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 359;
                    if (resetSliders) { sliderEffectProperty1.Value = 30; }

                    sliderEffectProperty2.Minimum = 1;
                    sliderEffectProperty2.Maximum = 800;
                    if (resetSliders) { sliderEffectProperty2.Value = 40; }

                    sliderEffectProperty3.Minimum = -100;
                    sliderEffectProperty3.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty3.Value = 8; }

                    sliderEffectProperty4.Minimum = 1;
                    sliderEffectProperty4.Maximum = 5;
                    if (resetSliders) { sliderEffectProperty4.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);

                    txtEffectProperty4.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty4;
                    sliderEffectProperty4.Tag = Globalization.GlobalStrings.EffectTileReflectionProperty4Tip;
                    sliderEffectProperty4_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Twist:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = -200;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 30; }

                    sliderEffectProperty2.Minimum = 1;
                    sliderEffectProperty2.Maximum = 20000;
                    if (resetSliders) { sliderEffectProperty2.Value = 100; }

                    sliderEffectProperty3.Minimum = 1;
                    sliderEffectProperty3.Maximum = 5;
                    if (resetSliders) { sliderEffectProperty3.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectTwistProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectTwistProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectTwistProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectTwistProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectTwistProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectTwistProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.AddNoise:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty1.Value = 64; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 400;
                    if (resetSliders) { sliderEffectProperty2.Value = 100; }

                    sliderEffectProperty3.Minimum = 0;
                    sliderEffectProperty3.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty3.Value = 100; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectAddNoiseProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectAddNoiseProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectAddNoiseProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectAddNoiseProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectAddNoiseProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectAddNoiseProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Median:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 10; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 50; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectMedianProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectMedianProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectMedianProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectMedianProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.ReduceNoise:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 10; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 40; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectReduceNoiseProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectReduceNoiseProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectReduceNoiseProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectReduceNoiseProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Glow:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 20;
                    if (resetSliders) { sliderEffectProperty1.Value = 6; }

                    sliderEffectProperty2.Minimum = -100;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 10; }

                    sliderEffectProperty3.Minimum = -100;
                    sliderEffectProperty3.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty3.Value = 10; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectGlowProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectGlowProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectGlowProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectGlowProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectGlowProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectGlowProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Sharpen:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 20;
                    if (resetSliders) { sliderEffectProperty1.Value = 2; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectSharpenProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectSharpenProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.SoftenPortrait:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 10;
                    if (resetSliders) { sliderEffectProperty1.Value = 5; }

                    sliderEffectProperty2.Minimum = -20;
                    sliderEffectProperty2.Maximum = 20;
                    if (resetSliders) { sliderEffectProperty2.Value = 0; }

                    sliderEffectProperty3.Minimum = 0;
                    sliderEffectProperty3.Maximum = 20;
                    if (resetSliders) { sliderEffectProperty3.Value = 10; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectSoftenPortraitProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectSoftenPortraitProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectSoftenPortraitProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectSoftenPortraitProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectSoftenPortraitProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectSoftenPortraitProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Vignette:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 40;
                    if (resetSliders) { sliderEffectProperty1.Value = 5; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 100; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectVignetteProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectVignetteProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectVignetteProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectVignetteProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Clouds:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 2;
                    sliderEffectProperty1.Maximum = 1000;
                    if (resetSliders) { sliderEffectProperty1.Value = 65; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 50; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectCloudsProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectCloudsProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectCloudsProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectCloudsProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.EdgeDetect:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 359;
                    if (resetSliders) { sliderEffectProperty1.Value = 45; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectEdgeDetectProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectEdgeDetectProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Emboss:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 359;
                    if (resetSliders) { sliderEffectProperty1.Value = 45; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectEmbossProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectEmbossProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Outline:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 1;
                    sliderEffectProperty1.Maximum = 200;
                    if (resetSliders) { sliderEffectProperty1.Value = 3; }

                    sliderEffectProperty2.Minimum = 0;
                    sliderEffectProperty2.Maximum = 100;
                    if (resetSliders) { sliderEffectProperty2.Value = 50; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectOutlineProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectOutlineProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectOutlineProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectOutlineProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.Relief:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = 0;
                    sliderEffectProperty1.Maximum = 359;
                    if (resetSliders) { sliderEffectProperty1.Value = 45; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectReliefProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectReliefProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.DodgeBurn:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = false;
                    sliderEffectProperty2.Enabled = false;
                    txtEffectProperty2.Visible = false;

                    sliderEffectProperty3.Visible = false;
                    sliderEffectProperty3.Enabled = false;
                    txtEffectProperty3.Visible = false;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = -50;
                    sliderEffectProperty1.Maximum = 50;
                    if (resetSliders) { sliderEffectProperty1.Value = 0; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectDodgeBurnProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectDodgeBurnProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);
                    break;
                case CmbxEffectOptions.RgbTint:
                    //Sets property visibility / enabledness.
                    sliderEffectProperty1.Visible = true;
                    sliderEffectProperty1.Enabled = true;
                    txtEffectProperty1.Visible = true;

                    sliderEffectProperty2.Visible = true;
                    sliderEffectProperty2.Enabled = true;
                    txtEffectProperty2.Visible = true;

                    sliderEffectProperty3.Visible = true;
                    sliderEffectProperty3.Enabled = true;
                    txtEffectProperty3.Visible = true;

                    sliderEffectProperty4.Visible = false;
                    sliderEffectProperty4.Enabled = false;
                    txtEffectProperty4.Visible = false;

                    //Sets the range of enabled sliders.
                    sliderEffectProperty1.Minimum = -255;
                    sliderEffectProperty1.Maximum = 255;
                    if (resetSliders) { sliderEffectProperty1.Value = 0; }

                    sliderEffectProperty2.Minimum = -255;
                    sliderEffectProperty2.Maximum = 255;
                    if (resetSliders) { sliderEffectProperty2.Value = 0; }

                    sliderEffectProperty3.Minimum = -255;
                    sliderEffectProperty3.Maximum = 255;
                    if (resetSliders) { sliderEffectProperty3.Value = 0; }

                    //Updates the text and tooltip of enabled sliders.
                    txtEffectProperty1.Tag = Globalization.GlobalStrings.EffectRgbTintProperty1;
                    sliderEffectProperty1.Tag = Globalization.GlobalStrings.EffectRgbTintProperty1Tip;
                    sliderEffectProperty1_ValueChanged(this, null);

                    txtEffectProperty2.Tag = Globalization.GlobalStrings.EffectRgbTintProperty2;
                    sliderEffectProperty2.Tag = Globalization.GlobalStrings.EffectRgbTintProperty2Tip;
                    sliderEffectProperty2_ValueChanged(this, null);

                    txtEffectProperty3.Tag = Globalization.GlobalStrings.EffectRgbTintProperty3;
                    sliderEffectProperty3.Tag = Globalization.GlobalStrings.EffectRgbTintProperty3Tip;
                    sliderEffectProperty3_ValueChanged(this, null);
                    break;
            }

            //Forces the sliders to be in a valid range.
            if (!resetSliders)
            {
                sliderEffectProperty1.Value = Utils.Clamp(sliderEffectProperty1.Value,
                    sliderEffectProperty1.Minimum, sliderEffectProperty1.Maximum);
                sliderEffectProperty2.Value = Utils.Clamp(sliderEffectProperty2.Value,
                    sliderEffectProperty2.Minimum, sliderEffectProperty2.Maximum);
                sliderEffectProperty3.Value = Utils.Clamp(sliderEffectProperty3.Value,
                    sliderEffectProperty3.Minimum, sliderEffectProperty3.Maximum);
                sliderEffectProperty4.Value = Utils.Clamp(sliderEffectProperty4.Value,
                    sliderEffectProperty4.Minimum, sliderEffectProperty4.Maximum);
            }

            //Applies an effect to the bitmap.
            ApplyFilter();
        }

        /// <summary>
        /// Allows the canvas to zoom in and out dependent on the mouse wheel.
        /// </summary>
        private void Zoom(int mouseWheelDetents, bool updateSlider)
        {
            //Causes the slider's update method to trigger, which calls this
            //and doesn't repeat anything since updateSlider is then false.
            if (updateSlider)
            {
                //Zooms in/out some amount for each mouse wheel movement.
                int zoom;
                if (sliderCanvasZoom.Value < 5)
                {
                    zoom = 1;
                }
                else if (sliderCanvasZoom.Value < 10)
                {
                    zoom = 3;
                }
                else if (sliderCanvasZoom.Value < 20)
                {
                    zoom = 5;
                }
                else if (sliderCanvasZoom.Value < 50)
                {
                    zoom = 10;
                }
                else if (sliderCanvasZoom.Value < 100)
                {
                    zoom = 15;
                }
                else if (sliderCanvasZoom.Value < 200)
                {
                    zoom = 30;
                }
                else if (sliderCanvasZoom.Value < 500)
                {
                    zoom = 50;
                }
                else if (sliderCanvasZoom.Value < 1000)
                {
                    zoom = 100;
                }
                else if (sliderCanvasZoom.Value < 2000)
                {
                    zoom = 200;
                }
                else
                {
                    zoom = 300;
                }

                zoom *= Math.Sign(mouseWheelDetents);

                //Updates the corresponding slider as well (within its range).
                sliderCanvasZoom.Value = Utils.Clamp(
                sliderCanvasZoom.Value + zoom,
                sliderCanvasZoom.Minimum,
                sliderCanvasZoom.Maximum);

                return;
            }

            //Calculates the zooming percent.
            float newZoomFactor = sliderCanvasZoom.Value / 100f;

            //Updates the canvas zoom factor.
            displayCanvasZoom = newZoomFactor;
            txtCanvasZoom.Text = String.Format(
                "{0} {1:p0}",
                Globalization.GlobalStrings.CanvasZoom,
                newZoomFactor);

            //Gets the new width and height, adjusted for zooming.
            int zoomWidth = (int)(bmpCurrentDrawing.Width * newZoomFactor);
            int zoomHeight = (int)(bmpCurrentDrawing.Height * newZoomFactor);

            //Sets the new canvas position (center) and size using zoom.
            displayCanvas.Bounds = new Rectangle(
                (displayCanvasBG.Width - zoomWidth) / 2,
                (displayCanvasBG.Height - zoomHeight) / 2,
                zoomWidth, zoomHeight);
        }

        /// <summary>
        /// Applies the brush at the specified point in the canvas.
        /// </summary>
        /// <param name="canvas">
        /// The image to draw on.
        /// </param>
        /// <param name="brush">
        /// The image to draw onto the canvas.
        /// </param>
        /// <param name="coords">
        /// The brush drawing position.
        /// </param>
        /// <param name="intensityMultiplier">
        /// A percentage, expresesd as a float, to multiply the intensity by.
        /// </param>
        public unsafe bool UncoverBitmap(
            Bitmap canvas,
            Bitmap brush,
            Point coords)
        {
            //Formats must be the same.
            if (canvas.PixelFormat != PixelFormat.Format32bppArgb ||
            brush.PixelFormat != PixelFormat.Format32bppArgb)
            {
                return false;
            }

            //Locks the pixels to be edited.
            BitmapData canvasData, brushData;

            //Gets the brush area to draw.
            int brushX = Utils.Clamp(-coords.X, 0, brush.Width);
            int brushY = Utils.Clamp(-coords.Y, 0, brush.Height);
            int brushWidth = Utils.Clamp(brush.Width - brushX, 0,
                Math.Min(brush.Width, Math.Max(0, canvas.Width - coords.X)));
            int brushHeight = Utils.Clamp(brush.Height - brushY, 0,
                Math.Min(brush.Height, Math.Max(0, canvas.Height - coords.Y)));

            Rectangle brushRect = new Rectangle(
                brushX, brushY, brushWidth, brushHeight);

            //Gets the affected area on the canvas.
            int canvasX = Utils.Clamp(coords.X, 0, canvas.Width);
            int canvasY = Utils.Clamp(coords.Y, 0, canvas.Height);
            int canvasWidth = Utils.Clamp(canvasX + brushWidth / 2, 0, Math.Max(0, canvas.Width - canvasX));
            int canvasHeight = Utils.Clamp(canvasY + brushHeight / 2, 0, Math.Max(0, canvas.Height - canvasY));
            Rectangle canvasRect = new Rectangle(canvasX, canvasY, canvasWidth, canvasHeight);

            //Does not lockbits for an invalid rectangle.
            if (brushRect.Width <= 0 || brushRect.Height <= 0 ||
                canvasRect.Width <= 0 || canvasRect.Height <= 0)
            {
                return false;
            }

            canvasData = canvas.LockBits(
                canvasRect,
                ImageLockMode.ReadWrite,
                canvas.PixelFormat);

            brushData = brush.LockBits(
                brushRect,
                ImageLockMode.ReadOnly,
                brush.PixelFormat);

            byte* canvasRow = (byte*)canvasData.Scan0;
            byte* brushRow = (byte*)brushData.Scan0;

            //Calculations performed outside loop for performance.
            float sliderIntensity = sliderBrushIntensity.Value / 100f;
            int canvWidth = canvas.Width;
            int canvHeight = canvas.Height;

            //Iterates through each pixel in parallel.         
            Parallel.For(0, brushRect.Height, (y) =>
            {
                for (int x = 0; x < brushRect.Width; x++)
                {
                    //Doesn't consider pixels outside of the canvas image.
                    if (x >= canvWidth ||
                        y >= canvHeight)
                    {
                        continue;
                    }

                    int brushPtr = y * brushData.Stride + x * 4;
                    int canvasPtr = y * canvasData.Stride + x * 4;

                    //Gets the pixel intensity to use for the effect strength.
                    byte intensity = (byte)HsvColor.FromColor(ColorBgra.FromBgr(
                        brushRow[brushPtr],
                        brushRow[brushPtr + 1],
                        brushRow[brushPtr + 2])).Value;

                    //Increases alpha by intensity to "uncover" the surface.
                    canvasRow[canvasPtr + 3] =
                        (byte)Utils.Clamp(canvasRow[canvasPtr + 3] +
                        (int)((100 - intensity) * sliderIntensity), 0,
                            bmpEffectAlpha[canvasX + x, canvasY + y]);
                }
            });

            canvas.UnlockBits(canvasData);
            brush.UnlockBits(brushData);

            return true;
        }

        /// <summary>
        /// Generates the effect bitmap from the original with a filter applied
        /// across it.
        /// </summary>
        private unsafe void ApplyFilter()
        {
            //Stores the common variables.
            RenderArgs srcArgs = null, dstArgs = null;
            PropertyBasedEffect effect = null;
            PropertyBasedEffectConfigToken effectParams = null;
            Rectangle bounds = new Rectangle(0, 0, bmpEffectDrawing.Width, bmpEffectDrawing.Height);
            Random rng = new Random();

            //Applies an effect to the bitmap.
            switch (((Tuple<string, CmbxEffectOptions>)cmbxEffectType.SelectedItem).Item2)
            {
                case CmbxEffectOptions.BlackAndWhite:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new HueAndSaturationAdjustment();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Saturation, 0);
                    break;
                case CmbxEffectOptions.BrightnessContrast:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new BrightnessAndContrastAdjustment();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(BrightnessAndContrastAdjustment.PropertyNames.Brightness, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(BrightnessAndContrastAdjustment.PropertyNames.Contrast, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.HueSaturation:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new HueAndSaturationAdjustment();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Hue, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Saturation, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Lightness, sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.InvertColors:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new InvertColorsEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    break;
                case CmbxEffectOptions.Posterize:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new PosterizeAdjustment();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(PosterizeAdjustment.PropertyNames.RedLevels, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(PosterizeAdjustment.PropertyNames.GreenLevels, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(PosterizeAdjustment.PropertyNames.BlueLevels, sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.Sepia:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new SepiaEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    break;
                case CmbxEffectOptions.FlipHorizontal:
                    Utils.CopyBitmapPure(bmpCurrentDrawing, bmpEffectDrawing);
                    bmpEffectDrawing.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
                case CmbxEffectOptions.FlipVertical:
                    Utils.CopyBitmapPure(bmpCurrentDrawing, bmpEffectDrawing);
                    bmpEffectDrawing.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    break;
                case CmbxEffectOptions.InkSketch:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new InkSketchEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(InkSketchEffect.PropertyNames.InkOutline, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(InkSketchEffect.PropertyNames.Coloring, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.OilPainting:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new OilPaintingEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(OilPaintingEffect.PropertyNames.BrushSize, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(OilPaintingEffect.PropertyNames.Coarseness, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.PencilSketch:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new PencilSketchEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(PencilSketchEffect.PropertyNames.PencilTipSize, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(PencilSketchEffect.PropertyNames.ColorRange, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.Fragment:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new FragmentEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(FragmentEffect.PropertyNames.Fragments, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(FragmentEffect.PropertyNames.Distance, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(FragmentEffect.PropertyNames.Rotation, (double)sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.Blur:
                    //Performs the built-in Gaussian blur.
                    if (sliderEffectProperty1.Value <= 8)
                    {
                        //Creates the scaffolding for the effect.
                        srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                        dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                        effect = new GaussianBlurEffect();
                        effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                        effectParams.SetPropertyValue(GaussianBlurEffect.PropertyNames.Radius, sliderEffectProperty1.Value);
                    }

                    //Performs the fast Gaussian blur.
                    else
                    {
                        bmpEffectDrawing = FastBlur.FastGaussianBlur(bmpCurrentDrawing, sliderEffectProperty1.Value);
                    }
                    break;
                case CmbxEffectOptions.MotionBlur:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new MotionBlurEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());

                    effectParams.SetPropertyValue(MotionBlurEffect.PropertyNames.Angle, (double)sliderEffectProperty1.Value);
                    if (sliderEffectProperty2.Value == 0)
                    {
                        effectParams.SetPropertyValue(MotionBlurEffect.PropertyNames.Centered, true);
                        effectParams.SetPropertyValue(MotionBlurEffect.PropertyNames.Distance, 1);
                    }
                    else
                    {
                        effectParams.SetPropertyValue(MotionBlurEffect.PropertyNames.Centered, false);
                        effectParams.SetPropertyValue(MotionBlurEffect.PropertyNames.Distance, sliderEffectProperty2.Value);
                    }
                    break;
                case CmbxEffectOptions.SurfaceBlur:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new SurfaceBlurEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(SurfaceBlurEffect.PropertyName.Radius, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(SurfaceBlurEffect.PropertyName.Threshold, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.Unfocus:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new UnfocusEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(UnfocusEffect.PropertyNames.Radius, sliderEffectProperty1.Value);
                    break;
                case CmbxEffectOptions.ZoomBlur:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new ZoomBlurEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(ZoomBlurEffect.PropertyNames.Amount, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(ZoomBlurEffect.PropertyNames.Offset, new Pair<double, double>(0, 0));
                    break;
                case CmbxEffectOptions.Bulge:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new BulgeEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(BulgeEffect.PropertyNames.Amount, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(BulgeEffect.PropertyNames.Offset, new Pair<double, double>(0, 0));
                    break;
                case CmbxEffectOptions.Crystalize:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new CrystalizeEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(CrystalizeEffect.PropertyNames.Size, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(CrystalizeEffect.PropertyNames.Quality, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(CrystalizeEffect.PropertyNames.Seed, rng.NextDouble());
                    break;
                case CmbxEffectOptions.Dents:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new DentsEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(DentsEffect.PropertyNames.Scale, (double)sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(DentsEffect.PropertyNames.Refraction, (double)sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(DentsEffect.PropertyNames.Roughness, (double)sliderEffectProperty3.Value);
                    effectParams.SetPropertyValue(DentsEffect.PropertyNames.Tension, (double)sliderEffectProperty4.Value);
                    effectParams.SetPropertyValue(DentsEffect.PropertyNames.Quality, 2);
                    effectParams.SetPropertyValue(DentsEffect.PropertyNames.Seed, rng.NextDouble());
                    break;
                case CmbxEffectOptions.FrostedGlass:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new FrostedGlassEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());

                    //Minimum scattering radius must be <= maximum scattering radius.
                    if (sliderEffectProperty2.Value > sliderEffectProperty1.Value)
                    {
                        sliderEffectProperty2.Value = sliderEffectProperty1.Value;
                    }

                    effectParams.SetPropertyValue(FrostedGlassEffect.PropertyNames.MaxScatterRadius, (double)sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(FrostedGlassEffect.PropertyNames.MinScatterRadius, (double)sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(FrostedGlassEffect.PropertyNames.NumSamples, sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.Pixelate:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new PixelateEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(PixelateEffect.PropertyNames.CellSize, sliderEffectProperty1.Value);
                    break;
                case CmbxEffectOptions.TileReflection:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new TileEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(TileEffect.PropertyNames.Rotation, (double)sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(TileEffect.PropertyNames.SquareSize, (double)sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(TileEffect.PropertyNames.Curvature, (double)sliderEffectProperty3.Value);
                    effectParams.SetPropertyValue(TileEffect.PropertyNames.Quality, sliderEffectProperty4.Value);
                    break;
                case CmbxEffectOptions.Twist:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new TwistEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(TwistEffect.PropertyNames.Amount, (double)sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(TwistEffect.PropertyNames.Size, sliderEffectProperty2.Value / 100d);
                    effectParams.SetPropertyValue(TwistEffect.PropertyNames.Quality, sliderEffectProperty3.Value);
                    effectParams.SetPropertyValue(TwistEffect.PropertyNames.Offset, new Pair<double, double>(0, 0));
                    break;
                case CmbxEffectOptions.AddNoise:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new AddNoiseEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(AddNoiseEffect.PropertyNames.Intensity, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(AddNoiseEffect.PropertyNames.Saturation, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(AddNoiseEffect.PropertyNames.Coverage, (double)sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.Median:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new MedianEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(MedianEffect.PropertyNames.Radius, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(MedianEffect.PropertyNames.Percentile, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.ReduceNoise:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new ReduceNoiseEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(ReduceNoiseEffect.PropertyNames.Radius, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(ReduceNoiseEffect.PropertyNames.Strength, sliderEffectProperty2.Value / 100d);
                    break;
                case CmbxEffectOptions.Glow:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new GlowEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(GlowEffect.PropertyNames.Radius, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(GlowEffect.PropertyNames.Brightness, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(GlowEffect.PropertyNames.Contrast, sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.Sharpen:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new SharpenEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(SharpenEffect.PropertyNames.Amount, sliderEffectProperty1.Value);
                    break;
                case CmbxEffectOptions.SoftenPortrait:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new SoftenPortraitEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(SoftenPortraitEffect.PropertyNames.Softness, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(SoftenPortraitEffect.PropertyNames.Lighting, sliderEffectProperty2.Value);
                    effectParams.SetPropertyValue(SoftenPortraitEffect.PropertyNames.Warmth, sliderEffectProperty3.Value);
                    break;
                case CmbxEffectOptions.Vignette:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new VignetteEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(VignetteEffect.PropertyNames.Radius, sliderEffectProperty1.Value / 100d);
                    effectParams.SetPropertyValue(VignetteEffect.PropertyNames.Amount, sliderEffectProperty2.Value / 100d);
                    effectParams.SetPropertyValue(VignetteEffect.PropertyNames.Offset, new Pair<double, double>(0, 0));
                    break;
                case CmbxEffectOptions.Clouds:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new CloudsEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(CloudsEffect.PropertyNames.Scale, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(CloudsEffect.PropertyNames.Power, sliderEffectProperty2.Value / 100d);
                    effectParams.SetPropertyValue(CloudsEffect.PropertyNames.BlendMode, LayerBlendMode.Overlay);
                    effectParams.SetPropertyValue(CloudsEffect.PropertyNames.Seed, rng.NextDouble());
                    break;
                case CmbxEffectOptions.EdgeDetect:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new EdgeDetectEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(EdgeDetectEffect.PropertyNames.Angle, (double)sliderEffectProperty1.Value);
                    break;
                case CmbxEffectOptions.Emboss:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new EmbossEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(EmbossEffect.PropertyNames.Angle, (double)sliderEffectProperty1.Value);
                    break;
                case CmbxEffectOptions.Outline:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new OutlineEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(OutlineEffect.PropertyNames.Thickness, sliderEffectProperty1.Value);
                    effectParams.SetPropertyValue(OutlineEffect.PropertyNames.Intensity, sliderEffectProperty2.Value);
                    break;
                case CmbxEffectOptions.Relief:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new ReliefEffect();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(ReliefEffect.PropertyNames.Angle, (double)sliderEffectProperty1.Value);
                    break;
                case CmbxEffectOptions.DodgeBurn:
                    //Creates the scaffolding for the effect.
                    srcArgs = new RenderArgs(Surface.CopyFromBitmap(bmpCurrentDrawing));
                    dstArgs = new RenderArgs(Surface.CopyFromBitmap(bmpEffectDrawing));
                    effect = new HueAndSaturationAdjustment();
                    effectParams = new PropertyBasedEffectConfigToken(effect.CreatePropertyCollection());
                    effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Hue, 0);

                    if (sliderEffectProperty1.Value < 0)
                    {
                        effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Saturation, 100 - sliderEffectProperty1.Value);
                        effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Lightness, -sliderEffectProperty1.Value / 2);
                    }
                    else if (sliderEffectProperty1.Value > 0)
                    {
                        effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Saturation, 100);
                        effectParams.SetPropertyValue(HueAndSaturationAdjustment.PropertyNames.Lightness, -sliderEffectProperty1.Value);
                    }
                    else
                    {
                        effect = null;
                        Utils.CopyBitmapPure(bmpCurrentDrawing, bmpEffectDrawing);
                    }
                    break;
                case CmbxEffectOptions.RgbTint:

                    //Locks bits.
                    BitmapData srcData = bmpCurrentDrawing.LockBits(
                        new Rectangle(0, 0,
                            bmpCurrentDrawing.Width,
                            bmpCurrentDrawing.Height),
                        ImageLockMode.ReadOnly,
                        bmpCurrentDrawing.PixelFormat);

                    BitmapData destData = bmpEffectDrawing.LockBits(
                        new Rectangle(0, 0,
                            bmpEffectDrawing.Width,
                            bmpEffectDrawing.Height),
                        ImageLockMode.WriteOnly,
                        bmpEffectDrawing.PixelFormat);

                    //Copies each pixel.
                    byte* srcRow = (byte*)srcData.Scan0;
                    byte* dstRow = (byte*)destData.Scan0;

                    int srcImgHeight = bmpCurrentDrawing.Height;
                    int srcImgWidth = bmpCurrentDrawing.Width;

                    //Copies the channels +/- some amount.
                    int slider1Val = sliderEffectProperty1.Value;
                    int slider2Val = sliderEffectProperty2.Value;
                    int slider3Val = sliderEffectProperty3.Value;
                    Parallel.For(0, srcImgHeight, (y) =>
                    {
                        for (int x = 0; x < srcImgWidth; x++)
                        {
                            int ptr = y * srcData.Stride + x * 4;

                            dstRow[ptr] = (byte)Utils.Clamp(srcRow[ptr] + slider3Val, 0, 255);
                            dstRow[ptr + 1] = (byte)Utils.Clamp(srcRow[ptr + 1] + slider2Val, 0, 255);
                            dstRow[ptr + 2] = (byte)Utils.Clamp(srcRow[ptr + 2] + slider1Val, 0, 255);
                            dstRow[ptr + 3] = srcRow[ptr + 3];
                        }
                    });

                    bmpCurrentDrawing.UnlockBits(srcData);
                    bmpEffectDrawing.UnlockBits(destData);
                    break;
            }

            //Copies the rendering over the filtered drawing.
            if (effect != null)
            {
                effect.SetRenderInfo(effectParams, dstArgs, srcArgs);

                //Renders in segments for images of 129 x 129 or greater.
                if (bounds.Width > 128 && bounds.Height > 128)
                {
                    Parallel.For(0, 1 + bounds.Width / 64, (row) =>
                    {
                        int x = row * 64;
                        for (int y = 0; y < bounds.Height; y += 64)
                        {
                            //Only adds rectangles with valid width and height.
                            if (bounds.Width - x > 0 &&
                                bounds.Height - y > 0)
                            {
                                var rect = new Rectangle(x, y,
                                    Utils.Clamp(64, 0, bounds.Width - x),
                                    Utils.Clamp(64, 0, bounds.Height - y));

                                effect.Render(new Rectangle[] { rect }, 0, 1);
                            }
                        }
                    });
                }
                else
                {
                    effect.Render(new Rectangle[] { bounds }, 0, 1);
                }

                bmpEffectDrawing = new Bitmap(dstArgs.Bitmap);
            }
            
            ApplyFilterAlpha();
        }

        /// <summary>
        /// Makes the filter effect invisible or semi-opaque for previews.
        /// </summary>
        private unsafe void ApplyFilterAlpha()
        {
            //Doesn't compute the effect if it hasn't loaded yet.
            if (bmpEffectAlpha == null)
            {
                return;
            }

            //Copies the affected bitmap's alpha, then sets it to 0 so it can
            //be "uncovered" by the user's brush strokes.
            BitmapData bmpData = bmpEffectDrawing.LockBits(
                new Rectangle(0, 0, bmpEffectDrawing.Width,
                    bmpEffectDrawing.Height),
                ImageLockMode.ReadWrite,
                bmpEffectDrawing.PixelFormat);

            //Copies alpha from each pixel.
            byte* pixRow = (byte*)bmpData.Scan0;

            int bitmapHeight = bmpEffectDrawing.Height;
            int bitmapWidth = bmpEffectDrawing.Width;
            Parallel.For(0, bitmapHeight, (y) =>
            {
                for (int x = 0; x < bitmapWidth; x++)
                {
                    int ptr = y * bmpData.Stride + x * 4;
                    bmpEffectAlpha[x, y] = pixRow[ptr + 3];
                    if (doPreview)
                    {
                        pixRow[ptr + 3] = 192;
                    }
                    else
                    {
                        pixRow[ptr + 3] = 0;
                    }
                }
            });

            bmpEffectDrawing.UnlockBits(bmpData);

            //Updates the displayed filter.
            displayCanvas.Refresh();
        }

        /// <summary>
        /// Applies the brush to the drawing region at the given location
        /// with the given radius. The brush is assumed square.
        /// </summary>
        /// <param name="loc">The location to apply the brush.</param>
        /// <param name="radius">The size to draw the brush at.</param>
        private void ApplyBrush(Point loc, int radius)
        {
            //Stores the differences in mouse coordinates for some settings.
            int deltaX;
            int deltaY;

            //Ensures the mouse is far enough away if min drawing dist != 0.
            if (sliderMinDrawDistance.Value != 0 &&
                mouseLocBrush.HasValue)
            {
                deltaX = mouseLocBrush.Value.X - mouseLoc.X;
                deltaY = mouseLocBrush.Value.Y - mouseLoc.Y;

                //Aborts if the minimum drawing distance isn't met.
                if (Math.Sqrt(deltaX * deltaX + deltaY * deltaY) <
                    sliderMinDrawDistance.Value * displayCanvasZoom)
                {
                    return;
                }
            }

            //Sets the new brush location because the brush stroke succeeded.
            mouseLocBrush = mouseLoc;

            //Shifts the size.
            if (sliderShiftSize.Value != 0)
            {
                int tempSize = sliderBrushSize.Value;
                if (isGrowingSize)
                {
                    tempSize += sliderShiftSize.Value;
                }
                else
                {
                    tempSize -= sliderShiftSize.Value;
                }
                if (tempSize > sliderBrushSize.Maximum)
                {
                    tempSize = sliderBrushSize.Maximum;
                    isGrowingSize = !isGrowingSize; //handles values < 0.
                }
                else if (tempSize < sliderBrushSize.Minimum)
                {
                    tempSize = sliderBrushSize.Minimum;
                    isGrowingSize = !isGrowingSize;
                }

                sliderBrushSize.Value = Utils.Clamp(tempSize,
                    sliderBrushSize.Minimum, sliderBrushSize.Maximum);
            }

            //Shifts the intensity.
            if (sliderShiftIntensity.Value != 0)
            {
                int tempIntensity = sliderBrushIntensity.Value;
                if (isGrowingIntensity)
                {
                    tempIntensity += sliderShiftIntensity.Value;
                }
                else
                {
                    tempIntensity -= sliderShiftIntensity.Value;
                }
                if (tempIntensity > sliderBrushIntensity.Maximum)
                {
                    tempIntensity = sliderBrushIntensity.Maximum;
                    isGrowingIntensity = !isGrowingIntensity; //handles values < 0.
                }
                else if (tempIntensity < sliderBrushIntensity.Minimum)
                {
                    tempIntensity = sliderBrushIntensity.Minimum;
                    isGrowingIntensity = !isGrowingIntensity;
                }

                sliderBrushIntensity.Value = Utils.Clamp(tempIntensity,
                    sliderBrushIntensity.Minimum, sliderBrushIntensity.Maximum);
            }

            //Shifts the rotation.
            if (sliderShiftRotation.Value != 0)
            {
                int tempRot = sliderBrushRotation.Value + sliderShiftRotation.Value;
                if (tempRot > sliderBrushRotation.Maximum)
                {
                    //The range goes negative, and is a total of 2 * max.
                    tempRot -= (2 * sliderBrushRotation.Maximum);
                }
                else if (tempRot < sliderBrushRotation.Minimum)
                {
                    tempRot += (2 * sliderBrushRotation.Maximum) - Math.Abs(tempRot);
                }

                sliderBrushRotation.Value = Utils.Clamp(tempRot,
                    sliderBrushRotation.Minimum, sliderBrushRotation.Maximum);
            }

            //Randomly shifts the image by some percent of the canvas size,
            //horizontally and/or vertically.
            if (sliderRandHorzShift.Value != 0 ||
                sliderRandVertShift.Value != 0)
            {
                loc.X = (int)(loc.X
                    - bmpCurrentDrawing.Width * (sliderRandHorzShift.Value / 200f)
                    + bmpCurrentDrawing.Width * (random.Next(sliderRandHorzShift.Value) / 100f));

                loc.Y = (int)(loc.Y
                    - bmpCurrentDrawing.Height * (sliderRandVertShift.Value / 200f)
                    + bmpCurrentDrawing.Height * (random.Next(sliderRandVertShift.Value) / 100f));
            }

            //This is used to randomly rotate the image by some amount.
            int rotation = sliderBrushRotation.Value
                - random.Next(sliderRandRotLeft.Value)
                + random.Next(sliderRandRotRight.Value);

            if (chkbxOrientToMouse.Checked)
            {
                //Adds to the rotation according to mouse direction. Uses the
                //original rotation as an offset.
                deltaX = mouseLoc.X - mouseLocPrev.X;
                deltaY = mouseLoc.Y - mouseLocPrev.Y;
                rotation += (int)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);
            }

            //Creates a brush from a rotation of the current brush.
            Bitmap bmpBrushRot = Utils.RotateImage(bmpBrush, rotation);

            //Rotating the brush increases image bounds, so brush space
            //must increase to avoid making it visually shrink.
            double radAngle = (Math.Abs(rotation) % 90) * Math.PI / 180;
            float rotScaleFactor = (float)(Math.Cos(radAngle) + Math.Sin(radAngle));
            int scaleFactor = (int)(radius * rotScaleFactor);

            //If new image size is <= 0 due to random size changes, don't render.
            if (scaleFactor <= 0)
            {
                return;
            }

            //Draws the scaled version of the image.
            using (Bitmap bmpSized = new Bitmap(scaleFactor, scaleFactor))
            {
                using (Graphics gScaled = Graphics.FromImage(bmpSized))
                {
                    gScaled.DrawRectangle(Pens.White,
                        new Rectangle(0, 0, bmpSized.Width, bmpSized.Height));

                    gScaled.DrawImage(bmpBrushRot, 0, 0, bmpSized.Width, bmpSized.Height);
                }

                float intensity = sliderBrushIntensity.Value / 100f;

                //Applies the brush.
                UncoverBitmap(bmpEffectDrawing, bmpSized, new Point(
                    loc.X - (scaleFactor / 2),
                    loc.Y - (scaleFactor / 2)));

                //Draws the brush horizontally reflected.
                if (cmbxSymmetry.SelectedIndex == 1)
                {
                    bmpSized.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    //Applies the brush.
                    UncoverBitmap(bmpEffectDrawing, bmpSized, new Point(
                        bmpEffectDrawing.Width - (loc.X - scaleFactor / 2),
                        loc.Y - (scaleFactor / 2)));
                }

                //Draws the brush vertically reflected.
                else if (cmbxSymmetry.SelectedIndex == 2)
                {
                    bmpSized.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    //Applies the brush.
                    UncoverBitmap(bmpEffectDrawing, bmpSized, new Point(
                        loc.X - (scaleFactor / 2),
                        bmpEffectDrawing.Height - (loc.Y - scaleFactor / 2)));
                }

                //Draws the brush horizontally and vertically reflected.
                else if (cmbxSymmetry.SelectedIndex == 3)
                {
                    bmpSized.RotateFlip(RotateFlipType.RotateNoneFlipXY);

                    //Applies the brush.
                    UncoverBitmap(bmpEffectDrawing, bmpSized, new Point(
                        bmpEffectDrawing.Width - (loc.X - (scaleFactor / 2)),
                        bmpEffectDrawing.Height - (loc.Y - (scaleFactor / 2))));
                }
            }

            //TODO: Dispose bmpBrushRot and find out why it won't let you.
        }

        /// <summary>
        /// Presents an open file dialog to the user, allowing them to select
        /// any number of brush files to load and add as the custom brushes.
        /// Returns false if the user cancels or an error occurred.
        /// </summary>
        /// <param name="doAddToSettings">
        /// If true, the brush will be added to the settings.
        /// </param>
        private bool ImportBrushes(bool doAddToSettings)
        {
            //Configures a dialog to get the brush(es) path(s).
            OpenFileDialog openFileDialog = new OpenFileDialog();

            string defPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog.InitialDirectory = defPath;
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Load custom brushes";
            openFileDialog.Filter = "Supported images|" +
                "*.png;*.bmp;*.jpg;*.gif;*.tif;*.exif*.jpeg;*.tiff;";

            //Displays the dialog. Loads the files if it worked.
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return ImportBrushes(openFileDialog.FileNames, doAddToSettings, true);
            }

            return false;
        }

        /// <summary>
        /// Attempts to load any number of brush files and add them as custom
        /// brushes. This does not interact with the user.
        /// </summary>
        /// <param name="fileAndPath">
        /// If empty, the user will be presented with a dialog to select
        /// files.
        /// </param>
        /// <param name="doAddToSettings">
        /// If true, the brush will be added to the settings.
        /// </param>
        /// <param name="displayError">
        /// Errors should only be displayed if it's a user-initiated action.
        /// </param>
        private bool ImportBrushes(
            string[] filePaths,
            bool doAddToSettings,
            bool doDisplayErrors)
        {
            //Attempts to load a bitmap from a file to use as a brush.
            foreach (string file in filePaths)
            {
                try
                {
                    using (Bitmap bmp = (Bitmap)Image.FromFile(file))
                    {
                        //Creates the brush space.
                        int size = Math.Max(bmp.Width, bmp.Height);
                        bmpBrush = new Bitmap(size, size);

                        //Pads the image to be square if needed and draws the
                        //altered loaded bitmap to the brush.
                        Utils.CopyBitmapPure(Utils.MakeBitmapSquare(
                            Utils.FormatImage(bmp, PixelFormat.Canonical)),
                            bmpBrush);
                    }

                    //Gets the last word in the filename without the path.
                    Regex getOnlyFilename = new Regex(@"[\w-]+\.");
                    string filename = getOnlyFilename.Match(file).Value;

                    //Removes the file extension dot.
                    if (filename.EndsWith("."))
                    {
                        filename = filename.Remove(filename.Length - 1);
                    }

                    //Appends invisible spaces to files with the same name
                    //until they're unique.
                    while (loadedBrushes.Any(a =>
                    { return (a.Name.Equals(filename)); }))
                    {
                        filename += " ";
                    }

                    //Adds the brush without the period at the end.
                    loadedBrushes.Add(
                        new BrushSelectorItem(filename, bmpBrush));

                    if (doAddToSettings)
                    {
                        //Adds the brush location into settings.
                        loadedBrushPaths.Add(file);
                    }

                    //Removes the custom brush so it can be appended on the end.
                    loadedBrushes.Remove(BrushSelectorItem.CustomBrush);
                    loadedBrushes.Add(BrushSelectorItem.CustomBrush);

                    //Makes the newest brush active (and not the custom brush).
                    bttnBrushSelector.SelectedIndex =
                        bttnBrushSelector.Items.Count - 2;
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                catch (OutOfMemoryException)
                {
                    if (doDisplayErrors)
                    {
                        MessageBox.Show("Cannot load brush: out of memory.");
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a list of files in the given directories. Any invalid
        /// or non-directory path is included directly in the output.
        /// </summary>
        private string[] FilesInDirectory(string[] dirs)
        {
            List<string> paths = new List<string>();

            foreach (string directory in dirs)
            {
                try
                {
                    //The path must exist and be a directory.
                    if (!File.Exists(directory) ||
                        !File.GetAttributes(directory)
                        .HasFlag(FileAttributes.Directory))
                    {
                        paths.Add(directory);
                    }

                    paths.AddRange(Directory.GetFiles(directory));
                }
                catch
                {
                    paths.Add(directory);
                }
            }

            //Excludes all non-image files.
            List<string> pathsToReturn = new List<string>();
            foreach (string str in paths)
            {
                if (str.EndsWith("png") || str.EndsWith("bmp") ||
                    str.EndsWith("jpg") || str.EndsWith("gif") ||
                    str.EndsWith("tif") || str.EndsWith("exif") ||
                    str.EndsWith("jpeg") || str.EndsWith("tiff"))
                {
                    pathsToReturn.Add(str);
                }
            }

            return pathsToReturn.ToArray();
        }

        /// <summary>
        /// Returns the amount of space between the display canvas and
        /// the display canvas background.
        /// </summary>
        private Rectangle GetRange()
        {
            //Gets the full region.
            Rectangle range = displayCanvas.ClientRectangle;

            //Calculates width.
            if (displayCanvas.ClientRectangle.Width >= displayCanvasBG.ClientRectangle.Width)
            {
                range.X = displayCanvasBG.ClientRectangle.Width - displayCanvas.ClientRectangle.Width;
                range.Width = displayCanvas.ClientRectangle.Width - displayCanvasBG.ClientRectangle.Width;
            }
            else
            {
                range.X = (displayCanvasBG.ClientRectangle.Width - displayCanvas.ClientRectangle.Width) / 2;
                range.Width = 0;
            }

            //Calculates height.
            if (displayCanvas.ClientRectangle.Height >= displayCanvasBG.ClientRectangle.Height)
            {
                range.Y = displayCanvasBG.ClientRectangle.Height - displayCanvas.ClientRectangle.Height;
                range.Height = displayCanvas.ClientRectangle.Height - displayCanvasBG.ClientRectangle.Height;
            }
            else
            {
                range.Y = (displayCanvasBG.ClientRectangle.Height - displayCanvas.ClientRectangle.Height) / 2;
                range.Height = 0;
            }

            return range;
        }

        /// <summary>
        /// Initializes all components. Auto-generated.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(winBrushFilter));
            this.timerRepositionUpdate = new System.Windows.Forms.Timer(this.components);
            this.txtTooltip = new System.Windows.Forms.Label();
            this.displayCanvasBG = new System.Windows.Forms.Panel();
            this.displayCanvas = new System.Windows.Forms.PictureBox();
            this.tabOther = new System.Windows.Forms.TabPage();
            this.bttnCustomBrushLocations = new System.Windows.Forms.Button();
            this.bttnClearSettings = new System.Windows.Forms.Button();
            this.bttnClearBrushes = new System.Windows.Forms.Button();
            this.sliderShiftIntensity = new System.Windows.Forms.TrackBar();
            this.txtShiftIntensity = new System.Windows.Forms.Label();
            this.sliderShiftRotation = new System.Windows.Forms.TrackBar();
            this.txtShiftRotation = new System.Windows.Forms.Label();
            this.sliderShiftSize = new System.Windows.Forms.TrackBar();
            this.txtShiftSize = new System.Windows.Forms.Label();
            this.txtMinDrawDistance = new System.Windows.Forms.Label();
            this.sliderMinDrawDistance = new System.Windows.Forms.TrackBar();
            this.grpbxBrushOptions = new System.Windows.Forms.GroupBox();
            this.cmbxSymmetry = new System.Windows.Forms.ComboBox();
            this.chkbxOrientToMouse = new System.Windows.Forms.CheckBox();
            this.tabJitter = new System.Windows.Forms.TabPage();
            this.sliderRandVertShift = new System.Windows.Forms.TrackBar();
            this.txtRandVertShift = new System.Windows.Forms.Label();
            this.sliderRandHorzShift = new System.Windows.Forms.TrackBar();
            this.txtRandHorzShift = new System.Windows.Forms.Label();
            this.sliderRandMaxIntensity = new System.Windows.Forms.TrackBar();
            this.txtRandMaxIntensity = new System.Windows.Forms.Label();
            this.sliderRandMinIntensity = new System.Windows.Forms.TrackBar();
            this.txtRandMinIntensity = new System.Windows.Forms.Label();
            this.sliderRandMaxSize = new System.Windows.Forms.TrackBar();
            this.txtRandMaxSize = new System.Windows.Forms.Label();
            this.sliderRandMinSize = new System.Windows.Forms.TrackBar();
            this.txtRandMinSize = new System.Windows.Forms.Label();
            this.sliderRandRotRight = new System.Windows.Forms.TrackBar();
            this.txtRandRotRight = new System.Windows.Forms.Label();
            this.sliderRandRotLeft = new System.Windows.Forms.TrackBar();
            this.txtRandRotLeft = new System.Windows.Forms.Label();
            this.tabControls = new System.Windows.Forms.TabPage();
            this.bttnRedo = new System.Windows.Forms.Button();
            this.sliderBrushIntensity = new System.Windows.Forms.TrackBar();
            this.txtBrushIntensity = new System.Windows.Forms.Label();
            this.txtBrushSize = new System.Windows.Forms.Label();
            this.sliderBrushSize = new System.Windows.Forms.TrackBar();
            this.sliderBrushRotation = new System.Windows.Forms.TrackBar();
            this.txtBrushRotation = new System.Windows.Forms.Label();
            this.bttnOk = new System.Windows.Forms.Button();
            this.bttnUndo = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.sliderCanvasZoom = new System.Windows.Forms.TrackBar();
            this.txtCanvasZoom = new System.Windows.Forms.Label();
            this.bttnBrushSelector = new System.Windows.Forms.ComboBox();
            this.tabBar = new System.Windows.Forms.TabControl();
            this.tabEffect = new System.Windows.Forms.TabPage();
            this.sliderEffectProperty2 = new System.Windows.Forms.TrackBar();
            this.txtEffectProperty2 = new System.Windows.Forms.Label();
            this.sliderEffectProperty1 = new System.Windows.Forms.TrackBar();
            this.txtEffectProperty1 = new System.Windows.Forms.Label();
            this.sliderEffectProperty4 = new System.Windows.Forms.TrackBar();
            this.txtEffectProperty4 = new System.Windows.Forms.Label();
            this.sliderEffectProperty3 = new System.Windows.Forms.TrackBar();
            this.txtEffectProperty3 = new System.Windows.Forms.Label();
            this.txtEffectType = new System.Windows.Forms.Label();
            this.cmbxEffectType = new System.Windows.Forms.ComboBox();
            this.displayCanvasBG.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayCanvas)).BeginInit();
            this.tabOther.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderShiftIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderShiftRotation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderShiftSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderMinDrawDistance)).BeginInit();
            this.grpbxBrushOptions.SuspendLayout();
            this.tabJitter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandVertShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandHorzShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMaxIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMinIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMaxSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMinSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandRotRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandRotLeft)).BeginInit();
            this.tabControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderBrushIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderBrushSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderBrushRotation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderCanvasZoom)).BeginInit();
            this.tabBar.SuspendLayout();
            this.tabEffect.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty3)).BeginInit();
            this.SuspendLayout();
            // 
            // timerRepositionUpdate
            // 
            this.timerRepositionUpdate.Interval = 5;
            this.timerRepositionUpdate.Tick += new System.EventHandler(this.RepositionUpdate_Tick);
            // 
            // txtTooltip
            // 
            resources.ApplyResources(this.txtTooltip, "txtTooltip");
            this.txtTooltip.BackColor = System.Drawing.SystemColors.Control;
            this.txtTooltip.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.txtTooltip.Name = "txtTooltip";
            // 
            // displayCanvasBG
            // 
            resources.ApplyResources(this.displayCanvasBG, "displayCanvasBG");
            this.displayCanvasBG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(207)))), ((int)(((byte)(207)))));
            this.displayCanvasBG.Controls.Add(this.displayCanvas);
            this.displayCanvasBG.Name = "displayCanvasBG";
            this.displayCanvasBG.MouseEnter += new System.EventHandler(this.displayCanvasBG_MouseEnter);
            this.displayCanvasBG.MouseLeave += new System.EventHandler(this.EnablePreview);
            // 
            // displayCanvas
            // 
            resources.ApplyResources(this.displayCanvas, "displayCanvas");
            this.displayCanvas.Name = "displayCanvas";
            this.displayCanvas.TabStop = false;
            this.displayCanvas.Paint += new System.Windows.Forms.PaintEventHandler(this.displayCanvas_Paint);
            this.displayCanvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.displayCanvas_MouseDown);
            this.displayCanvas.MouseEnter += new System.EventHandler(this.displayCanvas_MouseEnter);
            this.displayCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.displayCanvas_MouseMove);
            this.displayCanvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.displayCanvas_MouseUp);
            // 
            // tabOther
            // 
            this.tabOther.BackColor = System.Drawing.Color.Transparent;
            this.tabOther.Controls.Add(this.bttnCustomBrushLocations);
            this.tabOther.Controls.Add(this.bttnClearSettings);
            this.tabOther.Controls.Add(this.bttnClearBrushes);
            this.tabOther.Controls.Add(this.sliderShiftIntensity);
            this.tabOther.Controls.Add(this.txtShiftIntensity);
            this.tabOther.Controls.Add(this.sliderShiftRotation);
            this.tabOther.Controls.Add(this.txtShiftRotation);
            this.tabOther.Controls.Add(this.sliderShiftSize);
            this.tabOther.Controls.Add(this.txtShiftSize);
            this.tabOther.Controls.Add(this.txtMinDrawDistance);
            this.tabOther.Controls.Add(this.sliderMinDrawDistance);
            this.tabOther.Controls.Add(this.grpbxBrushOptions);
            resources.ApplyResources(this.tabOther, "tabOther");
            this.tabOther.Name = "tabOther";
            // 
            // bttnCustomBrushLocations
            // 
            resources.ApplyResources(this.bttnCustomBrushLocations, "bttnCustomBrushLocations");
            this.bttnCustomBrushLocations.Name = "bttnCustomBrushLocations";
            this.bttnCustomBrushLocations.UseVisualStyleBackColor = true;
            this.bttnCustomBrushLocations.Click += new System.EventHandler(this.bttnPreferences_Click);
            this.bttnCustomBrushLocations.MouseEnter += new System.EventHandler(this.bttnPreferences_MouseEnter);
            // 
            // bttnClearSettings
            // 
            resources.ApplyResources(this.bttnClearSettings, "bttnClearSettings");
            this.bttnClearSettings.Name = "bttnClearSettings";
            this.bttnClearSettings.UseVisualStyleBackColor = true;
            this.bttnClearSettings.Click += new System.EventHandler(this.bttnClearSettings_Click);
            this.bttnClearSettings.MouseEnter += new System.EventHandler(this.bttnClearSettings_MouseEnter);
            // 
            // bttnClearBrushes
            // 
            resources.ApplyResources(this.bttnClearBrushes, "bttnClearBrushes");
            this.bttnClearBrushes.Name = "bttnClearBrushes";
            this.bttnClearBrushes.UseVisualStyleBackColor = true;
            this.bttnClearBrushes.Click += new System.EventHandler(this.bttnClearBrushes_Click);
            this.bttnClearBrushes.MouseEnter += new System.EventHandler(this.bttnClearBrushes_MouseEnter);
            // 
            // sliderShiftIntensity
            // 
            resources.ApplyResources(this.sliderShiftIntensity, "sliderShiftIntensity");
            this.sliderShiftIntensity.LargeChange = 1;
            this.sliderShiftIntensity.Maximum = 100;
            this.sliderShiftIntensity.Minimum = -100;
            this.sliderShiftIntensity.Name = "sliderShiftIntensity";
            this.sliderShiftIntensity.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderShiftIntensity.ValueChanged += new System.EventHandler(this.sliderShiftIntensity_ValueChanged);
            this.sliderShiftIntensity.MouseEnter += new System.EventHandler(this.sliderShiftIntensity_MouseEnter);
            // 
            // txtShiftIntensity
            // 
            this.txtShiftIntensity.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtShiftIntensity, "txtShiftIntensity");
            this.txtShiftIntensity.Name = "txtShiftIntensity";
            // 
            // sliderShiftRotation
            // 
            resources.ApplyResources(this.sliderShiftRotation, "sliderShiftRotation");
            this.sliderShiftRotation.LargeChange = 1;
            this.sliderShiftRotation.Maximum = 180;
            this.sliderShiftRotation.Minimum = -180;
            this.sliderShiftRotation.Name = "sliderShiftRotation";
            this.sliderShiftRotation.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderShiftRotation.ValueChanged += new System.EventHandler(this.sliderShiftRotation_ValueChanged);
            this.sliderShiftRotation.MouseEnter += new System.EventHandler(this.sliderShiftRotation_MouseEnter);
            // 
            // txtShiftRotation
            // 
            this.txtShiftRotation.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtShiftRotation, "txtShiftRotation");
            this.txtShiftRotation.Name = "txtShiftRotation";
            // 
            // sliderShiftSize
            // 
            resources.ApplyResources(this.sliderShiftSize, "sliderShiftSize");
            this.sliderShiftSize.LargeChange = 1;
            this.sliderShiftSize.Maximum = 500;
            this.sliderShiftSize.Minimum = -500;
            this.sliderShiftSize.Name = "sliderShiftSize";
            this.sliderShiftSize.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderShiftSize.ValueChanged += new System.EventHandler(this.sliderShiftSize_ValueChanged);
            this.sliderShiftSize.MouseEnter += new System.EventHandler(this.sliderShiftSize_MouseEnter);
            // 
            // txtShiftSize
            // 
            this.txtShiftSize.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtShiftSize, "txtShiftSize");
            this.txtShiftSize.Name = "txtShiftSize";
            // 
            // txtMinDrawDistance
            // 
            resources.ApplyResources(this.txtMinDrawDistance, "txtMinDrawDistance");
            this.txtMinDrawDistance.BackColor = System.Drawing.Color.Transparent;
            this.txtMinDrawDistance.Name = "txtMinDrawDistance";
            // 
            // sliderMinDrawDistance
            // 
            resources.ApplyResources(this.sliderMinDrawDistance, "sliderMinDrawDistance");
            this.sliderMinDrawDistance.LargeChange = 1;
            this.sliderMinDrawDistance.Maximum = 100;
            this.sliderMinDrawDistance.Name = "sliderMinDrawDistance";
            this.sliderMinDrawDistance.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderMinDrawDistance.ValueChanged += new System.EventHandler(this.sliderMinDrawDistance_ValueChanged);
            this.sliderMinDrawDistance.MouseEnter += new System.EventHandler(this.sliderMinDrawDistance_MouseEnter);
            // 
            // grpbxBrushOptions
            // 
            this.grpbxBrushOptions.Controls.Add(this.cmbxSymmetry);
            this.grpbxBrushOptions.Controls.Add(this.chkbxOrientToMouse);
            resources.ApplyResources(this.grpbxBrushOptions, "grpbxBrushOptions");
            this.grpbxBrushOptions.Name = "grpbxBrushOptions";
            this.grpbxBrushOptions.TabStop = false;
            // 
            // cmbxSymmetry
            // 
            resources.ApplyResources(this.cmbxSymmetry, "cmbxSymmetry");
            this.cmbxSymmetry.BackColor = System.Drawing.Color.White;
            this.cmbxSymmetry.DropDownHeight = 140;
            this.cmbxSymmetry.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbxSymmetry.DropDownWidth = 20;
            this.cmbxSymmetry.FormattingEnabled = true;
            this.cmbxSymmetry.Name = "cmbxSymmetry";
            this.cmbxSymmetry.MouseEnter += new System.EventHandler(this.cmbxSymmetry_MouseEnter);
            // 
            // chkbxOrientToMouse
            // 
            resources.ApplyResources(this.chkbxOrientToMouse, "chkbxOrientToMouse");
            this.chkbxOrientToMouse.Name = "chkbxOrientToMouse";
            this.chkbxOrientToMouse.UseVisualStyleBackColor = true;
            this.chkbxOrientToMouse.MouseEnter += new System.EventHandler(this.chkbxOrientToMouse_MouseEnter);
            // 
            // tabJitter
            // 
            this.tabJitter.BackColor = System.Drawing.Color.Transparent;
            this.tabJitter.Controls.Add(this.sliderRandVertShift);
            this.tabJitter.Controls.Add(this.txtRandVertShift);
            this.tabJitter.Controls.Add(this.sliderRandHorzShift);
            this.tabJitter.Controls.Add(this.txtRandHorzShift);
            this.tabJitter.Controls.Add(this.sliderRandMaxIntensity);
            this.tabJitter.Controls.Add(this.txtRandMaxIntensity);
            this.tabJitter.Controls.Add(this.sliderRandMinIntensity);
            this.tabJitter.Controls.Add(this.txtRandMinIntensity);
            this.tabJitter.Controls.Add(this.sliderRandMaxSize);
            this.tabJitter.Controls.Add(this.txtRandMaxSize);
            this.tabJitter.Controls.Add(this.sliderRandMinSize);
            this.tabJitter.Controls.Add(this.txtRandMinSize);
            this.tabJitter.Controls.Add(this.sliderRandRotRight);
            this.tabJitter.Controls.Add(this.txtRandRotRight);
            this.tabJitter.Controls.Add(this.sliderRandRotLeft);
            this.tabJitter.Controls.Add(this.txtRandRotLeft);
            resources.ApplyResources(this.tabJitter, "tabJitter");
            this.tabJitter.Name = "tabJitter";
            // 
            // sliderRandVertShift
            // 
            resources.ApplyResources(this.sliderRandVertShift, "sliderRandVertShift");
            this.sliderRandVertShift.LargeChange = 1;
            this.sliderRandVertShift.Maximum = 100;
            this.sliderRandVertShift.Name = "sliderRandVertShift";
            this.sliderRandVertShift.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandVertShift.ValueChanged += new System.EventHandler(this.sliderRandVertShift_ValueChanged);
            this.sliderRandVertShift.MouseEnter += new System.EventHandler(this.sliderRandVertShift_MouseEnter);
            // 
            // txtRandVertShift
            // 
            this.txtRandVertShift.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtRandVertShift, "txtRandVertShift");
            this.txtRandVertShift.Name = "txtRandVertShift";
            // 
            // sliderRandHorzShift
            // 
            resources.ApplyResources(this.sliderRandHorzShift, "sliderRandHorzShift");
            this.sliderRandHorzShift.LargeChange = 1;
            this.sliderRandHorzShift.Maximum = 100;
            this.sliderRandHorzShift.Name = "sliderRandHorzShift";
            this.sliderRandHorzShift.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandHorzShift.ValueChanged += new System.EventHandler(this.sliderRandHorzShift_ValueChanged);
            this.sliderRandHorzShift.MouseEnter += new System.EventHandler(this.sliderRandHorzShift_MouseEnter);
            // 
            // txtRandHorzShift
            // 
            this.txtRandHorzShift.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtRandHorzShift, "txtRandHorzShift");
            this.txtRandHorzShift.Name = "txtRandHorzShift";
            // 
            // sliderRandMaxIntensity
            // 
            resources.ApplyResources(this.sliderRandMaxIntensity, "sliderRandMaxIntensity");
            this.sliderRandMaxIntensity.LargeChange = 1;
            this.sliderRandMaxIntensity.Maximum = 100;
            this.sliderRandMaxIntensity.Name = "sliderRandMaxIntensity";
            this.sliderRandMaxIntensity.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandMaxIntensity.ValueChanged += new System.EventHandler(this.sliderRandMaxIntensity_ValueChanged);
            this.sliderRandMaxIntensity.MouseEnter += new System.EventHandler(this.sliderRandMaxIntensity_MouseEnter);
            // 
            // txtRandMaxIntensity
            // 
            resources.ApplyResources(this.txtRandMaxIntensity, "txtRandMaxIntensity");
            this.txtRandMaxIntensity.BackColor = System.Drawing.Color.Transparent;
            this.txtRandMaxIntensity.Name = "txtRandMaxIntensity";
            // 
            // sliderRandMinIntensity
            // 
            resources.ApplyResources(this.sliderRandMinIntensity, "sliderRandMinIntensity");
            this.sliderRandMinIntensity.LargeChange = 1;
            this.sliderRandMinIntensity.Maximum = 100;
            this.sliderRandMinIntensity.Name = "sliderRandMinIntensity";
            this.sliderRandMinIntensity.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandMinIntensity.ValueChanged += new System.EventHandler(this.sliderRandMinIntensity_ValueChanged);
            this.sliderRandMinIntensity.MouseEnter += new System.EventHandler(this.sliderRandMinIntensity_MouseEnter);
            // 
            // txtRandMinIntensity
            // 
            resources.ApplyResources(this.txtRandMinIntensity, "txtRandMinIntensity");
            this.txtRandMinIntensity.BackColor = System.Drawing.Color.Transparent;
            this.txtRandMinIntensity.Name = "txtRandMinIntensity";
            // 
            // sliderRandMaxSize
            // 
            resources.ApplyResources(this.sliderRandMaxSize, "sliderRandMaxSize");
            this.sliderRandMaxSize.LargeChange = 1;
            this.sliderRandMaxSize.Maximum = 500;
            this.sliderRandMaxSize.Name = "sliderRandMaxSize";
            this.sliderRandMaxSize.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandMaxSize.ValueChanged += new System.EventHandler(this.sliderRandMaxSize_ValueChanged);
            this.sliderRandMaxSize.MouseEnter += new System.EventHandler(this.sliderRandMaxSize_MouseEnter);
            // 
            // txtRandMaxSize
            // 
            this.txtRandMaxSize.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtRandMaxSize, "txtRandMaxSize");
            this.txtRandMaxSize.Name = "txtRandMaxSize";
            // 
            // sliderRandMinSize
            // 
            resources.ApplyResources(this.sliderRandMinSize, "sliderRandMinSize");
            this.sliderRandMinSize.LargeChange = 1;
            this.sliderRandMinSize.Maximum = 500;
            this.sliderRandMinSize.Name = "sliderRandMinSize";
            this.sliderRandMinSize.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandMinSize.ValueChanged += new System.EventHandler(this.sliderRandMinSize_ValueChanged);
            this.sliderRandMinSize.MouseEnter += new System.EventHandler(this.sliderRandMinSize_MouseEnter);
            // 
            // txtRandMinSize
            // 
            this.txtRandMinSize.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtRandMinSize, "txtRandMinSize");
            this.txtRandMinSize.Name = "txtRandMinSize";
            // 
            // sliderRandRotRight
            // 
            resources.ApplyResources(this.sliderRandRotRight, "sliderRandRotRight");
            this.sliderRandRotRight.LargeChange = 1;
            this.sliderRandRotRight.Maximum = 180;
            this.sliderRandRotRight.Name = "sliderRandRotRight";
            this.sliderRandRotRight.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandRotRight.ValueChanged += new System.EventHandler(this.sliderRandRotRight_ValueChanged);
            this.sliderRandRotRight.MouseEnter += new System.EventHandler(this.sliderRandRotRight_MouseEnter);
            // 
            // txtRandRotRight
            // 
            resources.ApplyResources(this.txtRandRotRight, "txtRandRotRight");
            this.txtRandRotRight.BackColor = System.Drawing.Color.Transparent;
            this.txtRandRotRight.Name = "txtRandRotRight";
            // 
            // sliderRandRotLeft
            // 
            resources.ApplyResources(this.sliderRandRotLeft, "sliderRandRotLeft");
            this.sliderRandRotLeft.LargeChange = 1;
            this.sliderRandRotLeft.Maximum = 180;
            this.sliderRandRotLeft.Name = "sliderRandRotLeft";
            this.sliderRandRotLeft.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderRandRotLeft.ValueChanged += new System.EventHandler(this.sliderRandRotLeft_ValueChanged);
            this.sliderRandRotLeft.MouseEnter += new System.EventHandler(this.sliderRandRotLeft_MouseEnter);
            // 
            // txtRandRotLeft
            // 
            resources.ApplyResources(this.txtRandRotLeft, "txtRandRotLeft");
            this.txtRandRotLeft.BackColor = System.Drawing.Color.Transparent;
            this.txtRandRotLeft.Name = "txtRandRotLeft";
            // 
            // tabControls
            // 
            this.tabControls.BackColor = System.Drawing.Color.Transparent;
            this.tabControls.Controls.Add(this.bttnRedo);
            this.tabControls.Controls.Add(this.sliderBrushIntensity);
            this.tabControls.Controls.Add(this.txtBrushIntensity);
            this.tabControls.Controls.Add(this.txtBrushSize);
            this.tabControls.Controls.Add(this.sliderBrushSize);
            this.tabControls.Controls.Add(this.sliderBrushRotation);
            this.tabControls.Controls.Add(this.txtBrushRotation);
            this.tabControls.Controls.Add(this.bttnOk);
            this.tabControls.Controls.Add(this.bttnUndo);
            this.tabControls.Controls.Add(this.bttnCancel);
            this.tabControls.Controls.Add(this.sliderCanvasZoom);
            this.tabControls.Controls.Add(this.txtCanvasZoom);
            this.tabControls.Controls.Add(this.bttnBrushSelector);
            resources.ApplyResources(this.tabControls, "tabControls");
            this.tabControls.Name = "tabControls";
            // 
            // bttnRedo
            // 
            resources.ApplyResources(this.bttnRedo, "bttnRedo");
            this.bttnRedo.Name = "bttnRedo";
            this.bttnRedo.UseVisualStyleBackColor = true;
            this.bttnRedo.Click += new System.EventHandler(this.bttnRedo_Click);
            this.bttnRedo.MouseEnter += new System.EventHandler(this.bttnRedo_MouseEnter);
            // 
            // sliderBrushIntensity
            // 
            resources.ApplyResources(this.sliderBrushIntensity, "sliderBrushIntensity");
            this.sliderBrushIntensity.LargeChange = 1;
            this.sliderBrushIntensity.Maximum = 100;
            this.sliderBrushIntensity.Name = "sliderBrushIntensity";
            this.sliderBrushIntensity.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderBrushIntensity.ValueChanged += new System.EventHandler(this.sliderBrushIntensity_ValueChanged);
            this.sliderBrushIntensity.MouseEnter += new System.EventHandler(this.sliderBrushIntensity_MouseEnter);
            // 
            // txtBrushIntensity
            // 
            resources.ApplyResources(this.txtBrushIntensity, "txtBrushIntensity");
            this.txtBrushIntensity.BackColor = System.Drawing.Color.Transparent;
            this.txtBrushIntensity.Name = "txtBrushIntensity";
            // 
            // txtBrushSize
            // 
            resources.ApplyResources(this.txtBrushSize, "txtBrushSize");
            this.txtBrushSize.BackColor = System.Drawing.Color.Transparent;
            this.txtBrushSize.Name = "txtBrushSize";
            // 
            // sliderBrushSize
            // 
            resources.ApplyResources(this.sliderBrushSize, "sliderBrushSize");
            this.sliderBrushSize.LargeChange = 1;
            this.sliderBrushSize.Maximum = 500;
            this.sliderBrushSize.Minimum = 2;
            this.sliderBrushSize.Name = "sliderBrushSize";
            this.sliderBrushSize.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderBrushSize.Value = 10;
            this.sliderBrushSize.ValueChanged += new System.EventHandler(this.sliderBrushSize_ValueChanged);
            this.sliderBrushSize.MouseEnter += new System.EventHandler(this.sliderBrushSize_MouseEnter);
            // 
            // sliderBrushRotation
            // 
            resources.ApplyResources(this.sliderBrushRotation, "sliderBrushRotation");
            this.sliderBrushRotation.LargeChange = 1;
            this.sliderBrushRotation.Maximum = 180;
            this.sliderBrushRotation.Minimum = -180;
            this.sliderBrushRotation.Name = "sliderBrushRotation";
            this.sliderBrushRotation.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderBrushRotation.ValueChanged += new System.EventHandler(this.sliderBrushRotation_ValueChanged);
            this.sliderBrushRotation.MouseEnter += new System.EventHandler(this.sliderBrushRotation_MouseEnter);
            // 
            // txtBrushRotation
            // 
            resources.ApplyResources(this.txtBrushRotation, "txtBrushRotation");
            this.txtBrushRotation.BackColor = System.Drawing.Color.Transparent;
            this.txtBrushRotation.Name = "txtBrushRotation";
            // 
            // bttnOk
            // 
            resources.ApplyResources(this.bttnOk, "bttnOk");
            this.bttnOk.Name = "bttnOk";
            this.bttnOk.UseVisualStyleBackColor = true;
            this.bttnOk.Click += new System.EventHandler(this.bttnOk_Click);
            this.bttnOk.MouseEnter += new System.EventHandler(this.bttnOk_MouseEnter);
            // 
            // bttnUndo
            // 
            resources.ApplyResources(this.bttnUndo, "bttnUndo");
            this.bttnUndo.Name = "bttnUndo";
            this.bttnUndo.UseVisualStyleBackColor = true;
            this.bttnUndo.Click += new System.EventHandler(this.bttnUndo_Click);
            this.bttnUndo.MouseEnter += new System.EventHandler(this.bttnUndo_MouseEnter);
            // 
            // bttnCancel
            // 
            resources.ApplyResources(this.bttnCancel, "bttnCancel");
            this.bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.bttnCancel_Click);
            this.bttnCancel.MouseEnter += new System.EventHandler(this.bttnCancel_MouseEnter);
            // 
            // sliderCanvasZoom
            // 
            resources.ApplyResources(this.sliderCanvasZoom, "sliderCanvasZoom");
            this.sliderCanvasZoom.LargeChange = 1;
            this.sliderCanvasZoom.Maximum = 1600;
            this.sliderCanvasZoom.Minimum = 1;
            this.sliderCanvasZoom.Name = "sliderCanvasZoom";
            this.sliderCanvasZoom.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderCanvasZoom.Value = 100;
            this.sliderCanvasZoom.ValueChanged += new System.EventHandler(this.sliderCanvasZoom_ValueChanged);
            this.sliderCanvasZoom.MouseEnter += new System.EventHandler(this.sliderCanvasZoom_MouseEnter);
            // 
            // txtCanvasZoom
            // 
            resources.ApplyResources(this.txtCanvasZoom, "txtCanvasZoom");
            this.txtCanvasZoom.BackColor = System.Drawing.Color.Transparent;
            this.txtCanvasZoom.Name = "txtCanvasZoom";
            // 
            // bttnBrushSelector
            // 
            resources.ApplyResources(this.bttnBrushSelector, "bttnBrushSelector");
            this.bttnBrushSelector.BackColor = System.Drawing.Color.White;
            this.bttnBrushSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.bttnBrushSelector.DropDownHeight = 140;
            this.bttnBrushSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.bttnBrushSelector.DropDownWidth = 20;
            this.bttnBrushSelector.FormattingEnabled = true;
            this.bttnBrushSelector.Name = "bttnBrushSelector";
            this.bttnBrushSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.bttnBrushSelector_DrawItem);
            this.bttnBrushSelector.SelectedIndexChanged += new System.EventHandler(this.bttnBrushSelector_SelectedIndexChanged);
            this.bttnBrushSelector.MouseEnter += new System.EventHandler(this.bttnBrushSelector_MouseEnter);
            // 
            // tabBar
            // 
            this.tabBar.Controls.Add(this.tabControls);
            this.tabBar.Controls.Add(this.tabEffect);
            this.tabBar.Controls.Add(this.tabJitter);
            this.tabBar.Controls.Add(this.tabOther);
            resources.ApplyResources(this.tabBar, "tabBar");
            this.tabBar.Multiline = true;
            this.tabBar.Name = "tabBar";
            this.tabBar.SelectedIndex = 0;
            // 
            // tabEffect
            // 
            this.tabEffect.BackColor = System.Drawing.SystemColors.Menu;
            this.tabEffect.Controls.Add(this.sliderEffectProperty2);
            this.tabEffect.Controls.Add(this.txtEffectProperty2);
            this.tabEffect.Controls.Add(this.sliderEffectProperty1);
            this.tabEffect.Controls.Add(this.txtEffectProperty1);
            this.tabEffect.Controls.Add(this.sliderEffectProperty4);
            this.tabEffect.Controls.Add(this.txtEffectProperty4);
            this.tabEffect.Controls.Add(this.sliderEffectProperty3);
            this.tabEffect.Controls.Add(this.txtEffectProperty3);
            this.tabEffect.Controls.Add(this.txtEffectType);
            this.tabEffect.Controls.Add(this.cmbxEffectType);
            resources.ApplyResources(this.tabEffect, "tabEffect");
            this.tabEffect.Name = "tabEffect";
            // 
            // sliderEffectProperty2
            // 
            resources.ApplyResources(this.sliderEffectProperty2, "sliderEffectProperty2");
            this.sliderEffectProperty2.LargeChange = 1;
            this.sliderEffectProperty2.Maximum = 500;
            this.sliderEffectProperty2.Name = "sliderEffectProperty2";
            this.sliderEffectProperty2.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderEffectProperty2.ValueChanged += new System.EventHandler(this.sliderEffectProperty2_ValueChanged);
            this.sliderEffectProperty2.KeyUp += new System.Windows.Forms.KeyEventHandler(this.sliderEffectProperty_KeyUp);
            this.sliderEffectProperty2.MouseEnter += new System.EventHandler(this.sliderEffectProperty2_MouseEnter);
            this.sliderEffectProperty2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.sliderEffectProperty_MouseUp);
            // 
            // txtEffectProperty2
            // 
            this.txtEffectProperty2.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtEffectProperty2, "txtEffectProperty2");
            this.txtEffectProperty2.Name = "txtEffectProperty2";
            // 
            // sliderEffectProperty1
            // 
            resources.ApplyResources(this.sliderEffectProperty1, "sliderEffectProperty1");
            this.sliderEffectProperty1.LargeChange = 1;
            this.sliderEffectProperty1.Maximum = 500;
            this.sliderEffectProperty1.Name = "sliderEffectProperty1";
            this.sliderEffectProperty1.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderEffectProperty1.ValueChanged += new System.EventHandler(this.sliderEffectProperty1_ValueChanged);
            this.sliderEffectProperty1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.sliderEffectProperty_KeyUp);
            this.sliderEffectProperty1.MouseEnter += new System.EventHandler(this.sliderEffectProperty1_MouseEnter);
            this.sliderEffectProperty1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.sliderEffectProperty_MouseUp);
            // 
            // txtEffectProperty1
            // 
            this.txtEffectProperty1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.txtEffectProperty1, "txtEffectProperty1");
            this.txtEffectProperty1.Name = "txtEffectProperty1";
            // 
            // sliderEffectProperty4
            // 
            resources.ApplyResources(this.sliderEffectProperty4, "sliderEffectProperty4");
            this.sliderEffectProperty4.LargeChange = 1;
            this.sliderEffectProperty4.Maximum = 180;
            this.sliderEffectProperty4.Name = "sliderEffectProperty4";
            this.sliderEffectProperty4.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderEffectProperty4.ValueChanged += new System.EventHandler(this.sliderEffectProperty4_ValueChanged);
            this.sliderEffectProperty4.KeyUp += new System.Windows.Forms.KeyEventHandler(this.sliderEffectProperty_KeyUp);
            this.sliderEffectProperty4.MouseEnter += new System.EventHandler(this.sliderEffectProperty4_MouseEnter);
            this.sliderEffectProperty4.MouseUp += new System.Windows.Forms.MouseEventHandler(this.sliderEffectProperty_MouseUp);
            // 
            // txtEffectProperty4
            // 
            resources.ApplyResources(this.txtEffectProperty4, "txtEffectProperty4");
            this.txtEffectProperty4.BackColor = System.Drawing.Color.Transparent;
            this.txtEffectProperty4.Name = "txtEffectProperty4";
            // 
            // sliderEffectProperty3
            // 
            resources.ApplyResources(this.sliderEffectProperty3, "sliderEffectProperty3");
            this.sliderEffectProperty3.LargeChange = 1;
            this.sliderEffectProperty3.Maximum = 180;
            this.sliderEffectProperty3.Name = "sliderEffectProperty3";
            this.sliderEffectProperty3.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sliderEffectProperty3.ValueChanged += new System.EventHandler(this.sliderEffectProperty3_ValueChanged);
            this.sliderEffectProperty3.KeyUp += new System.Windows.Forms.KeyEventHandler(this.sliderEffectProperty_KeyUp);
            this.sliderEffectProperty3.MouseEnter += new System.EventHandler(this.sliderEffectProperty3_MouseEnter);
            this.sliderEffectProperty3.MouseUp += new System.Windows.Forms.MouseEventHandler(this.sliderEffectProperty_MouseUp);
            // 
            // txtEffectProperty3
            // 
            resources.ApplyResources(this.txtEffectProperty3, "txtEffectProperty3");
            this.txtEffectProperty3.BackColor = System.Drawing.Color.Transparent;
            this.txtEffectProperty3.Name = "txtEffectProperty3";
            // 
            // txtEffectType
            // 
            resources.ApplyResources(this.txtEffectType, "txtEffectType");
            this.txtEffectType.Name = "txtEffectType";
            // 
            // cmbxEffectType
            // 
            this.cmbxEffectType.FormattingEnabled = true;
            resources.ApplyResources(this.cmbxEffectType, "cmbxEffectType");
            this.cmbxEffectType.Name = "cmbxEffectType";
            this.cmbxEffectType.MouseEnter += new System.EventHandler(this.cmbxEffectType_MouseEnter);
            // 
            // winBrushFilter
            // 
            this.AcceptButton = this.bttnOk;
            resources.ApplyResources(this, "$this");
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.CancelButton = this.bttnCancel;
            this.Controls.Add(this.tabBar);
            this.Controls.Add(this.displayCanvasBG);
            this.Controls.Add(this.txtTooltip);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.MaximizeBox = true;
            this.Name = "winBrushFilter";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.winBrushFilter_FormClosing);
            this.Load += new System.EventHandler(this.winBrushFilter_DialogLoad);
            this.Shown += new System.EventHandler(this.winBrushFilter_DialogShown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.winBrushFilter_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.winBrushFilter_KeyUp);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.displayCanvas_MouseWheel);
            this.Resize += new System.EventHandler(this.winBrushFilter_Resize);
            this.displayCanvasBG.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.displayCanvas)).EndInit();
            this.tabOther.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.sliderShiftIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderShiftRotation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderShiftSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderMinDrawDistance)).EndInit();
            this.grpbxBrushOptions.ResumeLayout(false);
            this.grpbxBrushOptions.PerformLayout();
            this.tabJitter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandVertShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandHorzShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMaxIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMinIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMaxSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandMinSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandRotRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderRandRotLeft)).EndInit();
            this.tabControls.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.sliderBrushIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderBrushSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderBrushRotation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderCanvasZoom)).EndInit();
            this.tabBar.ResumeLayout(false);
            this.tabEffect.ResumeLayout(false);
            this.tabEffect.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderEffectProperty3)).EndInit();
            this.ResumeLayout(false);

        }

        /// <summary>
        /// Displays a context menu for changing background color options.
        /// </summary>
        /// <param name="sender">
        /// The control associated with the context menu.
        /// </param>
        /// <param name="location">
        /// The mouse location to appear at.
        /// </param>
        private void ShowBgContextMenu(Control sender, Point location)
        {
            ContextMenu contextMenu = new ContextMenu();

            //Options to set the background colors / image.
            contextMenu.MenuItems.Add(new MenuItem("Use transparent background",
                new EventHandler((a, b) =>
                {
                    displayCanvas.BackColor = Color.Transparent;
                    displayCanvas.BackgroundImageLayout = ImageLayout.Tile;
                    displayCanvas.BackgroundImage = Resources.CheckeredBg;
                })));
            contextMenu.MenuItems.Add(new MenuItem("Use white background",
                new EventHandler((a, b) =>
                {
                    displayCanvas.BackColor = Color.White;
                    displayCanvas.BackgroundImage = null;
                })));
            contextMenu.MenuItems.Add(new MenuItem("Use black background",
                new EventHandler((a, b) =>
                {
                    displayCanvas.BackColor = Color.Black;
                    displayCanvas.BackgroundImage = null;
                })));
            if (Clipboard.ContainsImage())
            {
                contextMenu.MenuItems.Add(new MenuItem("Use clipboard as background",
                    new EventHandler((a, b) =>
                    {
                        if (Clipboard.ContainsImage())
                        {
                            try
                            {
                                displayCanvas.BackgroundImage = Clipboard.GetImage();
                                displayCanvas.BackgroundImageLayout = ImageLayout.Stretch;
                                displayCanvas.BackColor = Color.Transparent;
                            }
                            catch
                            {
                                MessageBox.Show("Could not use clipboard image.");
                            }
                        }
                    })));
            }

            contextMenu.Show(sender, location);
        }
        #endregion

        #region Methods (event handlers)
        /// <summary>
        /// Configures the drawing area and loads text localizations.
        /// </summary>
        private void winBrushFilter_DialogLoad(object sender, EventArgs e)
        {
            //Sets the sizes of the canvas and drawing region.
            displayCanvas.Size = new RenderArgs(EffectSourceSurface).Bitmap.Size;
            bmpCurrentDrawing = new Bitmap(displayCanvas.Width, displayCanvas.Height);
            bmpEffectDrawing = new Bitmap(displayCanvas.Width, displayCanvas.Height);
            bmpEffectAlpha = new byte[bmpEffectDrawing.Width, bmpEffectDrawing.Height];
            Utils.CopyBitmapPure(new RenderArgs(EffectSourceSurface).Bitmap, bmpCurrentDrawing);

            //Sets the effect property labels, ranges, and visibility.
            SetEffectProperties(false);

            //Sets the canvas dimensions.
            displayCanvas.Left = (displayCanvasBG.Width - displayCanvas.Width) / 2;
            displayCanvas.Top = (displayCanvasBG.Height - displayCanvas.Height) / 2;

            //Adds versioning information to the window title.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Text = EffectPlugin.StaticName + " (version " +
                version.Major + "." + version.Minor + ")";

            //Loads globalization texts for regional support.
            txtBrushIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.Intensity, sliderBrushIntensity.Value);

            txtBrushRotation.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.Rotation, sliderBrushRotation.Value);

            txtBrushSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.Size, sliderBrushSize.Value);

            txtCanvasZoom.Text = String.Format("{0} {1}%",
                Globalization.GlobalStrings.CanvasZoom, sliderCanvasZoom.Value);

            txtEffectType.Text = Globalization.GlobalStrings.EffectType;

            txtMinDrawDistance.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.MinDrawDistance, sliderMinDrawDistance.Value);

            txtRandHorzShift.Text = String.Format("{0} {1}%",
                Globalization.GlobalStrings.RandHorzShift, sliderRandHorzShift.Value);

            txtRandMaxSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMaxSize, sliderRandMaxSize.Value);

            txtRandMinSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMinSize, sliderRandMinSize.Value);

            txtRandRotLeft.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.RandRotLeft, sliderRandRotLeft.Value);

            txtRandRotRight.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.RandRotRight, sliderRandRotRight.Value);

            txtRandMaxIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMaxIntensity, sliderRandMaxIntensity.Value);

            txtRandMinIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMinIntensity, sliderRandMinIntensity.Value);

            txtRandVertShift.Text = String.Format("{0} {1}%",
                Globalization.GlobalStrings.RandVertShift, sliderRandVertShift.Value);

            txtShiftIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.ShiftIntensity, sliderShiftIntensity.Value);

            txtShiftRotation.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.ShiftRotation, sliderShiftRotation.Value);

            txtShiftSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.ShiftSize, sliderShiftSize.Value);

            txtTooltip.Text = Globalization.GlobalStrings.GeneralTooltip;

            tabControls.Text = Globalization.GlobalStrings.TabControls;

            tabEffect.Text = Globalization.GlobalStrings.TabEffect;

            tabJitter.Text = Globalization.GlobalStrings.TabJitter;

            tabOther.Text = Globalization.GlobalStrings.TabOther;

            bttnUndo.Text = Globalization.GlobalStrings.Undo;

            bttnRedo.Text = Globalization.GlobalStrings.Redo;

            bttnOk.Text = Globalization.GlobalStrings.Ok;

            bttnCustomBrushLocations.Text = Globalization.GlobalStrings.CustomBrushLocations;

            bttnCancel.Text = Globalization.GlobalStrings.Cancel;

            bttnClearBrushes.Text = Globalization.GlobalStrings.ClearBrushes;

            bttnClearSettings.Text = Globalization.GlobalStrings.ClearSettings;

            chkbxOrientToMouse.Text = Globalization.GlobalStrings.OrientToMouse;

            grpbxBrushOptions.Text = Globalization.GlobalStrings.BrushOptions;
        }

        /// <summary>
        /// Sets the form resize restrictions.
        /// </summary>
        private void winBrushFilter_DialogShown(object sender, EventArgs e)
        {
            MinimumSize = new Size(835, 526);
            MaximumSize = Size;
        }

        /// <summary>
        /// Disposes resources and deletes temporary files when the window
        /// closes for any reason.
        /// </summary>
        private void winBrushFilter_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Deletes all temporary files stored as undo/redo history.
            string path = Path.GetTempPath();
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(path, "HistoryBmp*.undo"));
            files.AddRange(Directory.GetFiles(path, "HistoryBmp*.redo"));
            foreach (string file in files)
            {
                File.Delete(file);
            }

            //Disposes all form bitmaps.
            bmpBrush.Dispose();
            bmpCurrentDrawing.Dispose();
            bmpEffectDrawing.Dispose();
        }

        /// <summary>
        /// Handles keypresses for global commands.
        /// </summary>
        private void winBrushFilter_KeyDown(object sender, KeyEventArgs e)
        {
            //Ctrl + Z: Undo.
            if (e.Control && e.KeyCode == Keys.Z)
            {
                bttnUndo_Click(this, e);
            }

            //Ctrl + Y: Redo.
            if (e.Control && e.KeyCode == Keys.Y)
            {
                bttnRedo_Click(this, e);
            }

            //Prevents alt from making the form lose focus.
            if (e.Alt)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Re-applies a filter after undo or redo is released.
        /// </summary>
        private void winBrushFilter_KeyUp(object sender, KeyEventArgs e)
        {
            //When undo or redo is released, re-apply the filter.
            if (e.Control && (e.KeyCode == Keys.Z || e.KeyCode == Keys.Y))
            {
                ApplyFilter();
            }
        }

        /// <summary>
        /// Recalculates the drawing region to maintain accuracy on resize.
        /// </summary>
        private void winBrushFilter_Resize(object sender, EventArgs e)
        {
            displayCanvas.Left = (displayCanvasBG.Width - displayCanvas.Width) / 2;
            displayCanvas.Top = (displayCanvasBG.Height - displayCanvas.Height) / 2;
        }

        /// <summary>
        /// Sets up image panning and drawing to occur with mouse movement.
        /// </summary>
        private void displayCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            //Displays a context menu for the background.
            if (e.Button == MouseButtons.Right)
            {
                ShowBgContextMenu(displayCanvas, e.Location);
            }

            //Enables and records image panning.
            else if (e.Button == MouseButtons.Middle)
            {
                isUserPanning = true;
                mouseLocPrev = e.Location;
            }

            //Enables and records brush drawing.
            else if (e.Button == MouseButtons.Left)
            {
                isUserDrawing = true;
                mouseLocPrev = e.Location;

                //Repositions the canvas when the user draws out-of-bounds.
                timerRepositionUpdate.Enabled = true;

                //Adds to the list of undo operations.
                string path = Path.GetTempPath();

                //Saves the drawing to the file and saves the file path.
                bmpCurrentDrawing.Save(path + "HistoryBmp" + undoHistory.Count + ".undo");
                undoHistory.Push(path + "HistoryBmp" + undoHistory.Count + ".undo");
                if (!bttnUndo.Enabled)
                {
                    bttnUndo.Enabled = true;
                }

                //Removes all redo history.
                redoHistory.Clear();

                //Draws the brush on the first canvas click.
                if (!chkbxOrientToMouse.Checked)
                {
                    displayCanvas_MouseMove(sender, e);
                }
            }
        }

        /// <summary>
        /// Ensures focusable controls cannot intercept keyboard/mouse input
        /// while the user is hovered over the display canvas. Sets a tooltip.
        /// </summary>
        private void displayCanvas_MouseEnter(object sender, EventArgs e)
        {
            displayCanvas.Focus();

            txtTooltip.Text = Globalization.GlobalStrings.GeneralTooltip;
            DisablePreview(this, null);
        }

        /// <summary>
        /// Sets up for drawing and handles panning.
        /// </summary>
        private void displayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            //Forcefully loads if it hasn't yet.
            if (!hasLoaded)
            {
                hasLoaded = true;
                SetEffectProperties(false);
                return;
            }

            //Updates the new location.
            mouseLoc = e.Location;

            //Handles panning the screen.
            if (isUserPanning && !displayCanvasBG.ClientRectangle.Contains(displayCanvas.ClientRectangle))
            {
                Rectangle range = GetRange();

                //Moves the drawing region.
                int locx = displayCanvas.Left + (mouseLoc.X - mouseLocPrev.X);
                int locy = displayCanvas.Top + (mouseLoc.Y - mouseLocPrev.Y);

                //Ensures the user cannot pan beyond the image bounds.
                if (locx <= range.Left) { locx = range.Left; }
                if (locx >= range.Right) { locx = range.Right; }
                if (locy <= range.Top) { locy = range.Top; }
                if (locy >= range.Bottom) { locy = range.Bottom; }

                //Updates the position of the canvas.
                Point loc = new Point(locx, locy);
                displayCanvas.Location = loc;
                displayCanvas.Refresh();
            }
            else if (isUserDrawing)
            {
                //Gets the brush's location without respect to canvas zooming.
                Point brushPoint = new Point(
                    (int)(mouseLoc.X / displayCanvasZoom),
                    (int)(mouseLoc.Y / displayCanvasZoom));

                //Randomly alters the radius according to random ranges.
                int newRadius = Utils.Clamp(sliderBrushSize.Value
                    - random.Next(sliderRandMinSize.Value)
                    + random.Next(sliderRandMaxSize.Value), 0, int.MaxValue);

                //Applies the brush drawing.
                ApplyBrush(brushPoint, newRadius);

                mouseLocPrev = e.Location;
                displayCanvas.Refresh();
            }
            else
            {
                //Redraws to update the brush indicator (ellipse).
                displayCanvas.Refresh();
            }
        }

        /// <summary>
        /// Stops tracking panning and drawing.
        /// </summary>
        private void displayCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            //Finishes the brush stroke by merging the effect layer.
            if (isUserDrawing)
            {
                using (Graphics g = Graphics.FromImage(bmpCurrentDrawing))
                {
                    g.DrawImage(bmpEffectDrawing, 0, 0,
                        bmpCurrentDrawing.Width, bmpCurrentDrawing.Height);
                }

                //Re-applies an effect to the bitmap stroke.
                ApplyFilter();
            }

            isUserPanning = false;
            isUserDrawing = false;
            timerRepositionUpdate.Enabled = false;

            //Lets the user click anywhere to draw again.
            mouseLocBrush = null;
        }

        /// <summary>
        /// Zooms in and out of the drawing region.
        /// </summary>
        private void displayCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            //Ctrl + Wheel: Changes the brush size.
            if (ModifierKeys == Keys.Control)
            {
                //Ctrl + S + Wheel: Changes the brush size.
                if (System.Windows.Input.Keyboard.IsKeyDown
                    (System.Windows.Input.Key.S))
                {
                    int changeFactor = 10;

                    if (sliderBrushSize.Value < 5)
                    {
                        changeFactor = 1;
                    }
                    else if (sliderBrushSize.Value < 10)
                    {
                        changeFactor = 2;
                    }
                    else if (sliderBrushSize.Value < 30)
                    {
                        changeFactor = 5;
                    }
                    else if (sliderBrushSize.Value < 100)
                    {
                        changeFactor = 10;
                    }
                    else
                    {
                        changeFactor = 20;
                    }

                    sliderBrushSize.Value = Utils.Clamp(
                    sliderBrushSize.Value + Math.Sign(e.Delta) * changeFactor,
                    sliderBrushSize.Minimum,
                    sliderBrushSize.Maximum);
                }

                //Ctrl + R + Wheel: Changes the brush rotation.
                else if (System.Windows.Input.Keyboard.IsKeyDown
                    (System.Windows.Input.Key.R))
                {
                    sliderBrushRotation.Value = Utils.Clamp(
                    sliderBrushRotation.Value + Math.Sign(e.Delta) * 20,
                    sliderBrushRotation.Minimum,
                    sliderBrushRotation.Maximum);
                }

                //Ctrl + I + Wheel: Changes the brush intensity.
                else if (System.Windows.Input.Keyboard.IsKeyDown
                    (System.Windows.Input.Key.I))
                {
                    sliderBrushIntensity.Value = Utils.Clamp(
                    sliderBrushIntensity.Value + Math.Sign(e.Delta) * 10,
                    sliderBrushIntensity.Minimum,
                    sliderBrushIntensity.Maximum);
                }

                //Ctrl + Wheel: Zooms the canvas in/out.
                else
                {
                    Zoom(e.Delta, true);
                }
            }
        }

        /// <summary>
        /// Redraws the canvas and draws circles to indicate brush location.
        /// </summary>
        private void displayCanvas_Paint(object sender, PaintEventArgs e)
        {
            //Draws the whole canvas showing pixels and without smoothing.
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.SmoothingMode = SmoothingMode.None;

            //Draws the image with an intentionally truncated extra size.
            //TODO: Remove the workaround (extra size) and find the cause.
            e.Graphics.DrawImage(bmpCurrentDrawing, 0, 0,
                displayCanvas.ClientRectangle.Width + (sliderCanvasZoom.Value / 100),
                displayCanvas.ClientRectangle.Height + (sliderCanvasZoom.Value / 100));

            //Draws the effect surface for user drawing or as a preview.
            if (isUserDrawing || doPreview)
            {
                e.Graphics.DrawImage(bmpEffectDrawing, 0, 0,
                    displayCanvas.ClientRectangle.Width + (sliderCanvasZoom.Value / 100),
                    displayCanvas.ClientRectangle.Height + (sliderCanvasZoom.Value / 100));
            }

            //Draws the selection.
            if (Selection != null && Selection.GetRegionReadOnly() != null)
            {
                //Calculates the outline once the selection becomes valid.
                if (selectionOutline == null)
                {
                    selectionOutline = Selection.ConstructOutline(
                        new RectangleF(0, 0,
                        bmpCurrentDrawing.Width,
                        bmpCurrentDrawing.Height),
                        displayCanvasZoom);
                }

                //Scales to zoom so the drawing region accounts for scale.
                e.Graphics.ScaleTransform(displayCanvasZoom, displayCanvasZoom);

                //Creates the inverted region of the selection.
                var drawingArea = new Region(new Rectangle
                    (0, 0, bmpCurrentDrawing.Width, bmpCurrentDrawing.Height));
                drawingArea.Exclude(Selection.GetRegionReadOnly());

                //Draws the region as a darkening over unselected pixels.
                e.Graphics.FillRegion(
                    new SolidBrush(Color.FromArgb(63, 0, 0, 0)), drawingArea);

                //Draws the outline of the selection.
                if (selectionOutline?.GetRegionData() != null)
                {
                    e.Graphics.FillRegion(
                        SystemBrushes.Highlight,
                        selectionOutline.GetRegionReadOnly());
                }

                //Returns to ordinary scaling.
                e.Graphics.ScaleTransform(
                    1 / displayCanvasZoom,
                    1 / displayCanvasZoom);
            }

            //Draws the brush as a rectangle when not drawing by mouse.
            if (!isUserDrawing)
            {
                int radius = (int)(sliderBrushSize.Value * displayCanvasZoom);

                e.Graphics.DrawRectangle(
                    Pens.Black,
                    mouseLoc.X - (radius / 2),
                    mouseLoc.Y - (radius / 2),
                    radius,
                    radius);

                e.Graphics.DrawRectangle(
                    Pens.White,
                    mouseLoc.X - (radius / 2) - 1,
                    mouseLoc.Y - (radius / 2) - 1,
                    radius + 2,
                    radius + 2);
            }
        }

        /// <summary>
        /// Ensures focusable controls cannot intercept keyboard/mouse input
        /// while the user is hovered over the display canvas's panel. Sets a
        /// tooltip.
        /// </summary>
        private void displayCanvasBG_MouseEnter(object sender, EventArgs e)
        {
            displayCanvas.Focus();

            txtTooltip.Text = Globalization.GlobalStrings.GeneralTooltip;
            DisablePreview(this, null);
        }

        /// <summary>
        /// Displays a preview of the effect that was rendered.
        /// </summary>
        private void EnablePreview(object sender, EventArgs e)
        {
            if (!doPreview)
            {
                doPreview = true;
                ApplyFilterAlpha();
            }
        }

        /// <summary>
        /// Hides the preview of the effect that was rendered.
        /// </summary>
        private void DisablePreview(object sender, EventArgs e)
        {
            if (doPreview)
            {
                doPreview = false;
                ApplyFilterAlpha();
            }
        }

        /// <summary>
        /// Draws the current item's image and text. This is automatically
        /// called for each item to be drawn.
        /// </summary>
        private void bttnBrushSelector_DrawItem(object sender, DrawItemEventArgs e)
        {
            //Constrains the image drawing space of each item's picture so it
            //draws without distortion, which is why size is height * height.
            Rectangle pictureLocation = new Rectangle(2, e.Bounds.Top,
                e.Bounds.Height, e.Bounds.Height);

            //Repaints white over the image and text area.
            e.Graphics.FillRectangle(
                Brushes.White,
                new Rectangle(2, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height));

            //Draws the image of the current item to be repainted.
            if (loadedBrushes[e.Index].Brush != null)
            {
                e.Graphics.DrawImage(loadedBrushes[e.Index].Brush, pictureLocation);
            }

            //Draws the text of the current item to be repainted.
            //Draws the custom brush text centered as there is no picture.
            if (bttnBrushSelector.Items[e.Index] == BrushSelectorItem.CustomBrush)
            {
                e.Graphics.DrawString(
                    bttnBrushSelector.GetItemText(bttnBrushSelector.Items[e.Index]),
                    bttnBrushSelector.Font,
                    Brushes.Black,
                    new Point(e.Bounds.X + 4, e.Bounds.Y + 6));
            }
            else
            {
                e.Graphics.DrawString(
                    bttnBrushSelector.GetItemText(bttnBrushSelector.Items[e.Index]),
                    bttnBrushSelector.Font,
                    Brushes.Black,
                    new Point(e.Bounds.X + pictureLocation.Width, e.Bounds.Y + 6));
            }
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnBrushSelector_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.BrushSelectorTip;
        }

        /// <summary>
        /// Sets the brush when the user changes it with the selector.
        /// </summary>
        private void bttnBrushSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Gets the currently selected item.
            BrushSelectorItem currentItem =
                (bttnBrushSelector.SelectedItem as BrushSelectorItem);

            //Opens a file dialog for the user to load brushes.
            if (currentItem.Name.Equals(BrushSelectorItem.CustomBrush.Name))
            {
                ImportBrushes(true);
            }

            //Sets the brush otherwise.
            else
            {
                bmpBrush = Utils.FormatImage(
                    new Bitmap(currentItem.Brush),
                    PixelFormat.Format32bppArgb);
            }
        }

        /// <summary>
        /// Cancels and doesn't apply the effect.
        /// </summary>
        private void bttnCancel_Click(object sender, EventArgs e)
        {
            //Disables the button so it can't accidentally be called twice.
            bttnCancel.Enabled = false;

            this.Close();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnCancel_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.CancelTip;
        }

        /// <summary>
        /// Removes all brushes added by the user.
        /// </summary>
        private void bttnClearBrushes_Click(object sender, EventArgs e)
        {
            InitBrushes();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnClearBrushes_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.ClearBrushesTip;
        }

        /// <summary>
        /// Resets all settings back to their default values.
        /// </summary>
        private void bttnClearSettings_Click(object sender, EventArgs e)
        {
            InitSettings();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnClearSettings_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.ClearSettingsTip;
        }

        /// <summary>
        /// Accepts and applies the effect.
        /// </summary>
        private void bttnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            //Disables the button so it can't accidentally be called twice.
            //Ensures settings will be saved.
            bttnOk.Enabled = false;

            //Sets the bitmap to draw. Locks to prevent concurrency.
            lock (RenderSettings.BmpToRender)
            {
                RenderSettings.BmpToRender = new Bitmap(bmpCurrentDrawing);
            }

            //Updates the saved effect settings and OKs the effect.
            RenderSettings.DoApplyEffect = true;
            FinishTokenUpdate();

            this.Close();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnOk_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.OkTip;
        }

        /// <summary>
        /// Opens the preferences dialog to define persistent settings.
        /// </summary>
        private void bttnPreferences_Click(object sender, EventArgs e)
        {
            new Gui.BrushFilterPreferences().ShowDialog();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnPreferences_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.CustomBrushLocationsTip;
        }

        /// <summary>
        /// Reverts to a previously-undone drawing stored in a temporary file.
        /// </summary>
        private void bttnRedo_Click(object sender, EventArgs e)
        {
            //Does nothing if there is nothing to redo.
            if (redoHistory.Count == 0)
            {
                return;
            }

            //Prevents an error that would occur if redo was pressed in the
            //middle of a drawing operation by aborting it.
            isUserDrawing = false;

            //Acquires the bitmap from the file and loads it if it exists.
            string fileAndPath = redoHistory.Pop();
            if (File.Exists(fileAndPath))
            {
                //Saves the drawing to the file for undo.
                string path = Path.GetTempPath();
                bmpCurrentDrawing.Save(path + "HistoryBmp" + undoHistory.Count + ".undo");
                undoHistory.Push(path + "HistoryBmp" + undoHistory.Count + ".undo");

                //Clears the current drawing (in case parts are transparent),
                //and draws the saved version.
                using (Bitmap redoBmp = new Bitmap(fileAndPath))
                {
                    Utils.CopyBitmapPure(redoBmp, bmpCurrentDrawing);
                }

                displayCanvas.Refresh();
            }
            else
            {
                MessageBox.Show("File could not be found for redo.");
            }

            //Handles enabling undo or disabling redo for the user's clarity.
            if (redoHistory.Count == 0)
            {
                bttnRedo.Enabled = false;
                ApplyFilter();
            }
            if (!bttnUndo.Enabled && undoHistory.Count > 0)
            {
                bttnUndo.Enabled = true;
            }
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnRedo_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RedoTip;
        }

        /// <summary>
        /// Reverts to a previously-saved drawing stored in a temporary file.
        /// </summary>
        private void bttnUndo_Click(object sender, EventArgs e)
        {
            //Does nothing if there is nothing to undo.
            if (undoHistory.Count == 0)
            {
                return;
            }

            //Prevents an error that would occur if undo was pressed in the
            //middle of a drawing operation by aborting it.
            isUserDrawing = false;

            //Acquires the bitmap from the file and loads it if it exists.
            string fileAndPath = undoHistory.Pop();
            if (File.Exists(fileAndPath))
            {
                //Saves the drawing to the file for redo.
                string path = Path.GetTempPath();
                bmpCurrentDrawing.Save(path + "HistoryBmp" + redoHistory.Count + ".redo");
                redoHistory.Push(path + "HistoryBmp" + redoHistory.Count + ".redo");

                //Clears the current drawing (in case parts are transparent),
                //and draws the saved version.
                using (Bitmap undoBmp = new Bitmap(fileAndPath))
                {
                    Utils.CopyBitmapPure(undoBmp, bmpCurrentDrawing);
                }

                displayCanvas.Refresh();
            }
            else
            {
                MessageBox.Show("File could not be found for undo.");
            }

            //Handles enabling redo or disabling undo for the user's clarity.
            if (undoHistory.Count == 0)
            {
                bttnUndo.Enabled = false;
                ApplyFilter();
            }
            if (!bttnRedo.Enabled && redoHistory.Count > 0)
            {
                bttnRedo.Enabled = true;
            }
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void bttnUndo_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.UndoTip;
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void chkbxOrientToMouse_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.OrientToMouseTip;
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void cmbxEffectType_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.EffectTypeTip;
        }

        /// <summary>
        /// Determines which properties of the effect to show.
        /// </summary>
        private void cmbxEffectType_SelectedValueChanged(object sender, EventArgs e)
        {
            //Loads effect properties if the dialog has loaded.
            if (bmpEffectAlpha != null)
            {
                doPreview = true;
                SetEffectProperties(true);
            }
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void cmbxSymmetry_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.SymmetryTip;
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderBrushIntensity_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.BrushIntensityTip;
        }

        /// <summary>
        /// Adjusts the brush intensity text when it changes.
        /// </summary>
        private void sliderBrushIntensity_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtBrushIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.Intensity,
                sliderBrushIntensity.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderBrushSize_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.BrushSizeTip;
        }

        /// <summary>
        /// Adjusts the brush size text when it changes.
        /// </summary>
        private void sliderBrushSize_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtBrushSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.Size,
                sliderBrushSize.Value);

            //Updates to show changes in the brush indicator.
            displayCanvas.Refresh();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderBrushRotation_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.BrushRotationTip;
        }

        /// <summary>
        /// Adjusts the brush rotation text when it changes.
        /// </summary>
        private void sliderBrushRotation_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtBrushRotation.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.Rotation,
                sliderBrushRotation.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderCanvasZoom_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.CanvasZoomTip;
        }

        /// <summary>
        /// Zooms in and out of the drawing region.
        /// </summary>
        private void sliderCanvasZoom_ValueChanged(object sender, EventArgs e)
        {
            Zoom(0, false);
        }

        /// <summary>
        /// Re-applies the filter after any keyboard action which may change
        /// the value of a parameter for the filter.
        /// </summary>
        private void sliderEffectProperty_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Re-applies the filter after any mouse action which may change the
        /// value of a parameter for the filter.
        /// </summary>
        private void sliderEffectProperty_MouseUp(object sender, MouseEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderEffectProperty1_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = (string)sliderEffectProperty1?.Tag;
        }

        /// <summary>
        /// Adjusts the property effect 1 text when it changes.
        /// </summary>
        private void sliderEffectProperty1_ValueChanged(object sender, EventArgs e)
        {
            txtEffectProperty1.Text =
                txtEffectProperty1.Tag + ": " + sliderEffectProperty1.Value;

            doPreview = true;
            ApplyFilterAlpha();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderEffectProperty2_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = (string)sliderEffectProperty2?.Tag;
        }

        /// <summary>
        /// Adjusts the property effect 2 text when it changes.
        /// </summary>
        private void sliderEffectProperty2_ValueChanged(object sender, EventArgs e)
        {
            txtEffectProperty2.Text =
                txtEffectProperty2.Tag + ": " + sliderEffectProperty2.Value;

            doPreview = true;
            ApplyFilterAlpha();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderEffectProperty3_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = (string)sliderEffectProperty3?.Tag;
        }

        /// <summary>
        /// Adjusts the property effect 3 text when it changes.
        /// </summary>
        private void sliderEffectProperty3_ValueChanged(object sender, EventArgs e)
        {
            txtEffectProperty3.Text =
                txtEffectProperty3.Tag + ": " + sliderEffectProperty3.Value;

            doPreview = true;
            ApplyFilterAlpha();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderEffectProperty4_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = (string)sliderEffectProperty4?.Tag;
        }

        /// <summary>
        /// Adjusts the property effect 4 text when it changes.
        /// </summary>
        private void sliderEffectProperty4_ValueChanged(object sender, EventArgs e)
        {
            txtEffectProperty4.Text =
                txtEffectProperty4.Tag + ": " + sliderEffectProperty4.Value;

            doPreview = true;
            ApplyFilterAlpha();
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderMinDrawDistance_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.MinDrawDistanceTip;
        }

        /// <summary>
        /// Adjusts the brush minimum drawing distance text when it changes.
        /// </summary>
        private void sliderMinDrawDistance_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtMinDrawDistance.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.MinDrawDistance,
                sliderMinDrawDistance.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandHorzShift_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandHorzShiftTip;
        }

        /// <summary>
        /// Adjusts the random horizontal shift text when it changes.
        /// </summary>
        private void sliderRandHorzShift_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandHorzShift.Text = String.Format("{0} {1}%",
                Globalization.GlobalStrings.RandHorzShift,
                sliderRandHorzShift.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandMaxIntensity_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandMaxIntensityTip;
        }

        /// <summary>
        /// Adjusts the random max intensity text when it changes.
        /// </summary>
        private void sliderRandMaxIntensity_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandMaxIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMaxIntensity,
                sliderRandMaxIntensity.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandMaxSize_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandMaxSizeTip;
        }

        /// <summary>
        /// Adjusts the random max size text when it changes.
        /// </summary>
        private void sliderRandMaxSize_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandMaxSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMaxSize,
                sliderRandMaxSize.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandMinIntensity_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandMinIntensityTip;
        }

        /// <summary>
        /// Adjusts the random min intensity text when it changes.
        /// </summary>
        private void sliderRandMinIntensity_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandMinIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMinIntensity,
                sliderRandMinIntensity.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandMinSize_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandMinSizeTip;
        }

        /// <summary>
        /// Adjusts the random min size text when it changes.
        /// </summary>
        private void sliderRandMinSize_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandMinSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.RandMinSize,
                sliderRandMinSize.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandRotLeft_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandRotLeftTip;
        }

        /// <summary>
        /// Adjusts the random rotation to the left text when it changes.
        /// </summary>
        private void sliderRandRotLeft_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandRotLeft.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.RandRotLeft,
                sliderRandRotLeft.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandRotRight_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandRotRightTip;
        }

        /// <summary>
        /// Adjusts the random rotation to the right text when it changes.
        /// </summary>
        private void sliderRandRotRight_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandRotRight.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.RandRotRight,
                sliderRandRotRight.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderRandVertShift_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.RandVertShiftTip;
        }

        /// <summary>
        /// Adjusts the random vertical shift text when it changes.
        /// </summary>
        private void sliderRandVertShift_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtRandVertShift.Text = String.Format("{0} {1}%",
                Globalization.GlobalStrings.RandVertShift,
                sliderRandVertShift.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderShiftIntensity_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.ShiftIntensityTip;
        }

        /// <summary>
        /// Adjusts the slider shift intensity text when it changes.
        /// </summary>
        private void sliderShiftIntensity_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtShiftIntensity.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.ShiftIntensity,
                sliderShiftIntensity.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderShiftRotation_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.ShiftRotationTip;
        }

        /// <summary>
        /// Adjusts the slider shift rotation text when it changes.
        /// </summary>
        private void sliderShiftRotation_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtShiftRotation.Text = String.Format("{0} {1}°",
                Globalization.GlobalStrings.ShiftRotation,
                sliderShiftRotation.Value);
        }

        /// <summary>
        /// Sets a tooltip.
        /// </summary>
        private void sliderShiftSize_MouseEnter(object sender, EventArgs e)
        {
            txtTooltip.Text = Globalization.GlobalStrings.ShiftSizeTip;
        }

        /// <summary>
        /// Adjusts the slider shift size text when it changes.
        /// </summary>
        private void sliderShiftSize_ValueChanged(object sender, EventArgs e)
        {
            //Uses localized text drawn from a resource file.
            txtShiftSize.Text = String.Format("{0} {1}",
                Globalization.GlobalStrings.ShiftSize,
                sliderShiftSize.Value);
        }

        /// <summary>
        /// While drawing, moves the canvas automatically when trying to draw
        /// past the visible bounds.
        /// </summary>
        private void RepositionUpdate_Tick(object sender, EventArgs e)
        {
            /*Converts the mouse coordinates on the screen relative to the
             * background such that the top-left corner is (0, 0) up to its
             * width and height.*/
            Point mouseLocOnBG = displayCanvasBG.PointToClient(MousePosition);

            //Exits if the user isn't drawing out of the canvas boundary.
            if (!isUserDrawing ||
                displayCanvasBG.ClientRectangle.Contains(mouseLocOnBG) ||
                displayCanvasBG.ClientRectangle.Contains(displayCanvas.ClientRectangle))
            {
                return;
            }

            //Amount of space between the display canvas and background.
            Rectangle range = GetRange();

            //The amount to move the canvas while drawing.
            int nudge = (int)(displayCanvasZoom * 10);
            int canvasNewPosX, canvasNewPosY;

            //Nudges the screen horizontally when out of bounds and out of
            //the drawing region.
            if (displayCanvas.ClientRectangle.Width >=
                displayCanvasBG.ClientRectangle.Width)
            {
                if (mouseLocOnBG.X > displayCanvasBG.ClientRectangle.Width)
                {
                    canvasNewPosX = -nudge;
                }
                else if (mouseLocOnBG.X < 0)
                {
                    canvasNewPosX = nudge;
                }
                else
                {
                    canvasNewPosX = 0;
                }
            }
            else
            {
                canvasNewPosX = 0;
            }

            //Adds the left corner position to make it relative.
            if (range.Width != 0)
            {
                canvasNewPosX += displayCanvas.Left;
            }

            //Nudges the screen vertically when out of bounds and out of
            //the drawing region.
            if (displayCanvas.ClientRectangle.Height >=
                displayCanvasBG.ClientRectangle.Height)
            {
                if (mouseLocOnBG.Y > displayCanvasBG.ClientRectangle.Height)
                {
                    canvasNewPosY = -nudge;
                }
                else if (mouseLocOnBG.Y < 0)
                {
                    canvasNewPosY = nudge;
                }
                else
                {
                    canvasNewPosY = 0;
                }
            }
            else
            {
                canvasNewPosY = 0;
            }

            //Adds the top corner position to make it relative.
            if (range.Height != 0)
            {
                canvasNewPosY += displayCanvas.Top;
            }

            //Clamps all location values.
            if (canvasNewPosX <= range.Left) { canvasNewPosX = range.Left; }
            if (canvasNewPosX >= range.Right) { canvasNewPosX = range.Right; }
            if (canvasNewPosY <= range.Top) { canvasNewPosY = range.Top; }
            if (canvasNewPosY >= range.Bottom) { canvasNewPosY = range.Bottom; }

            //Updates with the new location and redraws the screen.
            displayCanvas.Location = new Point(canvasNewPosX, canvasNewPosY);
            displayCanvas.Refresh();
        }
        #endregion
    }
}
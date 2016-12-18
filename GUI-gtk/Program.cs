using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using CKAN;
using CKAN.Versioning;

namespace CKAN
{
    class InnerConnect
    {

        [Builder.Object]
        public Image FilterImage;
    }
    class Program: Window
    {
        [Builder.Object]
        private Toolbar ManageModsToolbar;
        [Builder.Object]
        private ListStore ModListstore;
        [Builder.Object]
        private TreeView ModListTreeView;
        [Builder.Object]
        private Image KspImage;
        [Builder.Object]
        private Image RefreshImage;
        [Builder.Object]
        private MenuToolButton FilterToolbarButton;

        private InnerConnect inner = new InnerConnect();

        static void Main(string[] args)
        {
            //
            //It seems it is NOT necessary to download GTK from http://www.mono-project.com/download/#download-win
            //if we are dependend on GtkSharp.Win32 but it should be checked
            //
            Console.WriteLine(Environment.GetEnvironmentVariable("PATH"));
            Console.WriteLine(String.Join(",", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()));

            //
            //
            //http://grbd.github.io/posts/2016/06/25/gtksharp-part-3-basic-example-with-vs-and-glade/
            //
            Application.Init();

            var gtkSettings = Gtk.Settings.Default;
            //BUGFIX
            // This enables clear text on Win32, makes the text look a lot less crappy
            //
            gtkSettings.XftRgba = "rgb";           
            gtkSettings.XftHinting = 1;
            gtkSettings.XftHintstyle = "hintfull";
            //
            //Modify default theme
            //
            Console.WriteLine("Old theme name:" + Gtk.Settings.Default.ThemeName);
            Gtk.Settings.Default.ThemeName = "gtk-win32";
            Console.WriteLine("New theme name:" + Gtk.Settings.Default.ThemeName);

            //
            //Modify default font size
            //
            Console.WriteLine("Font name:" + Gtk.Settings.Default.FontName);
            Gtk.Settings.Default.FontName = "Segoe UI 9";

            Builder builder = new Builder(null, "CKAN.resources.ckan-gtk-glade.glade", null);
                            
            Program p = new Program(builder, builder.GetObject("MainModWIndow").Handle);
            p.Show();

            Application.Run();


            //Application.Init();

            ////Create the Window
            //Window myWin = new Window("My first GTK# Application! ");
            //myWin.Resize(200, 200);

            ////Create a label and put some text in it.
            //Label myLabel = new Label();
            //myLabel.Text = "Hello World!!!!";

            ////Add the label to the form
            //myWin.Add(myLabel);

            ////Show Everything
            //myWin.ShowAll();

            //Application.Run();

            //DeleteEvent += OnLocalDeleteEvent;
        }

        enum UpdateState
        {
            SELECTED,
            NOT_SELECTED,
            CANNOT_SELECT
        }
        

        public Program(Builder builder, IntPtr handle): base(handle)
        {            
            DeleteEvent += (object sender, DeleteEventArgs args) =>
            {
                Application.Quit();
                args.RetVal = true;
            };


            //
            //Check if we can connect properties to many objects independly
            //
            builder.Autoconnect(this);
            builder.Autoconnect(this.inner);

            ManageModsToolbar.Style = ToolbarStyle.BothHoriz;
            for (int i = 0; i < ManageModsToolbar.NItems; ++i) {
                //
                //ToolbarStyle.BothHoriz works only for important icons, so lets mark all of them as important
                //
                ToolItem toolItem = ManageModsToolbar.GetNthItem(i);
                toolItem.IsImportant = true;
                if(toolItem is MenuToolButton)
                {
                    //BUGFIX
                    //If something is MenuToolButton for some reason it does not displays down arrow :(. 
                    //Lets fix this.
                    //

                    //http://www.mono-project.com/docs/gui/gtksharp/widgets/arrows/
                    //
                    Arrow arrow = new Arrow(ArrowType.Down, ShadowType.In);
                    arrow.Show();
                    Button buttonWithArrowToFix = ((Button)((Gtk.Box)toolItem.Child).Children[1]);
                    buttonWithArrowToFix.Image = arrow;
                }
            }
            //
            //BUGFIX
            //Load icons by hand - they are configured in Glade but it does not work :(
            //
            KspImage.Pixbuf = new Gdk.Pixbuf(null, "CKAN.resources.ksp.png");
            RefreshImage.Pixbuf = new Gdk.Pixbuf(null, "CKAN.resources.refresh.png");
            inner.FilterImage.Pixbuf = new Gdk.Pixbuf(null, "CKAN.resources.search.png");            

            //
            //create list view:
            //
            //
            //http://www.mono-project.com/docs/gui/gtksharp/widgets/treeview-tutorial/
            //

            Gtk.ListStore treeModel = new Gtk.ListStore(
                typeof(bool),
                typeof(UpdateState),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );
            


            CellRendererToggle installCellRenderer = AddToggleColumn(ModListTreeView, "Install", 0);
            AddUpdateColumn(ModListTreeView, "Update", 1);
            AddTextColumn(ModListTreeView, "Name", 2, 250);
            AddTextColumn(ModListTreeView, "Author", 3, 130);
            AddTextColumn(ModListTreeView, "Installed version", 4, 50);
            AddTextColumn(ModListTreeView, "Latest version", 5, 50);
            AddTextColumn(ModListTreeView, "Max KSP version", 6, 70);
            AddTextColumn(ModListTreeView, "Download(KB)", 7, 50);
            AddTextColumn(ModListTreeView, "Description", 8, 600);


            // Assign the model to the TreeView
            ModListTreeView.Model = treeModel;
            treeModel.AppendValues(false, UpdateState.SELECTED, "test1", "test2", "test3");
            treeModel.AppendValues(false, UpdateState.NOT_SELECTED, "test1", "test2", "test4");
            treeModel.AppendValues(false, UpdateState.CANNOT_SELECT, "test1", "test2", "test5");

            GtkUser user = new GtkUser();
            KSPManager manager = new KSPManager(user);
            if (manager.CurrentInstance == null && manager.GetPreferredInstance() == null)
            {
                Hide();

                return;
            }

            var CurrentInstance = manager.CurrentInstance;
            KspVersionCriteria versionCriteria = CurrentInstance.VersionCriteria();
            IRegistryQuerier registry = RegistryManager.Instance(CurrentInstance).registry;
            var mods = new HashSet<CkanModule>(registry.Available(versionCriteria));
            mods.UnionWith(registry.Incompatible(versionCriteria));
            var installed = registry.InstalledModules;

            int modLimitCounter = 0;
            List<string> emptyList = new List<string>();
            foreach (var mod in mods)
            {
                ++modLimitCounter;
                if(modLimitCounter > 20)
                {
                    //break;
                }
                treeModel.AppendValues(
                    false,
                    UpdateState.CANNOT_SELECT,
                    mod.name,
                    mod.author == null ? "N/A" : String.Join(", ", mod.author),
                    "-",
                    "-",
                    mod.HighestCompatibleKSP(),
                    toSizeString(mod.download_size),
                    (mod.@abstract == null ? "" : mod.@abstract).Replace("\n", ""));
            }           


            //
            //It seems to be for Gtk#2 so it does not work for GTK3:
            //
            //http://www.mono-project.com/docs/gui/gtksharp/beginners-guide/
            //
            //Glade.XML gxml = new Glade.XML("D:\\dokumenty\\programowanie\\projects\\ckan-fork\\CKAN-GUI-GTK\\resources\\ckan-gtk-glade.glade", "window1", null);
            //gxml.Autoconnect(this);
            //Application.Run();
        }

        private string toSizeString(long size)
        {
            if (size == 0)
                return "N/A";
            else if (size / 1024.0 < 1)
                return "1<KB";
            else
                return size / 1024 + "";
        }

        private CellRendererText AddTextColumn(TreeView modListTreeView, string title, int attributeIndex, int initialWidth)
        {
            TreeViewColumn column = new Gtk.TreeViewColumn();
            column.Resizable = true;
            column.Sizing = TreeViewColumnSizing.Fixed;
            column.MinWidth = 20;
            column.Expand = false;
            column.SortColumnId = attributeIndex;
            SetColumnTitle(column, title);            

            CellRendererText renderer = new Gtk.CellRendererText();
            column.PackStart(renderer, false);

            renderer.Width = initialWidth;
            renderer.Height = 20;
            renderer.SizePoints = 8;
            

            ModListTreeView.AppendColumn(column);

            column.AddAttribute(renderer, "text", attributeIndex);

            return renderer;
        }

        private static void SetColumnTitle(TreeViewColumn column, string title)
        {
            Label l = new Label(title);
            l.Wrap = true;
            l.OverrideFont(Pango.FontDescription.FromString("8"));
            l.ShowAll();
            column.Widget = l;
        }

        private CellRendererToggle AddToggleColumn(TreeView treeView, string title, int attributeIndex)
        {
            TreeViewColumn column = new Gtk.TreeViewColumn();
            column.SortColumnId = attributeIndex;

            SetColumnTitle(column, title);

            CellRendererToggle renderer = new Gtk.CellRendererToggle();
            column.PackStart(renderer, false);

            treeView.AppendColumn(column);

            column.AddAttribute(renderer, "active", attributeIndex);

            renderer.Toggled += (object o, ToggledArgs args) => {
                Gtk.TreeIter iter;

                ITreeModel treeModel = treeView.Model;


                treeModel.GetIter(out iter, new Gtk.TreePath(args.Path));

                bool oldValue = (bool)treeModel.GetValue(iter, attributeIndex);
                treeModel.SetValue(iter, attributeIndex, !oldValue);

            };

            return renderer;
        }

        private CellRendererToggle AddUpdateColumn(TreeView modListTreeView, string title, int attributeIndex)
        {
            //
            //we want to display either checkbox or string depending on value. it is tricky.
            //
            
            TreeViewColumn column = new Gtk.TreeViewColumn();
            column.SortColumnId = attributeIndex;
            SetColumnTitle(column, title);

            CellRendererToggle toggleRenderer = new Gtk.CellRendererToggle();
            
            column.PackStart(toggleRenderer, false);
            CellRendererText textRenderer = new Gtk.CellRendererText();
            column.PackStart(textRenderer, false);

            ModListTreeView.AppendColumn(column);

            column.SetCellDataFunc(toggleRenderer, (ICellLayout cellLayout, CellRenderer cell, ITreeModel treeModel, TreeIter iter) =>
            {
                textRenderer.Alignment = Pango.Alignment.Center;
                textRenderer.SetAlignment(0.5f, 0);
                UpdateState updateState = (UpdateState)treeModel.GetValue(iter, attributeIndex);
                if(updateState == UpdateState.SELECTED)
                {
                    toggleRenderer.Visible = true;
                    toggleRenderer.Active = true;
                    textRenderer.Visible = false;
                }
                if (updateState == UpdateState.NOT_SELECTED)
                {
                    toggleRenderer.Visible = true;
                    toggleRenderer.Active = false;
                    textRenderer.Visible = false;
                }
                if (updateState == UpdateState.CANNOT_SELECT)
                {
                    toggleRenderer.Visible = false;
                    toggleRenderer.Active = false;
                    textRenderer.Visible = true;
                    textRenderer.Text = "-";
                }
            }
            );


            toggleRenderer.Toggled += (object o, ToggledArgs args) => {
                Gtk.TreeIter iter;

                ITreeModel treeModel = modListTreeView.Model;

                treeModel.GetIter(out iter, new Gtk.TreePath(args.Path));

                UpdateState oldValue = (UpdateState)treeModel.GetValue(iter, attributeIndex);
                UpdateState newValue = oldValue;
                if(oldValue == UpdateState.SELECTED)
                {
                    newValue = UpdateState.NOT_SELECTED;
                }else if(oldValue == UpdateState.NOT_SELECTED)
                {
                    newValue = UpdateState.SELECTED;
                }
                treeModel.SetValue(iter, attributeIndex, newValue);

            };

            return toggleRenderer;
        }
    }
}

using System.Drawing;
using System.Runtime.InteropServices;

namespace OlAform
{
    public partial class Form1 : Form
    {
        private readonly List<ScriptAction> _actions = new();
        private Control? _draggingControl;
        private Point _dragStart;
        private readonly OlaPlugin _ola = new();

        public Form1()
        {
            InitializeComponent();
            lstActions.SelectedIndexChanged += LstActions_SelectedIndexChanged;
            propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;

            // Try to load OLA plugin (x64/x86) silently
            _ola.Load();
        }

        private void LstActions_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstActions.SelectedIndex >= 0 && lstActions.SelectedIndex < _actions.Count)
            {
                propertyGrid.SelectedObject = _actions[lstActions.SelectedIndex];
            }
            else
            {
                propertyGrid.SelectedObject = null;
            }
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // refresh representation
            RefreshDesignPanelItems();
        }

        private void btnAddMouse_Click(object sender, EventArgs e)
        {
            var a = new ScriptAction { Name = "MouseMove", ActionType = ActionType.MouseMove, X = 50, Y = 50 };
            AddAction(a);
        }

        private void btnAddKey_Click(object sender, EventArgs e)
        {
            var a = new ScriptAction { Name = "KeyPress", ActionType = ActionType.KeyPress, Key = "A" };
            AddAction(a);
        }

        private void btnAddOCR_Click(object sender, EventArgs e)
        {
            var a = new ScriptAction { Name = "OCR", ActionType = ActionType.OCR, X = 10, Y = 10, Width = 100, Height = 30 };
            AddAction(a);
        }

        private void AddAction(ScriptAction a)
        {
            _actions.Add(a);
            lstActions.Items.Add(a.Name);
            RefreshDesignPanelItems();
        }

        private void RefreshDesignPanelItems()
        {
            designPanel.Controls.Clear();
            for (int i = 0; i < _actions.Count; i++)
            {
                var act = _actions[i];
                var lbl = new Label
                {
                    Text = act.Name,
                    BackColor = Color.LightBlue,
                    AutoSize = false,
                    Width = Math.Max(60, act.Width),
                    Height = Math.Max(24, act.Height),
                    Tag = i
                };
                lbl.Location = new Point(act.X, act.Y);
                lbl.MouseDown += Item_MouseDown;
                lbl.MouseMove += Item_MouseMove;
                lbl.MouseUp += Item_MouseUp;
                lbl.Click += Item_Click;
                designPanel.Controls.Add(lbl);
            }
        }

        private void Item_Click(object? sender, EventArgs e)
        {
            if (sender is Control c && c.Tag is int idx)
            {
                lstActions.SelectedIndex = idx;
                propertyGrid.SelectedObject = _actions[idx];
            }
        }

        private void Item_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Control c)
            {
                _draggingControl = c;
                _dragStart = e.Location;
                c.Capture = true;
            }
        }

        private void Item_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_draggingControl != null && e.Button == MouseButtons.Left)
            {
                var newLoc = _draggingControl.Location;
                newLoc.Offset(e.X - _dragStart.X, e.Y - _dragStart.Y);
                _draggingControl.Location = newLoc;
                if (_draggingControl.Tag is int idx)
                {
                    var act = _actions[idx];
                    act.X = newLoc.X;
                    act.Y = newLoc.Y;
                }
            }
        }

        private void Item_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_draggingControl != null)
            {
                _draggingControl.Capture = false;
                _draggingControl = null;
            }
        }

        private void designPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Deselect
            lstActions.ClearSelected();
            propertyGrid.SelectedObject = null;
        }

        private void designPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // nothing
        }

        private void designPanel_MouseUp(object sender, MouseEventArgs e)
        {
            // nothing
        }

        // Expose an API to run the configured actions (simple sequential runner)
        private async Task RunActionsAsync()
        {
            foreach (var a in _actions)
            {
                switch (a.ActionType)
                {
                    case ActionType.MouseMove:
                        _ola.MouseMove(a.X, a.Y);
                        break;
                    case ActionType.MouseClick:
                        _ola.MouseClick(0);
                        break;
                    case ActionType.KeyPress:
                        _ola.KeyPress(a.Key ?? string.Empty);
                        break;
                    case ActionType.OCR:
                        var text = _ola.OcrRegion(a.X, a.Y, a.Width, a.Height);
                        MessageBox.Show($"OCR result: {text}");
                        break;
                    case ActionType.FindImage:
                        // not implemented - placeholder
                        break;
                }
                await Task.Delay(200);
            }
        }
    }
}

namespace OlAform
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lstActions = new ListBox();
            btnAddMouse = new Button();
            btnAddKey = new Button();
            btnAddOCR = new Button();
            btnRun = new Button();
            designPanel = new Panel();
            propertyGrid = new PropertyGrid();
            lblTargetHwnd = new Label();
            txtTargetHwnd = new TextBox();
            btnPickWindow = new Button();
            btnBindWindow = new Button();
            btnTestOcr = new Button();
            lblBindStatus = new Label();
            lblOcrResult = new Label();
            txtOcrResult = new TextBox();
            SuspendLayout();
            // 
            // lstActions
            // 
            lstActions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            lstActions.FormattingEnabled = true;
            lstActions.ItemHeight = 17;
            lstActions.Location = new Point(12, 12);
            lstActions.Name = "lstActions";
            lstActions.Size = new Size(180, 701);
            lstActions.TabIndex = 0;
            // 
            // btnAddMouse
            // 
            btnAddMouse.Location = new Point(198, 12);
            btnAddMouse.Name = "btnAddMouse";
            btnAddMouse.Size = new Size(120, 30);
            btnAddMouse.TabIndex = 1;
            btnAddMouse.Text = "Add Mouse";
            btnAddMouse.UseVisualStyleBackColor = true;
            btnAddMouse.Click += btnAddMouse_Click;
            // 
            // btnAddKey
            // 
            btnAddKey.Location = new Point(198, 48);
            btnAddKey.Name = "btnAddKey";
            btnAddKey.Size = new Size(120, 30);
            btnAddKey.TabIndex = 2;
            btnAddKey.Text = "Add Key";
            btnAddKey.UseVisualStyleBackColor = true;
            btnAddKey.Click += btnAddKey_Click;
            // 
            // btnAddOCR
            // 
            btnAddOCR.Location = new Point(198, 84);
            btnAddOCR.Name = "btnAddOCR";
            btnAddOCR.Size = new Size(120, 30);
            btnAddOCR.TabIndex = 3;
            btnAddOCR.Text = "Add OCR";
            btnAddOCR.UseVisualStyleBackColor = true;
            btnAddOCR.Click += btnAddOCR_Click;
            // 
            // btnRun
            // 
            btnRun.Location = new Point(198, 120);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(120, 30);
            btnRun.TabIndex = 4;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // designPanel
            // 
            designPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            designPanel.BackColor = SystemColors.ControlLight;
            designPanel.BorderStyle = BorderStyle.FixedSingle;
            designPanel.Location = new Point(324, 12);
            designPanel.Name = "designPanel";
            designPanel.Size = new Size(733, 710);
            designPanel.TabIndex = 5;
            designPanel.MouseDown += designPanel_MouseDown;
            designPanel.MouseMove += designPanel_MouseMove;
            designPanel.MouseUp += designPanel_MouseUp;
            // 
            // propertyGrid
            // 
            propertyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            propertyGrid.Location = new Point(1063, 12);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(194, 710);
            propertyGrid.TabIndex = 6;
            // 
            // lblTargetHwnd
            // 
            lblTargetHwnd.AutoSize = true;
            lblTargetHwnd.Location = new Point(198, 159);
            lblTargetHwnd.Name = "lblTargetHwnd";
            lblTargetHwnd.Size = new Size(90, 17);
            lblTargetHwnd.TabIndex = 7;
            lblTargetHwnd.Text = "Target HWND";
            // 
            // txtTargetHwnd
            // 
            txtTargetHwnd.Location = new Point(198, 177);
            txtTargetHwnd.Name = "txtTargetHwnd";
            txtTargetHwnd.Size = new Size(120, 23);
            txtTargetHwnd.TabIndex = 8;
            // 
            // btnPickWindow
            // 
            btnPickWindow.Location = new Point(198, 206);
            btnPickWindow.Name = "btnPickWindow";
            btnPickWindow.Size = new Size(120, 30);
            btnPickWindow.TabIndex = 9;
            btnPickWindow.Text = "Pick Window";
            btnPickWindow.UseVisualStyleBackColor = true;
            btnPickWindow.MouseDown += btnPickWindow_MouseDown;
            btnPickWindow.MouseUp += btnPickWindow_MouseUp;
            // 
            // btnBindWindow
            // 
            btnBindWindow.Location = new Point(198, 242);
            btnBindWindow.Name = "btnBindWindow";
            btnBindWindow.Size = new Size(120, 30);
            btnBindWindow.TabIndex = 10;
            btnBindWindow.Text = "Bind HWND";
            btnBindWindow.UseVisualStyleBackColor = true;
            btnBindWindow.Click += btnBindWindow_Click;
            // 
            // btnTestOcr
            // 
            btnTestOcr.Location = new Point(198, 278);
            btnTestOcr.Name = "btnTestOcr";
            btnTestOcr.Size = new Size(120, 30);
            btnTestOcr.TabIndex = 11;
            btnTestOcr.Text = "Test OCR";
            btnTestOcr.UseVisualStyleBackColor = true;
            btnTestOcr.Click += btnTestOcr_Click;
            // 
            // lblBindStatus
            // 
            lblBindStatus.Location = new Point(198, 311);
            lblBindStatus.Name = "lblBindStatus";
            lblBindStatus.Size = new Size(120, 164);
            lblBindStatus.TabIndex = 12;
            lblBindStatus.Text = "未绑定外部窗口";
            // 
            // lblOcrResult
            // 
            lblOcrResult.AutoSize = true;
            lblOcrResult.Location = new Point(198, 475);
            lblOcrResult.Name = "lblOcrResult";
            lblOcrResult.Size = new Size(73, 17);
            lblOcrResult.TabIndex = 13;
            lblOcrResult.Text = "OCR Result";
            // 
            // txtOcrResult
            // 
            txtOcrResult.Location = new Point(198, 493);
            txtOcrResult.Multiline = true;
            txtOcrResult.Name = "txtOcrResult";
            txtOcrResult.ReadOnly = true;
            txtOcrResult.ScrollBars = ScrollBars.Vertical;
            txtOcrResult.Size = new Size(120, 219);
            txtOcrResult.TabIndex = 14;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1269, 736);
            Controls.Add(txtOcrResult);
            Controls.Add(lblOcrResult);
            Controls.Add(lblBindStatus);
            Controls.Add(btnTestOcr);
            Controls.Add(btnBindWindow);
            Controls.Add(btnPickWindow);
            Controls.Add(txtTargetHwnd);
            Controls.Add(lblTargetHwnd);
            Controls.Add(propertyGrid);
            Controls.Add(designPanel);
            Controls.Add(btnRun);
            Controls.Add(btnAddOCR);
            Controls.Add(btnAddKey);
            Controls.Add(btnAddMouse);
            Controls.Add(lstActions);
            Name = "Form1";
            Text = "OLA Automation Configurator";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.ListBox lstActions;
        private System.Windows.Forms.Button btnAddMouse;
        private System.Windows.Forms.Button btnAddKey;
        private System.Windows.Forms.Button btnAddOCR;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Panel designPanel;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.Label lblTargetHwnd;
        private System.Windows.Forms.TextBox txtTargetHwnd;
        private System.Windows.Forms.Button btnPickWindow;
        private System.Windows.Forms.Button btnBindWindow;
        private System.Windows.Forms.Button btnTestOcr;
        private System.Windows.Forms.Label lblBindStatus;
        private System.Windows.Forms.Label lblOcrResult;
        private System.Windows.Forms.TextBox txtOcrResult;

        #endregion
    }
}

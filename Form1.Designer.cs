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
            components = new System.ComponentModel.Container();
            this.lstActions = new System.Windows.Forms.ListBox();
            this.btnAddMouse = new System.Windows.Forms.Button();
            this.btnAddKey = new System.Windows.Forms.Button();
            this.btnAddOCR = new System.Windows.Forms.Button();
            this.designPanel = new System.Windows.Forms.Panel();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // lstActions
            // 
            this.lstActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstActions.FormattingEnabled = true;
            this.lstActions.ItemHeight = 15;
            this.lstActions.Location = new System.Drawing.Point(12, 12);
            this.lstActions.Name = "lstActions";
            this.lstActions.Size = new System.Drawing.Size(180, 424);
            this.lstActions.TabIndex = 0;
            // 
            // btnAddMouse
            // 
            this.btnAddMouse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddMouse.Location = new System.Drawing.Point(198, 12);
            this.btnAddMouse.Name = "btnAddMouse";
            this.btnAddMouse.Size = new System.Drawing.Size(120, 30);
            this.btnAddMouse.TabIndex = 1;
            this.btnAddMouse.Text = "Add Mouse";
            this.btnAddMouse.UseVisualStyleBackColor = true;
            this.btnAddMouse.Click += new System.EventHandler(this.btnAddMouse_Click);
            // 
            // btnAddKey
            // 
            this.btnAddKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddKey.Location = new System.Drawing.Point(198, 48);
            this.btnAddKey.Name = "btnAddKey";
            this.btnAddKey.Size = new System.Drawing.Size(120, 30);
            this.btnAddKey.TabIndex = 2;
            this.btnAddKey.Text = "Add Key";
            this.btnAddKey.UseVisualStyleBackColor = true;
            this.btnAddKey.Click += new System.EventHandler(this.btnAddKey_Click);
            // 
            // btnAddOCR
            // 
            this.btnAddOCR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddOCR.Location = new System.Drawing.Point(198, 84);
            this.btnAddOCR.Name = "btnAddOCR";
            this.btnAddOCR.Size = new System.Drawing.Size(120, 30);
            this.btnAddOCR.TabIndex = 3;
            this.btnAddOCR.Text = "Add OCR";
            this.btnAddOCR.UseVisualStyleBackColor = true;
            this.btnAddOCR.Click += new System.EventHandler(this.btnAddOCR_Click);
            // 
            // designPanel
            // 
            this.designPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.designPanel.BackColor = System.Drawing.SystemColors.ControlLight;
            this.designPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.designPanel.Location = new System.Drawing.Point(324, 12);
            this.designPanel.Name = "designPanel";
            this.designPanel.Size = new System.Drawing.Size(364, 424);
            this.designPanel.TabIndex = 4;
            this.designPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.designPanel_MouseDown);
            this.designPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.designPanel_MouseMove);
            this.designPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.designPanel_MouseUp);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid.Location = new System.Drawing.Point(694, 12);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(194, 424);
            this.propertyGrid.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 450);
            this.Controls.Add(this.propertyGrid);
            this.Controls.Add(this.designPanel);
            this.Controls.Add(this.btnAddOCR);
            this.Controls.Add(this.btnAddKey);
            this.Controls.Add(this.btnAddMouse);
            this.Controls.Add(this.lstActions);
            this.Name = "Form1";
            this.Text = "OLA Automation Configurator";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.ListBox lstActions;
        private System.Windows.Forms.Button btnAddMouse;
        private System.Windows.Forms.Button btnAddKey;
        private System.Windows.Forms.Button btnAddOCR;
        private System.Windows.Forms.Panel designPanel;
        private System.Windows.Forms.PropertyGrid propertyGrid;

        #endregion
    }
}

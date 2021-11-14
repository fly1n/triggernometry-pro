namespace Triggernometry.Forms
{
    partial class VariableEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VariableEditorForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.tlsOptionsList = new System.Windows.Forms.ToolStrip();
            this.btnItemAdd = new System.Windows.Forms.ToolStripButton();
            this.btnItemInsert = new System.Windows.Forms.ToolStripButton();
            this.btnItemRemove = new System.Windows.Forms.ToolStripButton();
            this.dgvVariableData = new Triggernometry.CustomControls.DataGridViewEx();
            this.grpGeneral = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblVariableName = new System.Windows.Forms.Label();
            this.txtVariableName = new System.Windows.Forms.TextBox();
            this.panel10 = new System.Windows.Forms.Panel();
            this.tlsOptionsTable = new System.Windows.Forms.ToolStrip();
            this.btnColumnAdd = new System.Windows.Forms.ToolStripButton();
            this.btnColumnInsert = new System.Windows.Forms.ToolStripButton();
            this.btnColumnRemove = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRowAdd = new System.Windows.Forms.ToolStripButton();
            this.btnRowInsert = new System.Windows.Forms.ToolStripButton();
            this.btnRowRemove = new System.Windows.Forms.ToolStripButton();
            this.panel4.SuspendLayout();
            this.tlsOptionsList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVariableData)).BeginInit();
            this.grpGeneral.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tlsOptionsTable.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel3
            // 
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(13, 468);
            this.panel3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(886, 12);
            this.panel3.TabIndex = 16;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.btnCancel);
            this.panel4.Controls.Add(this.btnOk);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel4.Location = new System.Drawing.Point(13, 480);
            this.panel4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(886, 40);
            this.panel4.TabIndex = 17;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCancel.Location = new System.Drawing.Point(686, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(200, 40);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnOk.Location = new System.Drawing.Point(0, 0);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(200, 40);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // tlsOptionsList
            // 
            this.tlsOptionsList.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsOptionsList.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tlsOptionsList.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnItemAdd,
            this.btnItemInsert,
            this.btnItemRemove});
            this.tlsOptionsList.Location = new System.Drawing.Point(13, 124);
            this.tlsOptionsList.Name = "tlsOptionsList";
            this.tlsOptionsList.Size = new System.Drawing.Size(886, 27);
            this.tlsOptionsList.TabIndex = 18;
            // 
            // btnItemAdd
            // 
            this.btnItemAdd.Image = ((System.Drawing.Image)(resources.GetObject("btnItemAdd.Image")));
            this.btnItemAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnItemAdd.Name = "btnItemAdd";
            this.btnItemAdd.Size = new System.Drawing.Size(101, 24);
            this.btnItemAdd.Text = "Add item";
            this.btnItemAdd.Click += new System.EventHandler(this.btnItemAdd_Click);
            // 
            // btnItemInsert
            // 
            this.btnItemInsert.Image = ((System.Drawing.Image)(resources.GetObject("btnItemInsert.Image")));
            this.btnItemInsert.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnItemInsert.Name = "btnItemInsert";
            this.btnItemInsert.Size = new System.Drawing.Size(111, 24);
            this.btnItemInsert.Text = "Insert item";
            this.btnItemInsert.Click += new System.EventHandler(this.btnItemInsert_Click);
            // 
            // btnItemRemove
            // 
            this.btnItemRemove.Image = ((System.Drawing.Image)(resources.GetObject("btnItemRemove.Image")));
            this.btnItemRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnItemRemove.Name = "btnItemRemove";
            this.btnItemRemove.Size = new System.Drawing.Size(130, 24);
            this.btnItemRemove.Text = "Remove item";
            this.btnItemRemove.Click += new System.EventHandler(this.btnItemRemove_Click);
            // 
            // dgvVariableData
            // 
            this.dgvVariableData.AllowUserToAddRows = false;
            this.dgvVariableData.AllowUserToDeleteRows = false;
            this.dgvVariableData.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.dgvVariableData.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvVariableData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvVariableData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvVariableData.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvVariableData.Location = new System.Drawing.Point(13, 151);
            this.dgvVariableData.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dgvVariableData.MultiSelect = false;
            this.dgvVariableData.Name = "dgvVariableData";
            this.dgvVariableData.RowHeadersWidth = 51;
            this.dgvVariableData.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgvVariableData.ShowCellErrors = false;
            this.dgvVariableData.ShowEditingIcon = false;
            this.dgvVariableData.ShowRowErrors = false;
            this.dgvVariableData.Size = new System.Drawing.Size(886, 317);
            this.dgvVariableData.TabIndex = 19;
            this.dgvVariableData.VirtualMode = true;
            this.dgvVariableData.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgvVariableData_CellValueNeeded);
            this.dgvVariableData.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgvVariableData_CellValuePushed);
            this.dgvVariableData.SelectionChanged += new System.EventHandler(this.dgvVariableData_SelectionChanged);
            // 
            // grpGeneral
            // 
            this.grpGeneral.AutoSize = true;
            this.grpGeneral.Controls.Add(this.tableLayoutPanel1);
            this.grpGeneral.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpGeneral.Location = new System.Drawing.Point(13, 12);
            this.grpGeneral.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            this.grpGeneral.Size = new System.Drawing.Size(886, 73);
            this.grpGeneral.TabIndex = 20;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = " General settings ";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblVariableName, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtVariableName, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(13, 30);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(860, 31);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lblVariableName
            // 
            this.lblVariableName.AutoEllipsis = true;
            this.lblVariableName.AutoSize = true;
            this.lblVariableName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVariableName.Location = new System.Drawing.Point(4, 0);
            this.lblVariableName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVariableName.Name = "lblVariableName";
            this.lblVariableName.Size = new System.Drawing.Size(111, 31);
            this.lblVariableName.TabIndex = 0;
            this.lblVariableName.Text = "Variable name";
            this.lblVariableName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtVariableName
            // 
            this.txtVariableName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVariableName.Location = new System.Drawing.Point(123, 3);
            this.txtVariableName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtVariableName.Name = "txtVariableName";
            this.txtVariableName.Size = new System.Drawing.Size(733, 25);
            this.txtVariableName.TabIndex = 1;
            // 
            // panel10
            // 
            this.panel10.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel10.Location = new System.Drawing.Point(13, 85);
            this.panel10.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(886, 12);
            this.panel10.TabIndex = 27;
            // 
            // tlsOptionsTable
            // 
            this.tlsOptionsTable.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsOptionsTable.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tlsOptionsTable.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnColumnAdd,
            this.btnColumnInsert,
            this.btnColumnRemove,
            this.toolStripSeparator1,
            this.btnRowAdd,
            this.btnRowInsert,
            this.btnRowRemove});
            this.tlsOptionsTable.Location = new System.Drawing.Point(13, 97);
            this.tlsOptionsTable.Name = "tlsOptionsTable";
            this.tlsOptionsTable.Size = new System.Drawing.Size(886, 27);
            this.tlsOptionsTable.TabIndex = 28;
            // 
            // btnColumnAdd
            // 
            this.btnColumnAdd.Image = ((System.Drawing.Image)(resources.GetObject("btnColumnAdd.Image")));
            this.btnColumnAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnColumnAdd.Name = "btnColumnAdd";
            this.btnColumnAdd.Size = new System.Drawing.Size(122, 24);
            this.btnColumnAdd.Text = "Add column";
            this.btnColumnAdd.Click += new System.EventHandler(this.btnColumnAdd_Click);
            // 
            // btnColumnInsert
            // 
            this.btnColumnInsert.Image = ((System.Drawing.Image)(resources.GetObject("btnColumnInsert.Image")));
            this.btnColumnInsert.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnColumnInsert.Name = "btnColumnInsert";
            this.btnColumnInsert.Size = new System.Drawing.Size(132, 24);
            this.btnColumnInsert.Text = "Insert column";
            this.btnColumnInsert.Click += new System.EventHandler(this.btnColumnInsert_Click);
            // 
            // btnColumnRemove
            // 
            this.btnColumnRemove.Image = ((System.Drawing.Image)(resources.GetObject("btnColumnRemove.Image")));
            this.btnColumnRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnColumnRemove.Name = "btnColumnRemove";
            this.btnColumnRemove.Size = new System.Drawing.Size(151, 24);
            this.btnColumnRemove.Text = "Remove column";
            this.btnColumnRemove.Click += new System.EventHandler(this.btnColumnRemove_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // btnRowAdd
            // 
            this.btnRowAdd.Image = ((System.Drawing.Image)(resources.GetObject("btnRowAdd.Image")));
            this.btnRowAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRowAdd.Name = "btnRowAdd";
            this.btnRowAdd.Size = new System.Drawing.Size(96, 24);
            this.btnRowAdd.Text = "Add row";
            this.btnRowAdd.Click += new System.EventHandler(this.btnRowAdd_Click);
            // 
            // btnRowInsert
            // 
            this.btnRowInsert.Image = ((System.Drawing.Image)(resources.GetObject("btnRowInsert.Image")));
            this.btnRowInsert.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRowInsert.Name = "btnRowInsert";
            this.btnRowInsert.Size = new System.Drawing.Size(106, 24);
            this.btnRowInsert.Text = "Insert row";
            this.btnRowInsert.Click += new System.EventHandler(this.btnRowInsert_Click);
            // 
            // btnRowRemove
            // 
            this.btnRowRemove.Image = ((System.Drawing.Image)(resources.GetObject("btnRowRemove.Image")));
            this.btnRowRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRowRemove.Name = "btnRowRemove";
            this.btnRowRemove.Size = new System.Drawing.Size(125, 24);
            this.btnRowRemove.Text = "Remove row";
            this.btnRowRemove.Click += new System.EventHandler(this.btnRowRemove_Click);
            // 
            // VariableEditorForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(912, 532);
            this.Controls.Add(this.dgvVariableData);
            this.Controls.Add(this.tlsOptionsList);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.tlsOptionsTable);
            this.Controls.Add(this.panel10);
            this.Controls.Add(this.grpGeneral);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(661, 454);
            this.Name = "VariableEditorForm";
            this.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Variable editor";
            this.panel4.ResumeLayout(false);
            this.tlsOptionsList.ResumeLayout(false);
            this.tlsOptionsList.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVariableData)).EndInit();
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tlsOptionsTable.ResumeLayout(false);
            this.tlsOptionsTable.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button btnCancel;
        internal System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ToolStrip tlsOptionsList;
        private CustomControls.DataGridViewEx dgvVariableData;
        private System.Windows.Forms.GroupBox grpGeneral;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblVariableName;
        private System.Windows.Forms.TextBox txtVariableName;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.ToolStripButton btnItemRemove;
        private System.Windows.Forms.ToolStripButton btnItemAdd;
        private System.Windows.Forms.ToolStripButton btnItemInsert;
        private System.Windows.Forms.ToolStrip tlsOptionsTable;
        private System.Windows.Forms.ToolStripButton btnColumnAdd;
        private System.Windows.Forms.ToolStripButton btnColumnInsert;
        private System.Windows.Forms.ToolStripButton btnColumnRemove;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnRowAdd;
        private System.Windows.Forms.ToolStripButton btnRowInsert;
        private System.Windows.Forms.ToolStripButton btnRowRemove;
    }
}
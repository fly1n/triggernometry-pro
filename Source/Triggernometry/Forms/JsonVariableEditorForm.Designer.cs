namespace Triggernometry.Forms
{
    partial class JsonVariableEditorForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.grpGeneral = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblVariableName = new System.Windows.Forms.Label();
            this.txtVariableName = new System.Windows.Forms.TextBox();
            this.panel10 = new System.Windows.Forms.Panel();
            this.dgvVariableData = new Triggernometry.CustomControls.DataGridViewEx();
            this.panel4.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVariableData)).BeginInit();
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
            this.txtVariableName.TextChanged += new System.EventHandler(this.txtVariableName_TextChanged);
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
            // dgvVariableData
            // 
            this.dgvVariableData.AllowUserToAddRows = false;
            this.dgvVariableData.AllowUserToDeleteRows = false;
            this.dgvVariableData.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.dgvVariableData.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvVariableData.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvVariableData.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvVariableData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvVariableData.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvVariableData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvVariableData.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvVariableData.Location = new System.Drawing.Point(13, 97);
            this.dgvVariableData.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dgvVariableData.MultiSelect = false;
            this.dgvVariableData.Name = "dgvVariableData";
            this.dgvVariableData.RowHeadersWidth = 51;
            this.dgvVariableData.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgvVariableData.ShowCellErrors = false;
            this.dgvVariableData.ShowEditingIcon = false;
            this.dgvVariableData.ShowRowErrors = false;
            this.dgvVariableData.Size = new System.Drawing.Size(886, 371);
            this.dgvVariableData.TabIndex = 19;
            this.dgvVariableData.VirtualMode = true;
            this.dgvVariableData.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgvVariableData_CellValueNeeded);
            this.dgvVariableData.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgvVariableData_CellValuePushed);
            // 
            // JsonVariableEditorForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(912, 532);
            this.Controls.Add(this.dgvVariableData);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel10);
            this.Controls.Add(this.grpGeneral);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(661, 454);
            this.Name = "JsonVariableEditorForm";
            this.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Variable editor";
            this.panel4.ResumeLayout(false);
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVariableData)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button btnCancel;
        internal System.Windows.Forms.Button btnOk;
        private CustomControls.DataGridViewEx dgvVariableData;
        private System.Windows.Forms.GroupBox grpGeneral;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblVariableName;
        private System.Windows.Forms.TextBox txtVariableName;
        private System.Windows.Forms.Panel panel10;
    }
}
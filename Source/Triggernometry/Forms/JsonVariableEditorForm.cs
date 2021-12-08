using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Triggernometry.Variables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Triggernometry.Forms
{

    public partial class JsonVariableEditorForm : MemoryForm<VariableEditorForm>
    {

        public string VariableName
        {
            get
            {
                return txtVariableName.Text;
            }
            set
            {
                txtVariableName.Text = value;
                List<JToken> tokens = null;
                try
                {
                    tokens = VariableToEdit.Value.SelectTokens(txtVariableName.Text).ToList();

                }
                catch (Exception ex)
                {
                    tokens = null;
                }
                if ((tokens!=null) && (tokens.Count>0))
                {
                    txtVariableName.BackColor = Color.White;
                    TokensToEdit = tokens;
                    dgvVariableData.Enabled = true;
                    dgvVariableData.RowCount = tokens.Count;
                    //tokens.Count;

                    dgvVariableData.Refresh();
                }
                else {
                    dgvVariableData.Enabled = false;
                    txtVariableName.BackColor = Color.Pink;
                }
            }
        }
        public VariableJson VariableToEdit { get; set; } = null;
        public List<JToken> TokensToEdit = new List<JToken> ();
        public bool IsNew { get; set; } = false;

        private StringFormat sf = null;
        private int prevx;
        private int prevy;

        public JsonVariableEditorForm()
        {
            InitializeComponent();
            Disposed += JsonVariableEditorForm_Disposed;
            sf = new StringFormat();
            sf.Alignment = StringAlignment.Far;
            sf.LineAlignment = StringAlignment.Center;            
            Shown += JsonVariableEditorForm_Shown;
            dgvVariableData.RowHeadersDefaultCellStyle.Padding = new Padding(dgvVariableData.RowHeadersWidth);
            dgvVariableData.RowPostPaint += DgvVariableData_RowPostPaint;
            RestoredSavedDimensions();
        }

        private void JsonVariableEditorForm_Disposed(object sender, EventArgs e)
        {
            if (sf != null)
            {
                sf.Dispose();
                sf = null;
            }
        }

        private void DgvVariableData_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            object o = dgvVariableData.Rows[e.RowIndex].HeaderCell.Value;
            Rectangle r = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgvVariableData.RowHeadersWidth - 5, e.RowBounds.Height);
            e.Graphics.DrawString(o != null ? o.ToString() : "", dgvVariableData.Font, SystemBrushes.ControlText, r, sf);
        }

        private void JsonVariableEditorForm_Shown(object sender, EventArgs e)
        {
            InitializeValueDisplay();
            dgvVariableData.ClearSelection();
            txtVariableName.Focus();
        }

        private DataGridViewColumn CreateNewColumn(string name, string text)
        {
            DataGridViewColumn col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = text;            
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            return col;
        }

        private void InitializeValueDisplay()
        {
            DataGridViewColumn col = CreateNewColumn("col1", I18n.Translate("internal/VariableEditorForm/colname", "Name"));
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvVariableData.Columns.Add(col);
            col = CreateNewColumn("col2", I18n.Translate("internal/VariableEditorForm/colpath", "Path"));
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvVariableData.Columns.Add(col);
            col = CreateNewColumn("col3", I18n.Translate("internal/VariableEditorForm/colvalue", "Value"));
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvVariableData.Columns.Add(col);
            dgvVariableData.RowCount = 1;
            dgvVariableData.RowHeadersVisible = false;

            

        }

        private void dgvVariableData_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= TokensToEdit.Count) return;
            if (e.ColumnIndex == 0)
            {
                e.Value = e.RowIndex;
            }
            else if (e.ColumnIndex == 1)
            {
                e.Value = TokensToEdit[e.RowIndex].Path.Split('.').Last();
            }else if(e.ColumnIndex == 2)
            {
                e.Value = TokensToEdit[e.RowIndex].Path;
            }else if (e.ColumnIndex == 3)
            {
                e.Value = JsonConvert.SerializeObject(TokensToEdit[e.RowIndex]);
            }

        }

        private void dgvVariableData_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                //e.Value = JsonConvert.SerializeObject(TokensToEdit[e.RowIndex]);
                try
                {
                    var obj = JsonConvert.DeserializeObject((e != null && e.Value != null) ? e.Value.ToString() : "");
                    TokensToEdit[e.RowIndex].Replace(JToken.FromObject(obj));
                    VariableName = txtVariableName.Text;
                }catch(Exception ex)
                {
                    switch (DateTime.Now.Second % 4 )
                    {
                        case 0: MessageBox.Show("你是故意找"+ ex.Message+"是不是？"); break;
                        case 1: MessageBox.Show("你瞧瞧现在哪有瓜，这都是" + ex.Message); break;
                        case 2: MessageBox.Show("你TM劈我" + ex.Message+"是吧？"); break;
                        case 3: MessageBox.Show("这瓜要是" + ex.Message + "，我自己吃了它，你满意了吧？"); break;
                    }
                    
                }
            }
            

        }

        private void txtVariableName_TextChanged(object sender, EventArgs e)
        {
            VariableName = txtVariableName.Text; 
            
        }
    }

}
